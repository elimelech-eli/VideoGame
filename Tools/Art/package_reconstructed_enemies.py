from PIL import Image,ImageDraw,ImageFont,ImageOps
from pathlib import Path
import json,sys
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Enemies/v03'
themes=[('factory','Ruined Automation Factory'),('neon','Neon Street District'),('corporate','Corporate Office Breach'),('biotech','Biotech Quarantine'),('utilities','Undercity Utilities'),('transit','Elevated Transit Terminal')]
roles=[('melee','Quick melee'),('aoe','Ranged AOE'),('heavy','Heavy ranged'),('ranged','Regular ranged')]
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22);small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',16)
gallery=Image.new('RGB',(1440,6*510),(19,22,25));records=[];completed=0
for row,(biome,title) in enumerate(themes):
 if '--available' in sys.argv and not all((OUT/biome/role/'front.png').exists() for role,label in roles):continue
 board=Image.new('RGB',(1440,1070),(19,22,25));d=ImageDraw.Draw(board)
 d.text((15,8),title+' — approved concept above / actual raw 3D body below',font=font,fill='white')
 strip=Image.new('RGB',(1440,510),(19,22,25));sd=ImageDraw.Draw(strip);sd.text((15,5),title+' — untextured body reconstruction',font=font,fill='white')
 for col,(role,label) in enumerate(roles):
  folder=OUT/biome/role
  reference=Image.open(folder/'reference-crop.png').convert('RGB');reference=ImageOps.contain(reference,(354,470));board.paste(reference,(col*360+(360-reference.width)//2,48))
  render=Image.open(folder/'front.png').convert('RGB');render=ImageOps.contain(render,(354,470));board.paste(render,(col*360+(360-render.width)//2,560));strip.paste(render,(col*360+(360-render.width)//2,35))
  d.text((col*360+15,530),label,font=font,fill='white');sd.text((col*360+15,487),label,font=small,fill='white')
  records.append(json.loads((folder/'generation.json').read_text()))
 d.text((15,1045),'AI-generated shape study. High-poly, unrigged, untextured. Visible differences still require review.',font=small,fill=(190,195,200))
 board.save(OUT/biome/'reference-comparison.png');strip.save(OUT/biome/'lineup.png');gallery.paste(strip,(0,completed*510));completed+=1
if completed:gallery.crop((0,0,1440,completed*510)).save(OUT/'available-bodies.png')
(OUT/'manifest.json').write_text(json.dumps(dict(rendered_count=len(records),bodies=records,status='Stopped: visual accuracy gate failed. Raw studies only; requested 24-model redo is not complete.'),indent=2))
