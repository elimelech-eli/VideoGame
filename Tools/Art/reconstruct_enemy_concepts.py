"""Local image-conditioned body reconstruction. All outputs are review-only.

This does not claim animation-ready topology, faithful unseen sides, or CC0 rights.
"""
import os,sys,json,time,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Art/Enemies/v03';OUT.mkdir(parents=True,exist_ok=True)
os.environ['HF_HOME']=str(ROOT/'.tools/gamedev/image3d-models/hf')
os.environ['U2NET_HOME']=str(ROOT/'.tools/gamedev/image3d-models/rembg')
os.environ['HF_HUB_DISABLE_TELEMETRY']='1'
sys.path.insert(0,str(next((ROOT/'.tools/gamedev/image3d-source').glob('Hunyuan3D-*'))))
from PIL import Image
import numpy as np
from scipy import ndimage
from rembg import remove,new_session
themes=[('factory',110,728),('neon',100,720),('corporate',100,760),('biotech',100,707),('utilities',100,740),('transit',100,722)]
roles=['melee','aoe','heavy','ranged']
selection=[a for a in sys.argv[1:] if a in [x[0] for x in themes]]
session=new_session('u2net',providers=['CPUExecutionProvider']);jobs=[]
for biome,top,bottom in themes:
 if selection and biome not in selection:continue
 source_path=ROOT/f'Art/Enemies/v01/reference/{biome}-approved.png';source=Image.open(source_path).convert('RGB')
 for i,role in enumerate(roles):
  folder=OUT/biome/role;folder.mkdir(parents=True,exist_ok=True)
  # Each main character occupies its own quarter of the original approved board.
  # Illustrated gun barrels can cross the panel boundary. Preserve those overhangs;
  # the largest-component foreground filter removes disconnected neighboring art.
  left=max(0,i*384-(20 if role=='aoe' else 8 if role=='heavy' else -3))
  right=min(source.width,(i+1)*384+(72 if role=='heavy' else -4))
  crop=source.crop((left,top,right,bottom));crop.save(folder/'reference-crop.png')
  arr=np.array(remove(crop,session=session));mask=arr[:,:,3]>5
  labels,count=ndimage.label(mask);sizes=np.bincount(labels.ravel());sizes[0]=0
  mask=ndimage.binary_dilation(labels==sizes.argmax(),iterations=1)
  arr[:,:,:3]=np.array(crop);arr[:,:,3]=np.where(mask,255,0)
  Image.fromarray(arr).save(folder/'reference-input.png')
  jobs.append((biome,role,folder,hashlib.sha256(source_path.read_bytes()).hexdigest()))
print('SEGMENTATION_COMPLETE',len(jobs),flush=True)
import torch
from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline
torch.set_default_dtype(torch.float16)
pipeline=Hunyuan3DDiTFlowMatchingPipeline.from_single_file(ckpt_path=str(ROOT/'.tools/gamedev/image3d-models/full/model.fp16.safetensors'),config_path=str(ROOT/'.tools/gamedev/image3d-models/full/config.yaml'),use_safetensors=True,device='cpu',dtype=torch.float16)
pipeline.components={name:getattr(pipeline,name) for name in ['conditioner','model','vae']};pipeline.enable_model_cpu_offload();pipeline.device=torch.device('cuda')
start=time.perf_counter();records=[]
for biome,role,folder,source_hash in jobs:
 path=folder/'Raw_body.glb'
 if path.exists() and (folder/'generation.json').exists():
  records.append(json.loads((folder/'generation.json').read_text()));continue
 begin=time.perf_counter();print('RECONSTRUCTING',biome,role,flush=True)
 seed=2187+roles.index(role)*137
 mesh=pipeline(image=Image.open(folder/'reference-input.png'),num_inference_steps=30,octree_resolution=320,num_chunks=20000,mc_algo='mc',generator=torch.Generator(device='cuda').manual_seed(seed),output_type='trimesh',enable_pbar=False)[0]
 assert mesh is not None and np.isfinite(mesh.vertices).all()
 mesh.export(path)
 record=dict(biome=biome,role=role,model='tencent/Hunyuan3D-2',source_hash=source_hash,seed=seed,steps=30,resolution=320,seconds=time.perf_counter()-begin,vertices=len(mesh.vertices),faces=len(mesh.faces),status='AI-generated reference body reconstruction; raw/untextured/unrigged; not approved or runtime ready',license='Tencent Hunyuan Community License; regional restrictions apply to outputs. Not CC0.')
 (folder/'generation.json').write_text(json.dumps(record,indent=2));records.append(record)
 print('BODY_COMPLETE',biome,role,record['faces'],round(record['seconds'],1),flush=True)
 del mesh;torch.cuda.empty_cache()
(OUT/('generation-'+selection[0]+'.json' if selection else 'generation.json')).write_text(json.dumps(dict(seconds=time.perf_counter()-start,bodies=records),indent=2))
