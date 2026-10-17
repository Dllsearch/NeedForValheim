"""Compile MW parts into the normal NFVU2 model and styling contracts."""
import base64
import math
import re
import struct
import zlib
from functools import lru_cache
from nfs_formats import bin_hash,TexturePack
from project import LOCAL
from vehicle_geometry import badge_uv

SHARED_BRAKE_KEYS={0x17f9f794,*[bin_hash('BRAKE_STYLE%02d'%i) for i in range(4)]}

@lru_cache(maxsize=1)
def shared_brake_pack():
    # Both owned games share the brake UV contract. Some MW installs contain
    # empty shared texture packages; the local U2 atlas is an explicit donor.
    path=LOCAL/'global-decompressed.bin'
    return TexturePack(path) if path.is_file() else None


def deflate(data):return base64.b64encode(zlib.compress(data,9)[2:-4]).decode('ascii')


def body_position(mesh,point):
    # Car solids already bake their orientation, but retain a local translation.
    # Shared accessories instead use the car's attachment marker (not their
    # authoring-layout pivot, which can be several metres away).
    return tuple(point[i]+mesh['pivot'][12+i] for i in range(3))


def pack_geometry(part):
    data=bytearray()
    for v,n,uv in zip(part.pop('vertices'),part.pop('normals'),part.pop('uv')):
        data.extend(struct.pack('<8f',v['x'],v['y'],v['z'],n['x'],n['y'],n['z'],uv['u'],uv['v']))
    ids=part.pop('indices');part['vertexCount']=len(data)//32;part['indexCount']=len(ids)
    data.extend(struct.pack('<'+str(len(ids))+'I',*ids));part['packedGeometry']=deflate(data)
    return part


def tyre_size(mesh):
    points=[mesh['vertices'][i] for s in mesh['slices'] if s['materialName'].startswith('TIRE') and 'BACK' not in s['materialName'] for i in set(s['indices'])]
    if not points:raise ValueError('No original tyre surface: '+mesh['name'])
    return max(math.hypot(v[0],v[2]) for v in points),min(v[1] for v in points),max(v[1] for v in points)


class Compiler:
    def __init__(self,source,car,lift,radii,widths):
        self.source=source;self.car=car;self.lift=lift;self.radii=radii;self.widths=widths
        self.materials=[];self.textures=[];self.mat_ids={};self.tex_ids={};self.unresolved=set();self.generated=set();self.dependencies=set();self.adapted=set()
        paths=[f'CARS/{car}/TEXTURES.BIN',f'CARS/{car}/PREVINYL.BIN','CARS/TEXTURES.BIN','CARS/WHEELS/TEXTURES.BIN',
               'CARS/BRAKES/TEXTURES.BIN','CARS/ROOF/TEXTURES.BIN','CARS/SPOILER/TEXTURES.BIN',
               'GLOBAL/GlobalB.bun','GLOBAL/InGameA.bun','GLOBAL/InGameB.bun','GLOBAL/InGameC.bun']
        self.packs=[(p,source.pack(p)) for p in paths if (source.root/p).is_file()]

    def texture(self,key,optional=False):
        if key in self.tex_ids:return self.tex_ids[key]
        for path,pack in self.packs:
            if key in pack.slots or key in pack.records:
                name,image,_=pack.decode(key);image=image.convert('RGBA');self.dependencies.add(path)
                result=len(self.textures);self.tex_ids[key]=result
                self.textures.append(dict(name=name,width=image.width,height=image.height,rgbaDeflate=deflate(image.tobytes())))
                return result
        if not optional:self.unresolved.add(f'{key:08x}')
        self.tex_ids[key]=-1;return -1

    def shared_brake_texture(self,key):
        pack=shared_brake_pack()
        if pack is None or key not in pack.slots and key not in pack.records:return -1
        if self.tex_ids.get(key,-1)>=0:return self.tex_ids[key]
        name,image,_=pack.decode(key);image=image.convert('RGBA')
        index=len(self.textures);self.tex_ids[key]=index
        self.textures.append(dict(name='u2_shared_'+name,width=image.width,height=image.height,rgbaDeflate=deflate(image.tobytes())))
        self.adapted.add(f'{key:08x}: U2 GLOBAL/GlobalB shared brake atlas donor ({name}); matching original UV layout')
        return index

    def material(self,section,group):
        name=section['materialName'].upper();key=section['textureKey'];identity=(name,key,group)
        if identity in self.mat_ids:return self.mat_ids[identity]
        glass='WINDOW' in name or 'GLASS' in name
        if 'HEADLIGHT' in name:key=bin_hash(self.car+'_KIT00_HEADLIGHT_ON')
        elif 'BRAKELIGHT' in name:key=bin_hash(self.car+'_KIT00_BRAKELIGHT_ON')
        paint=name.startswith('BODY') and not any(x in name for x in ('CHROME','ALUMINUM','MOLDING','PLASTIC','CARBON','BOTTOM','BADGING'))
        generated={bin_hash(self.car+suffix) for suffix in ('_SKIN1','_SKIN2','_SKIN3','_WHEEL')}|{bin_hash(n) for n in ('GLOBAL_SKIN1','SPOILER_SKIN1','ROOF_SKIN','ROOF_SKIN1')}
        shared_brake=key in SHARED_BRAKE_KEYS
        disc=name.startswith('DISC') or 'ROTOR' in name
        # MW's shared brake packages have empty texture slots in this install.
        # Reuse its stock rotor artwork; retain each accessory's authored size.
        tex=-1 if glass or key in generated else self.texture(key,optional=shared_brake)
        if shared_brake and tex<0:tex=self.shared_brake_texture(key)
        if shared_brake and tex<0:
            tex=self.texture(bin_hash('ROTOR1')) if disc else -1
            self.adapted.add(f'{key:08x}: empty shared brake texture; stock MW rotor artwork or neutral solid hardware')
        if key in generated:self.generated.add(f'{key:08x}')
        colour=[1.,1.,1.,1.];metal=.08;smooth=.45;alpha='opaque'
        if paint:
            metal,smooth=.55,.7
            if tex<0:colour=[.68,.7,.73,1.]
        elif glass:
            colour,metal,smooth,alpha=([.75,.85,.9,.08] if 'HEADLIGHT' in name or 'BRAKELIGHT' in name else [.05,.08,.1,.42]),.1,.95,'blend'
        elif any(x in name for x in ('CHROME','SILVER','ALUMINUM','ROTOR','DISC')):metal,smooth=.85,.75
        elif name.startswith('TIRE'):smooth=.12
        elif 'CALIPER' in name:
            metal,smooth=.4,.55
            if tex<0:
                colour=[.18,.20,.22,1.]
                self.adapted.add(name+': generated MW caliper paint uses NFVU2 cast-metal tint')
        elif tex<0:colour=[.13,.13,.14,1.]
        if name=='DOORLINE' or any(tag in name for tag in ('BADGING','LICENSE_PLATE','DECAL')):alpha='blend'
        if disc or shared_brake and tex>=0:alpha='mask'
        index=len(self.materials);self.mat_ids[identity]=index
        self.materials.append(dict(name='mw_'+name.lower()+'_'+group,color=colour,metallic=metal,smoothness=smooth,
                                   texture=tex,alphaMode=alpha,bodyPaint=paint,wheelRubber=group=='wheel' and name.startswith('TIRE'),
                                   rimPaint=group=='wheel' and name.startswith('RIM') and 'PLASTIC' not in name))
        return index

    def compile(self,mesh,group='body',axle='',socket=None,option=None,material_override=None):
        result=[];number_axle=1 if axle=='rear' else 0
        if group=='wheel':
            radius,inner,outer=tyre_size(mesh);radial=self.radii[number_axle]/radius;axial=self.widths[number_axle]/(outer-inner)
        else:radial=axial=1;inner=0
        def vector(v,normal=False):
            if group=='body' and socket is None and not normal and mesh['name'].startswith(self.car+'_'):v=body_position(mesh,v)
            x,y,z=v
            if socket:x,y,z=(x*socket[a]+y*socket[4+a]+z*socket[8+a]+(0 if normal else socket[12+a]) for a in range(3))
            if group=='body':xyz=(y,z+(0 if normal else self.lift),x)
            elif group=='wheel':xyz=(-y/axial,z/radial,x/radial) if normal else (self.widths[number_axle]/2-(y-inner)*axial,z*radial,x*radial)
            else:xyz=(-y,z,x) if normal else (.04-y,z,x)
            if normal:
                length=math.sqrt(sum(c*c for c in xyz));xyz=tuple(c/length for c in xyz) if length else (0,1,0)
            return dict(zip(('x','y','z'),xyz))
        for n,s in enumerate(mesh['slices']):
            if not s['indices'] or s['materialName'].startswith('TIRE') and 'BACK' in s['materialName']:continue
            if group=='brake' and s['textureKey']==0x17f9f794 and s['materialName'].lower() in ('caliperdecal','texture bits'):
                material=self.material(s,group)
                if self.materials[material]['texture']<0:
                    self.adapted.add(mesh['name']+': unavailable alpha hardware decals omitted; original solid caliper retained')
                    continue
            ids=list(s['indices'])
            if group=='wheel':
                ids=[v for i in range(0,len(ids),3) if all(math.hypot(mesh['vertices'][v][0],mesh['vertices'][v][2])<=radius*1.012 for v in ids[i:i+3]) for v in ids[i:i+3]]
            if not ids:continue
            if group!='body':
                for i in range(0,len(ids),3):ids[i+1],ids[i+2]=ids[i+2],ids[i+1]
            used=sorted(set(ids));remap={old:new for new,old in enumerate(used)}
            role=('headlight' if 'HEADLIGHT' in s['materialName'] and 'BULB' in s['materialName'] else 'taillight' if 'BRAKELIGHT' in s['materialName'] and 'BULB' in s['materialName'] else '')
            coords=[dict(u=u if abs(u)<100 else u%1,v=1-v if abs(v)<100 else (1-v)%1) for u,v in mesh['uv']]
            if any(tag in s['materialName'].upper() for tag in ('BADGING','LICENSE_PLATE')):coords=badge_uv(mesh,s,coords)
            part=dict(name=f'{mesh["name"]}_{axle}_{n}',group=group,axle=axle,material=self.material(s,group) if material_override is None else material_override,lightRole=role,
                      rotatesWithWheel=group=='brake' and any(n in s['materialName'].upper() for n in ('ROTOR','DISC')),
                      vertices=[vector(mesh['vertices'][i]) for i in used],normals=[vector(mesh['normals'][i],True) for i in used],
                      uv=[coords[i] for i in used],
                      indices=[remap[i] for i in ids])
            size=re.search(r'_(17|18|19|20)_25_A$',mesh['name']) if group=='wheel' else None
            if size:
                sidewall=[mesh['vertices'][i] for section in mesh['slices'] if section['materialName'].startswith('TIRE') and 'SIDEWALL' in section['materialName'] for i in section['indices']]
                bead=min(math.hypot(v[0],v[2]) for v in sidewall)*radial
                part.update(rimInches=int(size[1]),wheelBead=bead,wheelOutside=self.radii[number_axle])
            if option:part.update(optionGroup='styling',option=option,name='Garage_'+option.replace(':','_')+'_'+part['name'])
            result.append(pack_geometry(part))
        return result

    def model(self,parts,**fields):
        if self.unresolved:raise ValueError('Missing MW textures: '+','.join(sorted(self.unresolved)))
        if len(self.textures)>128 or len(self.materials)>512 or len(parts)>2048:raise ValueError('MW asset library exceeds runtime limits')
        return dict(schemaVersion=3,preset='NFVU2_MW_'+self.car,parts=parts,materials=list(self.materials),textures=list(self.textures),**fields)
