"""Export every native MW decal projection slot and all original RECT/WIDE artwork."""
import argparse,json,re
from pathlib import Path
from mw_source import Source
from mw_assets import Compiler,deflate
from nfs_formats import bin_hash
from project import LOCAL,mw_root

def write(path,data):
 path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')

def export(source,out):
 entries=[];pack=source.pack('CARS/TEXTURES.BIN')
 for key in sorted(set(pack.slots)|set(pack.records)):
  name,image,_=pack.decode(key)
  if not name.endswith(('_RECT','_WIDE')):continue
  image=image.convert('RGBA');kind='WIDE' if name.endswith('_WIDE') else 'RECT'
  entries.append(dict(id=100+(key&0x3fffffff),name=name,title=name.removesuffix('_'+kind).replace('_',' '),kind=kind,width=image.width,height=image.height,data=deflate(image.tobytes())))
 if len({e['id'] for e in entries})!=len(entries):raise ValueError('Decal logo id collision')
 write(out/'decals/mw-logos.json',dict(schemaVersion=1,sourceGame='mw',entries=entries))
 counts={}
 for row in source.roster():
  car=row['car'];ident='mw-'+car.lower();provenance=json.loads((out/ident/'provenance.json').read_text());radii=provenance['tyreRadii'];widths=provenance['tyreWidths']
  compiler=Compiler(source,car,provenance['bodyLift'],radii,widths);parts=[];slots={};source_names=[]
  for mesh in source.meshes(car):
   match=re.fullmatch(re.escape(car)+r'_(?:KIT([0-9]{2})_)?DECAL_(.+)_A',mesh['name'])
   if not match:continue
   kit=int(match[1]) if match[1] else -1;location=match[2];kind='WIDE' if '_WIDE_' in location else 'RECT';source_names.append(mesh['name'])
   for number,section in enumerate(mesh['slices']):
    if not section['indices']:continue
    slot=location+'.'+section['materialName'];slots.setdefault(slot,dict(key=slot,title=(location+' '+section['materialName']).replace('_',' ')[:64],kind=kind))
    single=dict(mesh,slices=[section]);compiled=compiler.compile(single,material_override=0)
    for part in compiled:part.update(name=mesh['name']+'_'+str(number),optionGroup='decal',option=slot,sourceKit=kit)
    parts.extend(compiled)
  if parts:
   material=dict(name='mw_decal',color=[1,1,1,1],metallic=0,smoothness=.25,texture=-1,alphaMode='blend')
   write(out/'decals'/(ident+'.json'),dict(schemaVersion=3,preset='NFVU2_MW_DECALS_'+car,parts=parts,materials=[material],textures=[],decalSlots=list(slots.values())))
  profile_path=out/ident/'profile.json';profile=json.loads(profile_path.read_text())[0]
  profile['assets']['decalSlots']=list(slots)
  profile['assets']['decalFile']='decals/'+ident+'.json' if parts else None
  write(profile_path,[profile]);provenance['decalMeshes']=source_names;write(out/ident/'provenance.json',provenance)
  # The active kit id is explicit rather than inferred from a menu index.
  path=out/'styling'/(ident+'.json');style=json.loads(path.read_text())
  for option in style['styleOptions']:
   if option['category']=='Bodykit':
    part=next(p for p in style['parts'] if p['option']=='Bodykit:'+str(option['id']) and '_BODY_A_' in p['name'])
    option['sourceKit']=int(re.search(r'_KIT([0-9]{2})_BODY_A',part['name'])[1])
  write(path,style)
  # Restore the four original supports, activated only with an aftermarket spoiler.
  supports=[m for m in source.meshes(car) if m['name'].endswith('_UNIVERSAL_SPOILER_BASE_A')]
  if supports:
   path=out/ident/(ident+'.json');model=json.loads(path.read_text());existing={p['name'] for p in model['parts']}
   compiler.materials=model['materials'];compiler.textures=model['textures'];compiler.tex_ids={bin_hash(t['name']):i for i,t in enumerate(model['textures'])}
   for mesh in supports:
    if any(name.startswith(mesh['name']+'_') for name in existing):continue
    for part in compiler.compile(mesh):
     if part['name'] not in existing:part['optionGroup']='spoiler-support';model['parts'].append(part)
   if compiler.unresolved:raise ValueError('Missing support texture')
   model['materials']=compiler.materials;model['textures']=compiler.textures;write(path,model)
  counts[ident]=dict(meshes=len(source_names),parts=len(parts),slots=len(slots));print(car,len(source_names),'native decal meshes,',len(slots),'editable slots',flush=True)
 profiles=[json.loads((out/('mw-'+r['car'].lower())/'profile.json').read_text())[0] for r in source.roster()]
 write(out/'cars.json',profiles);write(out/'decal-manifest.json',dict(logos=len(entries),cars=counts))
 print('Original decal artwork',len(entries),'projection meshes',sum(c['meshes'] for c in counts.values()),flush=True)

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--source',type=Path);p.add_argument('--output',type=Path,default=LOCAL/'mw-export');a=p.parse_args();export(Source(a.source or mw_root()),a.output)
if __name__=='__main__':main()
