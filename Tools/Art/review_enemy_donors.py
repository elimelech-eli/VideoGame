"""Inspect licensed GLB candidates; render actual imported geometry, not thumbnails."""
import bpy,json,math,time,sys
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/ProductionReview/donor-inspection'
selected=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['player','red_robot']
if selected==['acquired']:selected=[p.parent.name for p in (ROOT/'.tools/gamedev/enemy-donors').glob('*/source.glb')]
for ident in selected:
    bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;folder=OUT/ident;folder.mkdir(parents=True,exist_ok=True)
    source=ROOT/'.tools/gamedev/enemy-donors'/ident/'source.glb'
    if not source.exists():source=ROOT/'.tools/gamedev/enemy-donors/godot-tps'/(ident+'.glb')
    bpy.ops.import_scene.gltf(filepath=str(source))
    scene.frame_set(1);meshes=[o for o in scene.objects if o.type=='MESH' and not o.hide_get() and not o.hide_viewport and o.name!='Icosphere'];rigs=[o for o in scene.objects if o.type=='ARMATURE']
    if ident=='hazmat-loco':
        for rig in rigs:rig.data.pose_position='REST'
        bpy.context.view_layer.update()
    for o in scene.objects:
        if o.type=='MESH' and o not in meshes:o.hide_render=True
    texroot=source.parent;prefix='player_robot' if ident=='player' else 'red_robot'
    for m in list(bpy.data.materials):
        if m.name not in ['playerobot','robotemitter','EvilRobo']:continue
        m.use_nodes=True;p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');ns=m.node_tree.nodes;ln=m.node_tree.links
        def image_node(suffix,color=False):
            im=bpy.data.images.load(str(texroot/(prefix+'_'+suffix+'.png')),check_existing=True)
            if not color:im.colorspace_settings.name='Non-Color'
            n=ns.new('ShaderNodeTexImage');n.image=im;return n
        base=image_node('albedo',True);ln.new(base.outputs['Color'],p.inputs['Base Color'])
        orm=image_node('orm');sep=ns.new('ShaderNodeSeparateColor');ln.new(orm.outputs['Color'],sep.inputs[0]);ln.new(sep.outputs['Green'],p.inputs['Roughness']);ln.new(sep.outputs['Blue'],p.inputs['Metallic'])
        normal=image_node('normal');nm=ns.new('ShaderNodeNormalMap');ln.new(normal.outputs['Color'],nm.inputs['Color']);ln.new(nm.outputs[0],p.inputs['Normal'])
        em=image_node('emissive' if ident=='player' else 'emission',True);ln.new(em.outputs['Color'],p.inputs['Emission Color']);p.inputs['Emission Strength'].default_value=.8
    report=dict(source='https://github.com/godotengine/tps-demo',license='CC-BY-3.0',attribution='Juan Linietsky and Fernando Miguel Calabró, Godot TPS Demo, 2018',model=ident,mesh_count=len(meshes),bones=[len(o.data.bones) for o in rigs],actions=[a.name for a in bpy.data.actions],textures=[im.name for im in bpy.data.images if im.source=='FILE'],status='candidate, not approved; imported and rendered, no runtime compatibility claim')
    if (source.parent/'provenance.json').exists():
        provenance=json.loads((source.parent/'provenance.json').read_text());report.update(source=provenance['source'],license=provenance['license'],attribution=provenance['author'])
    deps=bpy.context.evaluated_depsgraph_get();snap=[]
    for o in meshes:
        d=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);q=bpy.data.objects.new('REVIEW_'+o.name,d);scene.collection.objects.link(q)
        orientation=Matrix.Rotation(math.pi/2,3,'X') if ident=='hazmat-loco' else Matrix.Identity(3)
        for v in d.vertices:v.co=orientation@(o.matrix_world@v.co)
        snap.append(q);o.hide_render=True
    pts=[v.co for o in snap for v in o.data.vertices];lo=Vector(tuple(min(v[i] for v in pts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in pts) for i in range(3)));scale=1.85/(hi.z-lo.z);center=(hi+lo)/2;center.z=lo.z
    for o in snap:
        for v in o.data.vertices:v.co=(v.co-center)*scale
    report['source_bounds']=list(hi-lo);report['review_scale']=scale
    scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=32;scene.render.resolution_x=700;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.render.film_transparent=True
    scene.world=bpy.data.worlds.new('Neutral studio');scene.world.use_nodes=True;bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.16,.19,.22,1);bg.inputs[1].default_value=.6
    scene.view_settings.view_transform='Khronos PBR Neutral';scene.view_settings.look='None'
    for name,p,power,size in [('Key',(-3,-4,5),450,4),('Fill',(3,-2,2),300,3),('Rim',(0,3,4),500,3)]:
        d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=p;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
    d=bpy.data.cameras.new('Inspection');cam=bpy.data.objects.new('Inspection',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO';d.ortho_scale=max(2.3,scale*((hi.x-lo.x)+(hi.y-lo.y))*.8*900/700*1.1)
    for name,az,el in [('threequarter',25,10),('game',35,50)]:
        a,e=map(math.radians,(az,el));target=Vector((0,0,.95));cam.location=target+Vector((math.sin(a)*math.cos(e),-math.cos(a)*math.cos(e),math.sin(e)))*6;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(folder/(name+'.png'));bpy.ops.render.render(write_still=True)
    bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(folder/(ident+'_inspection.blend')),compress=True);(folder/'manifest.json').write_text(json.dumps(report,indent=2));print('DONOR_INSPECTED',ident,len(meshes),report['bones'],len(report['actions']),flush=True)
