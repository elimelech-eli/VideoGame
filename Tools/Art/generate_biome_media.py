"""Original tile textures and seamless ambient compositions, deterministic offline authoring.
Requires numpy/Pillow. Outputs Unity-ready PNG/WAV; no external samples.
"""
from pathlib import Path
import json
import wave
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2] / 'Assets/Game/Resources/Biomes'
(ROOT/'Textures').mkdir(parents=True,exist_ok=True)
(ROOT/'Audio').mkdir(parents=True,exist_ok=True)
Image.new('RGB',(4,4),(255,255,255)).save(ROOT/'Textures'/'White.png')
N=512
y,x=np.mgrid[0:N,0:N]
colors=[(49,57,65),(75,66,51),(100,110,118),(152,164,165),(70,76,65),(111,118,124)]
reports=[]
for biome in range(6):
    rng=np.random.default_rng(1201+biome)
    noise=rng.normal(0,2,(N,N))
    height=np.ones((N,N))*.55
    if biome==0:
        seam=(x%128<3)|(y%128<3);height[seam]=.15
        noise+=np.sin(x*.17)*np.sin(y*.11)*1.5
    elif biome==1:
        seam=(x%256<4)|(y%128<4);height[seam]=.15
        height[((x+y)%32<2)&(x%256>220)]=.25
        noise+=np.sin(x*.06+y*.03)*4
    elif biome==2:
        seam=(x%256<2)|(y%256<2);height[seam]=.3
        noise+=np.sin(x*.025+np.sin(y*.013)*3)**10*14
    elif biome==3:
        seam=(x%128<2)|(y%256<3);height[seam]=.3
        noise*=.35
        noise+=((x%128>116)&(y%256>224))*-15
    elif biome==4:
        seam=(x%24<6)|(y%128<8);height[seam]=.1
        noise+=np.sin(x*.13)*2
    else:
        seam=(x%256<3)|(y%128<3);height[seam]=.25
        noise+=rng.uniform(-10,10,(N,N))
    rgb=np.stack([np.clip(colors[biome][i]+noise+(height-.55)*55,0,255) for i in range(3)],-1)
    Image.fromarray(rgb.astype('uint8')).save(ROOT/'Textures'/f'{biome}_Floor.png')
    dx=(np.roll(height,1,axis=1)-np.roll(height,-1,axis=1))*.8
    dy=(np.roll(height,1,axis=0)-np.roll(height,-1,axis=0))*.8
    normal=np.stack([dx,dy,np.ones_like(dx)],-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
    Image.fromarray(((normal*.5+.5)*255).astype('uint8')).save(ROOT/'Textures'/f'{biome}_Normal.png')

def tone(t,hz,kind):
    phase=t*hz
    if kind=='bass': return np.sin(phase*2*np.pi)*.8+np.sin(phase*4*np.pi)*.16
    if kind=='bell': return (np.sin(phase*2*np.pi)+np.sin(phase*2*np.pi*2.01)*.3)*np.exp(-t*4)
    return np.sin(phase*2*np.pi)*.65+np.sin(phase*4*np.pi)*.15+np.sin(phase*6*np.pi)*.06

for biome in range(6):
    sr=22050
    bpm=[104,96,112,80,88,116][biome]
    beat=60/bpm;length=64*beat;count=round(length*sr)
    track=np.zeros((count,2),np.float64)
    rng=np.random.default_rng(6703+biome)
    root=[38,33,42,36,31,45][biome]
    chords=[0,3,7,5] if biome!=3 else [0,1,5,1]
    def add(signal,start,pan=0):
        indices=(round(start*sr)+np.arange(len(signal)))%count
        track[indices,0]+=signal*(.7-pan*.3)
        track[indices,1]+=signal*(.7+pan*.3)
    def note(midi,start,duration,amp,kind='pad',pan=0):
        t=np.arange(round(duration*sr))/sr
        hz=440*2**((midi-69)/12)
        attack=.12 if kind=='pad' else .008
        envelope=np.minimum(t/attack,1)*np.minimum((duration-t)/(.4 if kind=='pad' else .08),1)
        signal=tone(t,hz,kind)*envelope*amp
        add(signal,start,pan)
        if kind=='bell':add(signal*.22,start+beat*.75,-pan)
    for bar in range(16):
        chord=root+chords[(bar//4)%4];start=bar*4*beat
        for interval,pan in [(12,-.6),(19,.6),(24,0)]:
            note(chord+interval,start,4*beat,.045 if biome!=3 else .06,'pad',pan)
        for step in range(8):
            # Each biome has a different arrangement, tempo, register and percussion.
            if biome==3 and step%3!=0:continue
            midi=chord+24+[0,7,12,15,7,3,12,7][(step+bar%2)%8]
            if biome==4 and step%2:continue
            note(midi,start+step*beat*.5,beat*.45,.06 if biome in [0,2,5] else .035,'bell',(-1)**step*.5)
        for step in range(4):
            if biome==3:continue
            note(chord,start+step*beat,beat*.65,.12 if biome in [0,5] else .08,'bass')
            t=np.arange(round(.22*sr))/sr
            kick=np.sin(2*np.pi*(48*t+4*(1-np.exp(-t*40))))*np.exp(-t*22)
            if step%2==0:add(kick*.16,start+step*beat)
            if step in [1,3]:
                noise=rng.uniform(-1,1,len(t))*np.exp(-t*35)
                add(noise*(.07 if biome==1 else .035),start+step*beat,.2)
        if biome in [0,1,2,5]:
            for step in range(8):
                t=np.arange(round(.04*sr))/sr
                hiss=rng.uniform(-1,1,len(t));hiss=hiss-np.roll(hiss,1)
                add(hiss*np.exp(-t*95)*.022,start+step*beat*.5,-.4)
        if biome in [1,4]:
            note(chord+36,start+beat*3,beat*.6,.04,'bell',.8)
    peak=float(np.abs(track).max());track*=.72/max(peak,.001)
    with wave.open(str(ROOT/'Audio'/f'{biome}.wav'),'wb') as stream:
        stream.setnchannels(2);stream.setsampwidth(2);stream.setframerate(sr)
        stream.writeframes((np.clip(track,-1,1)*32767).astype('<i2').tobytes())
    reports.append(dict(biome=biome,bpm=bpm,seconds=count/sr,peak=float(np.abs(track).max()),rms=float(np.sqrt(np.mean(track**2)))))
(ROOT/'media-manifest.json').write_text(json.dumps(dict(author='Original project-authored procedural media',samples='No external audio samples',loops=reports),indent=2))
print('BIOME_MEDIA_COMPLETE',len(reports),'original loops and 12 textures')
