"""Prepare manual LAN-test launch and a small client DLL patch; start nothing."""
import argparse,hashlib,json,shutil,zipfile
from pathlib import Path
from install import checksum,process_names,prune_backups
from project import LOCAL,OUTPUT
from datetime import datetime


def package_client(include_u2_assets=False):
    source=OUTPUT/'network-client';stamp=json.loads((source/'build-variant.json').read_text(encoding='utf-8'))
    if stamp.get('variant')!='network' or stamp.get('sha256')!=checksum(source/'NeedForValheim.dll'):
        raise SystemExit('Rebuild the experimental client first.')
    names={'NeedForValheim.dll'}
    if include_u2_assets:
        profiles=json.loads((source/'cars.json').read_text(encoding='utf-8-sig'))
        u2=[p for p in profiles if p.get('sourceGame','u2')=='u2']
        if len(u2)!=30:raise SystemExit('Expected the complete 30-car U2 catalog.')
        names.add('cars.json')
        for profile in u2:names.update((profile['assets']['modelFile'],'styling/'+profile['id']+'.json'))
    for name in names:
        path=(source/name).resolve()
        if not path.is_relative_to(source.resolve()) or not path.is_file() or name!='NeedForValheim.dll' and path.suffix!='.json':
            raise SystemExit('Invalid/missing client patch input: '+name)
    archive=OUTPUT/('NFVU2-network-U2-update.zip' if include_u2_assets else 'NFVU2-network-client-patch.zip')
    temporary=archive.with_suffix('.zip.tmp');prefix='BepInEx/plugins/NeedForValheim/'
    digests={prefix+name:checksum(source/name) for name in sorted(names)}
    required={} if include_u2_assets else {name:checksum(source/name) for name in ('cars.json','body.json')}
    readme=('Experimental protocol3 client update. Requires the full NFVU2 client assets and BepInEx. '
            'Close Valheim before updating. Back up every matching installed file listed in manifest.json, '
            'then extract the BepInEx folder into the existing Valheim installation. Do not create a duplicate plugin. '
            'Both clients need this variant. Server uses the separate asset-free server payload. '
            'Settings, worlds, music and MW assets are not included. Multiplayer acceptance remains pending.\n')
    if include_u2_assets:readme+='Includes all 30 updated U2 model and styling JSON files, including native neon and NOS purge markers. This is still an update, not a full client installation.\n'
    with zipfile.ZipFile(temporary,'w',zipfile.ZIP_DEFLATED) as z:
        for name in sorted(names):z.write(source/name,prefix+name)
        z.writestr('manifest.json',json.dumps({'variant':'network-prototype','protocol':3,'dllSha256':stamp['sha256'],
            'requiresFullClientAssets':True,'includesU2Assets':include_u2_assets,'requiresInstalledAssets':required,'files':digests},indent=2))
        z.writestr('README.txt',readme)
    with zipfile.ZipFile(temporary) as z:
        if set(z.namelist())!=set(digests)|{'manifest.json','README.txt'}:raise RuntimeError('Unexpected client patch files')
        for name,digest in digests.items():
            with z.open(name) as stream:
                if hashlib.file_digest(stream,'sha256').hexdigest()!=digest:raise RuntimeError('Packaged file checksum mismatch: '+name)
    temporary.replace(archive)
    archive.with_suffix('.zip.sha256').write_text(checksum(archive)+'  '+archive.name+'\n',encoding='ascii')
    print('Client patch:',archive,'|',len(names),'payload files |',round(archive.stat().st_size/1048576,1),'MiB | SHA-256:',checksum(archive))
    return archive


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
    archive=package_client()
    prune_backups(backup.parent)
    print('Prepared manual launcher:',lab/'start_lan_test.cmd')
    print('Private connection details:',lab/'LAN_CONNECTION.txt')
    print('Client patch:',archive,'SHA-256:',checksum(archive))
    print('Test fixture disabled. No server/client launched; no installed gameplay DLL changed.')


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--stage-only',action='store_true',help='Update stopped lab payload only; do not rewrite launcher, fixture or patch.')
    parser.add_argument('--package-only',action='store_true',help='Package verified client build only; never touch the server lab or installed files.')
    parser.add_argument('--include-u2-assets',action='store_true',help='With package-only: include all 30 current U2 model/styling JSON files, no music or MW assets.')
    args=parser.parse_args()
    if args.package_only and args.stage_only:parser.error('Choose package-only or stage-only')
    if args.include_u2_assets and not args.package_only:parser.error('include-u2-assets requires package-only')
    if args.package_only:package_client(args.include_u2_assets)
    else:prepare(args.stage_only)
