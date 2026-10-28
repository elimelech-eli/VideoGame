import bpy,json,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Enemies/v02'/('local-image3d-probe-full' if '--full' in sys.argv else 'local-image3d-probe')
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(OUT/'factory-mortar-probe.glb'))
meshes=[o for o in bpy.data.objects if o.type=='MESH'];points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
scale=1.8/(hi.z-lo.z);center=(lo+hi)*.5
for o in meshes:
 for v in o.data.vertices:
  p=o.matrix_world@v.co;v.co=Vector(((p.x-center.x)*scale,(p.y-center.y)*scale,(p.z-lo.z)*scale))
 o.matrix_world.identity()
 for f in o.data.polygons:f.use_smooth=True
mat=bpy.data.materials.new('Neutral_clay');mat.use_nodes=True;p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.26,.28,.29,1);p.inputs['Roughness'].default_value=.64
for o in meshes:o.data.materials.clear();o.data.materials.append(mat)
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.view_settings.view_transform='Khronos PBR Neutral';scene.view_settings.look='None'
scene.world=bpy.data.worlds.new('Neutral_world');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.11,.11,.11,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
for name,loc,power,size in [('Key',(2,-4,4),500,4),('Fill',(-3,-1,2),200,3),('Rim',(1,3,3),400,3)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.9))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200);o=bpy.context.object;o.name='Review_floor';g=bpy.data.materials.new('Floor');g.diffuse_color=(.08,.085,.09,1);o.data.materials.append(g)
d=bpy.data.cameras.new('Camera');cam=bpy.data.objects.new('Camera',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO';d.ortho_scale=2.3
scene.render.resolution_x=600;scene.render.resolution_y=800;scene.render.resolution_percentage=100
for name,loc in [('front',(2,-5,2)),('back',(-2,5,2)),('side',(5,0,1.8))]:
 cam.location=loc;cam.rotation_euler=(Vector((0,0,.9))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Factory_mortar_shape_probe.blend'),compress=True)
