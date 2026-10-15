using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    SpawnAssetCache spawnCache=new SpawnAssetCache();
    bool spawnCacheUsed;
    StagedWork spawnWork;
    VehicleRuntime loadingVehicle;
    MonoBehaviour spawnOwner;
    CancellationTokenSource spawnCancellation;
    Task<SpawnAssets> spawnRead;
    Stopwatch spawnTimer;
    string spawnStage,loadingName;
    float spawnProgress;
    bool spawnCommitted;
    bool SpawnBusy=>spawnWork!=null;
    void RejectSpawn(string message) {
        status=workshopMessage=message;
        Logger.LogInfo("Spawn rejected for "+SelectedProfile.id+": "+message+" (fleet="+fleet.Count+", driving="+driving+", loading="+SpawnBusy+")");
        if(MessageHud.instance!=null)MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,Tr(message),log:false);
    }
    void Spawn(MonoBehaviour local) {
        if(!CheckGaragePurchase(local,SelectedProfile))return;
        if(local==null||Multiplayer())return;
        if(SpawnBusy){RejectSpawn("A vehicle is already loading. F8: cancel.");return;}
        if(driving){RejectSpawn("Exit before spawning.");return;}
        if(local is Player live&&live.IsDead())return;
        if(fleet.Any(v=>v.Profile.id==SelectedProfile.id)){RejectSpawn("This car is already in your garage. Select it in My garage.");return;}
        if(fleet.Count>=VehicleFleet<VehicleRuntime>.Capacity){RejectSpawn("Maximum four vehicles. Remove one first.");return;}
        var profile=SelectedProfile;
        if(!FindSpawnPoint(local,profile,out var origin)){RejectSpawn("No clear space ahead. Find an open area.");return;}
        workshopMessage="";
        CaptureGarageStationSpawn(local);
        CloseVehicleMenu();
        loadingName=profile.displayName;
        loadingVehicle=new VehicleRuntime{Profile=profile};spawnOwner=local;spawnCommitted=false;
        spawnCancellation=new CancellationTokenSource();spawnTimer=Stopwatch.StartNew();
        LoadStage("Reading files in the background",0);
        var folder=Path.GetDirectoryName(Info.Location);
        var cache=spawnCache;var token=spawnCancellation.Token;
        spawnCacheUsed=true;
        spawnRead=Task.Run(()=>SpawnAssets.Load(folder,profile,cache,token),token);
        // Observe failures even if the player cancels/leaves before the worker finishes.
        spawnRead.ContinueWith(task=>{var ignored=task.Exception;},CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);
        spawnWork=new StagedWork(BuildVehicle(local));
    }
    void LoadStage(string label,float progress) {
        spawnStage=label;spawnProgress=progress;
        status=loadingName+" loading \u2026 F8: cancel";
    }
    void TickSpawn(MonoBehaviour local) {
        if(!SpawnBusy)return;
        if(local==null||local!=spawnOwner||Multiplayer()||local is Player live&&(live.IsDead()||live.IsTeleporting())){CancelSpawn("Loading cancelled.");return;}
        var selected=current;
        try {
            current=loadingVehicle;
            if(spawnWork.Tick(3))return;
            Logger.LogInfo("Vehicle spawn completed in "+spawnTimer.Elapsed.TotalSeconds.ToString("0.00")+" s; longest main-thread step "+spawnWork.SlowestStepMilliseconds.ToString("0.0")+" ms.");
            selected=loadingVehicle;
            spawnWork=null;loadingVehicle=null;spawnOwner=null;spawnRead=null;
            spawnCancellation.Dispose();spawnCancellation=null;
            SaveGarageRoster();
            FinishGarageStationSpawn(reopenGarage);
            if(reopenGarage){reopenGarage=false;current=selected;garageView=1;menuTab=10;SetVehicleMenu(true);}
        }catch(Exception error) {
            Logger.LogError(error);
            CancelSpawn(error is GaragePurchaseException?error.Message:"Vehicle could not load. See the BepInEx log.");
        }finally {current=selected;}
    }
    void CancelSpawn(string message) {
        if(!SpawnBusy)return;
        spawnCancellation.Cancel();
        spawnWork.Dispose();spawnWork=null;
        var selected=current;
        try {if(loadingVehicle!=null&&!spawnCommitted){current=loadingVehicle;ReleaseVehicle();fleet.Remove(loadingVehicle);}}
        finally {
            FinishGarageStationSpawn(false);
            current=selected;loadingVehicle=null;spawnOwner=null;spawnRead=null;
            spawnCancellation.Dispose();spawnCancellation=null;
        }
        if(message!=null)status=message;
        reopenGarage=false;
        garageRestore=null;
    }
    void DrawSpawnProgress() {
        if(!SpawnBusy)return;
        float width=Mathf.Min(460,Screen.width-30),x=(Screen.width-width)*.5f;
        UiBox(new Rect(x,18,width,102),loadingName+" loading \u2026");
        UiLabel(new Rect(x+14,43,width-28,23),spawnStage);
        var color=GUI.color;
        GUI.color=new Color(.14f,.17f,.2f,1);GUI.DrawTexture(new Rect(x+14,72,width-28,6),Texture2D.whiteTexture);
        GUI.color=new Color(.2f,.85f,1,1);
        if(spawnProgress==0) {
            float travel=(float)(spawnTimer.Elapsed.TotalSeconds*.55%1);
            GUI.DrawTexture(new Rect(x+14+travel*(width-100),72,72,6),Texture2D.whiteTexture);
        }else GUI.DrawTexture(new Rect(x+14,72,(width-28)*spawnProgress,6),Texture2D.whiteTexture);
        GUI.color=color;
        if(UiButton(new Rect(x+width-139,86,125,25),"Cancel ("+(padActive?ButtonName("B","Circle"):"F8")+")"))CancelSpawn("Loading cancelled.");
    }
    IEnumerable<int> BuildVehicle(MonoBehaviour local) {
        // Give OnGUI a frame before the first Unity object allocation.
        yield return StagedWork.NextFrame;
        while(!spawnRead.IsCompleted)yield return StagedWork.NextFrame;
        var assets=spawnRead.GetAwaiter().GetResult();
        foreach(var warning in assets.Warnings)Logger.LogWarning(warning);
        var model=assets.Model;
        var profile=loadingVehicle.Profile;
        var rig=profile.rig;var caps=profile.capabilities;
        Logger.LogInfo("Vehicle data ready after "+spawnTimer.Elapsed.TotalSeconds.ToString("0.00")+" s; "+model.parts.Length+" body parts.");
        car=new GameObject(profile.prefabName);car.SetActive(false);
        originalWheels=model.schemaVersion>=2;
        if(originalWheels)wheelSizeVisuals=new WheelSizeVisuals(model,rig.rimNominal,rig.rimMin,rig.rimMax);
        wheelVisualLift=model.wheelVisualLift;frontWheelVisualOffset=model.frontWheelVisualOffset;rearWheelVisualOffset=model.rearWheelVisualOffset;
        LoadStage("Textures and materials",.25f);
        if(originalWheels)foreach(var step in VehicleMaterials.CreateForModelSteps(local,model,message=>Logger.LogInfo(message),(m,t)=>{vehicleMaterials=m;current.vehicleTextures=t;}))yield return step;
        else vehicleMaterials=VehicleMaterials.Create(local,message=>Logger.LogInfo(message));
        LoadStage("Body",.42f);
        var paint=vehicleMaterials[0];var dark=vehicleMaterials[1];
        foreach(var p in model.parts.Where(p=>!originalWheels||p.group=="body")){AddVisual(p,car.transform,model,paint,dark);yield return 0;}
        current.exhaustVisuals=new VehicleExhaustVisuals(car,model);
        current.exhaustVisuals.Apply(exhaustStyle.Value);
        LoadStage("Suspension and wheels",.54f);
        // A shorter, raised hull catches fewer small rocks with its bumper corners.
        var box=car.AddComponent<BoxCollider>();box.center=new Vector3(rig.boxCenterX,rig.boxCenterY,rig.boxCenterZ);box.size=new Vector3(rig.boxSizeX,rig.boxSizeY,rig.boxSizeZ);
        var interaction=car.AddComponent<VehicleInteraction>();interaction.Owner=this;interaction.Vehicle=current;
        chassisMaterial=new PhysicsMaterial("Vehicle_Chassis"){dynamicFriction=.08f,staticFriction=.08f,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum};box.sharedMaterial=chassisMaterial;
        body=car.AddComponent<Rigidbody>();body.mass=rig.mass;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        wheels=new WheelCollider[4];visuals=new Transform[4];brakeVisuals=new Transform[4];
        if(rig.spinners)current.spinners=new Transform[4];
        for(int i=0;i<4;i++) {
            var wheel=new GameObject("Wheel"+i);wheel.transform.SetParent(car.transform,false);wheel.transform.localPosition=new Vector3((i%2==0?-1:1)*rig.TrackWidth(i),rig.WheelHeight(i),i<2?rig.frontAxleZ:-rig.rearAxleZ);
            var wc=wheel.AddComponent<WheelCollider>();wc.radius=rig.WheelRadius(i);wheels[i]=wc;
            if(originalWheels) {
                var visual=new GameObject("OriginalWheel"+i);visual.transform.SetParent(car.transform,false);visuals[i]=visual.transform;
                var brakeVisual=new GameObject("OriginalBrake"+i);brakeVisual.transform.SetParent(car.transform,false);brakeVisuals[i]=brakeVisual.transform;
                foreach(var p in model.parts.Where(p=>ModelFile.ForAxle(p,i)&&(p.group=="wheel"||(p.group=="brake"&&p.rotatesWithWheel)))) {AddVisual(p,visual.transform,model,paint,dark);yield return 0;}
                if(rig.spinners){var spinner=new GameObject("Spinner"+i);spinner.transform.SetParent(car.transform,false);current.spinners[i]=spinner.transform;
                    foreach(var p in model.parts.Where(p=>p.group=="spinner")){AddVisual(p,spinner.transform,model,paint,dark);yield return 0;}}
                foreach(var p in model.parts.Where(p=>ModelFile.ForAxle(p,i)&&p.group=="brake"&&!p.rotatesWithWheel)) {AddVisual(p,brakeVisual.transform,model,paint,dark);yield return 0;}
            } else {
                var visual=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(visual.GetComponent<Collider>());visual.transform.SetParent(car.transform,false);visual.transform.localScale=new Vector3(.68f,.12f,.68f);visual.GetComponent<Renderer>().sharedMaterial=dark;visuals[i]=visual.transform;
            }
        }
        foreach(var step in BuildExtraRearAxle(model,paint,dark))yield return step;
        handling=new VehicleHandling(body,wheels,terrainMode.Value,message=>Logger.LogInfo(message));
        if(driftMode.Value&&caps.drift)handling.SetDriftMode();
        if(caps.suvTuning)SuvTuning.Configure(body,wheels);
        else if(profile.sourceGame==CarCatalog.MostWanted)MwChassisTuning.Configure(body,wheels);
        current.rideHeight=new VehicleRideHeight(body,wheels,profile.sourceGame==CarCatalog.MostWanted);
        current.rideHeight.Apply(lowering.Value);
        LoadStage("Gauges",.62f);
        if(assets.Hud!=null) {
            dashboard=new VehicleDashboard();
            foreach(var step in StagedWork.Optional(dashboard.LoadSteps(assets.Hud),error=>{dashboard.Dispose();dashboard=null;Logger.LogWarning("HUD unavailable: "+error);}))yield return step;
        }
        LoadStage("Effects and audio",.72f);
        vehicleImpact=car.AddComponent<VehicleImpact>();
        vehicleImpact.Initialize(body,box,message=>Logger.LogWarning(message));
        burnoutEffects=VehicleBurnoutEffects.Create(car,model,wheels,visuals,message=>Logger.LogInfo(message));
        if(caps.nitro)vehicleNitro=new VehicleNitro(body,wheels);
        InitializeFlight(assets.Flames,assets.Audio);
        yield return 0;
        try {if(caps.nitro)nitroEffects=VehicleNitroEffects.Create(car,model,message=>Logger.LogInfo(message));}
        catch(Exception e){Logger.LogWarning("Vehicle nitro effects unavailable: "+e);}
        if(caps.nitro)effectsAudio=VehicleEffectsAudio.Create(car,message=>Logger.LogInfo(message),assets.Audio);
        yield return 0;
        vehicleAudio=new VehicleAudio(profile);
        foreach(var step in StagedWork.Optional(vehicleAudio.LoadSteps(car,body,wheels,message=>Logger.LogInfo(message),assets.Audio),error=>{vehicleAudio.Dispose();vehicleAudio=null;Logger.LogWarning("Vehicle audio unavailable: "+error);}))yield return step;
        try {vehicleLighting=VehicleLighting.Create(car,model,message=>Logger.LogInfo(message));}
        catch(Exception e){Logger.LogWarning("Vehicle lighting unavailable: "+e);}
        try {vehicleNeon=VehicleNeon.Create(car,model,message=>Logger.LogInfo(message));}
        catch(Exception e){Logger.LogWarning("Vehicle neon unavailable: "+e);}
        ApplyNeonSettings();
        yield return 0;
        LoadStage("Turret",.8f);
        if(assets.Turret!=null) {
            turret=new VehicleTurret();
            foreach(var step in StagedWork.Optional(turret.LoadSteps(car,local,Path.Combine(Path.GetDirectoryName(Info.Location),profile.assets.turretFolder),assets.Turret,message=>Logger.LogInfo(message),assets.Audio),error=>{turret.Dispose();turret=null;Logger.LogWarning("Turret unavailable: "+error);}))yield return step;
        }
        LoadStage("Trunk and interior",.88f);
        vehicleRadio=VehicleRadio.Create(this,car,message=>Logger.LogWarning(message),false);
        if(model.animations!=null) {
            showroom=new VehicleShowroom(car,model,(part,parent)=>AddVisual(part,parent,model,paint,dark),dark);
            ApplyShowroomSettings();
        }
        yield return 0;
        if(assets.Interior!=null&&showroom!=null) {
            current.interior=new VehicleInterior();
            foreach(var step in StagedWork.Optional(current.interior.LoadSteps(car,local,assets.Interior,message=>Logger.LogInfo(message)),error=>{current.interior.Dispose();current.interior=null;Logger.LogWarning("Cabin unavailable; original visuals retained: "+error);}))yield return step;
        }
        LoadStage("Create vehicle",.98f);
        current.styling=new VehicleStyling(car,model,assets.Styling,vehicleMaterials,StylingWheels,(part,parent)=>AddVisual(part,parent,assets.Styling,paint,dark,current.styling.Materials),StylingBrakes);
        foreach(var step in current.styling.LoadSteps(local,message=>Logger.LogInfo(message)))yield return step;
        if(current.interior==null)current.interior=VehicleInterior.CreateFallback(car,profile,model);
        AttachVehicleSurfaceEffects(model,assets.Styling);
        if(assets.Decals!=null) {
            current.decals=new VehicleDecals(car,assets.Decals,assets.DecalArtwork,(part,parent,material)=>AddVisual(part,parent,assets.Decals,paint,dark,new[]{material}));
            foreach(var step in current.decals.LoadSteps(local,message=>Logger.LogInfo(message)))yield return step;
        }
        TickWorkshop();
        yield return StagedWork.NextFrame;
        // Player/world may have moved since F6. Resolve a free location again.
        if(!FindSpawnPoint(local,profile,out var origin))throw new InvalidOperationException("No clear spawn location remains.");
        car.transform.SetPositionAndRotation(origin,SpawnRotation(local));
        current.parkPosition=origin;current.parkRotation=car.transform.rotation;current.parkSupportTime=Time.unscaledTime;
        car.SetActive(true);
        fleet.Add(current);CompleteGaragePurchase(local,profile);fleetOwner=local;spawnCommitted=true;
        status=VehicleName+" ready. F7: enter | "+menuKey.Value+": vehicle menu | F8: remove";
        if(MessageHud.instance!=null)
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,Tr("Spawned:")+" "+VehicleName,log:false);
    }
}
}
