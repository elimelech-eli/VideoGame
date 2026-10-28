"""Download selected Poly Haven CC0 props via its public API, with hash checks.

Authoring dependencies only. No credentials, scraping, paid providers or runtime
network dependency. Identifies this tool as required by Poly Haven's API terms.
"""
import urllib.request,json,hashlib,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'.tools/gamedev/biome-donors'
IDS=['exterior_aircon_unit','trashbag','old_drill_press','industrial_wall_lamp','metal_office_desk','potted_plant_02','industrial_microscope','medical_box','old_military_compressor','power_box_01','security_camera_01','metal_trash_can']
def read(url):
    req=urllib.request.Request(url,headers={'User-Agent':'VideoGame-LocalArtReview/1.0'})
    with urllib.request.urlopen(req,timeout=90) as response:return response.read()
records=[]
for ident in IDS:
    folder=OUT/ident;folder.mkdir(parents=True,exist_ok=True)
    meta=json.loads(read('https://api.polyhaven.com/info/'+ident));files=json.loads(read('https://api.polyhaven.com/files/'+ident));gltf=files['gltf']['1k']['gltf']
    payload=[(Path(gltf['url']).name,gltf)]+list(gltf.get('include',{}).items())
    for name,item in payload:
        path=(folder/name).resolve();assert path.is_relative_to(folder.resolve())
        if not path.exists() or hashlib.md5(path.read_bytes()).hexdigest()!=item['md5']:
            data=read(item['url']);assert hashlib.md5(data).hexdigest()==item['md5'],name
            path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
    record=dict(id=ident,source='https://polyhaven.com/a/'+ident,license='CC0-1.0',authors=meta.get('authors',{}),gltf=Path(gltf['url']).name,files=len(payload),bytes=sum(x['size'] for _,x in payload))
    (folder/'provenance.json').write_text(json.dumps(record,indent=2));records.append(record);print('DONOR_READY',ident,record['bytes'],flush=True)
review=ROOT/'Art/ProductionReview';review.mkdir(exist_ok=True);(review/'donor-provenance.json').write_text(json.dumps(records,indent=2))
