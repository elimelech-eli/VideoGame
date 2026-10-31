"""Copy a small, unmodified CC0 selection for an isolated Unity art audition."""
import json, hashlib, shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = next((ROOT/'.tools/nexus/Standard').iterdir())
OUT = ROOT/'Assets/NexusShipReview'
names = ['Platform_Metal', 'Platform_DarkPlates', 'Platform_Squares',
         'WallAstra_Straight', 'WallAstra_Straight_Broken', 'WallAstra_Straight_Window',
         'ShortWall_MetalPlates_Straight', 'BottomMetal_Straight', 'TopCables_Straight',
         'Door_Frame_Square', 'Door_DarkMetal', 'Column_MetalSupport', 'Column_Simple',
         'Prop_Computer', 'Prop_AccessPoint', 'Prop_Chest', 'Prop_Crate3', 'Prop_Crate4',
         'Prop_Barrel_Large', 'Prop_Cable_1', 'Prop_Cable_3', 'Prop_Light_Wide',
         'Prop_Light_Floor', 'Prop_Vent_Wide', 'Prop_Vent_Small']
(OUT/'Models').mkdir(parents=True, exist_ok=True)
(OUT/'Textures').mkdir(exist_ok=True)
materials = {}
catalog = []
for name in names:
    src = next((SOURCE/'FBX (Unity)').rglob(name+'.fbx'))
    shutil.copy2(src, OUT/'Models'/src.name)
    data = json.loads(next((SOURCE/'glTF').rglob(name+'.gltf')).read_text())
    for mat in data.get('materials', []):
        def texture(ref):
            if not ref: return ''
            return data['images'][data['textures'][ref['index']]['source']]['uri']
        pbr = mat.get('pbrMetallicRoughness', {})
        materials[mat['name']] = dict(name=mat['name'], albedo=texture(pbr.get('baseColorTexture')),
            normal=texture(mat.get('normalTexture')), orm=texture(pbr.get('metallicRoughnessTexture')),
            emission=texture(mat.get('emissiveTexture')), color=pbr.get('baseColorFactor',[1,1,1,1]))
    pos = [data['accessors'][p['attributes']['POSITION']] for mesh in data['meshes'] for p in mesh['primitives']]
    lo = [min(p['min'][i] for p in pos) for i in range(3)]
    hi = [max(p['max'][i] for p in pos) for i in range(3)]
    catalog.append(dict(name=name,min=lo,max=hi))
for src in (SOURCE/'Textures').glob('*.png'): shutil.copy2(src, OUT/'Textures'/src.name)
shutil.copy2(SOURCE/'License_Standard.txt', OUT/'License_Standard.txt')
(OUT/'source-materials.json').write_text(json.dumps(dict(materials=list(materials.values()),modules=catalog),indent=2))
files = [dict(path=p.relative_to(OUT).as_posix(), bytes=p.stat().st_size,
              sha256=hashlib.sha256(p.read_bytes()).hexdigest(),role='source-art')
         for p in OUT.rglob('*') if p.is_file() and p.suffix != '.meta' and p.name != 'pack.json']
manifest=dict(schema='nexus.asset-pack/1',name='Quaternius Standard ship audition selection',genre='isometric sci-fi hub',version='2026-10-31',
 rights=dict(originality='Unmodified downloaded Quaternius meshes and textures; project-authored selection metadata.',third_party=[dict(source='https://quaternius.itch.io/modular-sci-fi-megakit',licence='CC0-1.0')],licence='CC0-1.0'),
 logical=dict(tick_hz=60,unit='metre',coordinate_system='Unity Y-up',facing='+Z',actor_pivot='feet'),
 targetResolution='1600x1000',display=dict(targetResolution='1600x1000',profiles={'industrial-3d':dict(kind='3d')}),
 styles=[dict(id='industrial-3d',kind='3d',art_root='Models',filter='n/a')],characters=[],levels=[],assets={},files=files,
 acceptance_status=dict(engine_import='not-run',human_playtest='not-run',audio_perception='not-run'))
(OUT/'pack.json').write_text(json.dumps(manifest,indent=2))
print(json.dumps(dict(models=len(names),textures=len(list((OUT/'Textures').glob('*.png'))),materials=list(materials),bounds=catalog),indent=2))
