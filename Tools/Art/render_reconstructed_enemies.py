"""Render raw, image-conditioned geometry in neutral material for honest review."""
import bpy,json,time,math,sys,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Enemies/v03';start=time.perf_counter();records=[]
for path in sorted(OUT.glob('*/*/Raw_body.glb')):
 folder=path.parent
 only=[a for a in sys.argv if a in ['factory','neon','corporate','biotech','utilities','transit']]
 if only and folder.parent.name not in only:continue
 fingerprint=hashlib.sha256(path.read_bytes()).hexdigest()
 if (folder/'review.json').exists() and all((folder/n).exists() for n in ['front.png','back.png','side.png','Body_review.blend']):
  previous=json.loads((folder/'review.json').read_text())
  if previous.get('raw_sha256')==fingerprint:records.append(previous);continue
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(path))
 meshes=[o for o in bpy.data.objects if o.type=='MESH'];assert meshes
 points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
 lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
 assert all(math.isfinite(v) for p in points for v in p)
 target=2.15 if folder.name=='heavy' else 1.65 if folder.name=='aoe' else 1.85
 scale=target/(hi.z-lo.z);center=(lo+hi)*.5
 for o in meshes:
  for v in o.data.vertices:
   p=o.matrix_world@v.co;v.co=Vector(((p.x-center.x)*scale,(p.y-center.y)*scale,(p.z-lo.z)*scale))
  o.matrix_world.identity()
  for f in o.data.polygons:f.use_smooth=True
 mat=bpy.data.materials.new('Untextured_reference_body');mat.use_nodes=True;p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.18,.19,.20,1);p.inputs['Roughness'].default_value=.66
 for o in meshes:o.data.materials.clear();o.data.materials.append(mat)
 scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.view_settings.view_transform='Khronos PBR Neutral';scene.view_settings.look='None'
 if '--cpu' in sys.argv:
  scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=12;scene.render.threads_mode='FIXED';scene.render.threads=4
 scene.world=bpy.data.worlds.new('Neutral_world');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.11,.11,.11,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.4
 for name,loc,power,size in [('Key',(2,-4,4),380,4),('Fill',(-3,-1,2),150,3),('Rim',(1,3,3),350,3)]:
  d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,target*.5))-o.location).to_track_quat('-Z','Y').to_euler()
 bpy.ops.mesh.primitive_plane_add(size=200);o=bpy.context.object;o.name='Review_floor';g=bpy.data.materials.new('Floor');g.diffuse_color=(.07,.075,.08,1);o.data.materials.append(g)
 d=bpy.data.cameras.new('Camera');cam=bpy.data.objects.new('Camera',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO';d.ortho_scale=2.6
 scene.render.resolution_x=480;scene.render.resolution_y=640;scene.render.resolution_percentage=100
 for name,loc in [('front',(2,-5,2.1)),('back',(-2,5,2.1)),('side',(5,0,1.8))]:
  cam.location=loc;cam.rotation_euler=(Vector((0,0,target*.5))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(folder/(name+'.png'));bpy.ops.render.render(write_still=True)
 cam.location=(2,-5,2.1);cam.rotation_euler=(Vector((0,0,target*.5))-cam.location).to_track_quat('-Z','Y').to_euler()
 bpy.ops.wm.save_as_mainfile(filepath=str(folder/'Body_review.blend'),compress=True)
 record=dict(biome=folder.parent.name,role=folder.name,vertices=sum(len(o.data.vertices) for o in meshes),triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),height=target,raw_sha256=fingerprint,source_hash=json.loads((folder/'generation.json').read_text())['source_hash'],rigged=False,textured=False,unity_integrated=False)
 (folder/'review.json').write_text(json.dumps(record,indent=2));records.append(record);print('BODY_RENDERED',folder.parent.name,folder.name,flush=True)
(OUT/'render-validation.json').write_text(json.dumps(dict(seconds=time.perf_counter()-start,count=len(records),models=records),indent=2))
