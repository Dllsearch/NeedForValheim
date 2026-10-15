"""Export original MW vinyl and precomposite textures to the layered garage contract."""
import argparse,base64,json,subprocess,zlib
from pathlib import Path
from PIL import Image
import numpy as np
from project import BASE,LOCAL,mw_root
from build_support import compile_cs,test_refs,runtime_config
from mw_source import Source
from mw_assets import deflate

def mask_key(key):
 for b in b'_MASK':key=(key*33+b)&0xffffffff
 return key

def export(source,car,out,decoder):
 records={};entries=[]
 for filename in ('VINYLS.BIN','PREVINYL.BIN'):
  path=source.root/'CARS'/car/filename
  if not path.is_file() or path.stat().st_size==0:continue
  temp=out/'decoded-vinyl'/(car+'-'+filename+'.json');temp.parent.mkdir(parents=True,exist_ok=True)
  subprocess.run(['dotnet',str(decoder),str(path),str(temp)],check=True,stdout=subprocess.DEVNULL)
  records.update({r['key']:r for r in json.loads(temp.read_text())})
 masks={mask_key(key) for key in records}
 for key,r in records.items():
  if key in masks or r['name'].endswith('_MASK'):continue
  mask=records.get(mask_key(key));w,h=r['width'],r['height']
  rgb=np.frombuffer(zlib.decompress(base64.b64decode(r['data']),-15),np.uint8).reshape(h,w,4).copy()
  if mask:
   if (mask['width'],mask['height'])!=(w,h):raise ValueError('MW vinyl mask dimensions')
   rgb[:,:,3]=np.frombuffer(zlib.decompress(base64.b64decode(mask['data']),-15),np.uint8).reshape(h,w,4)[:,:,0]
  # Standalone sponsor art retains its own original alpha.
  if not np.any(rgb[:,:,3]):continue
  name=r['name'].removeprefix(car+'_');fixed=mask is None or not name.startswith(('BODY','HOOD','STRIPE','FLAME','TRIBAL','RIP','TEAR'))
  category='Precomposite' if 'PRECOM' in name else 'Carbon' if 'CARBON' in name else 'Flags' if 'NF_' in name else 'Flames' if 'FLAME' in name else 'Stripes' if 'STRIPE' in name else 'Tribal' if 'TRIBAL' in name else 'Original MW'
  image=Image.fromarray(rgb).resize((512,512),Image.Resampling.BILINEAR);thumb=image.resize((64,64),Image.Resampling.LANCZOS)
  entries.append(dict(id=100+(key&0x3fffffff),name=name,title=name.replace('_',' '),category=category,fixedColors=fixed,width=512,height=512,data=deflate(image.tobytes()),thumbnail=deflate(thumb.tobytes())))
 if len({e['id'] for e in entries})!=len(entries):raise ValueError('MW vinyl id collision')
 target=out/'menu'/('vinyl-NFVU2_MW_'+car+'.json');target.parent.mkdir(parents=True,exist_ok=True)
 target.write_text(json.dumps(dict(schemaVersion=1,preset='NFVU2_MW_'+car,sourceGame='mw',entries=entries),separators=(',',':')))
 print(car,len(entries),'original MW vinyls',flush=True)
 return len(entries)

def main():
 p=argparse.ArgumentParser();p.add_argument('--car');p.add_argument('--source',type=Path);p.add_argument('--output',type=Path,default=LOCAL/'mw-export');a=p.parse_args();s=Source(a.source or mw_root())
 refs,tfm,version=test_refs();decoder=BASE/'artifacts/mw-checks/vinyl-decode.dll';compile_cs(decoder,[BASE/'tools/vinyl_decode.cs'],refs);runtime_config(decoder,tfm,version)
 totals={r['car']:export(s,r['car'],a.output,decoder) for r in s.roster() if not a.car or r['car']==a.car.upper()}
 (a.output/'vinyl-manifest.json').write_text(json.dumps(totals,indent=2));print('Total',sum(totals.values()),'MW vinyls')
if __name__=='__main__':main()
