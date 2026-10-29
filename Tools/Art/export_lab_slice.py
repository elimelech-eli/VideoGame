"""Convert the credited lab scene into a small review kit; never modify raw input."""
import bpy,bmesh,json,sys,math,hashlib
from pathlib import Path
from mathutils import Vector
import numpy as np
root=Path(sys.argv[sys.argv.index('--')+1])
bpy.ops.wm.open_mainfile(filepath='D:/VideoGame/.tools/gamedev/lab-source/source.blend')
out=root/'Assets/Game/Resources/LabReview'; (out/'Models').mkdir(parents=True,exist_ok=True);(out/'Textures').mkdir(exist_ok=True)
source={o.name:o for o in bpy.data.objects if o.type=='MESH' and o.name!='Cube'}
records=[]; used=set()
def module(name,names,origin=None,clip=None):
 objs=[]
 for key in names:
  src=source[key]; o=src.copy();o.data=src.data.copy();bpy.context.collection.objects.link(o)
  o.data.transform(o.matrix_world);o.matrix_world.identity()
  if clip:
   bm=bmesh.new();bm.from_mesh(o.data)
   for axis,lo,hi in clip:
    for val,direction in [(lo,-1),(hi,1)]:
     co=Vector((0,0,0));co[axis]=val;no=Vector((0,0,0));no[axis]=direction
     bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=co,plane_no=no,clear_outer=True,clear_inner=False)
   bm.to_mesh(o.data);bm.free()
  if not o.data.polygons:bpy.data.objects.remove(o,do_unlink=True);continue
  objs.append(o)
 if name.startswith('Floor'):
  fw,fd=(2,2) if name=='Floor2' else (2,4) if name=='Floor2x4' else (4,4)
  mesh=bpy.data.meshes.new('Floor substrate');mesh.from_pydata([(0,0,-.02),(fw,0,-.02),(fw,fd,-.02),(0,fd,-.02)],[],[(0,1,2,3)]);mesh.materials.append(bpy.data.materials['panel3'])
  uv=mesh.uv_layers.new()
  for loop,co in zip(uv.data,[(0,0),(1,0),(1,1),(0,1)]):loop.uv=co
  backing=bpy.data.objects.new('Object_29',mesh);bpy.context.collection.objects.link(backing);objs.append(backing)
 if origin is None:
  vs=[v.co for o in objs for v in o.data.vertices];origin=Vector(((min(v.x for v in vs)+max(v.x for v in vs))/2,(min(v.y for v in vs)+max(v.y for v in vs))/2,min(v.z for v in vs)))
 for o in objs:
  o.data.transform(__import__('mathutils').Matrix.Translation(-Vector(origin)));o.name=name+'_'+o.name
  used.update(s.material.name for s in o.material_slots if s.material)
 bpy.ops.object.select_all(action='DESELECT')
 for o in objs:o.select_set(True)
 bpy.context.view_layer.objects.active=objs[0]
 bpy.ops.export_scene.fbx(filepath=str(out/'Models'/f'{name}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='STRIP',use_mesh_modifiers=True)
 vs=[v.co for o in objs for v in o.data.vertices]
 records.append({'name':name,'singleMaterial':objs[0].material_slots[0].material.name if len(objs)==1 else '','triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objs),'source_objects':names,'bounds':[[min(v[a] for v in vs) for a in range(3)],[max(v[a] for v in vs) for a in range(3)]]})
 for o in objs:bpy.data.objects.remove(o,do_unlink=True)
module('Floor4',['Object_4','Object_29'],(2,2,0),[(0,0,4),(1,0,4)])
module('Wall4',['Object_27','Object_30'],(2,7,0),[(0,0,4),(1,6.85,7.1)])
module('Floor2',['Object_4','Object_29'],(1,1,0),[(0,0,2),(1,0,2)])
module('Floor2x4',['Object_4','Object_29'],(1,2,0),[(0,0,2),(1,0,4)])
for width in (1,2,4):
 if width!=4:module('Wall'+str(width),['Object_27','Object_30'],(width/2,7,0),[(0,0,width),(1,6.85,7.1)])
 module('WallLow'+str(width),['Object_27','Object_30'],(width/2,7,0),[(0,0,width),(1,6.85,7.1),(2,-.01,.85)])
module('Containment',['Object_11','Object_13'])
module('Examination',['Object_14','Object_17'])
module('SpecimenRack',['Object_18','Object_23'])
module('ControlBank',['Object_3'])
module('UtilityPipe',['Object_10','Object_25'])
module('Cabinet',['Object_16','Object_24'])
module('Door',['Object_9'])
module('Console',['Object_22'])
module('SampleJar',['Object_7','Object_6'])
module('SampleVial',['Object_21','Object_20'])
if '--geometry-only' in sys.argv:
 manifest_path=out/'manifest.json';manifest=json.loads(manifest_path.read_text());manifest['modules']=records;manifest_path.write_text(json.dumps(manifest,indent=2)+'\n');print('LAB_GEOMETRY_EXPORTED',len(records));sys.exit(0)
# All control/display colors and emissions are authored replacements. No source UI atlases are exported for these materials.
controls={'Panel_sterowania','Panel_sciana','tablica','stolik_szybka','szafka_szyba'}
materials=[]
def image_for(socket):
 if not socket or not socket.is_linked:return None
 n=socket.links[0].from_node
 if n.type=='TEX_IMAGE':return n.image
 for i in n.inputs:
  r=image_for(i)
  if r:return r
 return None
def save_map(im,path,limit=1024):
 work=im.copy(); w,h=work.size; scale=min(1,limit/max(w,h));work.scale(max(1,int(w*scale)),max(1,int(h*scale)));work.filepath_raw=str(path);work.file_format='PNG';work.save();bpy.data.images.remove(work)
def pixels(im,n=1024):
 work=im.copy();work.scale(n,n);data=np.empty(n*n*4,dtype=np.float32);work.pixels.foreach_get(data);bpy.data.images.remove(work);return data.reshape((n,n,4))
def write_pixels(data,filename,linear=False):
 h,w,_=data.shape;im=bpy.data.images.new(filename,width=w,height=h,alpha=True);im.colorspace_settings.name='Non-Color' if linear else 'sRGB';im.pixels.foreach_set(data.astype(np.float32).ravel());im.filepath_raw=str(out/'Textures'/filename);im.file_format='PNG';im.save();bpy.data.images.remove(im)
def telemetry(w,h,seed=0):
 y,x=np.mgrid[0:h,0:w];u=x/max(1,w-1);v=y/max(1,h-1);rgb=np.zeros((h,w,3),np.float32);rgb[:]=(.001,.006,.009)
 grid=(x%max(8,w//20)==0)|(y%max(8,h//16)==0);rgb[grid]=(.004,.023,.028)
 frame=(u<.006)|(u>.994)|(v<.008)|(v>.992);rgb[frame]=(.025,.25,.29)
 header=(v>.88)&(v<.895);rgb[header]=(.035,.4,.44)
 # Original sample-analysis schematic: two strands, measurement rungs and scan markers.
 dna=(v>.3)&(v<.8)&((abs(u-(.27+.105*np.sin(v*32+seed)))<.0035)|(abs(u-(.27-.105*np.sin(v*32+seed)))<.0035));rgb[dna]=(.055,.62,.55)
 for i in range(13):
  yy=.32+i*.035;left=.27-abs(.105*np.sin(yy*32+seed));right=.27+abs(.105*np.sin(yy*32+seed));rung=(abs(v-yy)<.0025)&(u>left)&(u<right);rgb[rung]=(.025,.28,.35)
 for i in range(3):
  base=.69-i*.14;wave=(abs(v-(base+.027*np.sin(u*(65+i*19)+seed)))<.003)&(u>.52)&(u<.94);rgb[wave]=(.08,.62,.57) if i!=1 else (.65,.28,.06)
 for i in range(12):
  lo=.53+i*.033;bar=(u>lo)&(u<lo+.014)&(v>.25)&(v<.26+((i*17+seed)%31)/280);rgb[bar]=(.025,.35,.45)
 font={
 '0':['01110','10011','10101','11001','01110'], '1':['00100','01100','00100','00100','01110'],
 '2':['11110','00001','01110','10000','11111'], '3':['11110','00001','01110','00001','11110'],
 '4':['10010','10010','11111','00010','00010'], '5':['11111','10000','11110','00001','11110'],
 '6':['01111','10000','11110','10001','01110'], '7':['11111','00010','00100','01000','01000'],
 '8':['01110','10001','01110','10001','01110'], '9':['01110','10001','01111','00001','11110'],
 'A':['01110','10001','11111','10001','10001'],'B':['11110','10001','11110','10001','11110'],
 'C':['01111','10000','10000','10000','01111'],'D':['11110','10001','10001','10001','11110'],
 'E':['11111','10000','11110','10000','11111'],'H':['10001','10001','11111','10001','10001'],
 'I':['11111','00100','00100','00100','11111'],'L':['10000','10000','10000','10000','11111'],
 'M':['10001','11011','10101','10001','10001'],'N':['10001','11001','10101','10011','10001'],
 'O':['01110','10001','10001','10001','01110'],'P':['11110','10001','11110','10000','10000'],
 'R':['11110','10001','11110','10100','10010'],'S':['01111','10000','01110','00001','11110'],
 'T':['11111','00100','00100','00100','00100'],'U':['10001','10001','10001','10001','01110'],
 'V':['10001','10001','10001','01010','00100'],'X':['10001','01010','00100','01010','10001'],
 '/':['00001','00010','00100','01000','10000'],'.':['00000','00000','00000','00100','00100'],
 '-':['00000','00000','11111','00000','00000']}
 def label(text,px,py,color):
  scale=max(1,min(w//160,h//100));cx=int(px*w);top=int(py*h)
  for char in text:
   for row,bits in enumerate(font.get(char,['00000']*5)):
    for col,bit in enumerate(bits):
     if bit=='1':
      xx=cx+col*scale;yy=top-row*scale
      if xx>=0 and yy>=0 and xx+scale<=w and yy+scale<=h:rgb[yy:yy+scale,xx:xx+scale]=color
   cx+=6*scale
 label('NEXUS / BIO',.045,.965,(.1,.55,.6));label('SAMPLE 04',.05,.84,(.05,.35,.4))
 label('CELL SCAN',.05,.21,(.04,.4,.42));label('36.8 C',.55,.16,(.65,.32,.06));label('STATUS / NOMINAL',.05,.095,(.04,.4,.35))
 return rgb
for name in sorted(used):
 m=bpy.data.materials[name];bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');rec={'name':name,'maps':{},'kind':'pbr'}
 glass='szyb' in name.lower() or name=='SZYBKA';display=name in controls and name!='Panel_sterowania'
 if glass:rec['kind']='glass'
 # Preserve original hardware maps. Imported screen graphics are never copied for displays/glass.
 for role,socket in [('Albedo','Base Color'),('Normal','Normal'),('Emission','Emission Color')]:
  im=image_for(bs.inputs.get(socket))
  if im and not ((display or glass) and role!='Normal') and not (name=='Panel_sterowania' and role=='Emission'):
   filename=f'{name}_{role}.png';save_map(im,out/'Textures'/filename,2048 if role in ('Albedo','Normal') else 1024);rec['maps'][role]=filename
 orm=image_for(bs.inputs.get('Roughness'))
 if orm:
  a=pixels(orm);ao=a[:,:,0].copy();metal=a[:,:,2].copy();smooth=1-a[:,:,1].copy();a[:,:,0]=metal;a[:,:,1:3]=0;a[:,:,3]=smooth
  filename=f'{name}_Metallic.png';write_pixels(a,filename,True);rec['maps']['Metallic']=filename
  a[:,:,:3]=ao[:,:,None];a[:,:,3]=1;filename=f'{name}_Occlusion.png';write_pixels(a,filename,True);rec['maps']['Occlusion']=filename
 if name=='Panel_sterowania':
  # This inspected albedo contains hardware plus plain green screen islands; the borrowed imagery was solely in emission.
  base=pixels(image_for(bs.inputs.get('Base Color')));mask=(base[:,:,1]>base[:,:,0]*1.3)&(base[:,:,1]>base[:,:,2]*1.25)&(base[:,:,1]>.025)
  emit=np.zeros_like(base);emit[:,:,3]=1
  # Preserve authored light-strip locations as a flat mask, discarding all source image detail.
  old=pixels(image_for(bs.inputs.get('Emission Color')));strips=(old[:,:,1]>.85)&(old[:,:,2]>.65)&(old[:,:,0]<.1);emit[strips,:3]=(.015,.45,.38)
  seen=np.zeros(mask.shape,bool);n=mask.shape[0]
  for sy,sx in np.argwhere(mask):
   if seen[sy,sx]:continue
   todo=[(int(sy),int(sx))];seen[sy,sx]=True;component=[]
   while todo:
    y,x=todo.pop();component.append((y,x))
    for yy,xx in ((y-1,x),(y+1,x),(y,x-1),(y,x+1)):
     if 0<=yy<n and 0<=xx<n and mask[yy,xx] and not seen[yy,xx]:seen[yy,xx]=True;todo.append((yy,xx))
   ys,xs=np.array(component).T;loy,hiy=ys.min(),ys.max()+1;lox,hix=xs.min(),xs.max()+1
   base[ys,xs,:3]=(.008,.022,.026)
   if len(component)>150:
    chart=telemetry(hix-lox,hiy-loy,int(lox+loy));emit[ys,xs,:3]=chart[ys-loy,xs-lox]
  for role,data in [('Albedo',base),('Emission',emit)]:
   filename=f'{name}_{role}.png';write_pixels(data,filename);rec['maps'][role]=filename
 elif display:
  n=1024;base=np.zeros((n,n,4),np.float32);base[:]=(.008,.02,.026,.22 if glass else 1);base[:,:,:3]+=telemetry(n,n)*.18
  emit=base.copy();emit[:,:,:3]=telemetry(n,n)
  for role,data in [('Albedo',base),('Emission',emit)]:
   filename=f'{name}_{role}.png';write_pixels(data,filename);rec['maps'][role]=filename
 elif glass:
  n=1024;y,x=np.mgrid[0:n,0:n];u=x/(n-1);v=y/(n-1);base=np.zeros((n,n,4),np.float32);base[:]=(.05,.23,.3,.1)
  band=(y%128<3);base[band,:]=(.12,.5,.6,.32);emit=base.copy();emit[:,:,:3]=(.003,.025,.032);emit[band,:3]=(.01,.3,.36)
  # A fresh faint sensor pattern, not the source's borrowed instrument artwork.
  emit[:,:,:3]+=telemetry(n,n)*.12
  for role,data in [('Albedo',base),('Emission',emit)]:
   filename=f'{name}_{role}.png';write_pixels(data,filename);rec['maps'][role]=filename
 materials.append(rec)
# Fresh original telemetry plate: instrument rings, sample lanes and status graph, no borrowed symbols or images.
n=512;y,x=np.mgrid[0:n,0:n];p=np.zeros((n,n,4),np.float32);p[:]=(.009,.025,.033,1)
grid=(x%64<2)|(y%64<2);p[grid,:3]=(.02,.12,.15)
ring=np.abs(np.sqrt((x-160)**2+(y-260)**2)-92)<3;p[ring,:3]=(.1,.85,.72)
for i in range(8):
 mask=(y>38+i*22)&(y<45+i*22)&(x>310)&(x<330+(i*23)%145);p[mask,:3]=(.05,.65,.62)
wave=(np.abs(y-(95+24*np.sin(x*.065)))<2)&(x>28)&(x<280);p[wave,:3]=(.2,.8,.65)
im=bpy.data.images.new('Original telemetry',width=n,height=n);im.pixels.foreach_set(p.ravel());im.filepath_raw=str(out/'Textures/Telemetry_Albedo.png');im.file_format='PNG';im.save()
manifest={'author':'Agata Wilczek','source':'https://sketchfab.com/3d-models/sci-fi-lab-a676e149e343403bb5e57686f3d31da5','license':'CC-BY-4.0','source_sha256':'77de92a6cb6f969eea1a3a3d96cd944e2f68338e1c7819a853d81fcf43bbfceb','modules':records,'materials':materials,'ui_replaced':sorted(controls)}
(out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('LAB_KIT_EXPORTED',len(records),len(materials),sum(r['triangles'] for r in records))
