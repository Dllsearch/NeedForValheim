"""Prepare manual LAN-test launch and a small client DLL patch; start nothing."""
import argparse,hashlib,json,shutil,zipfile
from pathlib import Path
from install import checksum,process_names,prune_backups
from project import LOCAL,OUTPUT
from datetime import datetime


def prepare(stage_only=False):
    lab=LOCAL/'server-lab'
    if not (lab/'valheim_server.exe').is_file():raise SystemExit('Prepare the isolated server lab first.')
    if any(n.lower()=='valheim_server.exe' for n in process_names()):
        raise SystemExit('Stop the dedicated server before preparing its test payload.')
    source=OUTPUT/'network-client';stamp=json.loads((source/'build-variant.json').read_text(encoding='utf-8'))
    if stamp.get('variant')!='network' or stamp.get('sha256')!=checksum(source/'NeedForValheim.dll'):
        raise SystemExit('Rebuild the experimental client first.')
    manifest=json.loads((OUTPUT/'server/manifest.json').read_text(encoding='utf-8'))
    for name,digest in manifest['files'].items():
        path=(OUTPUT/'server'/name).resolve()
        if not path.is_relative_to((OUTPUT/'server').resolve()) or checksum(path)!=digest:
            raise SystemExit('Server payload checksum mismatch.')
    target=lab/'BepInEx/plugins/NeedForValheim';backup=lab/'plugin-backups'/datetime.now().strftime('%Y%m%d-%H%M%S-%f')
    backup.mkdir(parents=True)
    for name in ('NeedForValheim.dll','cars.json'):
        if (target/name).exists():shutil.copy2(target/name,backup/name)
        shutil.copy2(OUTPUT/'server/BepInEx/plugins/NeedForValheim'/name,target/name)
        if checksum(target/name)!=checksum(OUTPUT/'server/BepInEx/plugins/NeedForValheim'/name):raise RuntimeError('Lab copy checksum mismatch')
    if stage_only:
        prune_backups(backup.parent)
        print('Stopped LAN server payload updated and SHA-256 verified.')
        return
    fixture=lab/'BepInEx/plugins/NativeServerSmoke.dll'
    if fixture.exists():
        saved=lab/'test-fixtures/NativeServerSmoke.dll';saved.parent.mkdir(exist_ok=True)
        if saved.exists() and checksum(saved)!=checksum(fixture):raise SystemExit('A different disabled fixture already exists; preserve it before continuing.')
        shutil.copy2(fixture,saved)
        if checksum(saved)!=checksum(fixture):raise RuntimeError('Fixture backup checksum mismatch')
        fixture.unlink()
    # Reuse the private generated password, but use a fresh LAN world/save folder.
    args=json.loads((lab/'launch.json').read_text(encoding='utf-8'))
    for flag,value in (('-name','NFVU2 LAN test'),('-world','nfvu2_lan'),('-savedir',str(lab/'lan-worlds')),('-logfile',str(lab/'lan-test.log'))):
        args[args.index(flag)+1]=value
    if any(any(c in value for c in ('"','%','!','\r','\n')) for value in args):raise SystemExit('Unsafe command argument')
    command=' '.join('"'+value+'"' for value in args)
    (lab/'start_lan_test.cmd').write_text('@echo off\nsetlocal DisableDelayedExpansion\ncd /d "%~dp0"\nset SteamAppId=892970\nvalheim_server.exe '+command+'\npause\n',encoding='utf-8')
    password=args[args.index('-password')+1]
    (lab/'LAN_CONNECTION.txt').write_text('Private LAN test. Port2476, public0.\nHost connects to127.0.0.1:2476; laptop connects to host LAN IPv4:2476.\nPassword: '+password+'\nStop using Ctrl+C in the server console and wait for world save/shutdown.\nNo firewall rules were changed.\n',encoding='utf-8')
    archive=OUTPUT/'NFVU2-network-client-patch.zip'
    required={name:checksum(source/name) for name in ('cars.json','body.json')}
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
        z.write(source/'NeedForValheim.dll','BepInEx/plugins/NeedForValheim/NeedForValheim.dll')
        z.writestr('manifest.json',json.dumps({'variant':'network-prototype','protocol':3,'dllSha256':stamp['sha256'],'requiresInstalledAssets':required},indent=2))
        z.writestr('README.txt','Experimental protocol3 client DLL patch. Requires the full NFVU2 client assets and BepInEx. Close Valheim, back up the installed NeedForValheim.dll, then replace that same DLL. Do not install a second copy under another plugin folder. Both clients need this variant. Server uses the separate asset-free server payload. Settings and worlds are not included. Multiplayer acceptance remains pending.\n')
    archive.with_suffix('.zip.sha256').write_text(checksum(archive)+'  '+archive.name+'\n',encoding='ascii')
    prune_backups(backup.parent)
    print('Prepared manual launcher:',lab/'start_lan_test.cmd')
    print('Private connection details:',lab/'LAN_CONNECTION.txt')
    print('Client patch:',archive,'SHA-256:',checksum(archive))
    print('Test fixture disabled. No server/client launched; no installed gameplay DLL changed.')


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--stage-only',action='store_true',help='Update stopped lab payload only; do not rewrite launcher, fixture or patch.')
    prepare(parser.parse_args().stage_only)
