"""Switch only the client DLL after matching the already-installed core assets.

Default is a read-only plan. --apply explicitly activates the requested build.
Never starts/stops Valheim or changes settings, assets, worlds or other plugins.
"""
import argparse,json,shutil
from datetime import datetime
from pathlib import Path
from install import checksum,contained,prune_backups,require_closed
from project import OUTPUT,valheim_dir


def activate(variant,apply=False):
    source=OUTPUT/('network-client' if variant=='network' else 'mod')
    destination=valheim_dir()/'BepInEx/plugins/NeedForValheim'
    dll=Path('NeedForValheim.dll')
    stamp=source/'build-variant.json'
    if not stamp.is_file():raise SystemExit('Rebuild this variant first; no verified build stamp exists.')
    info=json.loads(stamp.read_text(encoding='utf-8'))
    if info.get('variant')!=variant or info.get('sha256')!=checksum(source/dll):
        raise SystemExit('Build variant/checksum mismatch. Rebuild before activation.')
    for name in (dll,Path('cars.json'),Path('body.json')):
        contained(destination,name)
        if not (destination/name).is_file():raise SystemExit('Install the full standard client payload first: '+str(name))
    for name in ('cars.json','body.json'):
        if checksum(source/name)!=checksum(destination/name):
            raise SystemExit('Installed core assets differ: '+name+'. Use the full installer before DLL-only activation.')
    print('Variant:',variant,'| source SHA-256:',info['sha256'])
    print('Destination:',destination/dll)
    if not apply:
        print('Read-only plan; pass --apply with Valheim closed to activate.')
        return
    require_closed()
    backup=OUTPUT/'client-variant-backups'/datetime.now().strftime('%Y%m%d-%H%M%S-%f')
    backup.mkdir(parents=True,exist_ok=False)
    shutil.copy2(destination/dll,backup/dll)
    original=checksum(backup/dll)
    try:
        require_closed()
        if checksum(source/dll)!=info['sha256']:raise RuntimeError('Source changed after validation')
        shutil.copy2(source/dll,destination/dll)
        if checksum(destination/dll)!=info['sha256']:raise RuntimeError('Installed client checksum mismatch')
    except Exception:
        require_closed();shutil.copy2(backup/dll,destination/dll)
        if checksum(destination/dll)!=original:raise RuntimeError('Client rollback checksum mismatch')
        raise
    prune_backups(backup.parent)
    print('Activated and SHA-256 verified. Backup:',backup)


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('variant',choices=('singleplayer','network'))
    parser.add_argument('--apply',action='store_true',help='Replace installed DLL, with game-closed guard and backup; default only checks.')
    args=parser.parse_args();activate(args.variant,args.apply)
