"""Six editable modular environment studies. Review-only; never writes Unity Assets.

Blender 4.5.9: --background --factory-startup --python-exit-code 1 --python this.py
Use -- --biome factory to rebuild a single kit. Dimensions are inferred metres.
"""
import bpy, math, json, random, sys, time
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Art/ProductionReview/environments'
NAMES=['neon','factory','corporate','biotech','utilities','transit']
COLORS=[(.015,.40,.48),(.65,.22,.025),(.06,.34,.42),(.025,.50,.32),(.50,.21,.03),(.24,.10,.50)]
PARTS=[]; KITS={}; ACTIVE=None

def material(name,color,metal=0,rough=.6,wear=False,glow=0,glass=False):
    m=bpy.data.materials.new(name);m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    if glow:
        p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=glow
    if glass:
        p.inputs['Transmission Weight'].default_value=.65;p.inputs['IOR'].default_value=1.45
    if wear:
        ns=m.node_tree.nodes;ln=m.node_tree.links
        tex=ns.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=42;tex.inputs['Detail'].default_value=4
        bump=ns.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.18;bump.inputs['Distance'].default_value=.003
        ln.new(tex.outputs['Fac'],bump.inputs['Height']);ln.new(bump.outputs[0],p.inputs['Normal'])
        ramp=ns.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(*[x*.48 for x in color],1);ramp.color_ramp.elements[1].color=(*color,1)
        ln.new(tex.outputs['Fac'],ramp.inputs[0]);ln.new(ramp.outputs[0],p.inputs['Base Color'])
    return m

def finish(o,name,mat,bevel=.018):
    o.name=name
    for c in list(o.users_collection):c.objects.unlink(o)
    ACTIVE.objects.link(o);o.data.materials.append(mat);PARTS.append(o)
    if o.type=='MESH' and bevel:
        b=o.modifiers.new('Real edge radius','BEVEL');b.width=bevel;b.segments=3
        n=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');n.keep_sharp=True
    return o

def box(name,p,d,mat,bevel=.018):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.dimensions=d
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    uv=o.data.uv_layers.active
    for polygon in o.data.polygons:
        axis=max(range(3),key=lambda i:abs(polygon.normal[i]));axes=[i for i in range(3) if i!=axis]
        for idx in polygon.loop_indices:
            co=o.data.vertices[o.data.loops[idx].vertex_index].co;uv.data[idx].uv=(co[axes[0]]*.5,co[axes[1]]*.5)
    return finish(o,name,mat,bevel)

def surface(ident,metal=0):
    folder=ROOT/'.tools/gamedev/biome-surfaces'/ident;provenance=json.loads((folder/'provenance.json').read_text())
    m=material('PH_'+ident,(.2,.2,.2),metal,.6);p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');ns=m.node_tree.nodes;ln=m.node_tree.links
    for role,entry in provenance['files'].items():
        im=bpy.data.images.load(str(folder/entry['file']),check_existing=True)
        if role!='color':im.colorspace_settings.name='Non-Color'
        tex=ns.new('ShaderNodeTexImage');tex.image=im
        if role=='color':ln.new(tex.outputs['Color'],p.inputs['Base Color'])
        elif role=='roughness':ln.new(tex.outputs['Color'],p.inputs['Roughness'])
        else:
            nm=ns.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.65;ln.new(tex.outputs['Color'],nm.inputs['Color']);ln.new(nm.outputs[0],p.inputs['Normal'])
    return m

def pipe(name,pts,r,mat):
    d=bpy.data.curves.new(name,'CURVE');d.dimensions='3D';d.bevel_depth=r;d.bevel_resolution=3
    sp=d.splines.new('POLY');sp.points.add(len(pts)-1)
    for p,co in zip(sp.points,pts):p.co=(*co,1)
    return finish(bpy.data.objects.new(name,d),name,mat,0)

def tube(name,a,b,r,mat,r2=None):
    a,b=Vector(a),Vector(b);v=b-a
    bpy.ops.mesh.primitive_cone_add(vertices=32,radius1=r,radius2=r if r2 is None else r2,depth=v.length,location=(a+b)/2)
    o=bpy.context.object;o.rotation_euler=v.to_track_quat('Z','Y').to_euler()
    for f in o.data.polygons:f.use_smooth=True
    return finish(o,name,mat,.006)

def beam(name,a,b,w,d,mat):
    a,b=Vector(a),Vector(b);o=box(name,(a+b)/2,(w,d,(b-a).length),mat,.008)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o

def text(words,p,size,mat):
    d=bpy.data.curves.new('Wayfinding','FONT');d.body=words;d.size=size;d.extrude=.001;d.align_x='CENTER'
    o=bpy.data.objects.new('TXT_'+words,d);o.location=p;o.rotation_euler=(math.pi/2,0,0)
    return finish(o,o.name,mat,0)

def module(name,fn):
    global ACTIVE
    ACTIVE=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(ACTIVE)
    fn();KITS[name]=ACTIVE
    ACTIVE.hide_render=True;ACTIVE.hide_viewport=True

def instance(name,p,angle=0):
    o=bpy.data.objects.new(name,None);bpy.context.scene.collection.objects.link(o)
    o.instance_type='COLLECTION';o.instance_collection=KITS[name];o.location=p;o.rotation_euler.z=angle
    return o

def build(kind):
    global PARTS,KITS,ACTIVE
    begin=time.perf_counter();biome=NAMES[kind];folder=OUT/biome;folder.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True);PARTS=[];KITS={};scene=bpy.context.scene
    steel=material('Dark structural steel',(.035,.045,.052),.8,.35,True)
    chrome=material('Exposed stainless edges',(.26,.29,.30),.85,.28)
    concrete=material('Cast concrete aggregate',(.18,.17,.15),0,.85,True)
    ceramic=material('Off-white molded ceramic',(.43,.43,.38),.15,.4,kind not in [2,3])
    rubber=material('Cable and seals',(.009,.012,.014),0,.8)
    accent=material('Biome enamel',COLORS[kind],.4,.44,kind in [0,1,4,5])
    glow=material('Practical light',COLORS[kind],.1,.3,glow=3)
    white=material('Warm task light',(.75,.65,.45),0,.35,glow=2)
    glass=material('Tinted glazing',(.035,.10,.12),.1,.17,glass=True)
    ground=material('Ground substrate',[(.045,.052,.06),(.09,.085,.07),(.12,.14,.16),(.16,.18,.17),(.07,.065,.05),(.12,.13,.14)][kind],.25,.65,True)
    floor_finish=surface(['asphalt_02','metal_plate','floor_tiles_02','floor_tiles_02','concrete_floor_damaged_01','concrete_floor_damaged_01'][kind],.6 if kind==1 else 0)
    masonry=surface('brick_wall_001' if kind==0 else 'concrete_wall_003')
    # A 4 m socket is shared, construction and surface rhythm are biome-specific.
    def floor():
        box('Foundation',(0,0,-.13),(4,4,.26),floor_finish if kind==0 else ground)
        if kind==0:
            for x in [-1.75,1.75]:box('Stone pavement',(x,0,.015),(.48,4,.055),concrete)
            for y in [-1.5,-.5,.5,1.5]:box('Worn lane dash',(.1,y,.008),(.08,.55,.012),ceramic,.002)
            for x in [-1.42,1.42]:
                box('Gutter rail',(x,0,.01),(.08,4,.06),steel)
                for y in range(16):box('Drain slots',(x,-1.85+y*.25,.045),(.14,.035,.02),rubber,.002)
            # Thin irregular puddles are real geometry; reflection remains view dependent.
            bpy.ops.mesh.primitive_circle_add(vertices=16,radius=1,fill_type='NGON',location=(.7,.45,.011))
            o=bpy.context.object;o.scale=(.65,1.1,1);finish(o,'Street puddle',glass,0)
        elif kind in [1,4]:
            for x in [-1.5,-.5,.5,1.5]:
                for y in [-1.5,-.5,.5,1.5]:box('Bolted deck plate',(x,y,.015),(.975,.975,.04),floor_finish,.009)
            for y in range(24):box('Drain grate crossbar',(0,-1.9+y*.16,.055),(.42,.035,.03),chrome,.003)
            for x in [-.24,.24]:box('Grate frame',(x,0,.06),(.045,4,.04),accent,.004)
        elif kind==2:
            for x in [-1.5,-.5,.5,1.5]:
                for y in [-1.5,-.5,.5,1.5]:box('Polished stone tile',(x,y,.018),(.985,.985,.045),floor_finish,.005)
            for x in [-1.85,1.85]:box('Metal floor reveal',(x,0,.045),(.025,4,.015),chrome,.002)
        elif kind==3:
            for x in [-1,1]:
                for y in [-1,1]:box('Seamless resin floor',(x,y,.02),(1.98,1.98,.05),ceramic,.035)
            for x in [-1.8,1.8]:box('Containment route strip',(x,0,.052),(.06,4,.012),accent,.002)
        else:
            for x in range(8):
                for y in range(8):box('Concourse tile',(-1.75+x*.5,-1.75+y*.5,.015),(.49,.49,.04),floor_finish,.003)
            for x in [-1.5,1.5]:
                box('Tactile safety strip',(x,0,.04),(.28,4,.04),accent,.005)
                for y in range(32):box('Tactile ridges',(x,-1.94+y*.125,.068),(.23,.035,.018),ceramic,.004)
    module('Floor_4m',floor)

    def wall(variant):
        if kind==0:
            box('Brick facade',(0,.11,1.8),(4,.42,3.6),masonry)
            for x in [-1.85,1.85]:box('Masonry pilaster',(x,-.15,1.8),(.30,.42,3.6),concrete)
            box('Storefront cavity',(0,-.13,1.35),(3.30,.05,2.35),rubber)
            if variant==0:
                for i in range(22):box('Shutter lath',(0,-.20,.28+i*.10),(3.3,.08,.077),steel,.012)
                canopy=box('Rain canopy',(0,-.62,2.60),(3.65,1.2,.10),accent);canopy.rotation_euler.x=-.12
                for x in [-1.55,1.55]:beam('Canopy bracket',(x,-.14,2.25),(x,-1.02,2.58),.035,.035,steel)
            else:
                for x in [-1.1,0,1.1]:box('Shop glazing',(x,-.23,1.4),(1.03,.055,2.0),glass,.006)
                for x in [-1.65,-.55,.55,1.65]:box('Store mullion',(x,-.27,1.4),(.055,.10,2.1),chrome)
            box('Illuminated shop fascia',(0,-.2,2.98),(3.3,.18,.4),rubber)
            text('KOBAN / 24H' if not variant else 'SYNTH / REPAIR',(0,-.303,2.88),.20,glow)
            for x in [-1.05,1.05]:
                box('Upper window inset',(x,-.13,3.4),(1.5,.06,.35),glass)
                pipe('Electrical conduit',[(x+.78,-.25,.1),(x+.78,-.25,3.25),(x,-.25,3.25)],.022,chrome)
        elif kind==1:
            box('Concrete knee wall',(0,.1,.35),(4,.5,.7),masonry)
            for x in [-1.85,1.85]:
                box('I-column web',(x,.05,2),(.10,.45,4),steel)
                for y in [-.18,.28]:box('I-column flange',(x,y,2),(.34,.08,4),steel)
                box('Bolted foot plate',(x,.03,.06),(.5,.6,.1),chrome)
            for x in range(38):box('Corrugated cladding',(-1.7+x*.09,.22,2.1),(.065,.10,2.8),accent,.008)
            beam('Diagonal tension brace',(-1.65,-.03,.7),(1.65,-.03,3.7),.10,.10,steel)
            box('Overhead truss',(0,.05,3.85),(4,.5,.24),steel)
            for z in [1.1,1.55] if not variant else [2.8,3.2]:
                tube('Steam main',(-1.85,-.35,z),(1.85,-.35,z),.12,steel)
                for x in [-1.4,0,1.4]:
                    tube('Flange',(x-.05,-.35,z),(x+.05,-.35,z),.18,chrome)
                    for a in range(8):
                        t=a*math.tau/8;tube('Flange bolt',(x-.065,-.35+math.sin(t)*.15,z+math.cos(t)*.15),(x+.065,-.35+math.sin(t)*.15,z+math.cos(t)*.15),.012,steel)
            text('H-09  /  STEAM',(0,-.17,.30),.20,ceramic)
        elif kind==2:
            for x in [-1.95,0,1.95]:box('Aluminium glazing mullion',(x,0,1.7),(.07,.15,3.4),chrome,.006)
            for x in [-.98,.98]:box('Full-height office glass',(x,.025,1.7),(1.87,.045,3.35),glass,.004)
            for z in [.04,3.37]:box('Glazing rail',(0,0,z),(4,.15,.07),chrome,.006)
            for z in [1.15,1.24]:box('Privacy frit',(0,-.005,z),(3.9,.012,.035),ceramic,.001)
            box('Stone pilaster',(1.58,-.10,1.7),(.35,.35,3.4),ceramic)
            box('Access reader',(1.58,-.30,1.2),(.13,.045,.22),rubber,.015)
            box('Reader indicator',(1.58,-.327,1.24),(.08,.008,.015),glow,.002)
            text('HELIX / 07' if not variant else 'RESEARCH',(0,-.06,2.5),.23,chrome)
        elif kind==3:
            box('Containment shell',(0,.1,1.7),(4,.35,3.4),ceramic,.12)
            for x in [-1.82,1.82]:
                box('Softened rib',(x,-.15,1.7),(.24,.35,3.4),ceramic,.10)
                box('Recessed status strip',(x,-.335,1.9),(.045,.018,1.8),glow,.009)
            box('Observation recess',(0,-.10,1.9),(3.0,.08,1.6),rubber,.10)
            box('Armored observation glass',(0,-.16,1.9),(2.82,.04,1.42),glass,.09)
            for z in [.35,3.1]:box('Sanitary cable chase',(0,-.13,z),(3.5,.2,.2),ceramic,.07)
            for x in [-1.2,-.8,-.4,.0,.4,.8,1.2]:box('HEPA vent',(x,-.22,.63),(.22,.05,.05),steel,.006)
            text('BIO / ISOLATION 03',(0,-.25,3.04),.16,accent)
        elif kind==4:
            box('Stained retaining wall',(0,.16,1.8),(4,.55,3.6),masonry,.06)
            for x in [-1.65,-.9]:
                pipe('Bent service riser',[(x,-.35,.1),(x,-.35,2.9),(x+.18,-.35,3.1),(x+.18,.2,3.1)],.15,accent)
                for z in [.5,1.8,2.7]:tube('Riser collar',(x,-.35,z-.05),(x,-.35,z+.05),.19,steel)
            for z in [1.2,2.15,2.7]:
                tube('Cross-fed corroded main',(-.55,-.36,z),(1.9,-.36,z),.13 if z==1.2 else .07,steel)
            box('Patch sheet',(1.05,-.15,.7),(1.55,.08,1.15),accent,.004)
            for x in [.4,1.7]:
                for z in [.2,1.15]:tube('Patch anchor',(x,-.20,z),(x,-.24,z),.025,chrome)
            box('Electrical meter cabinet',(.5,-.3,2.7),(.75,.3,.65),steel)
            text('LIVE / 480V',(.5,-.463,2.64),.12,white)
        else:
            box('Platform ceramic bulkhead',(0,.05,1.65),(4,.36,3.3),ceramic,.04)
            for x in [-1.85,1.85]:box('Station portal rib',(x,-.15,1.85),(.25,.48,3.7),steel,.08)
            for z in [.4,.5]:box('Station route band',(0,-.145,z),(4,.035,.055),accent,.004)
            box('Map cabinet',(0,-.22,1.85),(2.6,.18,1.5),steel,.04)
            box('Route diagram backing',(0,-.322,1.85),(2.4,.025,1.3),rubber,.02)
            pipe('Route A',[(-1,-.35,1.65),(-.3,-.35,1.65),(.0,-.35,2.1),(1,-.35,2.1)],.018,glow)
            pipe('Route B',[(-1,-.35,2.1),(-.5,-.35,2.1),(.3,-.35,1.6),(1,-.35,1.6)],.018,white)
            text('03 / CENTRAL',(0,-.35,2.9),.25,white)
            for x in [-1.5,1.5]:box('Emergency intercom',(x,-.2,1.4),(.18,.12,.4),accent)
    for v in range(2):module('Wall_'+str(v)+'_4m',lambda v=v:wall(v))
    def corner():
        box('Corner pier',(0,0,1.7),(.48,.48,3.4),[concrete,steel,ceramic,ceramic,concrete,steel][kind],.05)
        for z in [.1,3.3]:box('Pier collar',(0,0,z),(.58,.58,.13),chrome)
    module('Corner_4m',corner)
    def door():
        for x in [-1.425,1.425]:box('Door jamb',(x,0,1.5),(.35,.45,3),steel,.05)
        box('Door lintel',(0,0,3.0),(3.15,.45,.3),steel,.05)
        box('Threshold',(0,0,.035),(2.5,.65,.07),chrome,.008)
        box('Door status',(0,-.25,2.96),(1.7,.025,.035),glow,.004)
    module('Door_4m',door)
    def feature():
        if kind==0:
            box('Food kiosk',(0,0,.65),(1.8,1.05,1.3),steel,.06)
            box('Stainless counter',(0,-.2,1.33),(2.1,1.5,.08),chrome)
            for x in [-.9,.9]:tube('Kiosk post',(x,.35,1.3),(x,.35,2.5),.035,steel)
            box('Kiosk sign',(0,.35,2.5),(2.05,.2,.38),rubber)
            text('RAMEN / 09',(0,.238,2.4),.20,glow)
            for x in [-.6,0,.6]:tube('Stock pot',(x,-.1,1.36),(x,-.1,1.64),.18,chrome)
            for i in range(8):box('Plastic supply crate',(-.65+i%2*.65,.75,.15+i//2*.26),(.58,.48,.24),accent)
        elif kind in [1,4]:
            # Reuse the project's licensed, textured industrial donor rather than primitive machinery.
            path=ROOT/'Assets/Game/Resources/Detailed/RawIndustrial'/('station.fbx' if kind==1 else 'tank_capsule.fbx')
            before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(path));imported=[o for o in bpy.data.objects if o not in before]
            meshes=[o for o in imported if o.type=='MESH'];pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
            lo=Vector(tuple(min(v[i] for v in pts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in pts) for i in range(3)))
            factor=2.5/max(hi-lo);center=(lo+hi)/2;center.z=lo.z
            for o in meshes:
                ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());d=bpy.data.meshes.new_from_object(ev);q=bpy.data.objects.new('CC0_Industrial_'+o.name,d);ACTIVE.objects.link(q)
                for v in d.vertices:v.co=(o.matrix_world@v.co-center)*factor
                PARTS.append(q)
            for o in imported:bpy.data.objects.remove(o,do_unlink=True)
            for x in [-1.5,1.5]:
                pipe('Auxiliary feed',[(x,.3,.1),(x,.3,1.9),(x*.5,.3,2.25)],.075,accent)
            text('PUMP / 14' if kind==4 else 'PRESS / 09',(0,-.7,2.4),.16,white)
        elif kind==2:
            box('Reception desk',(0,0,.54),(2.7,.9,1.08),ceramic,.12)
            box('Floating work surface',(0,-.02,1.1),(2.9,1.1,.08),chrome)
            for x in [-.7,.7]:
                box('Monitor stem',(x,.15,1.27),(.035,.06,.3),chrome)
                box('Monitor surround',(x,.13,1.52),(.65,.06,.42),steel)
                box('Monitor screen',(x,.09,1.52),(.59,.012,.35),glow,.006)
            text('HELIX',(0,-.468,.55),.30,accent)
        elif kind==3:
            for z in [.15,2.2]:tube('Containment ring',(0,0,z-.10),(0,0,z+.10),.68,ceramic)
            tube('Transparent specimen chamber',(0,0,.25),(0,0,2.1),.54,glass)
            for x,y in [(-.52,-.30),(.52,-.30),(-.52,.30),(.52,.30)]:
                tube('Structural spine',(x,y,.2),(x,y,2.2),.045,steel)
                tube('Status light',(x*1.02,y*1.02,.4),(x*1.02,y*1.02,1.9),.012,glow)
            pipe('Specimen branching stem',[(0,0,.3),(.12,0,.9),(-.1,0,1.3),(.05,0,1.9)],.05,accent)
            for i in range(7):
                z=.5+i*.19;pipe('Specimen tendril',[(0,0,z),(.23*(-1)**i,.1,z+.12),(.32*(-1)**i,0,z+.30)],.018,glow)
        else:
            box('Turnstile pedestal',(0,0,.58),(.45,1.1,1.16),steel,.08)
            box('Turnstile touch panel',(0,-.25,1.2),(.32,.4,.10),rubber)
            for s in [-1,1]:
                box('Gate glass',(s*.48,0,.65),(.65,.04,.70),glass,.08)
                box('Gate status',(s*.48,-.03,1.0),(.55,.02,.025),glow,.004)
            box('Departure pylon',(1,0,1.5),(.15,.3,3),steel)
            box('Departure board',(1,-.05,2.5),(1.6,.18,.8),rubber)
            text('NORTH / 02',(1,-.151,2.58),.16,white)
            text('CENTRAL  03',(1,-.151,2.29),.13,glow)
    module('Feature',feature)
    def service():
        if kind==0:
            box('Vending machine',(0,0,1),(.9,.7,2),accent,.07)
            box('Vending display',(0,-.36,1.27),(.65,.04,.95),rubber)
            for x in [-.2,0,.2]:
                for z in [.98,1.23,1.48]:tube('Display drink',(x,-.40,z-.08),(x,-.40,z+.08),.055,chrome)
            box('Collection slot',(0,-.40,.4),(.58,.12,.24),rubber)
            text('DRINK',(0,-.39,1.82),.18,glow)
        elif kind==1:
            for z in [.08,.25]:
                for x in [-.55,0,.55]:box('Stacked pallet stringer',(x,0,z),(.16,1.3,.13),concrete)
            for y in [-.55,-.27,0,.27,.55]:box('Pallet deck',(0,y,.34),(1.5,.20,.06),concrete)
            tube('Cable reel core',(0,-.34,.83),(0,.34,.83),.34,rubber)
            for y in [-.39,.39]:tube('Reel steel flange',(0,y-.035,.83),(0,y+.035,.83),.52,steel)
            for i in range(17):
                y=-.32+i*.04
                bpy.ops.mesh.primitive_torus_add(major_radius=.35,minor_radius=.024,major_segments=32,minor_segments=6,location=(0,y,.83),rotation=(math.pi/2,0,0));finish(bpy.context.object,'Cable winding',rubber,0)
        elif kind==2:
            box('Server cabinet',(0,0,1.2),(.8,.8,2.4),steel,.035)
            for i in range(12):
                z=.18+i*.18;box('Rack-mounted server',(0,-.405,z),(.68,.045,.135),chrome,.006)
                for x in [-.27,-.19,-.11]:box('Server status LED',(x,-.435,z),(.025,.008,.015),glow,.002)
            for x in [-.34,.34]:box('Rack rail',(x,-.43,1.2),(.025,.025,2.15),rubber,.002)
        elif kind==3:
            for x in [-.45,.45]:
                for y in [-.3,.3]:
                    tube('Medical trolley frame',(x,y,.12),(x,y,1.1),.018,chrome)
                    tube('Trolley castor',(x-.04,y,.08),(x+.04,y,.08),.07,rubber)
            for z in [.25,.85]:box('Sterile instrument tray',(0,0,z),(1,.7,.055),chrome)
            for x in [-.32,-.12,.08,.28]:tube('Sample vial',(x,0,.88),(x,0,1.02),.035,glass)
            box('Medical case',(0,.15,.37),(.68,.35,.19),ceramic,.025)
            text('STERILE',(0,-.218,.40),.085,accent)
        elif kind==4:
            for x in [-.5,.5]:box('Pump saddle',(x,0,.2),(.25,.8,.4),steel)
            tube('Pump motor',(-.7,0,.6),(.65,0,.6),.32,steel)
            for x in [-.65,.1,.6]:tube('Motor casing flange',(x-.03,0,.6),(x+.03,0,.6),.38,accent)
            pipe('Pump discharge',[(.55,0,.6),(.55,0,1.2),(.3,0,1.4),(-.2,0,1.4)],.10,steel)
            tube('Pressure gauge',(.2,-.12,1.15),(.2,-.25,1.15),.12,chrome)
            tube('Gauge face',(.2,-.255,1.15),(.2,-.26,1.15),.10,ceramic)
            beam('Gauge needle',(.2,-.27,1.15),(.25,-.27,1.20),.008,.008,steel)
        else:
            for x in [-1,0,1]:
                box('Bench pedestal',(x,0,.27),(.12,.35,.54),steel)
                box('Molded seat',(x,-.05,.54),(.80,.65,.10),ceramic,.10)
                back=box('Molded seat back',(x,.25,.88),(.80,.12,.65),ceramic,.10);back.rotation_euler.x=-.13
            tube('Bench frame',(-1.4,0,.48),(1.4,0,.48),.04,chrome)
            for x in [-1.4,1.4]:pipe('Arm rest',[(x,-.25,.55),(x,-.25,.78),(x,.18,.78),(x,.18,.55)],.025,chrome)
    module('Service',service)
    def clutter():
        if kind in [0,1,4]:
            for i in range(8):
                x=-.5+(i%3)*.38;y=-.28+(i//3)*.26
                o=box('Discarded offcut',(x,y,.06),(.34,.22,.10),concrete,.01);o.rotation_euler.z=i*.67
            pipe('Loose cable',[(-.8,.3,.03),(-.4,.5,.03),(.1,.38,.03),(.5,.6,.03),(.6,.9,.03)],.014,rubber)
        else:
            box('Equipment case',(0,0,.25),(.75,.5,.5),steel,.045)
            for x in [-.27,.27]:box('Case strap',(x,0,.25),(.06,.52,.52),chrome,.008)
            box('Case latch',(0,-.27,.3),(.12,.06,.14),accent,.01)
    module('Clutter',clutter)
    donors=[['exterior_aircon_unit','trashbag'],['old_drill_press','industrial_wall_lamp'],['metal_office_desk','potted_plant_02'],['industrial_microscope','medical_box'],['old_military_compressor','power_box_01'],['security_camera_01','metal_trash_can']][kind]
    donor_metrics=[]
    def donor(ident):
        folder=ROOT/'.tools/gamedev/biome-donors'/ident
        provenance=json.loads((folder/'provenance.json').read_text())
        before=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(folder/provenance['gltf']))
        imported=[o for o in bpy.data.objects if o not in before];meshes=[o for o in imported if o.type=='MESH']
        deps=bpy.context.evaluated_depsgraph_get();pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
        lo=Vector(tuple(min(v[i] for v in pts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in pts) for i in range(3)))
        center=(lo+hi)/2;center.z=lo.z
        for o in meshes:
            d=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);q=bpy.data.objects.new('PH_'+o.name,d);ACTIVE.objects.link(q)
            for v in d.vertices:v.co=o.matrix_world@v.co-center
            PARTS.append(q)
        donor_metrics.append(dict(id=ident,source_bounds_m=list(hi-lo),scale=1,source=provenance['source'],license='CC0-1.0'))
        for o in imported:bpy.data.objects.remove(o,do_unlink=True)
        if ident=='industrial_microscope':
            for q in list(ACTIVE.objects):q.location.z+=.90
            box('Laboratory instrument bench',(0,0,.87),(1.6,.9,.08),ceramic,.04)
            for x in [-.65,.65]:box('Bench pedestal',(x,0,.42),(.18,.65,.84),steel,.03)
    for ident in donors:module('Donor_'+ident,lambda ident=ident:donor(ident))
    # Review layout: two rooms connected by a 4 m corridor, 2.5 m clear door opening.
    for cx,cy in [(0,0),(20,8)]:
        for x in [-4,0,4]:
            for y in [-4,0,4]:instance('Floor_4m',(cx+x,cy+y,0))
        for x in [-4,0,4]:instance('Wall_'+str(int(x==0))+'_4m',(cx+x,cy+6,0))
        for y in [-4,0,4]:instance('Wall_'+str(int(y==0))+'_4m',(cx-6,cy+y,0),math.pi/2)
        instance('Feature',(cx-3.7,cy+3.4,0))
        for x,y,a in [(3.5,4.4,0),(-4.5,.5,math.pi/2),(-4.2,-3.6,math.pi/2)]:
            instance('Service',(cx+x,cy+y,0),a)
        for x,y in [(-3,2.3),(3.8,3.2),(-4,-3.7)]:instance('Clutter',(cx+x,cy+y,0))
        instance('Donor_'+donors[0],(cx+3.9,cy+(5.7 if kind==5 else 3.4),2.3 if kind==5 else 0))
        instance('Donor_'+donors[1],(cx-3.8,cy+(5.7 if kind in [1,4] else -3.7),1.6 if kind==1 else 1.1 if kind==4 else 0))
        for x in [-6,6]:instance('Corner_4m',(cx+x,cy+6,0))
    for p in [(8,0),(12,0),(12,4)]:instance('Floor_4m',(*p,0))
    instance('Door_4m',(6,0,0),math.pi/2);instance('Door_4m',(14,4,0),math.pi/2)
    # Human scale witness: approved operator, appended rather than a generic cube.
    source=ROOT/'Art/Operative/v03/CH_MaleOperative_master.blend'
    with bpy.data.libraries.load(str(source),link=False) as (src,dst):dst.collections=['LOW']
    witness=bpy.data.objects.new('Approved operator scale witness',None);scene.collection.objects.link(witness)
    witness.instance_type='COLLECTION';witness.instance_collection=dst.collections[0];witness.location=(.2,-.7,0)
    for o in list(dst.collections[0].all_objects):
        if o is not None:o.hide_render=False;o.hide_viewport=False
    scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=24
    scene.render.resolution_x=1440;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
    scene.world=bpy.data.worlds.new('Ambient');scene.world.use_nodes=True
    bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.12,.15,.20,1);bg.inputs[1].default_value=.5
    scene.view_settings.view_transform='Khronos PBR Neutral';scene.view_settings.look='None'
    for name,p,power,col,size in [('Cool skylight',(1,-3,14),2300,(.65,.76,1),14),('Warm room bounce',(-5,4,8),1600,(1,.72,.44),8),('Far room',(12,5,9),1700,(.65,.8,1),10)]:
        d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=col;d.shape='DISK';d.size=size
        o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=p;o.rotation_euler=(Vector((p[0],p[1],0))-o.location).to_track_quat('-Z','Y').to_euler()
    d=bpy.data.cameras.new('Gameplay inspection');cam=bpy.data.objects.new('Gameplay inspection',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO';d.ortho_scale=20
    target=Vector((0,0,1));cam.location=target+Vector((12,-17,19));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    # Source collections cannot be excluded: their collection instances must remain visible.
    # Hide original kit objects using an offset parent instead of hide_render on collection.
    for i,(name,c) in enumerate(KITS.items()):
        c.hide_render=False;c.hide_viewport=False
        for o in c.objects:o.hide_set(True)
    # Collection instances respect object hide_set (viewport only), and render source originals too.
    # Move originals into a linked-only catalog, unlink from scene; instances retain the collection.
    for c in KITS.values():scene.collection.children.unlink(c)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/(biome+'_kit.blend')),compress=True)
    scene.render.image_settings.file_format='PNG';scene.render.filepath=str(folder/'room.png');bpy.ops.render.render(write_still=True)
    # Separate atmosphere evidence from the neutral construction review.
    palettes=[[(.20,.32,.70),(1,.18,.40),(.1,.7,.8)],[(.42,.50,.62),(1,.53,.22),(.55,.65,.8)],[(.65,.8,1),(.4,.7,.8),(.7,.85,1)],[(.65,.9,.85),(.15,1,.6),(.5,.65,1)],[(.3,.4,.6),(1,.5,.14),(.2,.5,.35)],[(.5,.6,.85),(.55,.23,1),(.3,.5,1)]][kind]
    bg.inputs[1].default_value=.18 if kind in [0,4,5] else .3
    for light,color in zip([bpy.data.lights[n] for n in ['Cool skylight','Warm room bounce','Far room']],palettes):light.color=color
    if kind in [0,4,5]:bpy.data.lights['Cool skylight'].energy=900
    scene.render.filepath=str(folder/'atmosphere.png');bpy.ops.render.render(write_still=True)
    bg.inputs[1].default_value=.5
    for light,color,power in zip([bpy.data.lights[n] for n in ['Cool skylight','Warm room bounce','Far room']],[(.65,.76,1),(1,.72,.44),(.65,.8,1)],[2300,1600,1700]):light.color=color;light.energy=power
    target=Vector((10,4,0));cam.location=target+Vector((20,-28,30));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=43
    scene.render.resolution_x=1100;scene.render.resolution_y=800;scene.render.filepath=str(folder/'assembly.png');bpy.ops.render.render(write_still=True)
    report=dict(biome=biome,modules=list(KITS),donors=donor_metrics,source_meshes=sum(o.type=='MESH' for o in PARTS),source_curves=sum(o.type=='CURVE' for o in PARTS),seconds=round(time.perf_counter()-begin,2),status='editable environment study; review required; no Unity integration, UV bake, collision or runtime acceptance',grid_m=4,door_clear_width_m=2.5,layout='two 12m rooms plus 4m corridor; review only',license='Original authored geometry and shaders; industrial donor CC0 a52/rubberduck, see docs/ASSETS.md; additional CC0 donors Poly Haven, see donor-provenance.json')
    (folder/'manifest.json').write_text(json.dumps(report,indent=2));print('BIOME_COMPLETE',json.dumps(report),flush=True)

selected=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
for i,n in enumerate(NAMES):
    if not selected or n in selected:build(i)
