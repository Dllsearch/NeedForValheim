import argparse,hashlib,json,shutil
from project import BASE,LOCAL,OUTPUT,valheim_dir,bepinex_dir
from build_support import standard_refs,compile_cs

def build(compile_only=False,experimental_network=False):
    game=valheim_dir();managed=game/'valheim_Data/Managed'
    refs=standard_refs()
    for name in ('UnityEngine','UnityEngine.CoreModule','UnityEngine.ImageConversionModule','Unity.InputSystem','Unity.TextMeshPro','UnityEngine.UI','UnityEngine.UIModule','UnityEngine.AnimationModule','UnityEngine.PhysicsModule','UnityEngine.VehiclesModule','UnityEngine.InputLegacyModule','UnityEngine.IMGUIModule','UnityEngine.AudioModule','UnityEngine.ParticleSystemModule','UnityEngine.UnityWebRequestModule','UnityEngine.UnityWebRequestAudioModule','UnityEngine.TextRenderingModule','Newtonsoft.Json'):
        refs.append(managed/(name+'.dll'))
    refs.append(managed/'UnityEngine.AssetBundleModule.dll')
    loader=bepinex_dir()
    refs.append(loader/'core/BepInEx.dll')
    refs.append(loader/'core/0Harmony.dll')
    refs.append(managed/'assembly_valheim.dll')
    refs.append(managed/'assembly_utils.dll')
    refs.append(managed/'assembly_guiutils.dll')
    refs.append(managed/'Splatform.dll')
    missing=[str(p) for p in refs if not p.exists()]
    if missing:raise SystemExit('Missing installed game/mod-loader references:\n'+'\n'.join(missing))
    audio=sorted((LOCAL/'audio-assets').glob('*.wav'))
    if not compile_only and len(audio)!=19:raise SystemExit('Run the audio extraction, decoding and preparation scripts first (19 WAV resources required).')
    model=LOCAL/'model/body.json'
    catalog=LOCAL/'cars/cars.json'
    if not compile_only and not model.exists():raise SystemExit('Run tools/export_car.py 350z first to generate body.json.')
    if not compile_only and not catalog.exists():raise SystemExit('Run tools/build_catalog.py first to generate cars.json.')
    output=OUTPUT/('network-client' if experimental_network else 'mod')/'NeedForValheim.dll'
    shader=OUTPUT/'car-shader/nfvu2-car-shaders'
    verified=OUTPUT/'car-shader/verified-build.json'
    embedded=[]
    if shader.exists() and verified.exists():
        record=json.loads(verified.read_text(encoding='utf-8'))
        if hashlib.sha256(shader.read_bytes()).hexdigest()!=record['sha256']:
            raise SystemExit('Vehicle shader bundle checksum mismatch; rebuild it.')
        embedded=[(shader,'NeedForValheim.CarShaders')]
    compile_cs(output,sorted((BASE/'src').glob('*.cs')),refs,audio,library=True,defines=("NFVU2_EXPERIMENTAL_NETWORK",) if experimental_network else (),named_resources=embedded)
    if compile_only:
        print('Compiled without embedded assets (not installable):',output)
        return output
    shutil.copy2(model,output.parent/'body.json')
    shutil.copy2(catalog,output.parent/'cars.json')
    # Long stereo songs and optional original turbo clips stay external.
    from mod_files import sync_optional_assets
    sync_optional_assets(LOCAL,output.parent)
    (output.parent/'build-variant.json').write_text(json.dumps({
        'variant':'network' if experimental_network else 'singleplayer',
        'sha256':hashlib.sha256(output.read_bytes()).hexdigest()
    },indent=2),encoding='utf-8')
    print('Built:',output)
    return output

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--compile-only',action='store_true',help='Check that src compiles; skip asset requirements.')
    parser.add_argument("--experimental-network",action="store_true",help="Enable unfinished multiplayer prototype; not for gameplay installs.")
    args=parser.parse_args()
    build(args.compile_only,args.experimental_network)
