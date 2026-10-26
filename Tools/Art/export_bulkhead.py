import bpy, os
from pathlib import Path
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
parts=[]
def box(name, loc, scale, bevel=.03):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object; o.name=name; o.dimensions=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    m=o.modifiers.new('Machined edges','BEVEL'); m.width=bevel; m.segments=3
    bpy.ops.object.modifier_apply(modifier=m.name)
    parts.append(o); return o
box('Bulkhead plate',(0,0,1.15),(3.4,.2,2.3),.07)
for x in [-1.62,1.62]: box('Frame',(x,-.08,1.2),(.16,.25,2.4),.04)
for z in [.12,2.25]: box('Cross frame',(0,-.08,z),(3.4,.25,.16),.04)
for x in [-.95,0,.95]:
    box('Inset armor',(x,-.13,1.2),(.86,.1,1.85),.05)
    box('Lower vent',(x,-.2,.45),(.7,.07,.3),.025)
    for z in [.36,.43,.50]: box('Grille slat',(x,-.25,z),(.6,.04,.025),.005)
for x in [-1.5,1.5]:
    for z in [.3,1.2,2.1]:
        bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.045,depth=.06,location=(x,-.24,z),rotation=(1.5707963,0,0))
        parts.append(bpy.context.object)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join()
o=bpy.context.object; o.name='Bulkhead'
bpy.context.scene.cursor.location=(0,0,0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.smart_project(island_margin=.02); bpy.ops.object.mode_set(mode='OBJECT')
dest=str(Path(__file__).resolve().parents[2] / 'Assets/Game/Resources/Detailed/RawIndustrial/Bulkhead.fbx')
bpy.ops.export_scene.fbx(filepath=dest,use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y')
