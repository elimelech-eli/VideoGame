import pathlib,json,shutil,sys
from PIL import Image,ImageOps
base=pathlib.Path('D:/VideoGame/.tools/gamedev/factory-enemies/export');project=pathlib.Path(sys.argv[1]);out=project/'Assets/Game/Resources/Detailed/FactoryEnemies'
out.mkdir(parents=True,exist_ok=True);data={}
for folder in sorted(base.glob('Factory*')):
    info=json.loads((folder/'manifest.json').read_text());data[folder.name]=info
    for part in ['Models','Textures/'+folder.name]:(out/part).mkdir(parents=True,exist_ok=True)
    shutil.copy2(folder/(folder.name+'.fbx'),out/'Models'/(folder.name+'.fbx'));dest=out/'Textures'/folder.name
    for m in info['materials']:
        def image(channel,size=(1024,1024)):
            filename=m['maps'].get(channel)
            if filename:
                im=Image.open(folder/filename).convert('RGBA');ch=m['channels'].get(channel)
                if ch:im=im.getchannel(ch)
                return im
            value=m['constants'].get(channel,0)
            if isinstance(value,list):return Image.new('RGBA',(32,32),tuple(round(max(0,min(1,c))*255) for c in value[:4]))
            return Image.new('L',(32,32),round(max(0,min(1,value))*255))
        albedo=image('Albedo');albedo.save(dest/(m['id']+'_Albedo.png'));m['maps']['Albedo']=m['id']+'_Albedo.png'
        rough=image('Roughness').convert('L');metal=image('Metallic').convert('L').resize(rough.size);zero=Image.new('L',rough.size,0)
        Image.merge('RGBA',(metal,zero,zero,ImageOps.invert(rough))).save(dest/(m['id']+'_MetalSmooth.png'))
        for channel in ['Normal','Emission']:
            if channel in m['maps']:shutil.copy2(folder/m['maps'][channel],dest/m['maps'][channel])
        m['maps'].pop('Metallic',None);m['maps'].pop('Roughness',None)
(out/'manifest.json').write_text(json.dumps(data,indent=2))
print('FACTORY_PACKAGED',[(k,v['triangles'],v['unweighted']) for k,v in data.items()])
