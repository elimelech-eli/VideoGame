"""Selected CC0 1K surfaces from Poly Haven, using its named public API."""
import json,urllib.request,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'.tools/gamedev/biome-surfaces';records=[]
def read(url):
    with urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'VideoGame-LocalArtReview/1.0'}),timeout=90) as r:return r.read()
for ident in ['asphalt_02','brick_wall_001','concrete_floor_damaged_01','concrete_wall_003','floor_tiles_02','metal_plate']:
    folder=OUT/ident;folder.mkdir(parents=True,exist_ok=True);maps=json.loads(read('https://api.polyhaven.com/files/'+ident));info=json.loads(read('https://api.polyhaven.com/info/'+ident));files={}
    for role,names in [('color',['diffuse','diff']),('normal',['nor_gl']),('roughness',['rough','roughness'])]:
        key=next(k for k in maps if k.lower() in names);options=maps[key]['1k'];fmt='jpg' if 'jpg' in options else 'png';item=options[fmt];path=folder/(role+'.'+fmt)
        if not path.exists() or hashlib.md5(path.read_bytes()).hexdigest()!=item['md5']:
            data=read(item['url']);assert hashlib.md5(data).hexdigest()==item['md5'];path.write_bytes(data)
        files[role]=dict(file=path.name,url=item['url'],md5=item['md5'],sha256=hashlib.sha256(path.read_bytes()).hexdigest())
    rec=dict(id=ident,source='https://polyhaven.com/a/'+ident,license='CC0-1.0',authors=info.get('authors',{}),files=files);(folder/'provenance.json').write_text(json.dumps(rec,indent=2));records.append(rec);print('SURFACE_READY',ident,flush=True)
(ROOT/'Art/ProductionReview/surface-provenance.json').write_text(json.dumps(records,indent=2))
