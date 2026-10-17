"""Verify the real headless DLL and packaged bytes without launching Valheim."""
import argparse,hashlib,json,subprocess,zipfile
from project import BASE,OUTPUT
from build_server import build
from build_support import test_refs,compile_cs,runtime_config


def verify():
    archive=OUTPUT/'NFVU2-server.zip'
    expected=(OUTPUT/'NFVU2-server.zip.sha256').read_text(encoding='ascii').split()[0]
    if hashlib.sha256(archive.read_bytes()).hexdigest()!=expected:raise AssertionError('Archive checksum mismatch')
    with zipfile.ZipFile(archive) as z:
        manifest=json.loads(z.read('manifest.json'))
        if manifest['kind']!='headless-server' or manifest['networkReady'] is not False:raise AssertionError('Incorrect readiness metadata')
        names=set(z.namelist())
        allowed={'manifest.json','SERVER_README.txt','BepInEx/plugins/NeedForValheim/NeedForValheim.dll','BepInEx/plugins/NeedForValheim/cars.json'}
        if names!=allowed:raise AssertionError('Unexpected server payload files: '+str(names^allowed))
        for name,digest in manifest['files'].items():
            if hashlib.sha256(z.read(name)).hexdigest()!=digest:raise AssertionError('Packaged file checksum mismatch: '+name)
        profiles=json.loads(z.read('BepInEx/plugins/NeedForValheim/cars.json'))
        client=json.loads((OUTPUT/'mod/cars.json').read_text(encoding='utf-8-sig'))
        if profiles!=client:raise AssertionError('Server chassis/capability/gear data differs from client')
        if sum(i.file_size for i in z.infolist())>1024*1024:raise AssertionError('Server payload unexpectedly exceeds 1 MiB')
    out=OUTPUT/'tests/serverpackage/serverpackageTests.dll'
    refs,tfm,version=test_refs();compile_cs(out,[BASE/'tests/serverpackage/Program.cs'],refs);runtime_config(out,tfm,version)
    subprocess.run(['dotnet',str(out),str(OUTPUT/'server/BepInEx/plugins/NeedForValheim/NeedForValheim.dll')],check=True)
    print('Archive/file SHA-256, exact asset-free allowlist, client/server profile parity and size budget passed. Native server startup not tested.')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--skip-build',action='store_true');args=parser.parse_args()
    if not args.skip_build:build()
    verify()
