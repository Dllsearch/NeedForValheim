using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    readonly HashSet<ZDOID> networkDeferred=new HashSet<ZDOID>();
    ZDOMan networkObserverWorld;float networkScanTime;
    bool NetworkObserversEnabled=>ExperimentalNetwork&&ZNet.instance!=null&&!ZNet.IsSinglePlayer&&networkClient?.Ready==true;
    void TickNetworkObservers(MonoBehaviour local) {
        var selected=current;bool selectedWasFleet=fleet.Contains(selected);
        try {
            bool changed=networkObserverWorld!=ZDOMan.instance;
            if(changed){DisposeRemoteCabins();networkObserverWorld=ZDOMan.instance;networkDeferred.Clear();networkScanTime=0;}
            foreach(var vehicle in fleet.Where(v=>v.networkData!=null).ToArray()) {
                var data=vehicle.networkData;
                bool gone=changed||!NetworkObserversEnabled||ZDOMan.instance.GetZDO(data.m_uid)!=data||!VehiclePersistence.IsVehicle(data);
                bool distant=Vector3.Distance(local.transform.position,VehiclePersistence.Location(data))>180;
                if(!gone&&(!distant||vehicle.networkDriving))continue;
                if(vehicle.driving){current=vehicle;Exit();}
                if(!VehiclePersistence.IsVehicle(data)) {
                    var root=ZNetScene.instance?.FindInstance(data.m_uid);if(root!=null)root.GetComponent<VehicleCargoVisibility>()?.Show();
                }
                current=vehicle;ReleaseVehicle();
            }
            fleet.Limit=NetworkObserversEnabled?8:VehicleFleet<VehicleRuntime>.Capacity;
            if(!NetworkObserversEnabled||SpawnBusy||Time.unscaledTime<networkScanTime||ZNetScene.instance==null)return;
            networkScanTime=Time.unscaledTime+1;
            var ids=ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.String,VehiclePersistence.Model.GetStableHashCode())
                .Concat(ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Int,VehiclePersistence.LegacyModel.GetStableHashCode())).Distinct();
            var records=ids.Select(ZDOMan.instance.GetZDO).Where(d=>VehiclePersistence.IsVehicle(d)&&VehiclePersistence.ValidPose(d)&&!networkDeferred.Contains(d.m_uid)&&!networkClient.WasRemoved(d.m_uid))
                .Where(d=>Vector3.Distance(local.transform.position,VehiclePersistence.Location(d))<=160&&ZNetScene.instance.FindInstance(d.m_uid)!=null)
                .OrderBy(d=>(VehiclePersistence.Location(d)-local.transform.position).sqrMagnitude).Take(8).ToArray();
            var wanted=new HashSet<ZDOID>(records.Select(d=>d.m_uid));
            foreach(var vehicle in fleet.Where(v=>v.networkData!=null&&!v.networkDriving&&!wanted.Contains(v.networkData.m_uid)).ToArray()) {
                current=vehicle;ReleaseVehicle();
            }
            foreach(var data in records) {
                if(Vector3.Distance(local.transform.position,VehiclePersistence.Location(data))>160)break;
                var root=ZNetScene.instance.FindInstance(data.m_uid);if(root==null)continue;
                VehicleStorage.Hide(root);
                if(fleet.Any(v=>v.networkData==data))continue;
                if(fleet.Count>=fleet.Limit)break;
                var profile=vehicleCatalog.Profiles.FirstOrDefault(p=>p.id==VehiclePersistence.Profile(data));
                if(profile==null){networkDeferred.Add(data.m_uid);Logger.LogWarning("Network vehicle profile unavailable: "+VehiclePersistence.Profile(data));continue;}
                try {StartVehicleLoad(local,profile,data);}
                catch(Exception error){networkDeferred.Add(data.m_uid);Logger.LogWarning("Network vehicle load deferred: "+error.Message);}
                break;
            }
        } finally {current=selectedWasFleet&&!fleet.Contains(selected)?fleet.FirstOrDefault()??new VehicleRuntime():selected;}
    }
    void TickNetworkObserver() {
        var data=current.networkData;if(data==null||body==null)return;
        var target=VehiclePersistence.Location(data);var rotation=VehiclePersistence.Orientation(data);
        if(target!=current.networkLastPosition){current.networkLastPosition=target;current.networkSampleTime=Time.unscaledTime;}
        var velocity=data.GetVec3(VehicleNetworkProtocol.Velocity,Vector3.zero);
        float age=Mathf.Clamp(Time.unscaledTime-current.networkSampleTime,0,.1f);
        target+=velocity*age;
        float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*15);
        if(Vector3.Distance(car.transform.position,target)>12)blend=1;
        car.transform.SetPositionAndRotation(Vector3.Lerp(car.transform.position,target,blend),Quaternion.Slerp(car.transform.rotation,rotation,blend));
        TickWorkshop();TickExhaustCustomization();wheelSizeVisuals?.Apply(RimSetting.Value);
        for(int i=0;i<4;i++)UpdateWheelVisual(i);
        bool active=data.GetLong(VehicleNetworkProtocol.Driver)!=0;
        string key=VehicleMotionTelemetry.Prefix;bool flying=data.GetBool(key+"flying"),running=data.GetBool(key+"running",VehiclePersistence.WasDriven(data)&&keepRunning.Value);
        float throttle=data.GetFloat(key+"throttle"),fold=data.GetFloat(key+"fold"),thrust=data.GetFloat(key+"thrust");
        bool pulse=data.GetBool(key+"backfire");bool backfire=pulse&&!current.networkObservedBackfire;current.networkObservedBackfire=pulse;
        ApplyNeonSettings();vehicleNeon?.Tick(active||running,Time.unscaledDeltaTime);
        vehicleLighting?.SetHeadlights(headlightPreset.Value,headlightStrength.Value);
        vehicleLighting?.Tick(active||running,active&&data.GetBool(key+"brake"),Vector3.Dot(velocity,car.transform.forward),throttle,Time.unscaledDeltaTime,false);
        nitroEffects?.Tick(data.GetBool(VehicleNetworkProtocol.Boost),data.GetBool(key+"purge"),backfire,!active,Time.unscaledDeltaTime);
        current.flightEffects?.TickFlight(fold,thrust,audioBlocked,originalWheels);
        current.flightAudio?.TickFlight(flying,fold,thrust,audioBlocked,soundVolume.Value);
        var sound=new EngineSoundSnapshot{Rpm=active?data.GetFloat(key+"rpm"):current.Profile.sound.minRpm,Load=active?data.GetFloat(key+"load"):0,
            Gear=data.GetInt(key+"gear"),Shifting=data.GetBool(key+"shifting"),Speed=velocity.magnitude,ForwardSpeed=Vector3.Dot(velocity,car.transform.forward),
            WheelRpm=(Mathf.Abs(data.GetFloat(key+"wheelrpm2"))+Mathf.Abs(data.GetFloat(key+"wheelrpm3")))*.5f,Contact=data.GetFloat(key+"contact"),Slip=data.GetFloat(key+"slip")};
        vehicleAudio?.SetEngineStage(PerformanceTuning.Stage(engineStage.Value));
        vehicleAudio?.Tick(active,throttle,data.GetBool(key+"brake"),data.GetBool(key+"horn"),audioBlocked,data.GetInt(key+"groundmode")==0,soundVolume.Value,data.GetBool(key+"burnout"),!active&&running,flying,true,sound);
        effectsAudio?.Tick(data.GetBool(VehicleNetworkProtocol.Boost),data.GetBool(key+"purge"),backfire,!active||audioBlocked,soundVolume.Value);
        showroom?.Tick(Time.unscaledDeltaTime,velocity.magnitude>.5f||flying,doorStyle.Value,active||running,trunkNeonEnabled.Value,trunkNeonPulsing.Value,NeonColors[Mathf.Max(0,System.Array.IndexOf(ColorNames,trunkNeonColor.Value))]);
        current.interior?.Tick(data.GetFloat(VehicleNetworkProtocol.Steering),windowTint.Value,false);
        TickRemoteCabin();
    }
}
}
