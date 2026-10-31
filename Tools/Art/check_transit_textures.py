"""Validate the pinned Transit texture contract before Unity import (requires Pillow)."""
import argparse,json
from pathlib import Path
from PIL import Image,ImageStat
parser=argparse.ArgumentParser();parser.add_argument('--project-root',type=Path,default=Path(__file__).resolve().parents[2]);args=parser.parse_args()
root=args.project_root/'Assets/Game/Resources/Detailed/TransitEnemies';manifest=json.loads((root/'manifest.json').read_text());checked=0
for role,info in manifest.items():
 for surface in info['materials']:
  for file in [*surface['maps'].values(),surface['id']+'_MetalSmooth.png']:
   path=root/'Textures'/role/file
   with Image.open(path) as im:
    im.load();assert 0<im.width<=2048 and 0<im.height<=2048,(path,im.size)
   checked+=1
 primary='m07' if role=='TransitHeavy' else 'm00'
 with Image.open(root/'Textures'/role/(primary+'_Albedo.png')) as im:
  im=im.convert('RGB');assert min(im.size)>=1024,(role,'source detail resolution')
  assert max(ImageStat.Stat(im).stddev)>5,(role,'textured body is blank or flat')
 print(role,'source body detail verified')
print('PASS:',checked,'texture references, size caps and four detailed body maps')
