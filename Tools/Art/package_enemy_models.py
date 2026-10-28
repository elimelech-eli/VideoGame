"""Contact sheets and true-render motion previews; no concept-image substitution."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,shutil
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Enemies/v01'
themes=[('neon','Neon Street District','exec-15e1abee-987b-4381-b018-cd629dfdf742.png'),('factory','Ruined Automation Factory','exec-6055b328-2b0f-44cb-9fb1-970a6416c299.png'),('corporate','Corporate Office Breach','exec-1b6bcaed-bea6-47e9-b90c-6d12e69cf639.png'),('biotech','Biotech Quarantine','exec-18054df3-db41-406d-8fa0-085d34e2100a.png'),('utilities','Undercity Utilities','exec-8806c774-c6b9-4332-9385-ebf163dae920.png'),('transit','Elevated Transit Terminal','exec-3df15609-efdf-444d-b7c8-e9e3773e2553.png')]
roles=[('melee','Quick melee'),('aoe','Ranged AOE'),('heavy','Heavy ranged'),('ranged','Regular ranged')]
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22)
small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',16)
allmodels=[];gallery=Image.new('RGB',(1920,6*660),(16,19,23))
for row,(theme,title,ref) in enumerate(themes):
 board=Image.new('RGB',(1920,660),(16,19,23));draw=ImageDraw.Draw(board);draw.text((20,10),title+' — actual 3D models / review v01',fill='white',font=font)
 for i,(role,label) in enumerate(roles):
  image=Image.open(OUT/theme/role/'preview.png').convert('RGB');board.paste(image,(i*480,50));draw.text((i*480+20,625),label,fill='white',font=small)
  allmodels.append(json.loads((OUT/theme/role/'manifest.json').read_text()))
 board.save(OUT/theme/'lineup.png');gallery.paste(board,(0,row*660))
 reference=OUT/'reference';reference.mkdir(exist_ok=True)
 shutil.copy2(Path('C:/Users/Eli/.codex/generated_images/01a16753-915f-78c3-98e1-49db61931f59')/ref,reference/(theme+'-approved.png'))
gallery.thumbnail((1440,2970));gallery.save(OUT/'all-biomes.png')
for kind in ['move','attack']:
 frames=[]
 for frame in range(12):
  board=Image.new('RGB',(960,340),(16,19,23));draw=ImageDraw.Draw(board)
  for i,(role,label) in enumerate(roles):
   im=Image.open(OUT/'corporate'/role/'motion'/f'{kind}-{frame:02}.png').convert('RGB');board.paste(im,(i*240,30));draw.text((i*240+10,5),label,fill='white',font=small)
  frames.append(board)
 # Clips use different durations; this board compares sampled phases, not timing.
 frames[0].save(OUT/(kind+'-phase-comparison.gif'),save_all=True,append_images=frames[1:],duration=90,loop=0)
 for role,label in roles:
  model=next(m for m in allmodels if m['biome']=='corporate' and m['role']==role)
  duration=model.get('clip_seconds',{}).get(kind)
  # Exact per-clip timing comes from the Blender validation inventory below.
  if duration:
   frames=[Image.open(p).convert('RGB') for p in sorted((OUT/'corporate'/role/'motion').glob(kind+'-*.png'))]
   frames[0].save(OUT/'corporate'/role/(kind+'.gif'),save_all=True,append_images=frames[1:],duration=round(duration*1000/12),loop=0)
(OUT/'manifest.json').write_text(json.dumps(dict(models=allmodels,model_count=len(allmodels),status='Review assets only; no Unity integration'),indent=2))
