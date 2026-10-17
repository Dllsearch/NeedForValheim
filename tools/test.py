import argparse,shutil,subprocess,sys
from pathlib import Path
from project import BASE,LOCAL,OUTPUT,valheim_dir,bepinex_dir
from build import build
from build_support import compile_cs,test_refs,runtime_config

def main():
    suites=['model','mesh','handling','audio','radio','nitro','impact','showroom','driving','turret','targeting','terrain','blast','fleet','vehicle_assets','menu','cabin','flight','loading','lighting','controls','hotbar','rumble','nitroview','horde','hordehull','hordecorpses','turretfireaudio','hordeweather','horderadar','hordeelements','storage','carprofile','streaming']
    parser=argparse.ArgumentParser();parser.add_argument('--skip-build',action='store_true');parser.add_argument('--handling-only',action='store_true')
    suites.extend(('wheelsize','mw','garageprogression','surface','mapmarkers','vehicle_safety','cabin_entry','network','networksettings','networkintegration'))
    parser.add_argument('--suite',action='append',choices=suites,help='Run only these C# suites; repeat to combine.')
    parser.add_argument('--vehicle-assets-root',type=Path,default=LOCAL,help='generated vehicle profiles/models for the vehicle_assets suite')
    args=parser.parse_args()
    if not args.handling_only and not args.skip_build:build()
    if not args.handling_only and not args.suite:
        subprocess.run([sys.executable,'-m','unittest','discover','-s',str(BASE/'tests'),'-p','test_*.py'],check=True)
    references,tfm,version=test_refs()
    for suite in (['handling'] if args.handling_only else args.suite or suites):
        out=OUTPUT/'tests'/suite;out.mkdir(parents=True,exist_ok=True)
        sources=list((BASE/'tests'/suite).glob('*.cs'));refs=list(references);arguments=[]
        if suite=='handling':sources.extend(BASE/'src'/name for name in ('DrivingMath.cs','VehicleHandling.cs','VehicleDriftTuning.cs','SuvTuning.cs','MwChassisTuning.cs','VehicleRideHeight.cs','PerformanceTuning.cs'))
        elif suite=='network':sources.extend(BASE/'src'/name for name in ('VehicleNetworkProtocol.cs','VehicleNetworkClient.cs'))
        elif suite=='networkintegration':
            sources.extend(BASE/'src'/name for name in ('CarProfile.cs','VehiclePersistence.cs','VehicleStorageRules.cs','VehicleNetworkProtocol.cs','VehicleNetworkClient.cs','VehicleNetworkServer.cs','VehicleMotionTelemetry.cs'))
            arguments=[str(OUTPUT/'mod')]
        elif suite=='networksettings':
            sources.append(BASE/'src/NetworkVehicleSettings.cs')
            configdll=out/'BepInEx.dll';shutil.copy2(bepinex_dir()/'core/BepInEx.dll',configdll);refs.append(configdll)
            for name in ('UnityEngine.dll','UnityEngine.CoreModule.dll'):
                target=out/name;shutil.copy2(valheim_dir()/'valheim_Data/Managed'/name,target);refs.append(target)
        elif suite=='mesh':sources.append(BASE/'src/VehicleMesh.cs')
        elif suite=='streaming':sources.extend(BASE/'src'/name for name in ('VehicleWorldStreaming.cs','VehicleParkingRules.cs'))
        elif suite=='garageprogression':sources.append(BASE/'src/GarageProgression.cs')
        elif suite=='surface':sources.append(BASE/'src/VehicleSurfaceRules.cs')
        elif suite=='mapmarkers':sources.append(BASE/'src/VehicleMapMarkers.cs')
        elif suite=='vehicle_safety':sources.extend(BASE/'src'/name for name in ('VehiclePlacement.cs','VehicleParking.cs','VehicleParkingRules.cs','VehicleExitRules.cs'))
        elif suite=='lighting':sources.extend(BASE/'src'/name for name in ('VehicleLightingState.cs','DrivingMath.cs'))
        elif suite=='flight':sources.extend(BASE/'src'/name for name in ('FlightState.cs','VehicleFlight.cs','NitroState.cs','DrivingMath.cs'))
        elif suite=='impact':sources.extend(BASE/'src'/name for name in ('VehicleImpactRules.cs','VehicleImpact.cs'))
        elif suite=='nitroview':sources.append(BASE/'src/NitroCameraView.cs')
        elif suite=='storage':sources.extend(BASE/'src'/name for name in ('CarProfile.cs','VehicleStorageRules.cs','VehicleStorage.cs','VehiclePersistence.cs','VehicleNetworkProtocol.cs'))
        elif suite=='carprofile':sources.extend(BASE/'src'/name for name in ('CarProfile.cs','ModelFile.cs','TurretState.cs','VehicleStorageRules.cs','ControlRules.cs','DrivingMath.cs'))
        elif suite=='mw':
            sources.extend(BASE/'src'/name for name in ('CarProfile.cs','ModelFile.cs','TurretState.cs','VehicleVinylData.cs','VehicleDecalData.cs','WheelFitMath.cs','WheelSizeVisuals.cs'));sources.append(BASE/'tests/wheelsize/Doubles.cs')
        elif suite=='wheelsize':sources.extend(BASE/'src'/name for name in ('CarProfile.cs','ModelFile.cs','TurretState.cs','WheelFitMath.cs','WheelSizeVisuals.cs'))
        elif suite=='horde':sources.extend(BASE/'src'/name for name in ('HordeWave.cs','HordeRun.cs','HordePact.cs','HordeBonus.cs','HordeElementRules.cs'))
        elif suite=='hordeweather':sources.append(BASE/'src/HordeWeather.cs')
        elif suite=='horderadar':sources.append(BASE/'src/HordeMapProjection.cs')
        elif suite=='turretfireaudio':sources.extend(BASE/'src'/name for name in ('TurretFireAudio.cs','TurretState.cs'))
        elif suite=='terrain':sources.extend(BASE/'src'/name for name in ('TurretTerrain.cs','TurretState.cs'))
        elif suite=='targeting':sources.extend(BASE/'src'/name for name in ('TurretTargeting.cs','TurretState.cs'))
        elif suite=='blast':sources.extend(BASE/'src'/name for name in ('TurretDamage.cs','TurretState.cs'))
        elif suite in ('hotbar','rumble','hordehull','hordecorpses','hordeelements'):
            if suite=='hordeelements':sources.extend(BASE/'src'/name for name in ('HordeElementCombat.cs','HordeElementStatus.cs','HordeElementRules.cs','HordePact.cs'))
            elif suite=='hordehull':sources.extend(BASE/'src'/name for name in ('VehicleHordeHull.cs','VehicleImpactRules.cs'))
            elif suite=='hordecorpses':sources.extend(BASE/'src'/name for name in ('HordeCorpses.cs','HordeProjectiles.cs'))
            else:sources.append(BASE/'src'/('VehicleHotbarCapture.cs' if suite=='hotbar' else 'VehicleCameraShake.cs'))
            harmony=out/'0Harmony.dll';shutil.copy2(bepinex_dir()/'core/0Harmony.dll',harmony);refs.append(harmony)
        else:
            plugin=out/'NeedForValheim.dll';shutil.copy2(OUTPUT/'mod/NeedForValheim.dll',plugin);refs.append(plugin)
        if suite in ('model','storage','carprofile','wheelsize','mw','networksettings','networkintegration'):
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
        if suite=='mw':arguments=[str(LOCAL/'mw-export')]
        if suite=='model':arguments=[str(LOCAL/'model/body.json')]
        if suite=='carprofile':arguments=[str(OUTPUT/'mod')]
        if suite=='wheelsize':arguments=[str(OUTPUT/'mod')]
        if suite=='loading':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(OUTPUT/'mod')]
        if suite=='audio':arguments=[str(out/'preview-state.csv'),str(OUTPUT/'mod/audio/effects')]
        if suite=='radio':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
        if suite=='menu':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'menu')]
        if suite=='driving':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'hud/hud.json')]
        if suite=='cabin':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'interior/interior.json')]
        if suite=='vehicle_assets':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(args.vehicle_assets_root),str(LOCAL)]
        if suite=='turret':
            jsondll=out/'Newtonsoft.Json.dll';shutil.copy2(valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll',jsondll);refs.append(jsondll)
            arguments=[str(LOCAL/'turret')]
        dll=out/(suite+'Tests.dll');compile_cs(dll,sources,refs);runtime_config(dll,tfm,version)
        subprocess.run(['dotnet',str(dll),*arguments],check=True)
    print('Requested checks passed. These tests do not execute Unity physics or playback.')

if __name__=='__main__':main()
