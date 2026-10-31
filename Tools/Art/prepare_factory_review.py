"""Copy approved source FBX and adapt existing PBR maps; no geometry/texture baking."""
import pathlib, json, hashlib, shutil, sys
from PIL import Image, ImageChops
cache=pathlib.Path('D:/VideoGame/.tools/gamedev/factory-thomas')
dest=pathlib.Path(sys.argv[1])/'Assets/FactoryReview'
(dest/'Models').mkdir(parents=True,exist_ok=True)
(dest/'Textures').mkdir(exist_ok=True)
materials=[]
def material(category,name,prefix,style='arch'):
    files=list((cache/category/'textures').iterdir())
    if style=='arch':
        suffixes={'albedo':'_diff_4k','normal':'_nor_gl_4k','rough':'_rough_4k'}
    elif style=='trim':
        suffixes={'albedo':'_BaseColor','normal':'_Normal','rough':'_Roughness','metal':'_Metallic'}
    else:
        suffixes={'albedo':'_Albedo','normal':'_Normal','packed':'_MetallicSmo','emission':'_Emission'}
    maps={}
    for role,suffix in suffixes.items():
        matches=[p for p in files if p.name.startswith(prefix) and suffix in p.name]
        if not matches: continue
        im=Image.open(matches[0]).convert('RGBA');im.thumbnail((2048,2048),Image.Resampling.LANCZOS)
        maps[role]=im
    if 'albedo' not in maps: raise RuntimeError((name,prefix,'missing albedo'))
    entry=dict(name=name,albedo='',normal='',packed='',emission='')
    if 'rough' in maps:
        rough=maps['rough'].convert('L')
        metal=maps['metal'].convert('L') if 'metal' in maps else Image.new('L',rough.size,0)
        maps['packed']=Image.merge('RGBA',(metal,Image.new('L',rough.size,0),Image.new('L',rough.size,0),ImageChops.invert(rough)))
    for role in entry.keys()-{'name'}:
        if role not in maps:continue
        filename=name.replace(' ','_').replace('+','plus')+'_'+role+'.png'
        maps[role].save(dest/'Textures'/filename)
        entry[role]=filename
    materials.append(entry)
for name,prefix in [('bricks','factory_brick'),('concrete','concrete_layers_02'),('concrete_floor','painted_concrete_02')]:material('architecture',name,prefix)
material('architecture','Trim_metal','trim_trim_metal','trim')
for name,prefix in [('Big Generator','Generator_Big_'),('Misc_01','Misc_01_'),('Misc_02','Misc_02_'),('Tables','Tables_'),('Carts','Carts_'),('Crate','Crate_'),('Shelves','Shelves_'),('pipes_01','Pipes_pipes_01_'),('pipes_02','Pipes_pipes_02_'),('Lamps','Lamps_'),('Barrel + tools','Barrels_')]:material('props',name,prefix,'props')
for category in ['architecture','props']:
    shutil.copy2(next((cache/category/'source').glob('*.fbx')),dest/'Models'/f'{category}.fbx')
provenance=[]
for category,uid in [('architecture','3213637ed81d41138c6746a13bf585b9'),('props','ee08d29bc6cd411e845e50903e4060ca')]:
    archive=cache/(category+'.zip')
    provenance.append(dict(category=category,uid=uid,author='Thomas_Crozelon',license='CC-BY-4.0',source=f'https://sketchfab.com/3d-models/{uid}',sha256=hashlib.sha256(archive.read_bytes()).hexdigest()))
(dest/'catalog.json').write_text(json.dumps(dict(materials=materials,provenance=provenance,changes='Original FBX geometry, source texture maps resampled to max2048; roughness inverted into Unity smoothness alpha. No bake or generative processing.'),indent=2))
print('FACTORY_PREPARED',len(materials),'materials',len(list((dest/'Textures').glob('*.png'))),'maps')
