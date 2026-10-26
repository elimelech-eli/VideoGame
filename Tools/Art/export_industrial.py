import bpy, os, json, sys
from pathlib import Path
from mathutils import Vector
source = sys.argv[sys.argv.index('--') + 1]
bpy.ops.wm.open_mainfile(filepath=source, load_ui=False)
dest = str(Path(__file__).resolve().parents[2] / 'Assets/Game/Resources/Detailed/RawIndustrial')
os.makedirs(dest, exist_ok=True)
names = ['control_terminal','container','tank_2','tank_capsule','tank_control_panel','vent','vent_2','roof_thing','wall_thing','station ']
manifest = {}
for name in names:
    obj = bpy.data.objects.get(name)
    if obj is None: continue
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active=obj
    original=obj.location.copy()
    obj.location=Vector((0,0,0))
    safe=name.strip()
    manifest[safe]=[s.material.name for s in obj.material_slots if s.material]
    bpy.ops.export_scene.fbx(filepath=os.path.join(dest,safe+'.fbx'),use_selection=True,object_types={'MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
    obj.location=original
with open(os.path.join(dest,'manifest.json'),'w') as f: json.dump(manifest,f,indent=2)
print('EXPORTED='+json.dumps(manifest))
