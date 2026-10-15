"""Report what one NFSU2 car folder provides, to plan a model export.

Prints the mesh families, the texture coverage of their material slices, the
shared-part families the car references and the wheel-locator candidates in the
decompressed global record. Read-only: nothing is written outside `.local`.
"""
import argparse
import struct
from collections import Counter

from project import LOCAL
from export_geometry import parse
from nfs_formats import TexturePack, jdlz, bin_hash

ASSETS = LOCAL / 'nfs-assets'
LOD_SUFFIXES = ('_A', '_B', '_C', '_D')


def family(name):
    for suffix in LOD_SUFFIXES:
        if name.endswith(suffix):
            return name[:-len(suffix)]
    return name


def global_data():
    path = LOCAL / 'global-decompressed.bin'
    if path.is_file():
        return path.read_bytes()
    return jdlz((ASSETS / 'GLOBAL/GlobalB.lzc').read_bytes())


def texture_pool(car):
    pool = {}
    sources = [ASSETS / f'CARS/{car}/TEXTURES.BIN', ASSETS / 'CARS/TEXTURES.BIN',
               ASSETS / 'CARS/WHEELS/TEXTURES.BIN', LOCAL / 'global-decompressed.bin',
               ASSETS / 'GLOBAL/InGameCommon.bun', ASSETS / 'GLOBAL/GlobalTexturesNA.bin',
               ASSETS / 'GLOBAL/DYNTEX.BIN']
    for source in sources:
        if not source.is_file():
            continue
        try:
            pack = TexturePack(source)
        except Exception as error:
            print('texture pack unreadable', source.name, error)
            continue
        keys = set(pack.slots) | set(pack.records)
        print(f'  pack {source.name:32s} {len(keys)} textures')
        for key in keys:
            pool.setdefault(key, source.name)
    return pool


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('car', help='car folder name under CARS, e.g. SKYLINE')
    parser.add_argument('--shared', default='WHEELS,BRAKES,SPOILER,EXHAUST,MIRRORS_SUV',
                        help='shared part folders to scan for families of this car')
    parser.add_argument('--locators', nargs='?', const=4, type=int,
                        help='dump wheel-locator candidates from the global record')
    args = parser.parse_args()
    car = args.car.upper()

    parts = parse(ASSETS / f'CARS/{car}/GEOMETRY.BIN')
    print(f'{car}: {len(parts)} meshes')
    groups = Counter(family(p['name']) for p in parts)
    for name in sorted(groups):
        meshes = [p for p in parts if family(p['name']) == name]
        triangles = sum(len(p['indices']) // 3 for p in meshes)
        sockets = sum(len(p['sockets']) for p in meshes)
        print(f'  {name:44s} {len(meshes)} lod, {triangles:7d} tri, {sockets} sockets')

    pool = texture_pool(car)
    keys = {s['textureKey'] for p in parts for s in p['slices'] if s['textureKey']}
    missing = sorted(k for k in keys if k not in pool)
    print(f'textures: {len(keys)} referenced, {len(keys) - len(missing)} resolvable, {len(missing)} missing')
    for key in missing[:15]:
        print(f'  missing {key:08x} ({bin_hash.__name__})')

    for folder in [f.strip().upper() for f in args.shared.split(',') if f.strip()]:
        for geometry in (ASSETS / f'CARS/{folder}').glob('GEOMETRY*.BIN'):
            try:
                shared = parse(geometry)
            except Exception:
                continue
            hits = sorted({family(p['name']) for p in shared if family(p['name']).startswith(car)})
            if hits:
                print(f'{geometry.name}: {len(hits)} {car} families', ', '.join(hits[:8]))

    if args.locators:
        data = global_data()
        record = data.find(f'{car}\0'.encode())
        if record < 0:
            print('no global record for', car)
            return
        print(f'global record at {record:x}')
        for offset in range(0x100, 0x200, 0x30):
            triples = [struct.unpack_from('<3f', data, record + offset + i * 0x30) for i in range(args.locators)]
            plausible = all(0.7 < abs(y) < 1.1 and 1.2 < abs(x) < 2.0 and -0.1 < z < 0.3 for x, y, z in triples)
            print(f'  +{offset:#04x} {"wheel-locator shaped" if plausible else "  ":20s}',
                  ' '.join(f'({x:.4f},{y:.4f},{z:.4f})' for x, y, z in triples))


if __name__ == '__main__':
    main()
