import urllib.request,re,zipfile,io
from pathlib import Path
url='https://files2.makehumancommunity.org/asset_packs/makehuman_system_assets/makehuman_system_assets_cc0.zip'
root=Path(__file__).resolve().parents[2]/'.tools/gamedev/character-sources'
class RemoteZip(io.RawIOBase):
 def __init__(self):self.pos=0;self.size=280737770
 def seekable(self):return True
 def seek(self,offset,whence=0):
  self.pos=offset if whence==0 else self.pos+offset if whence==1 else self.size+offset
  return self.pos
 def tell(self):return self.pos
 def read(self,n=-1):
  if n<0:n=self.size-self.pos
  if not n:return b''
  req=urllib.request.Request(url,headers={'Range':f'bytes={self.pos}-{self.pos+n-1}'})
  with urllib.request.urlopen(req,timeout=45) as r:
   if r.status!=206:raise RuntimeError('Expected HTTP range response')
   data=r.read()
  self.pos+=len(data);return data
with zipfile.ZipFile(RemoteZip()) as z:
 names=[n for n in z.namelist() if any(a in n for a in ['male_worksuit01','male_casualsuit04','shoes05','shoes02']) and n.endswith(('.obj','.mhclo','.mhmat','.json'))]
 for n in names:z.extract(n,root/'garments')
 print('\n'.join(names))
