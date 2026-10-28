from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2];out=ROOT/'Art/Operative/v02'
frames=[Image.open(p).convert('RGB') for p in sorted((out/'walk-frames').glob('*.png'))]
frames[0].save(out/'walk.gif',save_all=True,append_images=frames[1:],duration=56,loop=0)
board=Image.new('RGB',(1280,850),(22,25,29));draw=ImageDraw.Draw(board)
for i,(label,path) in enumerate([('Rejected v01',ROOT/'Art/Operative/v01/renders/three-quarter.png'),('Human foundation retry — v02',out/'renders/three-quarter.png')]):
 im=Image.open(path).convert('RGB');im.thumbnail((640,800));board.paste(im,(i*640,40));draw.text((i*640+20,15),label,fill='white')
board.save(out/'comparison.png')
