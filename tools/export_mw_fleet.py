"""Export the installed MW racing fleet, original dependency archive and playable parts.
Original packages stay in the ignored staging archive; only compiled JSON/WAV enter the mod.
"""
import argparse,hashlib,json,math,re,shutil
from pathlib import Path
from mw_source import Source
from mw_assets import Compiler,tyre_size,body_position
from nfs_formats import bin_hash
from mw_wheel_fit import fit_arch
from project import LOCAL,mw_root

SHARED=('WHEELS','BRAKES','ROOF','SPOILER','SPOILER_HATCH','SPOILER_CARRERA','SPOILER_PORSCHES')
DISPLAY={'BMWM3GTR':'BMW M3 GTR Street','BMWM3GTRE46':'BMW M3 GTR Race','BMWM3':'BMW M3 (archived)',
 '997S':'Porsche 911 Carrera S','911TURBO':'Porsche 911 Turbo','911GT2':'Porsche 911 GT2','CARRERAGT':'Porsche Carrera GT',
 'SL500':'Mercedes-Benz SL 500','SL65':'Mercedes-Benz SL 65 AMG','SLR':'Mercedes-Benz SLR McLaren','CLK500':'Mercedes-Benz CLK 500',
 'CEMTR':'Cement Truck','GARB':'Garbage Truck','PIZZA':'Pizza Delivery','TAXI':'Taxi','MINIVAN':'Minivan','PICKUPA':'Pickup',
 'CORVETTEC6R':'Chevrolet Corvette C6.R','RX8SPEEDT':'Mazda RX-8 Speed T', 'FORDGT':'Ford GT'}

def write(path,value):
 path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,separators=(',',':')),encoding='utf-8')

def sha(path):return hashlib.file_digest(path.open('rb'),'sha256').hexdigest()

def arch(body,seed,radius):
 try:return fit_arch(body,seed)
 except ValueError:
  # The top of the opening is still observable when the complete arch is not circular.
  # Sample triangles on the outer painted shell at the exact original VLT axle.
  import numpy as np
  vertices=np.asarray(body['vertices']);triangles=[s['indices'][i:i+3] for s in body['slices']
   if s['materialName'].startswith('BODY') and not any(t in s['materialName'] for t in ('CHROME','ALUMINUM','MOLDING','BADGING'))
   for i in range(0,len(s['indices']),3)]
  if not triangles:raise ValueError('No body shell at axle')
  faces=vertices[triangles];a=faces.reshape(-1,3);b=np.roll(faces,-1,axis=1).reshape(-1,3);d=b-a
  mask=((a[:,0]-seed)*(b[:,0]-seed)<=0)&(abs(d[:,0])>1e-8)
  points=a[mask]+d[mask]*((seed-a[mask,0])/d[mask,0])[:,None]
  if not len(points):raise ValueError('No shell crossing at axle')
  outer=max(points[:,1]);points=points[points[:,1]>outer*.88]
  top=float(min(points[:,2]));hub=top-radius-.045
  return dict(axle=seed,hub=hub,radius=radius+.045,outer=float(outer),method='opening crown; 45 mm NFVU2 clearance')

def export_car(source,row,out,audio_cache):
 car=row['car'];ident='mw-'+car.lower();fallback=car=='BMWM3'
 attrs=source.attributes('BMWM3GTR' if fallback else car);ecar=attrs['ecar'];offsets=[ecar['TireOffsets'][i] for i in (0,2)]
 radii=[v[3] for v in offsets];widths=[ecar['TireSkidWidth'][i] for i in (0,2)]
 meshes=source.meshes(car);byname={m['name']:m for m in meshes};shell=byname[car+'_KIT00_BODY_A']
 wheel=byname[car+'_KIT00_FRONT_TIRE_A'];fits=[arch(shell,v[0],r) for v,r in zip(offsets,radii)]
 # One shared body origin; staggered radii keep their own collider hub heights.
 lift=sum(r-f['hub'] for r,f in zip(radii,fits))/2
 compiler=Compiler(source,car,lift,radii,widths)
 stock=[m for m in meshes if m['name'].endswith('_A') and (m['name']==car+'_BASE_A' or m['name'].startswith(car+'_KIT00_'))
        and not any(x in m['name'] for x in ('_DAMAGE','_DECAL','_DRIVER'))]
 bodies=[m for m in stock if not any(x in m['name'] for x in ('_TIRE_','_BRAKE_'))]
 parts=[]
 for mesh in bodies:
  compiled=compiler.compile(mesh)
  if '_UNIVERSAL_SPOILER_BASE_' in mesh['name']:
   for part in compiled:part['optionGroup']='spoiler-support'
  parts.extend(compiled)
 for axle in ('front','rear'):
  parts+=compiler.compile(wheel,'wheel',axle)
  brake=byname.get(car+'_KIT00_'+axle.upper()+'_BRAKE_A') or byname.get(car+'_KIT00_FRONT_BRAKE_A')
  if brake:parts+=compiler.compile(brake,'brake',axle)
 sockets=byname[car+'_BASE_A']['sockets']
 lights=[dict(role=role,position=dict(x=matrix[13],y=matrix[14]+lift,z=matrix[12]),direction=dict(x=0,y=-.12,z=direction))
   for key,role,direction in ((0x9db90133,'headlight',1),(0xd09091c6,'headlight',1),(0x31a66786,'taillight',-1),(0xbf700a79,'taillight',-1))
   if (matrix:=sockets.get(key))]
 light_fallbacks=[]
 for role,forward in (('headlight',True),('taillight',False)):
  if any(l['role']==role for l in lights):continue
  bounds=[v for m in bodies for v in m['vertices']];front=max(v[0] for v in bounds);rear=min(v[0] for v in bounds)
  height=min(v[2] for v in bounds)+(max(v[2] for v in bounds)-min(v[2] for v in bounds))*.36+lift
  for side in (-1,1):lights.append(dict(role=role,position=dict(x=side*abs(offsets[0 if forward else 1][1])*.72,y=height,z=front-.08 if forward else rear+.04),direction=dict(x=0,y=-.12,z=1 if forward else -1)))
  light_fallbacks.append(role+': NFVU2 projection fitted to original body bounds; original MW has no markers')
 emitters=[dict(role='exhaust',position=dict(x=matrix[13],y=matrix[14]+lift,z=matrix[12]),direction=dict(x=0,y=0,z=-1))
  for key in (0xbcf8a18b,0xbd7cf15e) if (matrix:=shell['sockets'].get(key))]
 # MW has single sprites rather than the U2 2x2 particle atlases.
 for side in (-1,1):emitters.append(dict(role='purge',position=dict(x=side*abs(offsets[0][1])*.65,y=.65,z=offsets[0][0]+.2),direction=dict(x=side*.25,y=1,z=.1)))
 flame=compiler.texture(bin_hash('FX_FIRE01_ADDITIVE'));purge=compiler.texture(bin_hash('FX_SMK01_BLEND'))
 model=compiler.model(parts,lights=lights,wheelVisualLift=0,effectEmitters=emitters,nitroFlameTexture=flame,nitroPurgeTexture=purge,nitroAtlasColumns=1,nitroAtlasRows=1)
 native_radius,_,_=tyre_size(wheel)
 rim_points=[wheel['vertices'][i] for section in wheel['slices'] if section['materialName'].startswith('RIM') for i in section['indices']]
 nominal=round(max(math.hypot(v[0],v[2]) for v in rim_points)*radii[0]/native_radius*2/.0254) if rim_points else 17
 nominal=max(17,min(20,nominal))
 styleparts=[];options=[];replacements=[]
 def add(category,meshlist,title,**metadata):
  idx=1+sum(o['category']==category for o in options);key=category+':'+str(idx);compiled=[]
  for m,group,axle,socket in meshlist:compiled+=compiler.compile(m,group,axle,socket,key)
  if compiled:styleparts.extend(compiled);options.append(dict(category=category,id=idx,title=title[:64],**metadata))
 def replace(category,predicate):replacements.extend(category+'|'+p['name'] for p in parts if predicate(p['name'],p['group']))
 kits=sorted((m for m in meshes if re.fullmatch(re.escape(car)+r'_KIT[0-9]{2}_BODY_A',m['name'])),key=lambda m:m['name'])
 for mesh in kits:
  kit=int(mesh['name'].split('_KIT')[1][:2]);scales=ecar.get('TireSkidWidthKitScale',[[1,1]])
  scale=scales[min(kit,len(scales)-1)]
  front=ecar.get('KitWheelOffsetFront',[0]);rear=ecar.get('KitWheelOffsetRear',[0])
  add('Bodykit',[(mesh,'body','',None)],'Stock body' if kit==0 else 'Body kit '+str(kit),
      sourceKit=kit,frontWheelOffset=front[min(kit,len(front)-1)]*.001,rearWheelOffset=rear[min(kit,len(rear)-1)]*.001,
      frontTyreScale=scale[0] or 1,rearTyreScale=scale[1] or 1)
 replace('Bodykit',lambda n,g:n.startswith(car+'_KIT00_BODY_A'))
 for mesh in meshes:
  if re.fullmatch(re.escape(car)+r'_(?:KIT00|STYLE[0-9]{2})_HOOD_A',mesh['name']):add('Hood',[(mesh,'body','',None)],mesh['name'].removeprefix(car+'_').removesuffix('_A').replace('_',' '))
 replace('Hood',lambda n,g:'_HOOD_A_' in n)
 if 0xc93b73fd in sockets:
  family='SPOILER_CARRERA' if car=='CARRERAGT' else 'SPOILER_PORSCHES' if car in ('997S','911GT2','911TURBO','CAYMANS') else 'SPOILER_HATCH' if car in ('A3','GTI','CLIO','PUNTO') else 'SPOILER'
  for mesh in source.meshes(family):
   if mesh['name'].endswith('_A'):add('Spoiler',[(mesh,'body','',sockets[0xc93b73fd])],mesh['name'].removesuffix('_A').replace('_',' '))
  compiler.dependencies.add('CARS/'+family+'/GEOMETRY.BIN')
  replace('Spoiler',lambda n,g:'_SPOILER_A_' in n)
 if 0x90c81258 in sockets:
  for mesh in source.meshes('ROOF'):
   if mesh['name'].endswith('_A'):add('Roof',[(mesh,'body','',sockets[0x90c81258])],mesh['name'].removesuffix('_A').replace('_',' '))
  compiler.dependencies.add('CARS/ROOF/GEOMETRY.BIN')
 for mesh in source.meshes('WHEELS'):
  size=re.search(r'_(17|18|19|20)_25_A$',mesh['name'])
  if size:
   add('Rims',[(mesh,'wheel',axle,None) for axle in ('front','rear')],mesh['name'].removesuffix('_25_A').replace('_',' ')+' in',rimInches=int(size[1]))
 replace('Rims',lambda n,g:g=='wheel')
 for mesh in source.meshes('BRAKES'):
  if mesh['name'].endswith('_A'):add('Brakes',[(mesh,'brake',axle,None) for axle in ('front','rear')],mesh['name'].removesuffix('_A').replace('_',' '))
 replace('Brakes',lambda n,g:g=='brake')
 compiler.dependencies.update(('CARS/'+car+'/GEOMETRY.BIN','CARS/'+car+'/TEXTURES.BIN','CARS/WHEELS/GEOMETRY.BIN','CARS/BRAKES/GEOMETRY.BIN','GLOBAL/attributes.bin','GLOBAL/GlobalB.bun'))
 if (source.root/'CARS'/car/'PREVINYL.BIN').is_file():compiler.dependencies.add('CARS/'+car+'/PREVINYL.BIN')
 styles=compiler.model(styleparts,styleOptions=options,styleReplacements=replacements)
 # Separate tables: the factory model must not eagerly load all accessory textures.
 write(out/ident/(ident+'.json'),model);write(out/'styling'/(ident+'.json'),styles)
 points=[body_position(m,v) for m in bodies for v in m['vertices']];lo=[min(v[i] for v in points) for i in range(3)];hi=[max(v[i] for v in points) for i in range(3)]
 engine=attrs['engine'][0]['fields'];trans=attrs['transmission'][0]['fields'];minrpm=engine.get('IDLE',800);maxrpm=engine.get('MAX_RPM',6500)
 bands=[round(minrpm+(maxrpm-minrpm)*i/7) for i in range(8)]
 from mw_audio import prepare
 stages=source.audio(attrs);stagefolders=[]
 for stage in stages:
  combo=(stage['accel'],stage['decel'],tuple(bands))
  if combo not in audio_cache:audio_cache[combo]=prepare(source,stage,bands,out)
  stagefolders.append(audio_cache[combo]);compiler.dependencies.update('SOUND/ENGINE/'+name for name in stage['banks'])
 profile=dict(id=ident,sourceGame='mw',displayName=DISPLAY.get(car,row['manufacturer'].title().replace('Cheverolet','Chevrolet')+' '+car),shortName=DISPLAY.get(car,car),
  summary='Original MW parts and engine banks; NFVU2 handling.'+(' BMWM3GTR attributes are an explicit fallback for this archived asset.' if fallback else ''),
  rig=dict(boxCenterY=(lo[2]+hi[2])/2+lift,boxSizeX=(hi[1]-lo[1])*.95,boxSizeY=(hi[2]-lo[2])*.65,boxSizeZ=(hi[0]-lo[0])*.95,
   mass=attrs['mass'],tyreWidth=widths[0],rearTyreWidth=widths[1],trackWidth=abs(offsets[0][1])-widths[0]/2,rearTrackWidth=abs(offsets[1][1])-widths[1]/2,
   extraRearAxleOffset=ecar.get('ExtraRearTireOffset',0),wheelHeight=radii[0],rearWheelHeight=radii[1],wheelRadius=radii[0],rearWheelRadius=radii[1],frontAxleZ=offsets[0][0],rearAxleZ=-offsets[1][0],
   spawnLift=max(0,lift),clearanceX=.28,clearanceZ=.45,rimNominal=nominal,rimMin=17,rimMax=20),
  capabilities=dict(radio=True,drift=True,nitro=True,turbo=bool(attrs['induction'])),sound=dict(gearRatios=[g for g in trans['GEAR_RATIO'][2:] if g>0],rpmBands=bands,minRpm=minrpm,maxRpm=maxrpm,
  upShiftRpm=min(maxrpm,engine.get('RED_LINE',maxrpm)*.94),downShiftRpm=max(minrpm,maxrpm*.3)),
  assets=dict(modelFile=ident+'/'+ident+'.json',engineFolder=stagefolders[0],engineStageFolders=stagefolders,engineCoast=True,
   audioFolders=['audio/effects','audio/turbo',*sorted(set(stagefolders))]))
 write(out/ident/'profile.json',[profile])
 provenance=dict(exportVersion=8,car=car,sourceGame='Most Wanted (2005) PC',attributes=attrs,attributeFallback='BMWM3GTR' if fallback else None,
  wheelArchFits=fits,bodyLift=lift,tyreRadii=radii,tyreWidths=widths,options=options,engineBanks=stages,
  lightFallbacks=light_fallbacks,generatedMaterials=sorted(compiler.generated),adaptedMaterials=sorted(compiler.adapted),unresolvedTextures=sorted(compiler.unresolved),dependencies=sorted(compiler.dependencies),
  adaptation='Original axle/width/radius/mass/gears/RPM/banks; vertical body alignment fitted from arches. NFVU2 dynamics, synthetic reverse/horn and common road/skid. Source SKIN paint is generated at runtime in MW. Original decals/native rim variants also have runtime catalogs; LOD/damage states stay archived. Car-solid local translations are applied before chassis bounds.')
 write(out/ident/'provenance.json',provenance)
 print(f'{car}: {len(parts)} factory parts, {len(options)} options, {len(styleparts)} accessory parts, {len(stages)} engine stages',flush=True)
 return profile,compiler.dependencies

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--source',type=Path);p.add_argument('--output',type=Path,default=LOCAL/'mw-export');p.add_argument('--car');p.add_argument('--no-archive',action='store_true');p.add_argument('--resume',action='store_true');args=p.parse_args()
 source=Source(args.source or mw_root());out=args.output.resolve();roster=source.roster();selected=[r for r in roster if not args.car or r['car']==args.car.upper()]
 if not selected:p.error('No registered MW racing car matches selection')
 profiles=[];dependencies=set();audio={};failures=[]
 for row in selected:
  try:
   ident='mw-'+row['car'].lower();cached=out/ident/'provenance.json'
   if args.resume and cached.is_file() and json.loads(cached.read_text()).get('exportVersion')==8 and (out/ident/(ident+'.json')).is_file() and (out/'styling'/(ident+'.json')).is_file():
    profile=json.loads((out/ident/'profile.json').read_text())[0];files=json.loads(cached.read_text())['dependencies']
    profiles.append(profile);dependencies.update(files);continue
   profile,files=export_car(source,row,out,audio);profiles.append(profile);dependencies.update(files)
  except Exception as e:
   import traceback;traceback.print_exc();failures.append(dict(car=row['car'],error=str(e)))
 manifest=dict(roster=roster,exported=[p['id'] for p in profiles],failures=failures,dependencies=[])
 if not args.no_archive:
  # Preserve every car package, source LOD, damage state and native part variant.
  for row in selected:
   dependencies.update(str(f.relative_to(source.root)).replace('\\','/') for f in (source.root/'CARS'/row['car']).iterdir() if f.is_file())
  for folder in SHARED:dependencies.update(str(f.relative_to(source.root)).replace('\\','/') for f in (source.root/'CARS'/folder).iterdir() if f.is_file())
 for relative in sorted(dependencies):
  original=source.root/relative
  if not original.is_file():raise ValueError('Missing source dependency '+relative)
  entry=dict(path=relative,bytes=original.stat().st_size,sha256=sha(original));manifest['dependencies'].append(entry)
  if not args.no_archive:
   target=out/'original'/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(original,target)
 write(out/'cars.json',profiles);write(out/'manifest.json',manifest)
 print(f'Exported {len(profiles)}/{len(selected)} cars; {len(dependencies)} complete dependencies; failures={len(failures)}',flush=True)
 if failures:raise SystemExit(1)
if __name__=='__main__':main()
