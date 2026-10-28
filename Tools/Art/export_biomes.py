"""Original modular biome meshes. Blender 4.5, metres, Z up -> Unity Y up.
Run: blender -b -t 4 --python Tools/Art/export_biomes.py
All sources are generated here; no external/paid asset service is used.
"""
import bpy
import math
import json
import os
import time
import sys
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
    # Publish atomically; Windows indexers may briefly hold a previously exported FBX.
    temporary=OUT/(name+'.exporting.fbx')
    bpy.ops.export_scene.fbx(filepath=str(temporary),use_selection=True,object_types={'MESH'},
        bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False)
    for attempt in range(20):
        try:
            os.replace(temporary,OUT/(name+'.fbx'));break
        except OSError:
            if attempt==19:raise
            time.sleep(.15)
    points=[obj.matrix_world@Vector(corner) for obj in joined for corner in obj.bound_box]
    low=[min(p[i] for p in points) for i in range(3)];high=[max(p[i] for p in points) for i in range(3)]
    tris=sum(len(poly.vertices)-2 for obj in joined for poly in obj.data.polygons)
    report.append(dict(asset=name,triangles=tris,source_bounds=[low,high],moving_node='Rotor' if moving else None))

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
def label(words, p, size=.22, material='Glow'):
    curve=bpy.data.curves.new('Sign typography','FONT');curve.body=words;curve.size=size
    curve.align_x='CENTER';curve.extrude=.002
    obj=bpy.data.objects.new('Lettering',curve);bpy.context.collection.objects.link(obj)
    obj.location=p;obj.rotation_euler=(math.pi/2,0,0)
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.object.convert(target='MESH');obj=bpy.context.object;obj.data.materials.append(MATS[material]);parts.append(obj);return obj

def beam(a,b,width=.1,material='Trim'):
    delta=Vector(b)-Vector(a);obj=box('Structural brace',(Vector(a)+Vector(b))/2,(width,width,delta.length),material,.012)
    obj.rotation_euler=delta.to_track_quat('Z','Y').to_euler();return obj

def ring(p, radius=.3, material='Trim', axis='Y'):
    bpy.ops.mesh.primitive_torus_add(major_segments=20,minor_segments=6,location=p,major_radius=radius,minor_radius=.045)
    obj=bpy.context.object;obj.name='Valve wheel' if axis=='Y' else 'Seal ring'
    if axis=='Y':obj.rotation_euler[0]=math.pi/2
    obj.data.materials.append(MATS[material]);parts.append(obj)

def wall(kind, variant=0):
    begin()
    # Same 4 m socket, six different constructions, two compositions each.
    if kind==0:
        box('Masonry parapet',(0,.08,.25),(4,.32,.5),'Shell')
        for x in [-1.85,1.85]:box('Brick pier',(x,0,1.5),(.3,.5,3),'Shell')
        box('Shop lintel',(0,0,2.78),(4,.5,.44),'Trim')
        box('Recessed storefront',(0,.06,1.5),(3.4,.12,2.1),'Dark')
        if variant==0:
            for z in range(13):box('Roller shutter',(0,-.08,.5+z*.16),(3.35,.12,.085),'Trim',.009)
            canopy=box('Fabric shop awning',(0,-.48,2.46),(3.6,1.1,.12),'Shell');canopy.rotation_euler[0]=-.13
            box('Shop sign',(0,-.36,2.82),(2.8,.14,.32),'Dark');label('NOODLE / 24H',(0,-.45,2.72),.23)
        else:
            for x in [-1.1,0,1.1]:box('Store window',(x,-.07,1.45),(1.02,.05,1.7),'Glass')
            for x in [-1.65,-.55,.55,1.65]:box('Window mullion',(x,-.12,1.5),(.06,.12,2),'Trim')
            box('Blade sign',(-1.5,-.4,2.1),(.38,.38,1.3),'Glow')
            label('OPEN',(.4,-.15,1.5),.3)
    elif kind==1:
        box('Broken concrete plinth',(0,.07,.3),(4,.4,.6),'Shell')
        for x in [-1.8,1.8]:
            box('I beam web',(x,0,1.5),(.12,.48,3),'Trim')
            for y in [-.22,.22]:box('I beam flange',(x,y,1.5),(.35,.09,3),'Trim')
        box('Industrial lintel',(0,0,2.87),(4,.45,.26),'Trim')
        for x in [-1.2,0,1.2]:box('Corrugated backing',(x,.16,1.6),(1.14,.1,2.3),'Shell')
        beam((-1.7,-.12,.65),(1.7,-.12,2.7),.17)
        if variant==0:
            for z in [1,1.35]:cylinder('Pressure main',(0,-.36,z),.13,3.8,'Trim','X')
            for x in [-1.2,1.2]:cylinder('Pipe union',(x,-.36,1),.2,.18,'Shell','X')
        else:
            for x in [-.95,.95]:
                box('Broken window',(x,-.02,2),(1.4,.08,.8),'Glass')
                beam((x-.6,-.12,1.65),(x+.6,-.12,2.35),.07)
        label('CAUTION  /  09',(0,-.22,.35),.21)
    elif kind==2:
        box('Polished stone base',(0,0,.35),(4,.32,.7),'Shell')
        for x in [-1.95,1.95]:box('Slim aluminium mullion',(x,0,1.5),(.1,.2,3),'Trim')
        box('Ceiling reveal',(0,0,2.94),(4,.28,.12),'Shell')
        if variant==0:
            for x in [-1.3,0,1.3]:box('Full height tinted glass',(x,0,1.8),(1.22,.055,2.2),'Glass',.008)
            for x in [-.65,.65]:box('Slim mullion',(x,-.02,1.8),(.045,.1,2.2),'Trim',.005)
            for z in [1.18,1.3]:box('Frosted privacy band',(0,-.04,z),(3.8,.02,.04),'Shell',0)
        else:
            box('Oak feature panel',(0,0,1.8),(3.8,.16,2.2),'Trim')
            for x in range(19):box('Acoustic slat',(-1.8+x*.2,-.15,1.8),(.065,.15,2.2),'Shell',.009)
            box('Company plaque',(0,-.25,1.85),(2.1,.1,.65),'Dark');label('AEON / SYSTEMS',(0,-.32,1.78),.19)
        box('Warm reveal',(0,-.12,.77),(3.8,.03,.035),'Glow',.006)
    elif kind==3:
        # Thick chamfered containment panels with circular observation ports.
        for x in [-1,1]:box('Sealed containment panel',(x,.05,1.5),(1.98,.38,3),'Shell',.18)
        for x in [-1.87,1.87]:box('Rubber gasket',(x,-.17,1.5),(.1,.1,2.65),'Dark')
        if variant==0:
            for x in [-.85,.85]:
                cylinder('Observation port',(x,-.2,1.7),.54,.07,'Dark','Y',32)
                cylinder('Observation glass',(x,-.25,1.7),.44,.025,'Glass','Y',32)
                ring((x,-.3,1.7),.48,'Trim')
            label('BIO / CONTAINMENT',(0,-.2,.65),.18,'Trim')
        else:
            box('Recessed surgical cabinet',(0,-.22,1.6),(2.7,.17,1.7),'Dark')
            for x in [-.9,0,.9]:
                cylinder('Wall specimen',(x,-.4,1.6),.18,1.1,'Glass')
                for z in [1.05,2.15]:cylinder('Specimen cap',(x,-.4,z),.22,.1,'Trim')
        for z in [.22,2.78]:box('Decontamination light',(0,-.2,z),(3.5,.06,.065),'Glow')
    elif kind==4:
        # Segmented tunnel lining; angled shoulders change the wall silhouette.
        box('Tunnel retaining base',(0,.16,.6),(4,.6,1.2),'Shell')
        box('Recessed concrete',(0,.26,1.85),(4,.32,2.3),'Shell')
        for x in [-1.85,1.85]:
            beam((x,-.18,0),(x,-.18,2),.2,'Shell')
            beam((x,-.18,2),(x,.28,3),.2,'Shell')
        for z in [.45,1.1,2.35] if variant==0 else [.55,2.35]:
            cylinder('Sewer main',(0,-.38,z),.17 if z<1 else .12,4,'Trim','X')
            for x in [-1.3,1.3]:cylinder('Bolted flange',(x,-.38,z),.23,.12,'Shell','X')
        if variant:
            box('Maintenance hatch',(0,-.15,1.4),(1.5,.16,1.7),'Trim')
            for z in [.7,1.05,1.4,1.75,2.1]:box('Ladder rung',(.9,-.45,z),(.4,.12,.06),'Shell')
        label('DRAIN / 07',(0,-.28,2.72),.22,'Trim')
    else:
        box('Platform retaining wall',(0,.1,.35),(4,.4,.7),'Shell')
        for x in [-1.8,1.8]:
            box('Station column',(x,0,1.5),(.38,.5,3),'Shell',.08)
            box('Column steel foot',(x,-.05,.35),(.43,.55,.7),'Trim')
        box('Station header',(0,0,2.8),(4,.48,.4),'Shell')
        if variant==0:
            for x in [-.9,.9]:box('Platform screen door',(x,0,1.7),(1.7,.07,2),'Glass')
            for x in [-1.65,0,1.65]:box('Door stile',(x,-.08,1.65),(.075,.1,2.1),'Trim')
            label('STAND CLEAR',(0,-.15,1.1),.22,'Shell')
        else:
            box('Route map panel',(0,-.16,1.7),(3,.15,1.45),'Dark')
            box('Metro line',(0,-.26,1.65),(2.5,.03,.035),'Glow')
            for x in [-1.1,-.55,0,.55,1.1]:cylinder('Station node',(x,-.3,1.65),.075,.025,'Shell','Y')
            label('03 / CENTRAL',(0,-.27,2),.25)
        box('Platform yellow line',(0,-.28,.73),(3.5,.08,.08),'Glow')
    if kind==0:
        box('Upper building facade',(0,.12,3.6),(4,.32,1.2),'Shell')
        for x in [-1.2,0,1.2]:
            box('Upper apartment window',(x,-.08,3.6),(.8,.08,.72),'Dark')
            box('Window sill',(x,-.18,3.2),(.95,.26,.08),'Trim')
        box('Roof coping',(0,.05,4.16),(4,.5,.08),'Trim')
    elif kind==1:
        for x in [-1.8,1.8]:box('High steel column',(x,0,3.6),(.3,.45,1.2),'Trim')
        box('Roof truss chord',(0,0,4.1),(4,.4,.2),'Trim')
        for x in [-1.4,-.45,.45,1.4]:beam((x-.4,0,3),(x+.4,0,4),.09)
    elif kind==4:
        for x in [-1.85,1.85]:beam((x,.28,3),(x,.5,3.6),.2,'Shell')
        cylinder('Upper trunk line',(0,.32,3.3),.15,4,'Trim','X')
    elif kind==5:
        for x in [-1.8,1.8]:box('Concourse column extension',(x,0,3.55),(.38,.5,1.1),'Shell')
        box('Concourse beam',(0,0,4.1),(4,.5,.2),'Trim')
        box('Departure display',(0,-.18,3.5),(2.7,.16,.55),'Dark')
        label('03 CENTRAL / 2 MIN',(0,-.28,3.43),.18)
    export(f'{kind}_Wall'+('B' if variant else ''))

def floor(kind):
    begin();box('Foundation',(0,0,-.10),(4,4,.2),'Shell',.012)
    if kind==0:
        # Asphalt with a curbside storm grate and offset road paint.
        for i in range(9):box('Storm grate',(-1.82,-1.6+i*.4,.008),(.22,.09,.015),'Trim',0)
        # Street markings belong to a room, not to every repeated floor tile.
    elif kind==1:
        for y in [-1.82,1.82]:box('Embedded rail',(0,y,.005),(4,.08,.01),'Trim',0)
    elif kind==2:
        for y in [-1.97,1.97]:box('Brass stone joint',(0,y,.004),(4,.025,.008),'Trim',0)
    elif kind==3:
        for x in [-1.6,1.6]:
            for y in [-1.6,1.6]:cylinder('Flush seal',(x,y,.004),.045,.008,'Trim',vertices=8)
    elif kind==4:
        for y in [-1.75,1.75]:
            box('Drain channel',(0,y,.005),(3.9,.32,.01),'Dark',0)
            for i in range(17):box('Drain grate',(-1.9+i*.235,y,.012),(.08,.32,.015),'Trim',0)
    else:
        for y in [-1.97,1.97]:box('Platform slab joint',(0,y,.004),(4,.03,.008),'Trim',0)
    export(f'{kind}_Floor')

def room_surface(kind):
    # Twelve-metre room overlay: shallow, walkable visuals. The authoritative
    # floor stays planar; these never add colliders or change traversal heights.
    begin()
    if kind==0:
        for x in [-4.7,4.7]:
            box('Broad pavement',(x,0,.025),(1.4,11,.05),'Shell',.015)
            for y in range(-5,6):box('Paving joint',(x,y,.052),(1.35,.025,.008),'Trim',0)
        for y in [-4,-2,0,2,4]:box('Dashed street centre',(0,y,.007),(.1,.9,.014),'Shell',0)
        for x in [-3,-2,-1,0,1,2,3]:box('Pedestrian crossing',(x,-3.5,.008),(.5,1,.016),'Shell',0)
    elif kind==1:
        box('Machine cell inset',(3.3,3.3,.004),(4.4,4.4,.008),'Dark',0)
        for x in [1.1,5.5]:box('Work cell perimeter',(x,3.3,.012),(.09,4.4,.02),'Glow',0)
        for y in [1.1,5.5]:box('Work cell perimeter',(3.3,y,.012),(4.4,.09,.02),'Glow',0)
        for y in [-5,5]:
            for x in range(-5,6):
                stripe=box('Loading bay chevron',(x,y,.012),(.4,.7,.02),'Glow',0);stripe.rotation_euler[2]=-.5
        label('ASSEMBLY / 09',(-2.5,-5.2,.02),.35,'Shell').rotation_euler=(0,0,0)
    elif kind==2:
        box('Office carpet island',(3.4,3.3,.006),(4.2,4.4,.012),'Trim',.03)
        for x in [1.3,5.5]:box('Carpet border',(x,3.3,.014),(.035,4.4,.01),'Shell',0)
        box('Lobby runner',(-3.5,0,.008),(1.4,10,.016),'Trim',.02)
    elif kind==3:
        for x in [1.3,5.5]:box('Sterile bay outline',(x,3.4,.012),(.06,4.2,.02),'Glow',0)
        for y in [1.3,5.5]:box('Sterile bay outline',(3.4,y,.012),(4.2,.06,.02),'Glow',0)
        for y in range(-5,6):box('Clinical lane indicator',(-2,y,.012),(.08,.35,.02),'Trim',0)
        box('Medical cross',(-3.5,2,.012),(.8,2,.02),'Trim',0)
        box('Medical cross',(-3.5,2,.014),(2,.8,.02),'Trim',0)
    elif kind==4:
        for x in [-4.3,4.3]:
            box('Recessed water channel',(x,0,.01),(.65,11,.02),'Glass',0)
            for y in range(-5,6):box('Channel cross-grate',(x,y,.035),(.8,.12,.03),'Trim',0)
        for y in [-4,4]:box('Service walkway',(0,y,.025),(6,1,.05),'Shell',0)
    else:
        for x in [-4.2,4.2]:
            box('Platform edge',(x,0,.012),(.32,11,.02),'Glow',0)
            for y in range(-5,6):box('Tactile ribs',(x,y,.027),(.28,.07,.03),'Trim',0)
        for x in [2.1,5.5]:box('Train rail',(x,3.8,.024),(.09,3.8,.04),'Trim',0)
        for y in [2.2,2.8,3.4,4,4.6,5.2]:box('Track sleeper',(3.8,y,.01),(3.6,.18,.02),'Dark',0)
        label('CENTRAL / 03',(-3.2,-4.7,.02),.4,'Trim').rotation_euler=(0,0,0)
    # The FBX importer's XY half-turn maps this room blueprint to Unity's
    # +X/+Z furnished corner. Rotate the complete geometry, including glyphs.
    for obj in parts:
        obj.location.x=-obj.location.x;obj.location.y=-obj.location.y
        obj.rotation_euler.z+=math.pi
    export(f'{kind}_Room')

def furniture(kind,role):
    begin()
    if kind==0:
        if role=='Landmark':
            box('Street food counter',(0,0,.6),(2.8,1.1,1.2),'Shell')
            box('Stainless worktop',(0,-.05,1.25),(3,1.25,.12),'Trim')
            for x in [-1.35,1.35]:box('Kiosk upright',(x,.3,1.9),(.1,.1,1.4),'Trim')
            box('Kiosk canopy',(0,.15,2.55),(3.15,1.5,.18),'Shell')
            box('Menu board',(0,.45,1.9),(2.2,.1,.65),'Dark');label('RAMEN / SYNTH',(0,.38,1.86),.22)
            for x in [-.7,0,.7]:cylinder('Cooking pot',(x,0,1.4),.18,.22,'Trim')
        elif role=='Service':
            box('Refuse dumpster',(0,0,.65),(1.65,.95,1.15),'Trim',.08)
            lid=box('Hinged lid',(0,0,1.28),(1.78,1.05,.12),'Shell');lid.rotation_euler[0]=.1
            for x in [-.65,.65]:cylinder('Caster',(x,0,.12),.12,.15,'Dark','X')
            label('RECLAIM',(0,-.49,.65),.18)
        else:
            cylinder('Street bollard',(0,0,.55),.13,1.1,'Trim')
            cylinder('Safety band',(0,0,.85),.15,.15,'Glow')
            for x in [-.45,.4]:
                bag=box('Waste bag',(x,0,.25),(.42,.42,.5),'Dark',.16);bag.rotation_euler[2]=x
    elif kind==1:
        if role=='Landmark':
            for x in [-1.3,1.3]:box('Press upright',(x,0,1.4),(.45,1.2,2.8),'Trim')
            box('Press crosshead',(0,0,2.55),(2.7,1.2,.45),'Shell')
            cylinder('Hydraulic ram',(0,0,1.9),.3,.9,'Trim')
            box('Press die',(0,0,1.35),(1.7,.95,.25),'Shell');box('Machining bed',(0,0,.55),(2.8,1.3,.6),'Trim')
            label('HYDRA / 08',(0,-.62,2.5),.22)
        elif role=='Service':
            box('Conveyor frame',(0,0,.65),(1.8,1.1,.25),'Shell')
            for x in [-.72,.72]:
                for y in [-.4,.4]:box('Conveyor foot',(x,y,.3),(.1,.1,.6),'Trim')
            for x in range(8):cylinder('Steel roller',(-.77+x*.22,0,.82),.075,.98,'Trim','Y')
        else:
            for x in [-.4,0,.4]:
                for z in [.13,.4]:cylinder('Cut pipe stack',(x,0,z),.13,1.1,'Trim','Y')
            box('Scrap pallet',(0,0,.06),(1.2,1.3,.12),'Shell')
    elif kind==2:
        if role=='Landmark':
            box('Reception island',(0,0,.6),(3,1.2,1.2),'Shell',.16)
            box('Black counter',(0,0,1.24),(3.15,1.35,.12),'Dark',.08)
            box('Front light',(0,-.63,.7),(2.6,.03,.035),'Glow')
            label('AEON',(0,-.65,.87),.27,'Trim')
            box('Reception screen',(.7,.15,1.57),(.8,.08,.5),'Glass')
        elif role=='Service':
            box('Upholstered chair',(0,0,.55),(.9,.85,.22),'Trim',.12)
            box('Chair back',(0,.3,.9),(.9,.22,.7),'Shell',.1)
            for x in [-.37,.37]:box('Armrest',(x,0,.78),(.12,.8,.15),'Shell')
            cylinder('Chair pedestal',(0,0,.25),.07,.5,'Trim');cylinder('Chair foot',(0,0,.04),.42,.08,'Trim')
        else:
            box('Planter',(0,0,.35),(.7,.7,.7),'Shell',.1)
            for i in range(7):
                a=i*2.4;beam((0,0,.5),(.35*math.cos(a),.35*math.sin(a),1.2+i*.08),.08,'Glass')
                leaf=box('Broad leaf',(.25*math.cos(a),.25*math.sin(a),1+i*.08),(.12,.45,.06),'Glass',.035);leaf.rotation_euler[2]=a
    elif kind==3:
        if role=='Landmark':
            box('Isolation bed base',(0,0,.45),(2.8,1.1,.9),'Shell',.2)
            box('Patient cradle',(0,0,.98),(2.5,.85,.2),'Trim',.12)
            for x in [-1.2,1.2]:
                ring((x,0,1.25),.55,'Shell','Z')
                for y in [-.5,.5]:box('Scanner support',(x,y,1.15),(.15,.12,.7),'Shell')
            beam((-.6,.35,1.2),(-.6,.35,2.2),.1);beam((-.6,.35,2.2),(.2,0,2.2),.1)
            cylinder('Surgical lamp',(.2,0,2.15),.25,.1,'Glow')
        elif role=='Service':
            box('Medical trolley',(0,0,.55),(1.2,.65,.12),'Shell')
            for x in [-.5,.5]:
                for y in [-.25,.25]:cylinder('Trolley post',(x,y,.4),.04,.8,'Trim')
            box('Lower shelf',(0,0,.22),(1.1,.6,.08),'Trim')
            for x in [-.3,0,.3]:cylinder('Sample jar',(x,0,.78),.09,.32,'Glass')
        else:
            for i in range(5):
                a=i*2.4;cylinder('Contaminant nodule',(.26*math.cos(a),.26*math.sin(a),.13+i*.045),.17,.25,'Glass',vertices=10)
                beam((.2*math.cos(a),.2*math.sin(a),.15),(.5*math.cos(a),.5*math.sin(a),.6),.07,'Glass')
    elif kind==4:
        if role=='Landmark':
            for x in [-1,1]:
                cylinder('Filter tank',(x,0,1.05),.48,2.1,'Shell',vertices=20)
                for z in [.25,1.8]:cylinder('Tank hoop',(x,0,z),.52,.1,'Trim')
            cylinder('Cross feed',(0,0,1.7),.13,2,'Trim','X')
            cylinder('Valve stem',(0,-.3,1.7),.08,.6,'Trim','Y');ring((0,-.65,1.7),.3,'Glow')
            for a in range(4):
                angle=a*math.pi/2;beam((0,-.65,1.7),(.3*math.cos(angle),-.65,1.7+.3*math.sin(angle)),.045,'Trim')
        elif role=='Service':
            box('Electrical junction',(0,0,.75),(1.1,.65,1.5),'Trim')
            for z in [.6,1]:box('Fuse bank',(0,-.36,z),(.8,.15,.22),'Shell')
            for x in [-.4,0,.4]:cylinder('Cable inlet',(x,0,1.7),.06,.45,'Dark')
            label('HIGH VOLT',(0,-.46,1.27),.16)
        else:
            box('Maintenance toolbox',(0,0,.23),(.9,.6,.46),'Shell')
            box('Tool handle',(0,0,.54),(.35,.12,.12),'Trim')
            ring((.65,0,.06),.27,'Dark','Z')
    else:
        if role=='Landmark':
            # Truncated parked metro carriage, contained in a 3.2 x 1.7 m envelope.
            box('Carriage body',(0,0,1.25),(3.2,1.5,2.5),'Shell',.22)
            box('Metro roof',(0,0,2.6),(3.2,1.55,.2),'Trim',.08)
            for x in [-1.05,1.05]:box('Train window',(x,-.78,1.75),(.72,.05,.8),'Glass',.08)
            box('Train door',(0,-.79,1.3),(.9,.04,2),'Trim')
            box('Door glazing',(0,-.82,1.75),(.62,.025,.7),'Glass')
            box('Train stripe',(0,-.81,.55),(3.1,.025,.14),'Glow');label('M / 03',(-1.03,-.83,.8),.18)
        elif role=='Service':
            for x in [-.6,0,.6]:
                box('Seat shell',(x,0,.55),(.52,.65,.12),'Shell',.09)
                back=box('Seat back',(x,.25,.9),(.52,.12,.6),'Shell',.07);back.rotation_euler[0]=-.14
            box('Bench rail',(0,0,.35),(1.8,.15,.14),'Trim')
            for x in [-.65,.65]:box('Bench support',(x,0,.16),(.15,.5,.32),'Trim')
        else:
            for x,z in [(-.28,.45),(.32,.32)]:
                case=box('Travel luggage',(x,0,z),(.48,.4,z*2),'Shell',.08);case.rotation_euler[2]=x
                box('Suitcase handle',(x,0,z*2+.12),(.2,.05,.24),'Trim')
    export(f'{kind}_{role}')

def animated(kind):
    begin()
    # Rotor is a shared motion socket, but the object around it is biome-specific.
    if kind==0:
        box('Holo projector',(0,0,.4),(.65,.65,.8),'Trim')
        start=len(parts);box('Rotating ad',(0,0,1.45),(.9,.09,1.1),'Glass');label('24 H',(0,-.06,1.45),.23)
    elif kind==1:
        for x in [-.55,.55]:box('Motor bracket',(x,0,.5),(.16,.8,1),'Shell')
        start=len(parts);cylinder('Flywheel',(0,0,.65),.56,.18,'Trim')
        for i in range(6):
            a=i*math.pi/3;box('Wheel hub marker',(.4*math.cos(a),.4*math.sin(a),.76),(.13,.13,.03),'Glow')
    elif kind==2:
        cylinder('Exhibit plinth',(0,0,.4),.48,.8,'Shell')
        start=len(parts)
        for i in range(3):
            block=box('Kinetic sculpture',(0,0,1+i*.3),(.6,.45,.15),'Trim');block.rotation_euler[2]=i*.6
    elif kind==3:
        cylinder('Centrifuge housing',(0,0,.4),.55,.8,'Shell')
        start=len(parts);cylinder('Sample carousel',(0,0,.83),.48,.08,'Trim')
        for i in range(8):
            a=i*math.pi/4;cylinder('Sample tube',(.35*math.cos(a),.35*math.sin(a),.96),.055,.2,'Glow')
    elif kind==4:
        cylinder('Extractor housing',(0,0,.4),.6,.8,'Shell')
        start=len(parts)
        for i in range(5):
            a=i*math.pi*2/5;blade=box('Impeller',(.28*math.cos(a),.28*math.sin(a),.83),(.5,.12,.045),'Trim');blade.rotation_euler[2]=a+.3
    else:
        box('Signal base',(0,0,.4),(.55,.55,.8),'Trim')
        box('Signal pole',(0,0,1.05),(.12,.12,1.3),'Shell')
        start=len(parts);box('Rotating signal',(0,0,1.75),(.7,.12,.36),'Glow');label('03',(0,-.08,1.66),.22,'Dark')
    export(f'{kind}_Vent',parts[start:])

def hazards(kind):
    begin()
    if kind in [0,2,5]:
        box('Power module',(0,0,.7),(.72,.68,1.4),'Shell',.09)
        for x in [-.29,.29]:box('Cell guard',(x,-.34,.7),(.08,.08,1.3),'Trim')
        for z in [.35,.7,1.05]:box('Charge indicator',(0,-.36,z),(.4,.03,.07),'Glow')
        label(['CELL','HYDRA','UPS','CRYO','GAS','TRACTION'][kind],(0,-.37,1.22),.12,'Trim')
    else:
        count=2 if kind==3 else 1
        for i in range(count):
            x=(i-.5)*.35 if count==2 else 0
            cylinder('Pressure bottle',(x,0,.65),.17 if count==2 else .35,1.2,'Shell',vertices=20)
            cylinder('Bottle collar',(x,0,1.25),.12,.12,'Trim')
            ring((x,0,.3),.18 if count==2 else .35,'Trim','Z')
        cylinder('Pressure warning',(0,0,.8),.37,.12,'Glow') if count==1 else None
    export(f'{kind}_Volatile')
    begin()
    box('Unstable partition',(0,0,1.2),(2.4,.16,2.4),'Shell',.03)
    if kind==0:
        for z in range(10):box('Shop shutter rib',(0,-.11,.15+z*.23),(2.35,.06,.07),'Trim',.008)
        label('DANGER',(0,-.16,1.8),.25)
    elif kind==1:
        beam((-1.1,-.11,.1),(1.1,-.11,2.3),.12)
        beam((1.1,-.11,.1),(-1.1,-.11,2.3),.12)
    elif kind==2:
        box('Office partition glass',(0,-.11,1.4),(2.1,.03,1.7),'Glass')
        for z in [.8,1.1]:box('Privacy stripe',(0,-.14,z),(2.1,.02,.04),'Trim',0)
    elif kind==3:
        for x in [-1,1]:box('Containment seal',(x,-.12,1.2),(.09,.06,2.1),'Dark')
        cylinder('Containment port',(0,-.12,1.5),.6,.04,'Glass','Y')
        label('ISOLATE',(0,-.15,.5),.22,'Trim')
    elif kind==4:
        for z in [.5,1.1,1.7]:box('Maintenance louvre',(0,-.11,z),(2.1,.06,.26),'Trim')
    else:
        box('Station barrier window',(0,-.11,1.5),(2.1,.03,1.3),'Glass')
        label('PLATFORM 03',(0,-.14,.5),.2)
    export(f'{kind}_Unstable')

surfaces_only='--room-surfaces-only' in sys.argv
if surfaces_only:
    report=[row for row in json.loads((OUT/'manifest.json').read_text())['assets'] if not row['asset'].endswith('_Room')]
for biome in range(6):
    if not surfaces_only:
        props(biome)
        wall(biome);wall(biome,1);floor(biome)
        for role in ['Service','Landmark','Detail']:furniture(biome,role)
        animated(biome);hazards(biome)
    room_surface(biome)
(OUT/'manifest.json').write_text(json.dumps(dict(version=2,units='metres',source_up='Z',unity_up='Y',wall_socket=[4,3],floor=[4,4,.2],assets=report),indent=2))
print('BIOME_EXPORT_COMPLETE',len(report),'modules',sum(row['triangles'] for row in report),'triangles')
