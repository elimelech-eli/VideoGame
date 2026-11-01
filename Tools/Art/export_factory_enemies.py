"""Approved Factory sources -> normalized weighted FBX, direct PBR maps (no baking)."""
import bpy,json,pathlib,math,sys
from mathutils import Vector,Matrix
base=pathlib.Path('D:/VideoGame/.tools/gamedev');root=base/'factory-enemies';audit=json.loads((root/'source-audit.json').read_text())
roles=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['heavy']
out=root/'export';out.mkdir(exist_ok=True)
maps={
'melee':{'Body_00':'hips','ArmL1_01':'upperarm.L','ArmL2_02':'forearm.L','PinceR_03':'hand.L','ArmR1_07':'upperarm.R','ArmR2_08':'forearm.R','PinceL_09':'hand.R'},
'ranged':{'Bone_01':'hips','Bone.001_02':'spine','Bone.003_04':'chest','Bone.005_06':'head','Bone.012_07':'upperarm.L','Bone.013_08':'forearm.L','Bone.014_00':'hand.L','Bone.022_016':'upperarm.R','Bone.023_017':'forearm.R','Bone.024_018':'hand.R','Bone.006_026':'thigh.L','Bone.007_027':'calf.L','Bone.008_028':'foot.L','Bone.011_031':'thigh.R','Bone.032_032':'calf.R','Bone.033_033':'foot.R'},
'heavy':{'Bone':'hips','Bone.001':'spine','Bone.002':'head',**{f'Bone.{n}.{s}':f'{r}.{s}' for s in ['L','R'] for n,r in [('003','upperarm'),('004','forearm'),('005','hand'),('009','thigh'),('010','calf')]}},
'aoe':{}}
ids={'heavy':'FactoryHeavy','melee':'FactoryMelee','ranged':'FactoryGunner','aoe':'FactoryAoe'}
def resolve_images(category):
    files=list((root/category).rglob('*')) if category in ['heavy','aoe'] else []
    for im in bpy.data.images:
        if im.packed_file:continue
        path=pathlib.Path(bpy.path.abspath(im.filepath));matches=[f for f in files if f.is_file() and f.stem.lower()==path.stem.lower()]
        if matches:im.filepath=str(matches[0]);im.reload()
def image_link(socket,seen=None):
    if not socket.is_linked:return None,None
    link=socket.links[0];node=link.from_node
    if node.type=='TEX_IMAGE':return node.image,None
    if node.type in ['SEPRGB','SEPARATE_COLOR']:
        im,_=image_link(node.inputs[0]);return im,{'R':'R','Red':'R','G':'G','Green':'G','B':'B','Blue':'B'}.get(link.from_socket.name)
    for inp in node.inputs:
        if inp.is_linked:
            im,ch=image_link(inp)
            if im:return im,ch
    return None,None
for role in roles:
    bpy.ops.wm.read_factory_settings(use_empty=True);file=pathlib.Path(audit[role]['file']);id=ids[role];dest=out/id;dest.mkdir(exist_ok=True)
    if file.suffix=='.glb':bpy.ops.import_scene.gltf(filepath=str(file))
    else:bpy.ops.import_scene.fbx(filepath=str(file))
    resolve_images(role);scene=bpy.context.scene;scene.frame_set(1);rig=next(o for o in scene.objects if o.type=='ARMATURE')
    # Preserve source animated pose for opaque GLB rigid-part hierarchies. FBX imports use rest pose.
    if role in ['heavy','aoe']:
        rig.animation_data_clear()
        for b in rig.pose.bones:b.matrix_basis=Matrix.Identity(4)
        bpy.context.view_layer.update()
    if role=='heavy':
        for side in ['L','R']:
            for name,direction in [(f'Bone.003.{side}',Vector((-.1 if side=='L' else .1,-.22,-1))), (f'Bone.004.{side}',Vector((0,-1,.15)))]:
                pb=rig.pose.bones[name];head=rig.matrix_world@pb.head;tail=rig.matrix_world@pb.tail
                q=(tail-head).rotation_difference(direction)
                pb.matrix=rig.matrix_world.inverted()@Matrix.Translation(head)@q.to_matrix().to_4x4()@Matrix.Translation(-head)@rig.matrix_world@pb.matrix
                bpy.context.view_layer.update()
    if role=='ranged':
        # Rebase the armed side out of the source showcase pose into a firing carry.
        rig.animation_data_clear()
        for name,child,direction in [('Bone.022_016','Bone.023_017',Vector((-.1,-.25,-1))),('Bone.023_017','Bone.024_018',Vector((0,-1,.1)))]:
            pb=rig.pose.bones[name];head=rig.matrix_world@pb.head;end=rig.matrix_world@rig.pose.bones[child].head
            if (end-head).length<.00001:raise RuntimeError('Cannot establish gunner arm direction')
            q=(end-head).rotation_difference(direction)
            pb.matrix=rig.matrix_world.inverted()@Matrix.Translation(head)@q.to_matrix().to_4x4()@Matrix.Translation(-head)@rig.matrix_world@pb.matrix
            bpy.context.view_layer.update()
    deps=bpy.context.evaluated_depsgraph_get();mapping=maps[role]
    bones=[(mapping.get(b.name,b.name),rig.matrix_world@b.head,rig.matrix_world@b.tail,mapping.get(b.parent.name,b.parent.name) if b.parent else None,rig.matrix_world.to_3x3()@b.matrix.to_3x3()) for b in rig.pose.bones]
    snaps=[]
    for obj in list(scene.objects):
        if obj.type!='MESH':continue
        mesh=bpy.data.meshes.new_from_object(obj.evaluated_get(deps),depsgraph=deps)
        for v in mesh.vertices:v.co=obj.matrix_world@v.co
        snap=bpy.data.objects.new('Skin_'+obj.name,mesh);scene.collection.objects.link(snap)
        snap.vertex_groups.clear()
        for vg in obj.vertex_groups:snap.vertex_groups.new(name=mapping.get(vg.name,vg.name))
        if obj.vertex_groups:
            if len(mesh.vertices)!=len(obj.data.vertices):raise RuntimeError('Source topology changed before weight copy '+obj.name)
            for v in obj.data.vertices:
                for g in v.groups:
                    if g.weight>0:snap.vertex_groups[g.group].add([v.index],g.weight,'REPLACE')
        # Rigid glTF parts can follow an animated bone through intervening empty objects.
        parent=obj;bone=''
        while parent:
            if parent.parent_type=='BONE':bone=mapping.get(parent.parent_bone,parent.parent_bone);break
            parent=parent.parent
        if not snap.vertex_groups:
            if not bone:
                if role in ['melee','ranged'] and obj.name=='Icosphere':
                    bpy.data.objects.remove(snap,do_unlink=True);print('EXCLUDED_UNBOUND_PREVIEW_SCENERY',obj.name);continue
                raise RuntimeError(f'{role}: rigid part has no bone ancestor: {obj.name}')
            g=snap.vertex_groups.new(name=bone);g.add(list(range(len(mesh.vertices))),1,'REPLACE')
        snaps.append(snap)
    for o in list(scene.objects):
        if o not in snaps:bpy.data.objects.remove(o,do_unlink=True)
    lo=Vector([min(v.co[i] for o in snaps for v in o.data.vertices) for i in range(3)]);hi=Vector([max(v.co[i] for o in snaps for v in o.data.vertices) for i in range(3)])
    scale=2/(hi.z-lo.z);offset=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
    for o in snaps:
        for v in o.data.vertices:v.co=(v.co-offset)*scale
    arm=bpy.data.armatures.new(id+'Rig');newrig=bpy.data.objects.new(id+'Rig',arm);scene.collection.objects.link(newrig);bpy.context.view_layer.objects.active=newrig;newrig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
    for name,h,t,parent,rot in bones:
        b=arm.edit_bones.new(name);b.head=(h-offset)*scale;b.tail=(t-offset)*scale
        if (b.tail-b.head).length<.0001:b.tail=b.head+Vector((0,0,.01))
        b.align_roll(rot@Vector((0,0,1)))
    for name,h,t,parent,rot in bones:
        if parent:arm.edit_bones[name].parent=arm.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    if role=='heavy':
        old=set(scene.objects);bpy.ops.import_scene.gltf(filepath=str(base/'enemy-donors/rifle-ulitka/source.glb'))
        gun=[o for o in scene.objects if o not in old and o.type=='MESH'];coords=[o.matrix_world@v.co for o in gun for v in o.data.vertices];mn=Vector([min(p[i] for p in coords) for i in range(3)]);mx=Vector([max(p[i] for p in coords) for i in range(3)]);size=mx-mn
        axis=max(range(3),key=lambda i:size[i]);direction=Vector((1,0,0)) if axis==0 else Vector((0,1,0)) if axis==1 else Vector((0,0,1));rot=direction.rotation_difference(Vector((0,-1,0)));factor=1.25/max(size);grip=arm.bones['hand.R'].head_local+Vector((0,-.32,.06))
        for obj in gun:
            mesh=bpy.data.meshes.new_from_object(obj.evaluated_get(bpy.context.evaluated_depsgraph_get()),depsgraph=bpy.context.evaluated_depsgraph_get())
            for v in mesh.vertices:v.co=rot@((obj.matrix_world@v.co-(mn+mx)/2)*factor)+grip
            o=bpy.data.objects.new('HeavyServiceRifle',mesh);scene.collection.objects.link(o);g=o.vertex_groups.new(name='hand.R');g.add(list(range(len(mesh.vertices))),1,'REPLACE');snaps.append(o)
        for obj in list(scene.objects):
            if obj not in snaps and obj!=newrig:bpy.data.objects.remove(obj,do_unlink=True)
    for o in snaps:
        mod=o.modifiers.new('Skin','ARMATURE');mod.object=newrig;o.parent=newrig
        if not o.data.uv_layers:raise RuntimeError('Missing source UV '+o.name)
        active=o.data.uv_layers.active;uv=[p.uv.copy() for p in active.data]
        for layer in list(o.data.uv_layers):o.data.uv_layers.remove(layer)
        layer=o.data.uv_layers.new(name='UVMap')
        for p,t in zip(layer.data,uv):p.uv=t
    entries=[]
    for idx,mat in enumerate(sorted({m for o in snaps for m in o.data.materials if m},key=lambda m:m.name)):
        p=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
        if p is None:raise RuntimeError('No source PBR '+mat.name)
        entry=dict(name=mat.name,id=f'm{idx:02}',maps={},channels={},constants={},emissionStrength=p.inputs['Emission Strength'].default_value)
        for channel,socket in [('Albedo','Base Color'),('Normal','Normal'),('Metallic','Metallic'),('Roughness','Roughness'),('Emission','Emission Color')]:
            im,ch=image_link(p.inputs[socket])
            if im:
                if not im.size[0]:raise RuntimeError('Unavailable image '+im.filepath)
                _=im.pixels[0];copy=im.copy();k=min(1,(2048 if channel in ['Albedo','Normal'] else 1024)/max(copy.size));copy.scale(max(1,round(copy.size[0]*k)),max(1,round(copy.size[1]*k)))
                filename=f'm{idx:02}_{channel}.png';copy.filepath_raw=str(dest/filename);copy.file_format='PNG';copy.save();entry['maps'][channel]=filename;entry['channels'][channel]=ch;bpy.data.images.remove(copy)
            else:
                value=p.inputs[socket].default_value
                entry['constants'][channel]=float(value) if isinstance(value,(int,float)) else list(value)
        entries.append(entry)
    bpy.ops.object.select_all(action='DESELECT')
    for o in snaps:o.select_set(True)
    bpy.context.view_layer.objects.active=snaps[0];bpy.ops.object.join();body=bpy.context.object;body.name='Geometry';valid={b.name for b in arm.bones}
    # Remove only unreferenced source helper vertices; never infer visible skin weights.
    loose=[v.index for v in body.data.vertices if not any(g.weight>0 and body.vertex_groups[g.group].name in valid for g in v.groups)]
    if role=='heavy' and len(loose)<=2:
        used={i for p in body.data.polygons for i in p.vertices}
        if any(i in used for i in loose):raise RuntimeError('Loader has visible unweighted geometry; replacement required')
        import bmesh
        bm=bmesh.new();bm.from_mesh(body.data);bm.verts.ensure_lookup_table()
        bmesh.ops.delete(bm,geom=[bm.verts[i] for i in loose],context='VERTS');bm.to_mesh(body.data);bm.free()
        print('REMOVED_UNUSED_SOURCE_VERTICES',len(loose),flush=True)
    missing=sum(not any(g.weight>0 and body.vertex_groups[g.group].name in valid for g in v.groups) for v in body.data.vertices)
    if missing:raise RuntimeError(f'{id}: unweighted vertices {missing}; groups {[(g.index,g.name) for g in body.vertex_groups]}; valid {sorted(valid)}; sample {[(v.index,[(g.group,g.weight) for g in v.groups]) for v in body.data.vertices if not any(g.weight>0 and body.vertex_groups[g.group].name in valid for g in v.groups)][:3]}')
    bpy.ops.object.select_all(action='DESELECT');body.select_set(True);newrig.select_set(True);bpy.context.view_layer.objects.active=newrig
    bpy.ops.export_scene.fbx(filepath=str(dest/(id+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=False)
    bpy.ops.wm.save_as_mainfile(filepath=str(dest/(id+'.blend')))
    result=dict(materials=entries,bones=len(arm.bones),triangles=sum(len(p.vertices)-2 for p in body.data.polygons),unweighted=missing)
    (dest/'manifest.json').write_text(json.dumps(result,indent=2));print('FACTORY_EXPORTED',id,result['triangles'],result['bones'],flush=True)
