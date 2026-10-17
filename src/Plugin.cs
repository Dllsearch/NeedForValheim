using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace NeedForValheim {
// Local vehicles have no ZNetView; test enemies use nonpersistent native views.
[BepInPlugin("local.needforvalheim.prototype", "NeedForValheim vehicles", "0.28.0")]
[DefaultExecutionOrder(10000)]
public partial class Plugin : BaseUnityPlugin {
    VehicleOrbitCamera orbitCamera;
    ValheimMusicDucking fleetMusic;
    bool audioBlocked;
    ConfigEntry<bool> invertMouseY;
    ConfigEntry<float> mouseSensitivity;
    MonoBehaviour player, gameCamera; Rigidbody playerBody; bool playerEnabled, cameraEnabled, playerKinematic;
    Renderer[] playerRenderers; bool[] renderStates; Collider[] playerColliders; bool[] colliderStates;
    string status = "";
    Vector3 lastExitPosition;
    VehicleCabinDriver cabinDriver;
    bool DriverInputReady=>cabinDriver==null||cabinDriver.Motion.CanDrive;
    Type playerType, netType; bool ready;
    bool ParkedRunning=>car!=null&&hasBeenDriven&&!driving&&keepRunning.Value;
    void Awake() {
        bool autoSave=Config.SaveOnConfigSet;Config.SaveOnConfigSet=false;
        try {
        var asm=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetType("Player")!=null);
        playerType=asm?.GetType("Player");netType=asm?.GetType("ZNet");
        // Vehicle settings are bound per catalog entry, so the catalog must exist first.
        InitializeGarage();
        terrainMode=BindVehicle("Driving","TerrainMode",true,"Terrain suspension, traction and front-wheel assistance. Change in the vehicle menu; off uses rear-wheel drive.");
        mouseSensitivity=Config.Bind("Camera","MouseSensitivity",1f,new ConfigDescription("Driving mouse orbit speed. Adjustable in Options / Driving camera.",new AcceptableValueRange<float>(.02f,4f)));
        var mouseSpeedUpdated=Config.Bind("Camera","MouseSpeedUpdated",false,"Tracks the one-time upgrade of the old 0.15 default. Custom sensitivity is preserved.");
        if(!mouseSpeedUpdated.Value){if(Mathf.Approximately(mouseSensitivity.Value,.15f))mouseSensitivity.Value=1f;mouseSpeedUpdated.Value=true;}
        invertMouseY=Config.Bind("Camera","InvertMouseY",false,"Invert vertical mouse look while driving.");
        soundVolume=BindVehicle("Audio","Volume",.8f,new ConfigDescription("Vehicle sound volume. 0 mutes engine, tyres and horn; 1 is full volume.",new AcceptableValueRange<float>(0,1)));
        orbitCamera=new VehicleOrbitCamera(message=>Logger.LogInfo(message));
        InitializeControls();
        InitializeMenu();
        InitializeGarageEconomy();
        InitializeGarageStation();
        InitializeVehicleWorldFeatures();
        InitializeExhaustMenu();
        InitializeNitroVisuals();
        InitializeBoostInput();
        fleetMusic=new ValheimMusicDucking(message=>Logger.LogWarning(message));
        ready=playerType!=null;Logger.LogInfo(ExperimentalNetwork?"NFVU2 loaded. Experimental network prototype enabled.":"NFVU2 loaded. Persistent single-player vehicles; multiplayer disabled.");
        }finally{Config.SaveOnConfigSet=autoSave;Config.Save();}
        var audit=Environment.GetEnvironmentVariable("NFVU2_MW_AUDIT_REPORT");if(!string.IsNullOrEmpty(audit))StartCoroutine(VehicleImportAudit.Run(this,vehicleCatalog,audit,message=>Logger.LogInfo(message)));
        var visualAudit=Environment.GetEnvironmentVariable("NFVU2_VISUAL_AUDIT_REPORT");if(!string.IsNullOrEmpty(visualAudit))StartCoroutine(RunVisualAudit(visualAudit));
    }
    MonoBehaviour LocalPlayer() => playerType?.GetField("m_localPlayer",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null) as MonoBehaviour;
    bool Multiplayer() {
        if(NetworkObserversEnabled)return false;
        if(netType==null)return true;
        var single=netType.GetProperty("IsSinglePlayer",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
        if(single!=null)return !(bool)single.GetValue(null);
        object net=netType.GetProperty("instance",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null)
            ??netType.GetField("instance",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null);
        if(net==null)return true;
        var m=netType.GetMethod("IsSinglePlayer",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if(m!=null)return !(bool)m.Invoke(net,null);
        return true; // Fail closed until the installed game's API has been verified.
    }
    bool UIVisible() {
        // Includes the map's native opening/closing grace frames. The seated
        // GameCamera is disabled, so our orbit camera must release its cursor.
        if(Minimap.IsOpen())return true;
        foreach(var name in new[]{"Console","Chat","InventoryGui","Menu","TextInput"}) {
            var t=playerType.Assembly.GetType(name);if(t==null)continue;
            var m=t.GetMethod("IsVisible",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic,null,Type.EmptyTypes,null)
                ??t.GetMethod("IsMenuVisible",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic,null,Type.EmptyTypes,null);
            if(m!=null && m.ReturnType==typeof(bool) && (bool)m.Invoke(null,null))return true;
            if(name=="Chat"){
                var chat=t.GetProperty("instance",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(null);
                var visible=t.GetMethod("IsDialogVisible",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,Type.EmptyTypes,null);
                if(chat!=null&&visible!=null&&(bool)visible.Invoke(chat,null))return true;
            }
        }return false;
    }
    void Update() {
        if(visualAuditActive)return;
        if(!ready)return;
        TickVehicleNetwork();
        TickNetworkCargo();
        SampleControls();
        var local=LocalPlayer();
        TickInventoryGarage();
        TickHordeWeather(local);
        CheckHordeContext(local);
        if(local==null){CleanupAll();return;}
        if((fleetOwner!=null && local!=fleetOwner) || (driving && local!=player) || (fleet.Count>0 && Multiplayer()&&!NetworkObserversEnabled)){CleanupAll();return;}
        if(SpawnBusy&&Application.isFocused&&!UIVisible()&&!menuOpen&&PadDown("back")){CancelSpawn("Loading cancelled.");return;}
        TickNetworkObservers(local);
        TickWorldVehicles(local);
        TickSpawn(local);
        if(driving&&local is Player livePlayer&&(livePlayer.IsDead()||livePlayer.IsTeleporting())) {
            Exit(livePlayer.IsTeleporting()?(Vector3?)livePlayer.transform.position:null);return;
        }
        TickNetworkDriving();
        TickNetworkSetups();
        TickGarageRestore(local);

        if(!driving&&!menuOpen)SelectNearest(local);
        audioBlocked=UIVisible()||!Application.isFocused||Time.timeScale<=0;
        if(inventoryGarageRequested&&Application.isFocused&&OpenRequestedInventoryGarage()){BlockDrivingInput();return;}
        if(audioBlocked){CloseVehicleMenu();BlockDrivingInput();return;}
        try {
            if(menuOpen && !CanUseVehicleMenu(local))CloseVehicleMenu();
            if((MenuShortcutDown||PadDown("menu")||driving&&!menuOpen&&Input.GetKeyDown(KeyCode.Tab)) && CanUseVehicleMenu(local)){SetVehicleMenu(!menuOpen);BlockDrivingInput();return;}
            if(menuOpen){TickControllerMenu();BlockDrivingInput(false);if(menuOpen)TickMenuCamera();return;}
            TickHorde(local);
            if(hordeWave.Active&&hordeWave.Choosing){BlockDrivingInput();TickHordePactInput();return;}
            TickControlActions(local);
            if(driving&&!DriverInputReady){BlockDrivingInput();return;}
            if(driving)ReadVehicleControls();
            TickSongToast();
        }catch(Exception e){Logger.LogError(e);CleanupAll();status="Error \u00b7 see BepInEx/LogOutput.log";}
    }
    Transform AddVisual(ModelPart p, Transform parent, ModelData model, Material paint, Material dark, Material[] materials=null) {
        var obj=new GameObject(p.name);obj.transform.SetParent(parent,false);
        Mesh mesh;
        if(p.group!="audio"||!audioMeshes.TryGetValue(p,out mesh)) {
            mesh=new Mesh{name=p.name};vehicleMeshes.Add(mesh);
            mesh.vertices=p.vertices.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray();mesh.triangles=p.indices;
            if(p.normals!=null)mesh.normals=p.normals.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray();else mesh.RecalculateNormals();
            mesh.uv=p.uv!=null?p.uv.Select(v=>new Vector2(v.u,v.v)).ToArray():new Vector2[p.vertices.Length];
            mesh.colors=Enumerable.Repeat(Color.white,p.vertices.Length).ToArray();
            // Audio membranes use the original vertex-to-model mapping.
            if(p.group!="audio")VehicleMesh.AddBackfaces(mesh);
            mesh.RecalculateTangents();mesh.RecalculateBounds();
            if(p.group=="wheel")wheelSizeVisuals?.Track(mesh,p);
            if(p.group=="audio")audioMeshes.Add(p,mesh);
        }
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=model.schemaVersion>=2?(materials??vehicleMaterials)[p.material]:(p.name.Contains("BASE")?dark:paint);
        if(p.group=="wheel")renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
        return obj.transform;
    }
    void Enter(MonoBehaviour local) {
        if(Multiplayer())return;
        if(SpawnBusy){status="Wait for loading or use Cancel in the garage.";return;}
        if(Flying){status="Wait until the vehicle has landed.";return;}
        if(local is Player native&&(native.IsDead()||native.IsAttached()||native.IsTeleporting())){status="Finish your current action before entering.";return;}
        if(body.linearVelocity.magnitude>.5f){status="The vehicle must be stationary to enter.";return;}
        Vector3 outside=Vector3.zero;
        if(current.interior!=null&&!FindCabinExit(local,out outside)){ShowVehicleWarning("The driver door is blocked. Make some room.");return;}
        if(current.networkData!=null&&!current.networkDriving){RequestNetworkDriver(local);return;}
        CloseVehicleMenu();
        player=local;playerBody=player.GetComponent<Rigidbody>();if(playerBody==null){player=null;return;}
        lastExitPosition=player.transform.position;playerEnabled=player.enabled;playerKinematic=playerBody.isKinematic;
        playerRenderers=player.GetComponentsInChildren<Renderer>();renderStates=playerRenderers.Select(r=>r.enabled).ToArray();
        playerColliders=player.GetComponentsInChildren<Collider>();colliderStates=playerColliders.Select(c=>c.enabled).ToArray();
        if(current.interior!=null) {
            try {cabinDriver=new VehicleCabinDriver(local,car.transform,current.interior,outside);}
            catch(Exception e){Logger.LogWarning("Cabin driver unavailable: "+e);cabinDriver?.Dispose();cabinDriver=null;}
        }
        if(cabinDriver==null)foreach(var r in playerRenderers)r.enabled=false;
        foreach(var c in playerColliders)c.enabled=false;
        player.enabled=false;playerBody.isKinematic=true;
        var cameraType=playerType.Assembly.GetType("GameCamera");
        gameCamera=cameraType?.GetProperty("instance",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null) as MonoBehaviour;
        if(gameCamera!=null){cameraEnabled=gameCamera.enabled;gameCamera.enabled=false;}
        orbitCamera.Begin(car.transform,IsSuv);
        driving=true;hasBeenDriven=true;
        showroom?.State.CloseAll();
        nitroInput.Reset();nitroHeld=false;
    }
    void FixedUpdate() {hostCollisions?.FixedTick();ForEachVehicle(StepVehicle);}
    void StepVehicle() {
        if(body==null||wheels==null||(current.networkData!=null&&!current.networkDriving))return;
        if(UpdateParkedVehicle())return;
        PreparePerformance();
        if(!Flying)current.rideHeight?.Apply(lowering.Value);
        if(vehicleImpact!=null)vehicleImpact.EnemyDamageMultiplier=HordeVehicle?HordePacts.Impact(hordeRun.Pact):1;
        vehicleImpact?.Step(player,driving&&!Flying&&!audioBlocked&&!menuOpen&&Application.isFocused&&(!HordeVehicle||!hordeWave.Choosing),
            DriverInputReady&&brake<=0&&!handbrakeHeld&&!burnoutHeld?throttle:0);
        if(Flying) {
            current.flight.Step(driving&&DriverInputReady&&!audioBlocked&&!menuOpen&&Application.isFocused,throttle,steer,current.flightVertical,nitroHeld&&nitroEnabled.Value);
            TraceFlightBoost();
            if(!Flying){if(driving)BlockDrivingInput();ApplyDriveMode();}
        } else {
            bool handbrake=EffectiveDrift&&driving&&handbrakeHeld&&!audioBlocked&&!menuOpen&&Application.isFocused;
            handling?.Step(driving&&DriverInputReady,throttle,steer,handbrake?0:brake,burnoutHeld&&!audioBlocked&&!menuOpen&&Application.isFocused,handbrake,
                PerformanceTuning.Torque(engineStage.Value,turboStage.Value,gearStage.Value),PerformanceTuning.Speed(engineStage.Value,turboStage.Value,gearStage.Value),PerformanceTuning.Brakes(brakeStage.Value));
            // Scale controller pedal braking after the unchanged wheel handler.
            if(padActive&&driving&&DriverInputReady&&!audioBlocked&&!menuOpen&&!handbrake&&brake>0)
                foreach(var wheel in wheels)wheel.brakeTorque*=brake;
            if(padActive&&driving&&DriverInputReady&&!audioBlocked&&!menuOpen&&handbrakeHeld&&!EffectiveDrift) {
                for(int i=0;i<4;i++){wheels[i].motorTorque=0;wheels[i].brakeTorque=i<2?0:4200;}
            }
            if(current.Profile.capabilities.suvTuning)SuvTuning.AfterStep(wheels);
            ApplyPerformanceGrip();
            vehicleNitro?.Step(driving,audioBlocked||menuOpen||!DriverInputReady||burnoutHeld||!Application.isFocused||!nitroEnabled.Value,nitroHeld,throttle,brake>0,EffectiveTerrain);
        }
        if(driving&&player!=null&&cabinDriver==null)player.transform.position=car.transform.TransformPoint(new Vector3(0,.8f,.3f));
    }
    void LateUpdate() {if(visualAuditActive){TickGarageFrontend();return;}ForEachVehicle(TickVehicle);TickVehicleWorldFeatures();if(driving)VehicleWorldStreaming.Follow(player);TickGarageFrontend();LateMenuCamera();TickNitroVisuals();UpdateGameCrosshair();UpdateVehicleHints();TickHordeRadar();fleetMusic?.Tick(driving&&!audioBlocked&&vehicleRadio!=null&&vehicleRadio.CabinAudible);}
    void TickVehicle() {
        if(car==null)return;
        if(current.networkData!=null&&!current.networkDriving){TickNetworkObserver();return;}
        TickWorkshop();
        TickVehicleStorage();
        TickExhaustCustomization();
        if(showroom!=null)try {
            ApplyShowroomSettings();
            int color=Math.Max(0,Array.IndexOf(ColorNames,trunkNeonColor.Value));
            float showroomDt=driving&&cabinDriver!=null&&(audioBlocked||!Application.isFocused||menuOpen&&!DriverInputReady)?0:Time.deltaTime;
            int activeDoorStyle=driving&&cabinDriver!=null&&!DriverInputReady?showroom.State.DoorStyle:doorStyle.Value;
            showroom.Tick(showroomDt,Flying||body.linearVelocity.magnitude>.5f,activeDoorStyle,driving||ParkedRunning,trunkNeonEnabled.Value,trunkNeonPulsing.Value,NeonColors[color]);
        }
        catch(Exception e){showroom.Dispose();showroom=null;Logger.LogWarning("Vehicle showroom disabled: "+e);}
        current.interior?.Tick(wheels!=null?wheels[0].steerAngle:0,windowTint.Value,driving&&cabinDriver!=null);
        if(driving&&cabinDriver!=null)try {
            bool directionBrake=DrivingMath.DirectionBrake(Vector3.Dot(body.linearVelocity,car.transform.forward),throttle);
            cabinDriver.SetControls(directionBrake?0:throttle,directionBrake?1:brake,handbrakeHeld,vehicleAudio?.Gear??0,steer);
            cabinDriver.Tick(Time.deltaTime,audioBlocked||menuOpen||!Application.isFocused,showroom?.State);
            if(cabinDriver.Motion.DoorTimedOut) {
                Logger.LogWarning("Cabin door timed out during "+cabinDriver.Motion.Phase+"; speed="+body.linearVelocity.magnitude.ToString("0.00")+" m/s");
                if(cabinDriver.Motion.Phase==CabinPhase.OpenExit)cabinDriver.Motion.CancelExit();
                else Exit();
                ShowVehicleWarning("The driver door is blocked. Park elsewhere.");return;
            }
            if(cabinDriver.Failed){Exit();ShowVehicleWarning("Path to the driver door blocked.");return;}
            if(cabinDriver.Motion.Phase==CabinPhase.Complete){
                if(!FindCabinExit(player,out var clearExit,true)){cabinDriver.Motion.CancelExit();ShowVehicleWarning("Exit has become blocked.");}
                else {cabinDriver.SetExitPosition(clearExit);Exit();return;}
            }
        } catch(Exception e){Logger.LogError("Cabin animation stopped: "+e);Exit();return;}
        ApplyNeonSettings();
        bool burnout=!Flying&&driving&&burnoutHeld&&handling!=null&&handling.BurnoutActive&&!audioBlocked&&!menuOpen&&Application.isFocused;
        wheelSizeVisuals?.Apply(RimSetting.Value);
        if(wheels!=null && visuals!=null)for(int i=0;i<4;i++)if(wheels[i]!=null && visuals[i]!=null){
            UpdateWheelVisual(i);
        }
        TickFlight();
        if(driving)orbitCamera.LateFrame(body.linearVelocity,dynamicCamera.Value,cameraFollow.Value,audioBlocked||menuOpen,!Flying&&turret!=null&&(HordeVehicle||turretEnabled.Value));
        TickTurret();
        if(vehicleLighting!=null)try {
            vehicleLighting.SetHeadlights(headlightPreset.Value,headlightStrength.Value);
            vehicleLighting.ShowOffEnabled=headlightShowOff.Value;
            float speed=Vector3.Dot(body.linearVelocity,car.transform.forward);
            vehicleLighting.Tick(driving,driving&&(burnoutHeld||brake>0||DrivingMath.DirectionBrake(speed,throttle)),speed,burnout?1:throttle,Time.deltaTime,!Application.isFocused||Time.timeScale<=0);
        }
        catch(Exception e){vehicleLighting.Dispose();vehicleLighting=null;Logger.LogWarning("Vehicle lighting disabled: "+e);}
        if(vehicleNeon!=null)try {vehicleNeon.Tick(driving||ParkedRunning,Time.deltaTime);}
        catch(Exception e){vehicleNeon.Dispose();vehicleNeon=null;Logger.LogWarning("Vehicle neon disabled: "+e);}
        if(vehicleRadio!=null)try {
            // NFS RIGHT maps to Unity -X (the physical left door), as in the menu.
            vehicleRadio.Tick(driving,audioBlocked,radioVolume.Value,radioEnabled.Value,ParkedRunning,
                showroom?.State.Progress[1]??0,showroom?.State.Progress[0]??0,showroom?.State.Progress[3]??0);
        }
        catch(Exception e){vehicleRadio.Dispose();vehicleRadio=null;Logger.LogWarning("Vehicle radio disabled: "+e);}
        if(showroom!=null)try {showroom.TickSubwoofers(vehicleRadio?.SubwooferOffset??0);}
        catch(Exception e){showroom.Dispose();showroom=null;Logger.LogWarning("Vehicle subwoofer visuals disabled: "+e);}
        if(vehicleAudio!=null)try {
            vehicleAudio.Tick(driving,throttle,brake>0||burnoutHeld&&!burnout,honking,audioBlocked,EffectiveTerrain,soundVolume.Value,burnout,ParkedRunning,Flying,!menuOpen&&DriverInputReady);
        } catch(Exception e) {
            vehicleAudio.Dispose();vehicleAudio=null;Logger.LogWarning("Vehicle sound disabled after audio error: "+e);
        }
        bool effectsBlocked=!driving||!DriverInputReady||audioBlocked||menuOpen||!Application.isFocused;
        if(burnoutEffects!=null)try {burnoutEffects.Tick(burnout,Flying||effectsBlocked,EffectiveDrift&&body!=null&&body.linearVelocity.magnitude>3);}
        catch(Exception e){burnoutEffects.Dispose();burnoutEffects=null;Logger.LogWarning("Vehicle tyre smoke disabled: "+e);}
        bool backfire=backfireState.Step(Time.deltaTime,!Flying&&!effectsBlocked,
            vehicleAudio!=null?vehicleAudio.Rpm:950,throttle,
            body!=null?Vector3.Dot(body.linearVelocity,body.transform.forward):0,backfireEnabled.Value);
        current.networkBackfire|=backfire;
        // Exhaust follows actual boost/fuel consumption in either driving mode.
        // Wheel thrusters keep their separate hover loop; purge/backfire stay on the ground.
        bool boost=nitroEnabled.Value&&ActiveNitro!=null&&ActiveNitro.Boosting;
        bool purge=!Flying&&nitroEnabled.Value&&vehicleNitro!=null&&vehicleNitro.State.Purging;
        if(nitroEffects!=null)try {nitroEffects.Tick(boost,purge,backfire,effectsBlocked,Time.deltaTime);}
        catch(Exception e){nitroEffects.Dispose();nitroEffects=null;Logger.LogWarning("Vehicle effects disabled: "+e);}
        if(effectsAudio!=null)try {effectsAudio.Tick(boost,purge,backfire,effectsBlocked,soundVolume.Value);}
        catch(Exception e){effectsAudio.Dispose();effectsAudio=null;Logger.LogWarning("Vehicle effect sounds disabled: "+e);}
    }
    bool FindCabinExit(MonoBehaviour local,out Vector3 position,bool alternatives=false) {
        float side=current.interior?.ExitSide??-Mathf.Max(1.3f,current.Profile.rig.boxSizeX*.5f+.4f);
        float door=current.interior?.SeatPosition.z??-.12f;
        foreach(float offset in new[]{0f,-.36f,.34f}) {
            var origin=car.transform.TransformPoint(new Vector3(side,Mathf.Max(2.2f,current.Profile.rig.boxCenterY+current.Profile.rig.boxSizeY*.5f+.7f),door+offset));
            if(FindExitSurface(local,origin,5,out position))return true;
        }
        if(alternatives) {
            // World-up search still works when the chassis rests on its roof or
            // its physical driver door faces into a rock. Never rotate gravity
            // with the overturned vehicle.
            float height=car.transform.position.y+Mathf.Max(3,current.Profile.rig.boxSizeY+1);
            float yaw=car.transform.eulerAngles.y;
            float near=Mathf.Max(1.8f,current.Profile.rig.boxSizeX*.5f+.6f);
            foreach(float radius in new[]{near,near+1.2f,near+2.5f})for(int i=0;i<12;i++) {
                var point=car.transform.position+Quaternion.Euler(0,yaw+i*30,0)*Vector3.left*radius;point.y=height;
                if(FindExitSurface(local,point,12,out position))return true;
            }
        }
        position=Vector3.zero;return false;
    }
    bool FindExitSurface(MonoBehaviour local,Vector3 origin,float distance,out Vector3 position) {
        foreach(var hit in Physics.RaycastAll(origin,Vector3.down,distance,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)) {
            var collider=hit.collider;
            if(collider.transform.IsChildOf(car.transform)||collider.transform.IsChildOf(local.transform))continue;
            // Do not search through an obstructing roof or platform to hidden ground.
            if(hit.normal.y<.65f||collider.GetComponentInParent<Character>()!=null||hit.rigidbody!=null&&!hit.rigidbody.isKinematic)break;
            var foot=hit.point+Vector3.up*.06f;
            if(ZoneSystem.instance!=null&&(!ZoneSystem.instance.IsZoneLoaded(foot)||ZoneSystem.instance.IsLava(foot))||ZNetScene.instance!=null&&!ZNetScene.instance.IsAreaReady(foot))break;
            if(foot.y<Floating.GetLiquidLevel(foot)+.1f)break;
            bool occupied=Physics.OverlapCapsule(foot+Vector3.up*.32f,foot+Vector3.up*1.5f,.27f,~0,QueryTriggerInteraction.Ignore)
                .Any(c=>!c.transform.IsChildOf(local.transform));
            if(occupied)break;position=foot;return true;
        }
        position=Vector3.zero;return false;
    }
    bool UnspawnCurrent() {
        if(car==null||SpawnBusy)return false;
        if(current.networkData!=null){RequestNetworkRemoval(current);return true;}
        if(driving) {
            if(player==null||!FindCabinExit(player,out var safe,true)){ShowVehicleWarning("No safe ground nearby. The vehicle has not been removed.");return false;}
            Exit(safe);
        }
        Cleanup();current=fleet.FirstOrDefault()??new VehicleRuntime();
        workshopMessage="Vehicle unspawned. Cargo is retained in its chest.";return true;
    }
    void RequestExit() {
        if(Flying){status="Open the vehicle menu, choose a ground mode and land to get out.";return;}
        if(cabinDriver==null){Exit();return;}
        if(!cabinDriver.Motion.CanDrive)return;
        if(body.linearVelocity.magnitude>.5f){status="Stop before exiting.";return;}
        if(!FindCabinExit(player,out var destination,true)){ShowVehicleWarning("No safe ground nearby. The vehicle has not been removed.");return;}
        if(car.transform.up.y<.65f){Exit(destination);return;}
        if(HordeVehicle)ClearHorde();
        cabinDriver.BeginExit(destination);BlockDrivingInput();
    }
    void Exit(Vector3? safeDestination=null) {
        bool animated=cabinDriver!=null&&cabinDriver.Motion.Phase==CabinPhase.Complete;
        Vector3 destination=safeDestination??(animated?cabinDriver.ExitPosition:lastExitPosition);
        // Resolve while the car/colliders still exist. A visual cleanup failure
        // must never strand the real player in disabled cabin state.
        if(!safeDestination.HasValue&&!animated&&player!=null&&car!=null)
            try {if(FindCabinExit(player,out var nearby,true))destination=nearby;}
            catch(Exception error){Logger.LogWarning("Emergency exit search: "+error.Message);}
        try {
            if(HordeVehicle)ClearHorde();
            ReleaseCameraRumble(gameCamera);exitAfterLanding=false;controlsGate.Block();
            ClearNitroVisuals();
            cabinDriver?.Dispose();
            current.interior?.SetDriverWindowOpen(false);showroom?.State.CloseAll();
            RestoreGameCrosshair();CloseVehicleMenu();
            current.flightVertical=0;current.flight?.Land();vehicleImpact?.Step(null,false);
            nitroInput.Reset();vehicleNitro?.State.Stop();orbitCamera?.End();
        }catch(Exception error){Logger.LogWarning("Vehicle exit visual cleanup: "+error);}
        finally {
            cabinDriver=null;driving=false;exitAfterLanding=false;
            StopNetworkDriving();
            throttle=steer=0;brake=1;honking=burnoutHeld=nitroHeld=handbrakeHeld=turretHeld=false;
            RestoreVehicleHints();
            if(player!=null) {
                player.transform.SetPositionAndRotation(destination,Quaternion.Euler(0,player.transform.eulerAngles.y,0));
                for(int i=0;playerRenderers!=null&&i<playerRenderers.Length;i++)if(playerRenderers[i]!=null)playerRenderers[i].enabled=renderStates[i];
                for(int i=0;playerColliders!=null&&i<playerColliders.Length;i++)if(playerColliders[i]!=null)playerColliders[i].enabled=colliderStates[i];
                if(playerBody!=null){playerBody.isKinematic=playerKinematic;if(!playerKinematic){playerBody.linearVelocity=Vector3.zero;playerBody.angularVelocity=Vector3.zero;}}
                player.enabled=playerEnabled;VehicleExitSafety.Arm(player as Player,car);
            }
            if(gameCamera!=null)gameCamera.enabled=cameraEnabled;
            VehicleWorldStreaming.Follow(player);player=null;gameCamera=null;status="";
        }
    }
    void Cleanup(){
        bool cargoLeft=current.storage!=null&&current.storage.Count!=0;
        if(driving)Exit();
        RestoreGameCrosshair();
        CloseVehicleMenu();
        ReleaseVehicle();
        SaveGarageRoster();
        if(cargoLeft)ShowVehicleWarning("Cargo remains in a chest at the vehicle's location.");
    }
    void ReleaseVehicle(){
        ReleaseRemoteCabin(current);
        ReleaseVehicleWorldFeatures();
        StopNetworkDriving();
        current.storage?.Release(leavingGarage||restoringVehicle!=null&&current==loadingVehicle);current.storage=null;
        current.flight?.Dispose();current.flight=null;
        current.flightEffects?.Dispose();current.flightEffects=null;
        current.flightAudio?.Dispose();current.flightAudio=null;
        turret?.Dispose();turret=null;
        dashboard?.Dispose();dashboard=null;
        current.decals?.Dispose();current.decals=null;
        current.styling?.Dispose();current.styling=null;
        current.interior?.Dispose();current.interior=null;
        showroom?.Dispose();showroom=null;audioMeshes.Clear();
        burnoutHeld=false;burnoutEffects?.Dispose();burnoutEffects=null;
        nitroHeld=false;nitroInput.Reset();vehicleNitro=null;
        nitroEffects?.Dispose();nitroEffects=null;
        effectsAudio?.Dispose();effectsAudio=null;
        vehicleRadio?.Dispose();vehicleRadio=null;
        vehicleLighting?.Dispose();vehicleLighting=null;
        vehicleNeon?.Dispose();vehicleNeon=null;
        vehicleAudio?.Dispose();vehicleAudio=null;
        if(car!=null){car.SetActive(false);Destroy(car);}
        hasBeenDriven=false;vehicleImpact=null;
        foreach(var mesh in vehicleMeshes)if(mesh!=null)Destroy(mesh);vehicleMeshes.Clear();
        wheelSizeVisuals=null;
        if(vehicleMaterials!=null)foreach(var material in vehicleMaterials)if(material!=null)Destroy(material);
        if(vehicleTextures!=null)foreach(var texture in vehicleTextures)if(texture!=null)Destroy(texture);
        if(chassisMaterial!=null)Destroy(chassisMaterial);
        if(fleet.Contains(current))fleet.Remove(current);
        handling=null;chassisMaterial=null;vehicleMaterials=null;vehicleTextures=null;car=null;body=null;wheels=null;visuals=null;brakeVisuals=null;originalWheels=false;
    }
    void ShowVehicleWarning(string message) {
        status=message;
        if(MessageHud.instance!=null)MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,Tr(message),log:false);
    }
    void OnDestroy(){DisposeRemoteCabins();hostCollisions?.Dispose();DisposeNetworkCargo();networkClient?.Reset("Vehicle plugin unloaded");DisposeVehicleWorldFeatures();DisposeGarageStation();DisposeGarageFrontend();DisposeInventoryGarage();ReleaseCameraRumble(gameCamera);ReleaseCameraRumble(menuCamera);controlsHarmony?.UnpatchSelf();if(ControlsOwner==this)ControlsOwner=null;ClearNitroVisuals();CleanupAll();hordeWeather?.Dispose();hordeWeather=null;DisposeVehicleHints();fleetMusic?.Dispose();DisposeMenu();}
}
}
