"""Contact sheets and GIFs assembled from actual Blender renders only."""
from pathlib import Path
import json,zipfile
from PIL import Image,ImageDraw,ImageOps,ImageFont

ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Operative/v01'
font_path='C:/Windows/Fonts/segoeui.ttf'
font=ImageFont.truetype(font_path,20);small=ImageFont.truetype(font_path,15)
board=Image.new('RGB',(1260,820),'#252c32');draw=ImageDraw.Draw(board)
items=[('Approved concept',OUT/'reference/approved-concept.png',(25,22,323,642)),
       ('Actual 3D / rest pose',OUT/'renders/front.png',(105,25,535,780)),
       ('Actual 3D / rifle ready',OUT/'renders/rifle-ready.png',(70,25,570,780))]
for i,(title,path,crop) in enumerate(items):
    draw.text((i*420+20,15),title,font=font,fill='white')
    image=Image.open(path).convert('RGB').crop(crop);image=ImageOps.contain(image,(400,735))
    board.paste(image,(i*420+(420-image.width)//2,55+(735-image.height)//2))
draw.text((20,795),'REVIEW v01 / original mesh, rig and materials / not integrated into Unity',font=small,fill='#b9c6ce')
board.save(OUT/'comparison.png')

details=Image.new('RGB',(1280,850),'#252c32');d=ImageDraw.Draw(details)
for i,(name,title) in enumerate([('three-quarter.png','Rest / three-quarter'),('side.png','Profile'),('back.png','Rear equipment'),('game-angle.png','Isometric close view')]):
    im=ImageOps.contain(Image.open(OUT/'renders'/name).convert('RGB'),(320,480))
    details.paste(im,(i*320+(320-im.width)//2,35));d.text((i*320+10,8),title,font=small,fill='white')
for i,(name,title) in enumerate([('helmet-close.png','Helmet / visor / respirator'),('hands-boots.png','Lower-body equipment and soles')]):
    im=ImageOps.contain(Image.open(OUT/'renders'/name).convert('RGB'),(640,320));x=i*640
    details.paste(im,(x+(640-im.width)//2,525));d.text((x+10,505),title,font=small,fill='white')
details.save(OUT/'details.png')

manifest=json.loads((OUT/'manifest.json').read_text())
names=['Idle_Rifle','Walk_Rifle','Run_Rifle','Death','Fire_Rifle','Grenade_Throw','Dash','Hit']
titles=['Rifle idle','Walk / preview travel','Run / preview travel','Death / replay','Rifle fire','Grenade throw','Ground dash','Hit reaction']
cache={}
for name in names:
    for path in (OUT/'motion_frames'/name).glob('*.png'):
        cache[(name,int(path.stem))]=Image.open(path).convert('RGB').resize((240,300),Image.Resampling.LANCZOS)
frames=[]
for frame in range(48):
    sheet=Image.new('RGB',(960,650),'#20282e');d=ImageDraw.Draw(sheet)
    for i,(name,title) in enumerate(zip(names,titles)):
        # Thirty source FPS sampled at fifteen FPS; actual clip timing retained.
        source=(frame*2)%manifest['clips'][name]['frames'];source-=source%2
        x=(i%4)*240;y=(i//4)*325
        sheet.paste(cache[(name,source)],(x,y+25));d.text((x+8,y+3),title,font=small,fill='white')
    frames.append(sheet)
palette=frames[10].quantize(colors=192,method=Image.Quantize.MEDIANCUT)
indexed=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in frames]
indexed[0].save(OUT/'motion.gif',save_all=True,append_images=indexed[1:],duration=[60,70,70]*16,loop=0,optimize=True,disposal=2)

# Frame strips make start/mid/end poses inspectable without a GIF decoder.
strip=Image.new('RGB',(1000,8*190),'#252c32');d=ImageDraw.Draw(strip)
for row,name in enumerate(names):
    d.text((8,row*190+3),name,font=small,fill='white')
    end=manifest['clips'][name]['frames']
    for col,q in enumerate([0,.25,.5,.75,1]):
        frame=round(end*q/2)*2;frame=min(end,frame)
        im=cache[(name,frame)].resize((132,165),Image.Resampling.LANCZOS)
        strip.paste(im,(col*200+34,row*190+24))
strip.save(OUT/'motion-contact-sheet.png')
print('OPERATIVE_REVIEW_PACKAGED',len(frames),'GIF frames; source renders retained')
