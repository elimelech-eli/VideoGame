"""Original modular biome meshes. Blender 4.5, metres, Z up -> Unity Y up.
Run: blender -b -t 4 --python Tools/Art/export_biomes.py
All sources are generated here; no external/paid asset service is used.
"""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Game/Resources/Biomes/Models'
OUT.mkdir(parents=True, exist_ok=True)
MATS = {}
for name, color in [('Shell', (.32,.36,.4,1)), ('Trim', (.12,.15,.18,1)),
                    ('Glow', (.1,.8,1,1)), ('Dark', (.025,.035,.045,1)), ('Glass', (.08,.3,.38,1))]:
    material = bpy.data.materials.new(name); material.diffuse_color = color; MATS[name] = material
parts = []
report = []

def box(name, p, dims, material='Shell', bevel=.035):
    bpy.ops.mesh.primitive_cube_add(size=1, location=p)
    obj=bpy.context.object; obj.name=name; obj.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        modifier=obj.modifiers.new('Machined edge','BEVEL');modifier.width=bevel;modifier.segments=2
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.data.materials.append(MATS[material]);parts.append(obj);return obj

def cylinder(name, p, radius, depth, material='Trim', axis='Z', vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=p)
    obj=bpy.context.object;obj.name=name
    if axis=='Y':obj.rotation_euler[0]=math.pi/2
    if axis=='X':obj.rotation_euler[1]=math.pi/2
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    obj.data.materials.append(MATS[material]);parts.append(obj);return obj

def begin():
    global parts
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);parts=[]

def export(name, moving=None):
    # Separate rotating geometry retains an explicit centre pivot.
    body=[obj for obj in parts if obj not in (moving or [])]
    groups=[('Body',body)]+([('Rotor',moving)] if moving else [])
    joined=[]
    for label, objects in groups:
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join()
        obj=bpy.context.object;obj.name=label
        bpy.context.scene.cursor.location=(0,0,.65) if label=='Rotor' else (0,0,0)
        bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        # Metric box projection: stable tile density, including the top floor plane.
        uv=obj.data.uv_layers.new(name='UVMap') if not obj.data.uv_layers else obj.data.uv_layers.active
        for polygon in obj.data.polygons:
            axis=max(range(3),key=lambda i:abs(polygon.normal[i]))
            axes=[i for i in range(3) if i!=axis]
            for loop in polygon.loop_indices:
                v=obj.data.vertices[obj.data.loops[loop].vertex_index].co
                uv.data[loop].uv=(v[axes[0]]/4+.5,v[axes[1]]/4+.5)
        joined.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in joined:obj.select_set(True)
    bpy.context.view_layer.objects.active=joined[0]
    bpy.context.scene.unit_settings.system='METRIC'
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},
        bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False)
    points=[obj.matrix_world@Vector(corner) for obj in joined for corner in obj.bound_box]
    low=[min(p[i] for p in points) for i in range(3)];high=[max(p[i] for p in points) for i in range(3)]
    tris=sum(len(poly.vertices)-2 for obj in joined for poly in obj.data.polygons)
    report.append(dict(asset=name,triangles=tris,source_bounds=[low,high],moving_node='Rotor' if moving else None))

def wall(kind):
    begin();box('Wall core',(0,0,1.5),(4,.28,3),bevel=.04)
    for x in [-1.94,1.94]:box('Pillar',(x,-.08,1.5),(.12,.45,3),'Trim')
    for z in [.12,2.88]:box('Cornice',(0,-.1,z),(4,.4,.16),'Trim')
    if kind==0: # Shuttered shop front and inset advertisement.
        box('Shutter inset',(-.6,-.18,1.45),(2.35,.06,2.25),'Dark')
        for i in range(15):box('Shutter slat',(-.6,-.24,.42+i*.14),(2.3,.09,.07),'Shell',.008)
        box('Sign frame',(.95,-.22,1.7),(.7,.12,1.9),'Trim')
        box('Neon advertisement',(.95,-.3,1.7),(.56,.025,1.66),'Glow')
        for z in [1.2,1.5,1.8,2.1]:box('Sign glyph',(.95,-.33,z),(.38,.02,.05),'Dark',0)
        box('Shop canopy',(-.5,-.4,2.64),(2.8,.9,.14),'Trim')
    elif kind==1:
        for x in [-1.2,0,1.2]:
            box('Steel panel',(x,-.18,1.5),(1,.12,2.35),'Shell')
            for z in [.4,2.6]:cylinder('Rivet',(x,-.28,z),.055,.06,'Trim','Y')
        for x in [-1.76,1.76]:cylinder('Conduit',(x,-.3,1.45),.09,2.55,'Trim')
        box('Hazard strip',(0,-.27,.5),(3.3,.07,.16),'Glow')
    elif kind==2:
        for x in [-1.05,.4]:
            box('Glazed office panel',(x,-.18,1.65),(1.25,.07,2),'Glass')
            box('Glazing mullion',(x+.67,-.24,1.5),(.055,.12,2.6),'Trim')
        box('Stone skirting',(0,-.24,.36),(3.8,.1,.45),'Shell')
        box('Architectural light',(0,-.25,2.73),(3.6,.04,.035),'Glow',.008)
    elif kind==3:
        for x in [-1,1]:box('Clinical panel',(x,-.2,1.5),(1.86,.13,2.4),'Shell',.09)
        box('Observation glass',(-.5,-.29,1.65),(2.3,.03,.8),'Glass')
        for z in [.65,2.5]:box('Seal line',(0,-.3,z),(3.65,.025,.04),'Glow',.008)
        box('Interface',(1.35,-.32,1.6),(.42,.14,.65),'Dark')
        for z in [1.45,1.6,1.75]:box('Interface status',(1.35,-.41,z),(.26,.02,.035),'Glow',0)
    elif kind==4:
        for z in [.5,1.1,2.4]:
            cylinder('Utility pipe',(0,-.38,z),.14,3.9,'Trim','X')
            for x in [-1.25,1.25]:cylinder('Pipe flange',(x,-.38,z),.2,.12,'Shell','X')
        for x in [-1.8,1.8]:box('Pipe bracket',(x,-.27,1.5),(.09,.35,2.5),'Trim')
        box('Power junction',(.5,-.39,1.75),(.8,.4,.7),'Shell')
    else:
        box('Station glazing',(-.3,-.18,1.8),(2.9,.08,1.6),'Glass')
        for x in [-1.8,1.8]:box('Steel fin',(x,-.25,1.6),(.14,.3,2.6),'Trim')
        box('Route header',(0,-.25,2.65),(3.55,.15,.26),'Dark')
        for x in [-1.4,-.7,0,.7,1.4]:box('Route marker',(x,-.34,2.65),(.35,.02,.07),'Glow',.005)
        box('Station seat',(0,-.5,.5),(2.6,.65,.15),'Trim')
        for x in [-1,1]:box('Seat leg',(x,-.45,.25),(.1,.3,.5),'Shell')
    export(f'{kind}_Wall')

def floor(kind):
    begin();box('Floor module',(0,0,-.10),(4,4,.2),'Shell',.015)
    # Visible seams and edge rails add physical detail without blocking traversal.
    for x in [-1.985,1.985]:box('Panel seam',(x,0,.003),(.025,3.95,.008),'Trim',0)
    if kind in [1,4]:
        for x in [-1.7,1.7]:
            for y in [-1.7,1.7]:cylinder('Floor bolt',(x,y,.009),.045,.01,'Trim',vertices=8)
    if kind==5:
        for y in [-1.8,1.8]:box('Tactile floor strip',(0,y,.007),(3.7,.12,.015),'Trim',.004)
    export(f'{kind}_Floor')

def props(kind):
    begin()
    if kind==0:
        box('Vending base',(0,0,1.05),(1.15,.8,2.1),'Shell',.08)
        box('Display',(0,-.42,1.4),(.9,.06,1.05),'Glow')
        for x in [-.27,0,.27]:
            for z in [1.15,1.45,1.75]:cylinder('Product',(x,-.48,z),.075,.07,'Trim','Y')
        box('Dispenser',(0,-.45,.44),(.7,.1,.27),'Dark')
    elif kind==1:
        cylinder('Furnace vessel',(0,0,.9),.7,1.65,'Shell')
        for z in [.2,1.1,1.7]:cylinder('Vessel band',(0,0,z),.76,.12,'Trim')
        cylinder('Luminous core',(0,0,1.81),.55,.04,'Glow')
        for x in [-.8,.8]:box('Heavy support',(x,0,.8),(.18,.8,1.6),'Trim')
    elif kind==2:
        box('Executive desk',(0,0,.85),(2.25,1.1,.12),'Shell')
        for x in [-.9,.9]:box('Desk support',(x,0,.4),(.15,.9,.8),'Trim')
        box('Display stand',(.3,.2,1.02),(.1,.12,.3),'Trim')
        box('Workstation display',(.3,.28,1.35),(1,.09,.55),'Glow')
        box('Keyboard',(.2,-.2,.94),(.65,.23,.035),'Dark')
        box('Data pedestal',(-.78,.2,1.05),(.2,.35,.3),'Trim')
    elif kind==3:
        cylinder('Specimen pod',(0,0,1),.52,1.65,'Glass',vertices=24)
        for z in [.15,1.9]:cylinder('Pod seal',(0,0,z),.63,.22,'Shell',vertices=24)
        for x,y in [(-.48,-.3),(.48,-.3),(-.48,.3),(.48,.3)]:box('Pod frame',(x,y,1.03),(.09,.09,1.7),'Trim')
        cylinder('Luminous sample',(0,0,1.02),.18,1.15,'Glow')
    elif kind==4:
        cylinder('Pump housing',(0,0,.7),.55,1.7,'Trim','X')
        for x in [-.65,.65]:cylinder('Pump collar',(x,0,.7),.66,.14,'Shell','X')
        for x in [-.55,.55]:box('Pump foot',(x,0,.15),(.24,.85,.3),'Shell')
        cylinder('Pressure gauge',(.2,-.5,1.3),.2,.15,'Shell','Y')
        cylinder('Gauge dial',(.2,-.59,1.3),.16,.02,'Glow','Y')
    else:
        box('Ticket kiosk',(0,0,.8),(.85,.65,1.6),'Shell',.1)
        box('Ticket screen',(0,-.34,1.22),(.66,.04,.57),'Glow')
        box('Ticket slot',(0,-.35,.7),(.52,.035,.06),'Dark')
        box('Route pylon',(.65,.1,1.45),(.18,.2,2.9),'Trim')
        box('Route badge',(.65,.08,2.2),(.48,.25,.68),'Glow')
    export(f'{kind}_Feature')
    begin();box('Service cabinet',(0,0,.65),(1,.65,1.3),'Shell')
    box('Recessed front',(0,-.35,.7),(.78,.05,.95),'Dark')
    for z in [.35,.5,.65,.8,1]:box('Cooling grille',(0,-.39,z),(.65,.03,.035),'Trim',.005)
    for x in [-.28,.28]:box('Status lamp',(x,-.4,1.12),(.1,.03,.04),'Glow',.003)
    if kind==2 or kind==3:
        for x in [-.5,.5]:box('Clean outer edge',(x,0,.65),(.07,.72,1.3),'Trim')
    elif kind==4:
        cylinder('Feed pipe',(.65,0,.85),.09,1.7,'Trim')
    elif kind==0:
        box('Advertising topper',(0,0,1.65),(.9,.4,.55),'Glow')
    export(f'{kind}_Service')
    begin();cylinder('Fan housing',(0,0,.4),.68,.7,'Shell',vertices=24)
    cylinder('Inset well',(0,0,.77),.58,.06,'Dark',vertices=24)
    start=len(parts)
    cylinder('Rotor hub',(0,0,.82),.12,.12,'Glow')
    for i in range(5):
        a=i*math.pi*2/5
        blade=box('Rotor blade',(math.cos(a)*.3,math.sin(a)*.3,.82),(.48,.14,.045),'Trim',.01)
        blade.rotation_euler[2]=a+.4
    moving=parts[start:]
    for x in [-.55,.55]:box('Fan foot',(x,0,.08),(.14,.9,.16),'Trim')
    export(f'{kind}_Vent',moving)

for biome in range(6):
    wall(biome);floor(biome);props(biome)
(OUT/'manifest.json').write_text(json.dumps(dict(version=1,units='metres',source_up='Z',unity_up='Y',wall_core=[4,.28,3],floor=[4,4,.2],assets=report),indent=2))
print('BIOME_EXPORT_COMPLETE',len(report),'modules',sum(row['triangles'] for row in report),'triangles')
