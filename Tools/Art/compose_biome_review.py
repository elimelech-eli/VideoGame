"""Compose review boards from actual Blender renders; Pillow only, no image synthesis."""
from PIL import Image,ImageDraw
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]/'Art/ProductionReview'
for variant in ['room','atmosphere']:
    sheet=Image.new('RGB',(1440,1550),'#151b23');d=ImageDraw.Draw(sheet)
    for i,name in enumerate(['neon','factory','corporate','biotech','utilities','transit']):
        image=Image.open(ROOT/'environments'/name/(variant+'.png')).convert('RGB');image.thumbnail((720,485));x=i%2*720;y=i//2*515;sheet.paste(image,(x,y+25));d.text((x+16,y+8),name.upper()+' / '+variant.upper(),fill='white')
    sheet.save(ROOT/('environment-'+variant+'-review.jpg'),quality=91)
print('Created two six-biome review boards')
