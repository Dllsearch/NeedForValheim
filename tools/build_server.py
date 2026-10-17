"""Build an asset-free dedicated-server payload, separate from artifacts/mod."""
import argparse,hashlib,json,shutil,zipfile
from pathlib import Path
from project import BASE,LOCAL,OUTPUT,valheim_dir,bepinex_dir
from build_support import standard_refs,compile_cs


def build(game_dir=None):
    game=Path(game_dir).resolve() if game_dir else valheim_dir()
    managed=game/('valheim_server_Data' if (game/'valheim_server_Data').is_dir() else 'valheim_Data')/'Managed';loader=bepinex_dir()
    refs=standard_refs()+[managed/(name+'.dll') for name in (
        'UnityEngine','UnityEngine.CoreModule','UnityEngine.PhysicsModule',
        'assembly_valheim','assembly_utils','Newtonsoft.Json')]
    refs += [loader/'core/BepInEx.dll',loader/'core/0Harmony.dll']
    missing=[str(path) for path in refs if not path.is_file()]
    if missing:raise SystemExit('Missing build references:\n'+'\n'.join(missing))
    catalog=LOCAL/'cars/cars.json'
    if not catalog.is_file():raise SystemExit('Run tools/build_catalog.py first.')
    root=OUTPUT/'server';payload=root/'BepInEx/plugins/NeedForValheim'
    sources=sorted((BASE/'server').glob('*.cs'))+[BASE/'src'/name for name in ('CarProfile.cs','VehiclePersistence.cs','VehicleStorageRules.cs','VehicleNetworkProtocol.cs','VehicleNetworkServer.cs','VehicleMotionTelemetry.cs','VehicleServerCollisions.cs','VehicleNetworkPlacement.cs')]
    dll=payload/'NeedForValheim.dll'
    compile_cs(dll,sources,refs,library=True,defines=('NFVU2_DEDICATED_SERVER',))
    profiles=json.loads(catalog.read_text(encoding='utf-8-sig'))
    # Chassis/capabilities/gear tables remain available for network validation;
    # the server neither opens nor distributes the referenced client assets.
    (payload/'cars.json').write_text(json.dumps(profiles,separators=(',',':')),encoding='utf-8')
    readme=root/'SERVER_README.txt'
    readme.write_text('NFVU2 dedicated-server payload\n\nRequires matching Valheim and BepInEx. Copy BepInEx into the server folder while stopped. Do not install this DLL alongside the client DLL.\n\nDevelopment multiplayer prototype: driver leases, motion, setup, roster and native cargo ownership are implemented. Real dedicated-server/client gameplay is not yet verified; this is not a multiplayer-ready release. Matching protocol version 3 clients are required.\nNo models, textures, audio or original game files are included.\n',encoding='utf-8')
    files=[dll,payload/'cars.json',readme]
    entries={p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
    manifest=root/'manifest.json';manifest.write_text(json.dumps({'kind':'headless-server','networkReady':False,'files':entries},indent=2),encoding='utf-8');files.append(manifest)
    archive=OUTPUT/'NFVU2-server.zip'
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
        for path in files:z.write(path,path.relative_to(root).as_posix())
    digest=hashlib.sha256(archive.read_bytes()).hexdigest()
    archive.with_suffix('.zip.sha256').write_text(digest+'  '+archive.name+'\n',encoding='ascii')
    print('Server package:',archive.name,archive.stat().st_size,'bytes;',len(profiles),'profiles; SHA-256',digest)
    return archive

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--game-dir',help='Compile against this installed dedicated-server directory instead of client references.')
    build(parser.parse_args().game_dir)
