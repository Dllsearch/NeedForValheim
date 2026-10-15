"""Decode exact VLT-linked MW acceleration/coast GIN banks to phase-aligned loops."""
import hashlib,json,struct,subprocess,wave
import numpy as np
from project import vgmstream_path
from mw_assets import deflate

def prepare(source,stage,bands,out):
 identity=json.dumps([stage['accel'],stage['decel'],bands],separators=(',',':')).encode()
 ident='mwbank-'+hashlib.sha256(identity).hexdigest()[:16];folder=out/'audio'/ident;folder.mkdir(parents=True,exist_ok=True)
 manifest=dict(collection=stage['collection'],bands=bands,sources=[],files=[])
 for prefix,name in (('engine',stage['accel']),('coast',stage['decel'] or stage['accel'])):
  original=source.root/'SOUND/ENGINE'/name;data=original.read_bytes();digest=hashlib.sha256(data).hexdigest()
  decoded=out/'decoded-audio'/(digest+'.wav');decoded.parent.mkdir(parents=True,exist_ok=True)
  if not decoded.is_file():subprocess.run([str(vgmstream_path()),'-i','-o',str(decoded),str(original)],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.PIPE)
  minimum,maximum=struct.unpack_from('<ff',data,8);na,nb=struct.unpack_from('<II',data,16)
  if not 1<=na<=10000 or not 12<=nb<=100000:raise ValueError('Invalid GIN phase tables: '+name)
  amplitude=np.asarray(struct.unpack_from('<'+str(na+1)+'I',data,32));cuts=np.asarray(struct.unpack_from('<'+str(nb+1)+'I',data,32+(na+1)*4))
  with wave.open(str(decoded),'rb') as w:
   if w.getnchannels()!=1 or w.getsampwidth()!=2:raise ValueError('Expected mono PCM16 GIN: '+name)
   rate=w.getframerate();samples=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
  if any(np.diff(cuts)<=0) or cuts[-1]>=len(samples):raise ValueError('Invalid GIN cycle boundaries: '+name)
  manifest['sources'].append(dict(file='SOUND/ENGINE/'+name,sha256=digest,rpmRange=[minimum,maximum]))
  for rpm in bands:
   center=np.interp(rpm,np.linspace(minimum,maximum,len(amplitude)),amplitude);idx=int(np.argmin(abs(cuts-center)))
   start=max(0,min(idx-6,len(cuts)-13));length=round(120*rate/rpm);cycles=[]
   for k in range(start,start+12):
    chunk=samples[cuts[k]:cuts[k+1]];cycles.append(np.interp(np.arange(length)*len(chunk)/length,np.arange(len(chunk)+1),np.append(chunk,samples[cuts[k+1]])))
   loop=np.concatenate(cycles);t=np.linspace(0,1,length);loop=np.concatenate((loop[length:-length],loop[-length:]*(1-t)+loop[:length]*t));loop-=loop.mean()
   rms=float(np.sqrt(np.mean(loop*loop)));peak=float(np.max(abs(loop)))
   if rms<.0001:raise ValueError('Silent MW GIN loop: '+name)
   loop*=min(.18/rms,.82/peak);target=folder/(prefix+'_'+str(rpm)+'.wav')
   with wave.open(str(target),'wb') as w:w.setparams((1,2,rate,len(loop),'NONE','not compressed'));w.writeframes(np.rint(loop*32767).astype('<i2').tobytes())
   manifest['files'].append(dict(file=target.name,rpm=rpm,sha256=hashlib.sha256(target.read_bytes()).hexdigest(),samples=len(loop),sampleRate=rate))
 (folder/'manifest.json').write_text(json.dumps(manifest,separators=(',',':')),encoding='utf-8')
 return 'audio/'+ident
