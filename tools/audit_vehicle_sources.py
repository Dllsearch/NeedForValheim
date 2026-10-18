"""Read-only inventory of authored vehicle markers and material contracts.

Writes metadata only to ignored artifacts; never copies original binaries or
assigns guessed names to unknown marker hashes. Geometry uses source NFS axes.
"""
import argparse,hashlib,json
from collections import Counter
from project import LOCAL,OUTPUT,mw_root
from export_car import SPECS,source_name
from export_geometry import parse as parse_u2
from mw_geometry import parse as parse_mw

KNOWN={0xc93b73fd:'spoiler',0x90c81258:'roof',0xbcf8a18b:'exhaust',0xbd7cf15e:'exhaust',
       0x9db90133:'headlight',0xd09091c6:'headlight',0x31a66786:'taillight',0xbf700a79:'taillight',0xe3de69db:'nos_purge_left',0x601a6cce:'nos_purge_right'}
from nfs_formats import bin_hash
for area,count in (('FRONT',3),('SIDE',2),('REAR',2)):
    for i in range(count):
        for endpoint in ('START','END'):
            name=f'{area}_NEON_{i}_{endpoint}';KNOWN[bin_hash(name)]=name.lower()

def audit(ident):
    mw=ident.startswith('mw-')
    car=ident[3:].upper() if mw else source_name(ident)
    relative='CARS/'+car+'/GEOMETRY.BIN'
    path=(mw_root() if mw else LOCAL/'nfs-assets')/relative
    meshes=(parse_mw if mw else parse_u2)(path)
    markers=[];surfaces=Counter()
    for mesh in meshes:
        for key,matrix in mesh['sockets'].items():
            markers.append(dict(mesh=mesh['name'],hash=f'{key:08x}',role=KNOWN.get(key,'unresolved'),matrix=list(matrix)))
        for section in mesh['slices']:
            label=(section.get('materialName',''),section.get('materialKey'),section.get('effect'),section.get('textureKey'))
            surfaces[label]+=1
    return dict(id=ident,sourceGame='mw' if mw else 'u2',sourceFile=relative,
        sha256=hashlib.sha256(path.read_bytes()).hexdigest(),meshCount=len(meshes),markers=markers,
        materialContracts=[dict(name=n,materialKey=k,effect=e,textureKey=t,sections=count) for (n,k,e,t),count in surfaces.items()],
        neonSolids=[m['name'] for m in meshes if 'NEON' in m['name']],
        limits='Raw source metadata only. Unknown hashes are not named; shader binaries and native rendering parameters are not reconstructed.')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('vehicles',nargs='+');args=parser.parse_args()
    output=OUTPUT/'vehicle-source-audit';output.mkdir(parents=True,exist_ok=True)
    for ident in args.vehicles:
        if ident not in SPECS and not ident.startswith('mw-'):raise SystemExit('Unknown U2 vehicle: '+ident)
        if not ident.replace('-','').isalnum():raise SystemExit('Invalid vehicle ID')
        record=audit(ident);(output/(ident+'.json')).write_text(json.dumps(record,indent=2),encoding='utf-8')
        print(ident,record['meshCount'],'meshes;',len(record['markers']),'markers;',len(record['materialContracts']),'material contracts;',len(record['neonSolids']),'neon solids')
