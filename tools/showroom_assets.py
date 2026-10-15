"""Read-only UG2 showroom import. Original meshes, sockets and animation samples.

Animation layout cross-checked with TVasya/PartAnimatorulator; no editor code
or original binary payload is distributed. Runtime playback duration is ours.
"""
import math
import re
import struct
from export_geometry import chunks, unpad
from nfs_formats import bin_hash

PIVOTS=['HOOD_REG','HOOD_FRONT','LEFT_HOOD_SPLIT','RIGHT_HOOD_SPLIT',
        'FRONT_HOOD_SPLIT','BACK_HOOD_SPLIT','LEFT_DOOR_REG','LEFT_DOOR_SCISSOR',
        'LEFT_DOOR_SUICIDE','RIGHT_DOOR_REG','RIGHT_DOOR_SCISSOR','RIGHT_DOOR_SUICIDE',
        'TRUNK_REG',*[f'{side}_DOOR_WIDE{i}' for side in ('LEFT','RIGHT') for i in range(1,5)]]
WANTED=['HOOD_REG','TRUNK_REG',*[f'{side}_DOOR_{style}' for side in ('LEFT','RIGHT') for style in ('REG','SUICIDE','SCISSOR')]]

def vector(v,lift=0):return dict(x=v[1],y=v[2]+lift,z=v[0])

def animations(path,lift,car):
    banks={};pivots=None;source=path.read_bytes()
    for ident,a,b in chunks(source):
        data=unpad(source[a:b])
        if ident==0x37100:
            if len(data)!=16+21*64 or struct.unpack_from('<II',data)!=(12,bin_hash(car)):
                raise ValueError('Unexpected vehicle pivot table')
            pivots={name:struct.unpack_from('<16f',data,16+i*64) for i,name in enumerate(PIVOTS)}
        elif ident==0xe34010:
            if data[:7]!=b'\x7fELF\x01\x01\x01':raise ValueError('Expected little-endian ELF32 animation')
            shoff=struct.unpack_from('<I',data,32)[0]
            section=struct.unpack_from('<10I',data,shoff+40)
            block=data[section[4]:section[4]+section[5]]
            name=re.search(re.escape(car.encode())+rb'_ZAM_([A-Z0-9_]+)_q\0',block)
            if not name:raise ValueError('Missing animation name')
            name=name[1].decode()
            if name not in WANTED:continue
            headers=list(re.finditer(bytes.fromhex('01000000010001001e303030'),block))
            if len(headers)!=2:raise ValueError('Expected quaternion and translation channels')
            values=[]
            for channel,h in enumerate(headers):
                kind,stride,count=struct.unpack_from('<3I',block,h.start()+16)
                if kind==1 or stride==0:values.append(None);continue # constant: use the pivot table
                if kind not in (2,3) or stride!=(4 if channel==0 else 3) or not 2<=count<=120:
                    raise ValueError('Unsupported animation channel')
                start=h.start()+(52 if kind==3 else 44)
                frames=[struct.unpack_from('<'+'f'*stride,block,start+i*stride*4) for i in range(count)]
                if any(not math.isfinite(x) or abs(x)>10 for frame in frames for x in frame):raise ValueError('Invalid animation samples')
                values.append(frames)
            banks[name]=values
    if pivots is None or set(banks)!=set(WANTED):raise ValueError('Missing showroom animations')
    result=[]
    for name in WANTED:
        matrix=pivots[name]
        basis=[matrix[i:i+3] for i in (0,4,8)]
        norms=[math.sqrt(sum(v*v for v in row)) for row in basis]
        if min(norms)<.1 or max(norms)>10 or any(abs(sum(a*b for a,b in zip(basis[i],basis[j])))>.001 for i in range(3) for j in range(i)):
            raise ValueError('Unsupported pivot basis: '+car+'/'+name)
        pivot=matrix[12:15];quats,positions=banks[name]
        if quats is None:raise ValueError('Missing rotation frames')
        if positions and len(positions)!=len(quats):raise ValueError('Mismatched motion channels')
        frames=[]
        rest=quats[0]
        inverse=(-rest[0],-rest[1],-rest[2],rest[3])
        def multiply(a,b):
            x,y,z,w=a;X,Y,Z,W=b
            return (w*X+x*W+y*Z-z*Y,w*Y-x*Z+y*W+z*X,w*Z+x*Y-y*X+z*W,w*W-x*X-y*Y-z*Z)
        for i,q in enumerate(quats):
            if abs(sum(c*c for c in q)-1)>.001:raise ValueError('Invalid animation quaternion')
            q=multiply(q,inverse)
            delta=[positions[i][j]-positions[0][j] for j in range(3)] if positions else (0,0,0)
            frames.append({'rotation':dict(x=q[1],y=q[2],z=q[0],w=q[3]),'offset':vector(delta)})
        result.append({'name':name,'pivot':vector(pivot,lift),'frames':frames})
    return result

def catalog(car,audio,pick,transform,source):
    selected=[];layouts=[];components=[]
    for level in range(4):
        name=f'{source}_KIT{level:02d}_TRUNK_AUDIO';part=pick(car,name)
        selected.append((part,'body',None,name))
        sockets=[]
        for i in range(6):
            key=bin_hash(f'MARKER_AUDIO_COMP_{i}')
            if key not in part['sockets']:continue
            m=part['sockets'][key]
            # Local audio vertices retain the NFS XY mounting plane; the socket
            # basis is converted to Unity along with the rest of the vehicle.
            sockets.append({'position':transform((0,0,0),m),
                'right':transform((1,0,0),m,normal=True),'up':transform((0,1,0),m,normal=True),
                'forward':transform((0,0,1),m,normal=True),'size':'LARGE' if level<3 else 'MEDIUM'})
        layouts.append({'title':('Stock' if level==0 else f'Build {level}'),'sockets':sockets})
    for size,inches in (('SMALL',8),('MEDIUM',12),('LARGE',15)):
        specs=[(f'AUDIO_COMP_SPEAKER{i}_{inches}',f'{inches}-inch Subwoofer {i}','speaker') for i in range(1,5)]
        specs += [(f'AUDIO_COMP_{kind}_{i}_{size}',f'{label} {i+1}',kind.lower()) for kind,label in (('AMP','Amplifier'),('LCD','Display')) for i in range(4)]
        specs.insert(0,(f'AUDIO_COMP_EMPTY_{size}','Cover','empty'))
        for name,label,kind in specs:
            selected.append((pick(audio,name),'audio',None,name))
            components.append({'id':name,'title':label,'kind':kind,'size':size})
    return selected,layouts,components
