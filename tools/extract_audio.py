from project import BASE, LOCAL, OUTPUT, disc_root, vgmstream_path
import argparse
from asset_source import extract_matching

parser=argparse.ArgumentParser(description='Extract local NFS vehicle audio; original data stays in .local.')
parser.add_argument('--turbo-only',action='store_true')
parser.add_argument('--car',action='append',default=[],help='engine bank to extract, matched by name (e.g. 350Z, Skyline)')
parser.add_argument('--gin',action='append',default=[],help='exact bank file under SOUND/ENGINE; cars share banks')
args=parser.parse_args()
cars=[car.upper() for car in args.car]
gins=[gin.upper() for gin in args.gin]
def wanted(upper):
    turbo=upper.startswith('SOUND/TURBO/TURBO_MED_') and upper.endswith('.ABK') or upper=='SOUND/EVT_SYS/UG2_TURBO.CSI'
    engine=upper.startswith('SOUND/ENGINE/') and upper.endswith('.GIN') and (any(car in upper for car in cars)
        or upper.rsplit('/',1)[-1] in gins)
    return turbo or (not args.turbo_only and (engine or upper.startswith('SOUND/SKIDS/') or upper in ('SOUND/IG_GLOBAL/ROADNOISE_00_MB.ABK','SOUND/IG_GLOBAL/FX_MAIN_MEM_MB.ABK')))
extracted=extract_matching(LOCAL/'nfs-assets',wanted)
for name in sorted(extracted):print(name)
if not extracted:raise SystemExit('No audio sources found. Check the local NFSU2 data configuration.')
