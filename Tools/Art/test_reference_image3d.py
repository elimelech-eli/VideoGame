"""Local shape-only fidelity probe. AI output, review-only, no game integration.

Hunyuan output has regional license restrictions; do not treat it as CC0.
"""
import os,sys,time,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
os.environ['HF_HOME']=str(ROOT/'.tools/gamedev/image3d-models/hf')
os.environ['U2NET_HOME']=str(ROOT/'.tools/gamedev/image3d-models/rembg')
os.environ['HF_HUB_DISABLE_TELEMETRY']='1'
sys.path.insert(0,str(next((ROOT/'.tools/gamedev/image3d-source').glob('Hunyuan3D-*'))))
from PIL import Image
import numpy as np
from scipy import ndimage
from rembg import remove,new_session
model='full' if '--full' in sys.argv else 'mini'
OUT=ROOT/'Art/Enemies/v02'/('local-image3d-probe-full' if model=='full' else 'local-image3d-probe');OUT.mkdir(parents=True,exist_ok=True)
source=Image.open(ROOT/'Art/Enemies/v01/reference/factory-approved.png').convert('RGB')
crop=source.crop((385,110,778,728));crop.save(OUT/'reference-crop.png')
session=new_session('u2net',providers=['CPUExecutionProvider'])
cut=remove(crop,session=session)
arr=np.array(cut);mask=arr[:,:,3]>5
labels,count=ndimage.label(mask)
sizes=np.bincount(labels.ravel());sizes[0]=0;largest=sizes.argmax();mask=ndimage.binary_dilation(labels==largest,iterations=1)
arr[:,:,:3]=np.array(crop);arr[:,:,3]=np.where(mask,255,0);cut=Image.fromarray(arr);cut.save(OUT/'reference-input.png')
print('SEGMENTED_REFERENCE_READY',flush=True)
import torch
assert torch.cuda.is_available(),'Local CUDA is required'
from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline
start=time.perf_counter();torch.set_default_dtype(torch.float16)
pipeline=Hunyuan3DDiTFlowMatchingPipeline.from_single_file(
 ckpt_path=str(ROOT/f'.tools/gamedev/image3d-models/{model}/model.fp16.safetensors'),
 config_path=str(ROOT/f'.tools/gamedev/image3d-models/{model}/config.yaml'),
 use_safetensors=True,device='cpu',dtype=torch.float16)
pipeline.components={name:getattr(pipeline,name) for name in ['conditioner','model','vae']}
pipeline.enable_model_cpu_offload()
pipeline.device=torch.device('cuda')
result=pipeline(image=cut,num_inference_steps=30,octree_resolution=256,num_chunks=6000,mc_algo='mc',generator=torch.Generator(device='cuda').manual_seed(2187),output_type='trimesh')[0]
assert result is not None
result.export(OUT/'factory-mortar-probe.glb')
(OUT/'generation.json').write_text(json.dumps(dict(source='Factory approved concept, AOE panel',model='tencent/Hunyuan3D-2' if model=='full' else 'tencent/Hunyuan3D-2mini',source_commit=json.loads((ROOT/'.tools/gamedev/image3d-source/version.json').read_text())['commit'],seed=2187,steps=30,resolution=256,seconds=time.perf_counter()-start,vertices=len(result.vertices),faces=len(result.faces),status='AI-generated local shape probe only; not rigged, not textured, not visually approved',license='Tencent Hunyuan community license; regional restrictions apply to outputs. Not CC0.'),indent=2))
print('LOCAL_IMAGE3D_PROBE_COMPLETE',flush=True)
