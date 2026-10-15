"""Read PC Most Wanted (2005) solids, independently of the U2 mesh layout.

Format references and limitations: docs/MW_IMPORT.md. Material names, repeated
vertex buffers and their effect boundaries are retained instead of collapsed.
"""
import math
import struct
from pathlib import Path

from export_geometry import chunks, objects


def leaf_ranges(data, start, end):
    for tag, a, b in chunks(data, start, end):
        if tag & 0x80000000:
            yield from leaf_ranges(data, a, b)
        elif tag:
            yield tag, a, b


def aligned(data, a, b, alignment):
    begin = (a + alignment - 1) & -alignment
    if begin > b or any(v != 0x11 for v in data[a:begin]):
        raise ValueError(f'Invalid MW alignment at {a:#x}')
    return data[begin:b]


def parse(path):
    data = Path(path).read_bytes()
    result = []
    for a, b in objects(data):
        records = list(leaf_ranges(data, a, b))
        table = {tag: (s, e) for tag, s, e in records}
        if 0x134011 not in table:
            raise ValueError('MW solid has no header')
        header = aligned(data, *table[0x134011], 16)
        if len(header) < 161 or header[12] != 0x16 or header[13]:
            raise ValueError('Unsupported MW solid header (PC little-endian expected)')
        name = header[160:].split(b'\0', 1)[0].decode('ascii')
        if 0x134b02 not in table:
            continue
        groups = aligned(data, *table[0x134b02], 16)
        if len(groups) % 104:
            raise ValueError('Invalid MW material records: ' + name)
        texture_data = data[slice(*table[0x134012])]
        if len(texture_data) % 8:
            raise ValueError('Invalid MW texture slots: ' + name)
        textures = [struct.unpack_from('<I', texture_data, i)[0] for i in range(0, len(texture_data), 8)]
        names = [data[s:e].split(b'\0', 1)[0].decode('ascii') for tag, s, e in records if tag == 0x134c02]
        buffers = [aligned(data, s, e, 128) for tag, s, e in records if tag == 0x134b01]
        if not buffers:
            continue
        materials = []
        stream = 0
        last_effect = None
        for i in range(0, len(groups), 104):
            effect = struct.unpack_from('<I', groups, i + 48)[0]
            if last_effect is not None and effect != last_effect:
                stream += 1
            slot = groups[i + 24]
            if slot >= len(textures):
                raise ValueError('Invalid MW diffuse slot: ' + name)
            vertices, tris = struct.unpack_from('<II', groups, i + 60)
            materials.append(dict(stream=stream, effect=effect, count=tris * 3, vertexCount=vertices,
                                  textureKey=textures[slot], materialName=names[i // 104] if i // 104 < len(names) else f'effect_{effect}'))
            last_effect = effect
        if len(buffers) != stream + 1:
            raise ValueError('MW effect/vertex stream mismatch: ' + name)
        vertices, normals, uv, offsets = [], [], [], []
        for number, buffer in enumerate(buffers):
            members = [m for m in materials if m['stream'] == number]
            count = sum(m['vertexCount'] for m in members)
            if not count or len(buffer) % count:
                raise ValueError('Invalid MW vertex count: ' + name)
            stride = len(buffer) // count
            expected = {0: 36, 1: 60, 2: 60, 3: 60, 4: 36, 5: 36, 6: 36, 19: 44}.get(members[0]['effect'])
            if expected != stride:
                raise ValueError(f'Unsupported MW effect/stride {members[0]["effect"]}/{stride}: {name}')
            offsets.append(len(vertices))
            for i in range(0, len(buffer), stride):
                vertices.append(struct.unpack_from('<3f', buffer, i))
                normals.append(struct.unpack_from('<3f', buffer, i + 12))
                uv.append(struct.unpack_from('<2f', buffer, i + 28))
        ib = aligned(data, *table[0x134b03], 16)
        if len(ib) % 2:
            raise ValueError('Invalid MW indices: ' + name)
        indices = list(struct.unpack('<' + 'H' * (len(ib) // 2), ib))
        slices, cursor, all_indices = [], 0, []
        for material in materials:
            end = cursor + material['count']
            stream = material['stream']
            capacity = len(vertices) - offsets[stream] if stream + 1 == len(offsets) else offsets[stream + 1] - offsets[stream]
            section = indices[cursor:end]
            if end > len(indices) or any(i >= capacity for i in section):
                raise ValueError('Invalid MW material index range: ' + name)
            section = [i + offsets[stream] for i in section]
            slices.append(dict(material, indices=section))
            all_indices.extend(section)
            cursor = end
        # Retail chunks round an odd index count up to a 32-bit boundary.
        if len(indices) != cursor + (cursor % 2) or any(indices[cursor:]):
            raise ValueError('Incomplete MW index partition: ' + name)
        if any(not math.isfinite(v) or abs(v) > 100 for p in vertices + normals for v in p) or any(not math.isfinite(v) for p in uv for v in p):
            raise ValueError('Invalid MW coordinates: ' + name)
        sockets = {}
        if 0x13401a in table:
            markers = aligned(data, *table[0x13401a], 16)
            if len(markers) % 80:
                raise ValueError('Invalid MW markers: ' + name)
            sockets = {struct.unpack_from('<I', markers, i)[0]: struct.unpack_from('<16f', markers, i + 16)
                       for i in range(0, len(markers), 80)}
        result.append(dict(name=name, key=struct.unpack_from('<I', header, 16)[0],
                           pivot=struct.unpack_from('<16f', header, 64), vertices=vertices,
                           normals=normals, uv=uv, indices=all_indices, slices=slices, sockets=sockets))
    if not result:
        raise ValueError('No uncompressed MW PC solids; compressed/console geometry is not supported')
    return result
