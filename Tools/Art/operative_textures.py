"""Original shared PBR atlas for the operative review asset. Offline NumPy/Pillow."""
from pathlib import Path
import numpy as np
from PIL import Image

OUT=Path(__file__).resolve().parents[2]/'Art/Operative/v01/textures'
OUT.mkdir(parents=True,exist_ok=True)
N=2048;S=512
colors=[(39,43,45),(181,179,167),(19,23,25),(72,79,82),(91,84,66),
        (8,14,20),(34,184,219),(43,48,52),(170,116,47)]
albedo=np.zeros((N,N,3),np.uint8);rough=np.full((N,N),180,np.uint8)
metal=np.zeros((N,N),np.uint8);height=np.full((N,N),.5,np.float32)
emission=np.zeros((N,N,3),np.uint8);mask=np.zeros((N,N,3),np.uint8)
y,x=np.mgrid[:S,:S]
for tile,base in enumerate(colors):
    rng=np.random.default_rng(4109+tile)
    ox=(tile%4)*S;oy=(3-tile//4)*S;sl=np.s_[oy:oy+S,ox:ox+S]
    grain=rng.normal(0,1,(S,S));variation=grain*1.5
    h=np.ones((S,S))*.5
    if tile in [0,4]:
        weave=((x%6<3)^(y%6<3)).astype(float)
        variation+=weave*4+np.sin(y*.09)*1.8;h+=weave*.025
    if tile in [1,3,7,8]:
        if tile==1:
            weather=np.sin(x*.021+np.sin(y*.014)*2)+np.sin(y*.029+x*.009)
            variation+=weather*5+grain*2
            variation[weather>1.35]-=19
        # Scuffs vary roughness; no painted directional lighting.
        for _ in range(160):
            px,py=rng.integers(2,S-30,2);length=int(rng.integers(3,25))
            variation[py:py+1,px:px+length]-=rng.uniform(9,28)
            h[py:py+1,px:px+length]-=.025
    if tile==2:
        variation+=((x+y)%12<2)*3;h+=((x+y)%12<2)*.018
    rgb=np.stack([np.clip(c+variation,0,255) for c in base],-1).astype('uint8')
    albedo[sl]=rgb;height[sl]=h
    rough[sl]=np.clip(([205,155,210,100,210,45,90,150,160][tile]+grain*3),0,255).astype('uint8')
    metal[sl]=[0,110,0,220,0,100,0,150,80][tile]
    if tile==6:emission[sl]=rgb
    if tile==1:mask[sl]=[255,0,0]
    if tile==0:mask[sl]=[0,255,0]
    if tile in [3,7]:mask[sl]=[0,0,255]
dx=(np.roll(height,1,1)-np.roll(height,-1,1))*.7
dy=(np.roll(height,1,0)-np.roll(height,-1,0))*.7
norm=np.stack([dx,dy,np.ones_like(dx)],-1);norm/=np.linalg.norm(norm,axis=-1,keepdims=True)
for name,data in [('Albedo',albedo),('Roughness',rough),('Metallic',metal),('Emission',emission),
                  ('CustomizationMask',mask),('Normal',((norm*.5+.5)*255).astype('uint8'))]:
    Image.fromarray(data).save(OUT/f'Operative_{name}.png')
packed=np.stack([metal,np.zeros_like(metal),np.zeros_like(metal),255-rough],-1)
Image.fromarray(packed).save(OUT/'Operative_UnityMetallicSmoothness.png')
print('OPERATIVE_TEXTURES_COMPLETE: 2048 PBR atlas and customization mask')
