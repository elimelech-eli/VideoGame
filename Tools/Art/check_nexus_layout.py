"""Export conservative design envelopes for static preflight, not Unity collision proof."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
rooms={'cargo':[[-6,0,-2],[-2,3,6]],'ops':[[-2,0,-2],[6,3,6]],
       'bridge':[[-2,0,6],[6,3,10]],'engineering':[[6,0,-2],[10,3,6]],
       'cabin':[[-2,0,-6],[6,3,-2]]}
def point(p): return [p[0],p[1],-p[2]]
def bounds(b): return [[b[0][0],b[0][1],-b[1][2]],[b[1][0],b[1][1],-b[0][2]]]
portals=[('cargo-door',['cargo','ops'],'x',[-2,0,2]),
 ('bridge-door',['ops','bridge'],'z',[2,0,6]),
 ('engineering-door',['ops','engineering'],'x',[6,0,2]),
 ('cabin-door',['ops','cabin'],'z',[2,0,-2])]
solids={'cargo-stack':[[-5.6,0,-.4],[-3.9,2,2]],
 'mess-table':[[4.1,0,-.5],[5.3,1.4,.7]],
 'mission-consoles':[[3.35,0,4.1],[5.45,1.9,4.95]],
 'reactor':[[8.2,0,3],[9.8,2.5,4.4]],
 'repair-desk':[[6.55,0,-1.1],[8.45,1.4,-.1]],
 'cabin-bed':[[-1.55,0,-5.3],[.75,1.2,-3.9]],
 'cabin-desk':[[4,0,-4.7],[5,1.4,-3.1]]}
layout=dict(schema_version=1,layout_id='nexus-full-ship-visual-v1',units='m',basis='right-handed-y-up',
 player=dict(radius=.4,height=2.15,clearance=.1,max_step=.15),
 rooms=[dict(id=k,bounds=bounds(b)) for k,b in rooms.items()],
 solids=[dict(id=k,bounds=bounds(b)) for k,b in solids.items()],
 spawns=[dict(id='ops-reference-spawn',room='ops',position=point([.2,0,1.4]))],
 portals=[dict(id=k,rooms=r,axis=a,center=point(p),width=2,height=2.5) for k,r,a,p in portals],
 required_rooms=list(rooms))
out=ROOT/'Art/NexusShipReview/layout-design.json'
out.write_text(json.dumps(layout,indent=2))
print('Exported design envelopes; Unity Z inverted for preflight basis. Dimensions are declared, not collision measurements.')
