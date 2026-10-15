from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
import json, math, re, struct
from pathlib import Path
from nfs_formats import bin_hash

UNASSIGNED = 0xffffffff

def restore_mesh_name(prefix, key):
    if bin_hash(prefix)==key:return prefix
    # Retail headers have multiple string limits (including 24 and 27). Only
    # accept a completion when it matches the original complete-name hash.
    endings=[suffix+lod for suffix in ('','_') for lod in 'ABCD']
    endings += [f'{part}_{lod}' for part in ('LEFT','RIGHT','HEADLIGHT_LEFT','HEADLIGHT_RIGHT',
                'BRAKELIGHT_LEFT','BRAKELIGHT_RIGHT','DOOR_PANEL_LEFT','DOOR_PANEL_RIGHT',
                'DOOR_SILL_LEFT','DOOR_SILL_RIGHT','LEFT_SIDE_MIRROR','RIGHT_SIDE_MIRROR') for lod in 'ABCD']
    endings += [f'{part}_{lod}' for part in ('R','T','HT','FT') for lod in 'ABCD']
    for ending in endings:
        for cut in range(len(ending)):
            if cut and not prefix.endswith(ending[:cut]):continue
            candidate=prefix+ending[cut:]
            if bin_hash(candidate)==key:return candidate
    return prefix

def chunks(data, start=0, end=None, depth=0):
    end = len(data) if end is None else end
    if depth > 20: raise ValueError('excessive nesting')
    while start + 8 <= end:
        ident, size = struct.unpack_from('<II', data, start)
        body = start + 8
        if body + size > end: raise ValueError(f'chunk exceeds parent at {start:x}')
        yield ident, body, body + size
        start = body + size

def leaves(data, start, end):
    for ident, a, b in chunks(data, start, end):
        if ident >> 28 == 8:
            yield from leaves(data, a, b)
        elif ident:
            yield ident, data[a:b]

def objects(data, start=0, end=None):
    for ident, a, b in chunks(data, start, end):
        if ident == 0x80134010:
            yield a, b
        elif ident >> 28 == 8:
            yield from objects(data, a, b)

def unpad(data, pair=False):
    offset = 0
    step = 2 if pair else 1
    filler = b'\x11' * step
    while data[offset:offset+step] == filler: offset += step
    return data[offset:]

def face_normals(vertices, indices):
    """Normals for vertex records that store none, accumulated from the faces."""
    accumulated = [[0., 0., 0.] for _ in vertices]
    for i in range(0, len(indices) - 2, 3):
        a, b, c = (vertices[indices[i + j]] for j in range(3))
        u = tuple(b[k] - a[k] for k in range(3)); v = tuple(c[k] - a[k] for k in range(3))
        face = (u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0])
        for index in indices[i:i + 3]:
            for k in range(3): accumulated[index][k] += face[k]
    normals = []
    for x, y, z in accumulated:
        length = math.sqrt(x * x + y * y + z * z)
        normals.append((x / length, y / length, z / length) if length > 1e-9 else (0., 1., 0.))
    return normals


def parse(path):
    data = Path(path).read_bytes()
    result = []
    for a,b in objects(data):
        table = dict(leaves(data,a,b))
        header = unpad(table.get(0x134011,b''))
        names = re.findall(rb'[A-Z0-9][A-Z0-9_]{4,}', header)
        names = [n for n in names if b'_' in n]
        if not names: continue
        name = max(names,key=len).decode()
        name=restore_mesh_name(name,struct.unpack_from('<I',header,16)[0])
        vb = unpad(table.get(0x134b01,b''))
        ib = unpad(table.get(0x134b03,b''),True)
        if not vb or len(ib)%2: continue
        indices = list(struct.unpack('<'+'H'*(len(ib)//2),ib))
        indices = indices[:len(indices)//3*3]
        # Vertex records are 36 bytes (position, normal, reserved, uv); a mesh
        # without normals stores 24 (position, reserved, uv), which the index
        # buffer only addresses at that stride.
        stride = next((s for s in (36, 24) if len(vb) % s == 0 and len(vb) // s > max(indices, default=-1)), None)
        if stride is None: continue
        vertices = [struct.unpack_from('<3f',vb,i) for i in range(0,len(vb),stride)]
        uv = [struct.unpack_from('<2f',vb,i+(28 if stride==36 else 16)) for i in range(0,len(vb),stride)]
        normals = [struct.unpack_from('<3f',vb,i+12) for i in range(0,len(vb),36)] if stride==36 else face_normals(vertices,indices)
        if any(i>=len(vertices) for i in indices): raise ValueError('invalid index '+name)
        if any(not math.isfinite(v) or abs(v)>100 for xyz in vertices for v in xyz): raise ValueError('invalid position '+name)
        slots = lambda ident: [struct.unpack_from('<I',table[ident],i)[0] for i in range(0,len(table.get(ident,b'')),8)]
        textures,materials=slots(0x134012),slots(0x134013)
        sm=unpad(table.get(0x134b02,b''),True); slices=[]; position=0
        if len(sm)%60: raise ValueError('Invalid material records '+name)
        for i in range(0,len(sm),60):
            count,tex,mat,start=[struct.unpack_from('<I',sm,i+j)[0] for j in (12,28,32,52)]
            # Index 255 marks an unassigned slot; meshes without normals (the
            # 24-byte format) carry no 0x134013 leaf at all.
            if start!=position or count%3 or start+count>len(indices) \
               or (tex>=len(textures) and tex!=255) or (mat>=len(materials) and mat!=255):
                raise ValueError('Invalid material partition '+name)
            slices.append({'indices':indices[start:start+count],'textureKey':textures[tex] if tex<len(textures) else UNASSIGNED,
                           'materialKey':materials[mat] if mat<len(materials) else UNASSIGNED});position+=count
        if position!=len(indices): raise ValueError('Incomplete material partition '+name)
        sockets={}; socket_data=unpad(table.get(0x13401a,b''))
        for i in range(0,len(socket_data),80):
            sockets[struct.unpack_from('<I',socket_data,i)[0]]=struct.unpack_from('<16f',socket_data,i+16)
        result.append({'name':name,'key':struct.unpack_from('<I',header,16)[0], 'vertices':vertices,'normals':normals,'uv':uv,'indices':indices,'slices':slices,'sockets':sockets})
    return result
