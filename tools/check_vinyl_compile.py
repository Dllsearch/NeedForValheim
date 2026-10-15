"""Compile current sources into an isolated artifact without touching mod payloads."""
from pathlib import Path
from project import BASE,valheim_dir,bepinex_dir
from build_support import compile_cs,standard_refs
managed=valheim_dir()/'valheim_Data/Managed'
names=('UnityEngine','UnityEngine.CoreModule','Unity.InputSystem','Unity.TextMeshPro','UnityEngine.UI','UnityEngine.UIModule','UnityEngine.AnimationModule','UnityEngine.PhysicsModule','UnityEngine.VehiclesModule','UnityEngine.InputLegacyModule','UnityEngine.IMGUIModule','UnityEngine.AudioModule','UnityEngine.ParticleSystemModule','UnityEngine.UnityWebRequestModule','UnityEngine.UnityWebRequestAudioModule','UnityEngine.TextRenderingModule','Newtonsoft.Json','assembly_valheim','assembly_utils','assembly_guiutils')
refs=standard_refs()+[managed/(n+'.dll') for n in names]+[bepinex_dir()/'core/BepInEx.dll',bepinex_dir()/'core/0Harmony.dll']
compile_cs(BASE/'artifacts/vinyl-checks/NeedForValheim.dll',sorted((BASE/'src').glob('*.cs')),refs,library=True)
print('Isolated source compile passed; not an installable payload.')
