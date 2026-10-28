"""Deterministic, original architectural surfaces; no external image inputs."""
from pathlib import Path
import numpy as np
from PIL import Image

out=Path(__file__).resolve().parents[2]/'Assets/Game/Resources/Biomes/Textures'
n=512
y,x=np.mgrid[:n,:n]
for biome in range(6):
    rng=np.random.default_rng(4817+biome)
    grain=rng.normal(0,1,(n,n))
    cloud=(np.sin(x*.023+np.sin(y*.017)*2)+np.sin(y*.041+x*.008))*3
    for wall in [False,True]:
        h=np.ones((n,n))*.5
        if biome==0:
            if wall:
                joint=(y%64<4)|((x+((y//64)%2)*64)%128<4)
                value=167+grain*9+cloud*2;value[joint]=75;h[joint]=.1
            else:
                value=48+grain*8+cloud
                crack=np.abs(np.sin(y*.027)*40+x%256-126)<1.5
                value[crack]=22;h[crack]=.1
        elif biome==1:
            joint=(x%256<5)|(y%128<5)
            value=145+grain*5+cloud*2;value[joint]=55;h[joint]=.1
            rust=(np.sin(x*.042+np.sin(y*.025)*4)+grain*.15)>1
            value[rust]-=30
        elif biome==2:
            joint=(x%256<2)|(y%256<2)
            vein=np.sin(x*.022+y*.008+np.sin(y*.015)*2)**16
            value=208+grain*.8-vein*26;value[joint]=120;h[joint]=.35
        elif biome==3:
            joint=(x%256<4)|(y%256<4)
            value=224+grain*.7;value[joint]=104;h[joint]=.2
            if wall:
                gasket=(x%256>244);value[gasket]=80;h[gasket]=.25
        elif biome==4:
            value=145+grain*10+cloud*3
            if wall:
                joint=(y%128<5);value[joint]=62;h[joint]=.15
                stains=(np.sin(x*.083)+np.sin(x*.027+y*.003))>1.3
                value[stains]-=38
            else:
                grate=(x%32<8)|(y%128<8);value[grate]=45;h[grate]=.12
        else:
            joint=(x%128<3)|(y%64<3) if wall else (x%256<4)|(y%128<4)
            value=198+grain*(1.4 if wall else 12);value[joint]=90;h[joint]=.2
        tint=[(1,.96,.94),(.98,.91,.79),(.96,.98,1),(.94,1,.99),(.87,.94,.82),(.96,.98,1)][biome]
        rgb=np.stack([np.clip(value*c,0,255) for c in tint],-1).astype('uint8')
        role='Shell' if wall else 'Floor'
        Image.fromarray(rgb).save(out/f'{biome}_{role}.png')
        dx=(np.roll(h,1,1)-np.roll(h,-1,1))*.9
        dy=(np.roll(h,1,0)-np.roll(h,-1,0))*.9
        normal=np.stack([dx,dy,np.ones_like(dx)],-1)
        normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
        suffix='ShellNormal' if wall else 'Normal'
        Image.fromarray(((normal*.5+.5)*255).astype('uint8')).save(out/f'{biome}_{suffix}.png')
print('SURFACES_COMPLETE: six wall/floor families, 24 maps')
