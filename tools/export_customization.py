"""Export original NFSU2 body parts and branded wheels as an optional garage library.

Uses the user's extracted game files; outputs stay in ignored .local/styling.
Per-section vertex compaction keeps the catalog inexpensive to parse and cache.
"""
import argparse
import base64
import json
import math
import re
from pathlib import Path
from functools import lru_cache
from export_car import SPECS, source_name, ASSETS, LOCAL, parse, pick, family, TexturePack, PAINT, RUBBER, LIP, CHROME, GLASS, DARK, CLEAR
from vehicle_geometry import wheel_source_radius, badge_uv, badge_rows_uv, BADGE_OVERLAY
from nfs_formats import bin_hash

CATEGORIES=('Bodykit','Front','Rear','Skirts','Hood','Spoiler','Rims')

@lru_cache(maxsize=40)
def geometry(path):
    return parse(Path(path))

@lru_cache(maxsize=40)
def texture_pack(path):
    return TexturePack(Path(path))

def replacement_category(name):
    name=name.upper()
    if 'FRONT_BUM' in name:return 'Front'
    if 'REAR_BUM' in name:return 'Rear'
    if '_SKIRT' in name:return 'Skirts'
    if '_HOOD' in name or name.startswith('NFVU2_HOODINNER'):return 'Hood'
    if 'SPOILER' in name:return 'Spoiler'
    if '_KIT' in name and any(s in name for s in ('_BODY','_DOOR')):return 'Bodykit'
    return None

def export(profile):
    car=source_name(profile['id'])
    source=ASSETS/f'CARS/{car}/GEOMETRY.BIN'
    if not source.exists():raise ValueError('Missing source geometry: '+str(source))
    meshes=geometry(str(source));families=sorted({family(p['name']) for p in meshes})
    base_path=LOCAL/profile['assets']['modelFile']
    if not base_path.exists() and profile['assets']['modelFile']=='body.json':base_path=LOCAL/'model/body.json'
    model=json.loads(base_path.read_text(encoding='utf-8'))
    provenance_path=base_path.parent/'provenance.json'
    provenance=json.loads(provenance_path.read_text()) if provenance_path.exists() else {}
    fit=provenance.get('visualFit',provenance)
    lift=fit.get('bodyLift',profile['rig']['spawnLift'])
    radius=fit.get('wheelRadius',fit.get('tyreRadius',profile['rig']['wheelRadius']))
    width=fit.get('wheelWidth',fit.get('tyreWidth'))
    if width is None:
        stock_wheel=next(pick(meshes,n) for n in families if 'FRONT_WHE' in n)
        rubber={i for s in stock_wheel['slices'] if s['materialKey']==RUBBER for i in s['indices']}
        width=max(stock_wheel['vertices'][i][1] for i in rubber)-min(stock_wheel['vertices'][i][1] for i in rubber)
    options=[];selected=[];replacements=[]
    for part in model['parts']:
        category='Rims' if part['group'] in ('wheel','spinner') else replacement_category(part['name']) if part['group']=='body' else None
        if category:
            replacements.append(category+'|'+part['name'])
            if category in ('Front','Rear','Skirts'):replacements.append('Bodykit|'+part['name'])
    def add(category,title,items):
        if not items:return
        index=1+sum(o['category']==category for o in options)
        options.append(dict(category=category,id=index,title=title))
        for mesh,name,group,socket in items:selected.append((mesh,name,group,socket,category,index))
    def body_items(names):return [(pick(meshes,n),n,'body',None) for n in names]
    for kit in ('KIT00','KITW01','KITW02','KITW03','KITW04','KITW05'):
        names=[n for n in families if n.startswith(f'{car}_{kit}_') and replacement_category(n)=='Bodykit']
        # SUVs contain KITW door-window placeholders without a widebody shell.
        # Advertising those as kits would hide the complete stock body/bumpers.
        if not any(n.endswith('_BODY') and len(pick(meshes,n)['indices'])>90 for n in names):continue
        if kit!='KIT00':names += [n for n in families if n.startswith(f'{car}_{kit}_') and replacement_category(n) in ('Front','Rear','Skirts')]
        add('Bodykit','Standard body' if kit=='KIT00' else 'Widebody '+kit[-2:],body_items(names))
    for category,ending in (('Front','FRONT_BUM'),('Rear','REAR_BUM'),('Skirts','SKIRT')):
        for kit in range(31):
            names=[n for n in families if n.startswith(f'{car}_KIT{kit:02}_') and ending in n]
            add(category,'Stock' if kit==0 else 'Style '+str(kit).zfill(2),body_items(names))
    for style in range(31):
        prefix=f'{car}_KIT00_' if style==0 else f'{car}_STYLE{style:02}_'
        names=[n for n in families if n.startswith(prefix) and n.endswith('_HOOD')]
        add('Hood','Stock hood' if style==0 else 'Hood '+str(style).zfill(2),body_items(names))
    base=pick(meshes,car+'_BASE')
    socket=base.get('sockets',{}).get(0xc93b73fd)
    if socket:
        # NFS's hatch/SUV spoiler packs use the same car-local attachment socket.
        pack='SPOILER_SUV' if car in ('HUMMER','ESCALADE','NAVIGATOR') else 'SPOILER_HATCH' if car in ('A3','GOLF','CORSA','FOCUS','CIVIC','PEUGOT','PEUGOT106','RSX') else 'SPOILER'
        spoiler_meshes=geometry(str(ASSETS/f'CARS/{pack}/GEOMETRY.BIN'))
        names=sorted({family(p['name']) for p in spoiler_meshes if re.search(r'STYLE\d\d$',family(p['name']))})
        for name in names:add('Spoiler',name.split('_')[-1].replace('STYLE','Spoiler '),[(pick(spoiler_meshes,name),name,'body',socket)])
    for brand in ('BBS','ENKEI','OZ','VOLK','WORK','RACINGHART','5ZIGEN','MOMO','KONIG','ROTA'):
        path=ASSETS/f'CARS/WHEELS/GEOMETRY_{brand}.BIN'
        if not path.exists():continue
        rims=geometry(str(path))
        candidates=sorted({family(p['name']) for p in rims if re.search(r'_20_\d+$',family(p['name']))})
        # Different width suffixes are the same design; use one original 20-inch mesh.
        designs={}
        for name in candidates:designs.setdefault(name.split('_20_')[0],name)
        for name in designs.values():add('Rims',name.split('_20_')[0].replace('_STYLE',' '),[(pick(rims,name),name,'wheel',None)])
    paths=[ASSETS/f'CARS/{car}/TEXTURES.BIN',ASSETS/'CARS/TEXTURES.BIN',ASSETS/'CARS/WHEELS/TEXTURES.BIN',LOCAL/'global-decompressed.bin',ASSETS/'GLOBAL/InGameCommon.bun']
    packs=[texture_pack(str(p)) for p in paths if p.exists()]
    textures=[];texture_ids={};materials=[];material_ids={};parts=[]
    def texture(key):
        if key in texture_ids:return texture_ids[key]
        for pack in packs:
            if key in pack.slots or key in pack.records:
                _,image,_=pack.decode(key);image=image.convert('RGBA')
                texture_ids[key]=len(textures)
                textures.append(dict(name=f'garage_{key:08x}',width=image.width,height=image.height,rgba=base64.b64encode(image.tobytes()).decode()))
                return texture_ids[key]
        texture_ids[key]=-1;return -1
    original_color=next((m['color'] for m in model['materials'] if m.get('bodyPaint')), [.4,.4,.45,1])
    def material(key,tex,group,badge=False):
        ident=(key,tex,group,badge)
        if ident in material_ids:return material_ids[ident]
        colour=[1,1,1,1];metal=.1;smooth=.5
        if key==PAINT:colour=original_color;metal=.72;smooth=.88
        elif key in CHROME:
            colour=[1,1,1,1] if group=='wheel' and tex>=0 else [.92,.94,.96,1];metal=.85;smooth=.88
            if group!='wheel':tex=-1
        elif key==RUBBER:colour=[.8,.8,.8,1];metal=0;smooth=.12
        elif key in GLASS:colour=[.035,.05,.07,1];metal=.3;smooth=.9;tex=-1
        elif key in DARK:colour=[.14,.14,.15,1];smooth=.2
        elif tex<0:colour=[.12,.12,.14,1];smooth=.25
        index=len(materials);material_ids[ident]=index
        materials.append(dict(name=f'{key:08x}_{group}_{index}',color=colour,metallic=metal,smoothness=smooth,texture=tex,bodyPaint=key==PAINT and group=='body',alphaMode='blend' if badge else 'opaque',rimPaint=group=='wheel' and key!=RUBBER and key not in GLASS and key not in DARK))
        return index
    for mesh,name,group,socket,category,index in selected:
        radial=radius/wheel_source_radius(mesh) if group=='wheel' else 1
        axial=[mesh['vertices'][i][1] for s in mesh['slices'] if s['materialKey']==RUBBER for i in s['indices']] if group=='wheel' else []
        axial_scale=width/(max(axial)-min(axial)) if axial else 1
        inner=min(axial) if axial else 0
        def convert(v,normal=False):
            x,y,z=v
            if group=='wheel':point=(-y/axial_scale,z/radial,x/radial) if normal else (width/2-(y-inner)*axial_scale,z*radial,x*radial)
            else:
                if socket:x,y,z=(x*socket[a]+y*socket[4+a]+z*socket[8+a]+(0 if normal else socket[12+a]) for a in range(3))
                point=(y,z+(0 if normal else lift),x)
            if normal:
                length=math.sqrt(sum(a*a for a in point));point=tuple(a/length for a in point) if length else (0,1,0)
            return dict(zip(('x','y','z'),point))
        for number,section in enumerate(mesh['slices']):
            key=section['materialKey']
            if key in CLEAR:continue
            ids=section['indices'][:]
            if group=='wheel' and key==LIP:
                # Original INTERIOR atlas backing quads extend beyond the tyre;
                # they are not rim geometry and otherwise become white squares.
                source_radius=radius/radial
                ids=[i for start in range(0,len(ids),3) if all(math.hypot(mesh['vertices'][v][0],mesh['vertices'][v][2])<=source_radius*1.01 for v in ids[start:start+3]) for i in ids[start:start+3]]
            if group=='wheel':
                for i in range(0,len(ids),3):ids[i+1],ids[i+2]=ids[i+2],ids[i+1]
            used=sorted(set(ids));remap={old:i for i,old in enumerate(used)}
            if len(used)<3:continue
            coords=[dict(u=u if abs(u)<100 else u%1,v=1-v if abs(v)<100 else (1-v)%1) for u,v in mesh['uv']]
            is_badge=section['textureKey']==bin_hash(car+'_BADGING') and key==BADGE_OVERLAY
            rule=SPECS.get(profile['id'],{}).get('badgeRows')
            if is_badge:
                coords=badge_rows_uv(mesh,section,coords,rule) if rule and name==rule['family'] else badge_uv(mesh,section,coords)
            moving='hood' if category=='Hood' else 'trunk' if category=='Spoiler' else 'leftDoor' if 'DOOR_LEFT' in name or 'PANEL_LEFT' in name else 'rightDoor' if 'DOOR_RIGHT' in name or 'PANEL_RIGH' in name else ''
            parts.append(dict(name=f'Garage_{category}_{index}_{name}_{number}',group=group,material=material(key,texture(section['textureKey']),group,is_badge),movingPart=moving,optionGroup='styling',option=f'{category}:{index}',
                vertices=[convert(mesh['vertices'][i]) for i in used],normals=[convert(mesh['normals'][i],True) for i in used],uv=[coords[i] for i in used],indices=[remap[i] for i in ids]))
    options=[o for o in options if any(p['option']==f'{o["category"]}:{o["id"]}' for p in parts)]
    output=LOCAL/'styling';output.mkdir(exist_ok=True)
    target=output/(profile['id']+'.json')
    data=dict(schemaVersion=3,preset='NFVU2_GARAGE_'+car,parts=parts,materials=materials,textures=textures,styleOptions=options,styleReplacements=sorted(set(replacements)),neonTexture=-1,nitroFlameTexture=-1,nitroPurgeTexture=-1)
    if len(parts)>2048 or len(textures)>128 or len(materials)>256:raise ValueError('Styling library exceeds runtime limits: '+car)
    target.write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
    print(profile['id'],len(options),'options',len(parts),'meshes',round(target.stat().st_size/1048576,1),'MiB')
    return target

def main():
    parser=argparse.ArgumentParser();parser.add_argument('cars',nargs='*');args=parser.parse_args()
    profiles=json.loads((LOCAL/'cars/cars.json').read_text(encoding='utf-8'))
    profiles=[dict(id='350z',assets=dict(modelFile='model/body.json'),rig=dict(wheelRadius=.34,spawnLift=.35)),
              dict(id='h2',assets=dict(modelFile='h2/h2.json'),rig=dict(wheelRadius=.4364,spawnLift=.3)),*profiles]
    profiles={p['id']:p for p in profiles if p.get('sourceGame','u2')=='u2'}.values()
    for profile in profiles:
        if not args.cars or profile['id'] in args.cars:export(profile)
if __name__=='__main__':main()
