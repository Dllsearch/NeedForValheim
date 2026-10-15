from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
import argparse
import json
from pathlib import Path
from asset_source import archives

# Shared part folders every car borrows from; the 350Z stays the default car.
SHARED = ('CARS/TEXTURES.BIN', 'CARS/WHEELS/', 'CARS/SPOILER/', 'CARS/SPOILER_HATCH/',
          'CARS/SPOILER_SUV/', 'CARS/BRAKES/', 'CARS/EXHAUST/', 'CARS/AUDIO/',
          'CARS/MIRRORS/', 'CARS/MIRRORS_BODY/', 'CARS/MIRRORS_POST/',
          'CARS/MIRRORS_SUV/', 'CARS/MIRRORS_HUMMER/', 'CARS/SPINNER/',
          'CARS/PLATES/', 'CARS/ROOF/', 'CARS/NEON/', 'CARS/SUV/', 'GLOBAL/')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--car', action='append', default=[],
                        help='car folder under CARS to import; repeatable, the 350Z is always included')
    args = parser.parse_args()
    prefixes = SHARED + tuple(f'CARS/{car.upper()}/' for car in args.car)

    base = LOCAL/'nfs-assets'
    base.mkdir(parents=True, exist_ok=True)
    inventory = []
    for label, archive in archives():
        for item in archive.infolist():
            name = item.filename.replace('\\', '/')
            inventory.append({'disc': label, 'name': name, 'size': item.file_size})
            upper = name.upper()
            wanted = upper.startswith(prefixes)
            if not wanted or item.is_dir() or upper.endswith(('.EXE', '.DLL')): continue
            target = (base / name).resolve()
            if not target.is_relative_to(base): raise ValueError('unsafe archive path')
            target.parent.mkdir(parents=True, exist_ok=True)
            data = archive.read(item)
            target.write_bytes(data)
            print('Extracted', name, len(data))
    (LOCAL/'zip_inventory.json').write_text(json.dumps(inventory, indent=2), encoding='utf-8')
    print('Archive entries:', len(inventory))

if __name__ == '__main__':
    main()
