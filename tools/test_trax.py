"""Targeted radio/menu validation and isolated full-source compile; never builds mod/."""
import shutil,subprocess
from project import BASE,LOCAL,valheim_dir,bepinex_dir
from build_support import compile_cs,test_refs,runtime_config,standard_refs

def main():
    out=BASE/'artifacts/trax-checks';out.mkdir(parents=True,exist_ok=True);refs,tfm,version=test_refs();managed=valheim_dir()/'valheim_Data/Managed'
    jsonlib=managed/'Newtonsoft.Json.dll';shutil.copy2(jsonlib,out/jsonlib.name)
    exe=out/'radio-tests.dll';sources=[BASE/'tests/radio/Program.cs']+[BASE/'src'/n for n in ('RadioPlaybackState.cs','RadioTrax.cs','RadioAcoustics.cs')]
    compile_cs(exe,sources,refs+[jsonlib]);runtime_config(exe,tfm,version);subprocess.run(['dotnet',str(exe)],check=True)
    names=('UnityEngine','UnityEngine.CoreModule','Unity.InputSystem','Unity.TextMeshPro','UnityEngine.UI','UnityEngine.UIModule','UnityEngine.AnimationModule','UnityEngine.PhysicsModule','UnityEngine.VehiclesModule','UnityEngine.InputLegacyModule','UnityEngine.IMGUIModule','UnityEngine.AudioModule','UnityEngine.ParticleSystemModule','UnityEngine.UnityWebRequestModule','UnityEngine.UnityWebRequestAudioModule','UnityEngine.TextRenderingModule','UnityEngine.ImageConversionModule','Newtonsoft.Json','assembly_valheim','assembly_utils','assembly_guiutils','Splatform')
    allrefs=standard_refs()+[managed/(n+'.dll') for n in names]+[bepinex_dir()/'core/BepInEx.dll',bepinex_dir()/'core/0Harmony.dll']
    compile_cs(out/'NeedForValheim.dll',sorted((BASE/'src').glob('*.cs')),allrefs,library=True)
    menu=out/'menu-tests.dll';compile_cs(menu,[BASE/'tests/menu/Program.cs'],refs+[jsonlib,out/'NeedForValheim.dll']);runtime_config(menu,tfm,version);subprocess.run(['dotnet',str(menu),str(LOCAL/'menu')],check=True)
    print('Isolated current-source compile passed. No game started; no installed files changed.')
if __name__=='__main__':main()
