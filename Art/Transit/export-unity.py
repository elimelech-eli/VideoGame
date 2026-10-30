import bpy,bmesh,math,json,re,numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
P=Path('D:/VideoGame/.tools/gamedev/transit-source');OUT=P/'unity-export';OUT.mkdir(exist_ok=True);(OUT/'Models').mkdir(exist_ok=True);(OUT/'Textures').mkdir(exist_ok=True)
# Reuse the reviewed extraction coordinates, without assembling/rendering another review scene.
code=(P/'build-connected-station.py').read_text();exec(code[:code.index('def place(')])
OUT=P/'unity-export';(OUT/'Models').mkdir(parents=True,exist_ok=True);(OUT/'Textures').mkdir(exist_ok=True)
modules={}
for w in [1,2,4]:
 for d in [1,2,3]:modules[f'Floor{w}x{d}']=extract('SueloModular',(0,-6.2,-.1),(w,-6.2+d,.2),(w/2,-6.2+d/2,.14))
for w in [1,2,4]:
 for low,height in [('',3.2),('Low',.85)]:modules[f'Wall{low}{w}']=extract('ParedesModulares',(0,-6.84,.14),(w,-6.15,height),(w/2,-6.5,.14))
modules.update(Column=column,Bench=bench,Fixture=light,Electrical=electrical,FireCabinet=fire)
# Read the polished composition for whole train, kiosk and CC0 equipment. Bake transforms into shared-material groups.
names={k:[d.name for d in v] for k,v in modules.items()};bpy.data.libraries.write(str(P/'architecture.blend'),{d for v in modules.values() for d in v});bpy.ops.wm.open_mainfile(filepath=str(P/'connected-station.blend'));scene=bpy.context.scene
with bpy.data.libraries.load(str(P/'architecture.blend'),link=False) as (src,dst):dst.meshes=list({n for ns in names.values() for n in ns})
loaded={n:d for n,d in zip(list({n for ns in names.values() for n in ns}),dst.meshes)};modules={k:[loaded[n] for n in ns] for k,ns in names.items()}

def fromobjects(objects,origin):
 out=[]
 for o in objects:
  dg=o.evaluated_get(bpy.context.evaluated_depsgraph_get());d=bpy.data.meshes.new_from_object(dg);d.transform(Matrix.Translation(-Vector(origin))@o.matrix_world);out.append(d)
 return out

def group(prefix,origin):return fromobjects([o for o in scene.objects if o.type=='MESH' and o.name.startswith(prefix)],origin)
modules['Train']=group('WHOLE CARRIAGE',(0,0,0))
modules['Track']=fromobjects([o for o in scene.objects if o.type=='MESH' and (o.name.startswith('Reserved straight track') or o.name.startswith('End-of-track') or o.name.startswith('Buffer reflector'))],(0,0,0))
# First instance only; its scene world position is the intended contact pivot.
for name,prefix,origin in [('Kiosk','Modern ticket kiosk',(-17,-9.3,0)),('PowerCabinet','CC0 power cabinet',(17.6,-10.6,0)),('Compressor','CC0 maintenance compressor',(12.5,-10.7,0))]:
 objects=[o for o in scene.objects if o.type=='MESH' and o.name.startswith(prefix)]
 # CC0 source origins vary: select one spatial cluster and normalize its measured bounds, keeping all subparts.
 if name!='Kiosk':
  first=objects[0];anchor=first.matrix_world.translation;objects=[o for o in objects if (o.matrix_world.translation-anchor).length<2]
 else:objects=objects[:1]
 data=fromobjects(objects,(0,0,0));pts=[v.co for d in data for v in d.vertices];lo=Vector(tuple(min(v[i] for v in pts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in pts) for i in range(3)));pivot=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
 for d in data:
  for v in d.vertices:v.co-=pivot
 modules[name]=data
# Mesh data captured before opening second blend must be retained via separate extraction, not freed references.

exportscene=bpy.data.scenes.new('Export');bpy.context.window.scene=exportscene
materials={};reports=[]
def safe(s):return re.sub(r'[^a-zA-Z0-9_]+','_',s)
for name,data in modules.items():
 for o in list(exportscene.objects):bpy.data.objects.remove(o,do_unlink=True)
 objects=[]
 for d in data:
  o=bpy.data.objects.new('part',d);exportscene.collection.objects.link(o);objects.append(o)
 # Separate into material groups then join each group: preserves UVs, reduces the train's 744 objects to material batches.
 groups={}
 for o in objects:
  for slot,m in enumerate(o.data.materials):
   if m is None:continue
   key=safe(m.name);materials[key]=m
   d=o.data.copy();bm=bmesh.new();bm.from_mesh(d);bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index!=slot],context='FACES');bm.to_mesh(d);bm.free()
   if not d.polygons:continue
   d.materials.clear();d.materials.append(m)
   for f in d.polygons:f.material_index=0
   q=bpy.data.objects.new('M_'+key,d);exportscene.collection.objects.link(q);groups.setdefault(key,[]).append(q)
 for o in objects:bpy.data.objects.remove(o,do_unlink=True)
 combined=[]
 for key,obs in groups.items():
  bpy.ops.object.select_all(action='DESELECT')
  for o in obs:o.select_set(True)
  bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();o=bpy.context.object;o.name='M_'+key;combined.append(o)
 bpy.ops.object.select_all(action='DESELECT')
 for o in combined:o.select_set(True)
 assert combined,'Empty '+name
 bpy.context.view_layer.objects.active=combined[0]
 bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
 pts=[v.co for o in combined for v in o.data.vertices];lo=[min(v[i] for v in pts) for i in range(3)];hi=[max(v[i] for v in pts) for i in range(3)]
 reports.append(dict(name=name,singleMaterial=next(iter(groups)) if len(groups)==1 else None,min=lo,max=hi,renderers=len(combined),triangles=sum(len(o.data.loop_triangles) or sum(len(f.vertices)-2 for f in o.data.polygons) for o in combined)))
# Export textures from Principled graph; preserve glTF ORM channels as Unity metallic R/smoothness A.

def image_for(socket,visited=None):
 if not socket.is_linked:return None
 visited=visited or set()
 for link in socket.links:
  n=link.from_node
  if n in visited:continue
  visited.add(n)
  if n.type=='TEX_IMAGE':return n.image
  for i in n.inputs:
   found=image_for(i,visited)
   if found:return found
 return None

def save_image(im,key,role):
 if im is None:return None
 copy=im.copy();size=copy.size[:]
 if max(size)>2048:copy.scale(round(size[0]*2048/max(size)),round(size[1]*2048/max(size)))
 file=key+'_'+role+'.png';copy.filepath_raw=str(OUT/'Textures'/file);copy.file_format='PNG';copy.save();bpy.data.images.remove(copy);return file
entries=[]
for key,m in materials.items():
 p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if m.use_nodes else None
 if not p:continue
 maps={};maps['Albedo']=save_image(image_for(p.inputs['Base Color']),key,'Albedo');maps['Normal']=save_image(image_for(p.inputs['Normal']),key,'Normal');maps['Emission']=save_image(image_for(p.inputs['Emission Color']),key,'Emission')
 orm=image_for(p.inputs['Roughness']);metal=image_for(p.inputs['Metallic'])
 # Source glTF metallic/roughness maps are shared RGB: G roughness, B metallic.
 if orm:
  cp=orm.copy();w,h=cp.size
  if max(w,h)>2048:cp.scale(round(w*2048/max(w,h)),round(h*2048/max(w,h)))
  w,h=cp.size;pix=np.array(cp.pixels[:],dtype=np.float32).reshape(h,w,4);result=np.zeros_like(pix);result[:,:,0]=pix[:,:,2] if metal==orm else p.inputs['Metallic'].default_value;result[:,:,3]=1-pix[:,:,1]
  packed=bpy.data.images.new(key+'_Packed',width=w,height=h,alpha=True);packed.colorspace_settings.name='Non-Color';packed.pixels.foreach_set(result.ravel());maps['Metallic']=save_image(packed,key,'Metallic');bpy.data.images.remove(packed);bpy.data.images.remove(cp)
 color=list(p.inputs['Base Color'].default_value);em=list(p.inputs['Emission Color'].default_value);strength=p.inputs['Emission Strength'].default_value
 entries.append(dict(name=key,maps=maps,color=color,metallic=p.inputs['Metallic'].default_value,smoothness=1-p.inputs['Roughness'].default_value,emission=[v*strength for v in em[:3]]))
(OUT/'manifest.json').write_text(json.dumps(dict(version=1,units='metres',sourceAxes='XY ground, Z up; FBX converted to Unity Y up',materials=entries,modules=reports),indent=2))
print('TRANSIT_EXPORT_COMPLETE',json.dumps(reports))
