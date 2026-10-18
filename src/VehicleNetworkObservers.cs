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
            var loadedIds=new HashSet<ZDOID>(fleet.Where(v=>v.networkData!=null).Select(v=>v.networkData.m_uid));
            var records=ids.Select(ZDOMan.instance.GetZDO).Where(d=>VehiclePersistence.IsVehicle(d)&&VehiclePersistence.ValidPose(d)&&!networkDeferred.Contains(d.m_uid)&&!networkClient.WasRemoved(d.m_uid))
                .Where(d=>VehicleWorldStreaming.KeepObserver(Vector3.Distance(local.transform.position,VehiclePersistence.Location(d)),loadedIds.Contains(d.m_uid),ZNetScene.instance.FindInstance(d.m_uid)!=null))
                .OrderByDescending(d=>loadedIds.Contains(d.m_uid)).ThenBy(d=>(VehiclePersistence.Location(d)-local.transform.position).sqrMagnitude).Take(8).ToArray();
            var wanted=new HashSet<ZDOID>(records.Select(d=>d.m_uid));
            foreach(var vehicle in fleet.Where(v=>v.networkData!=null&&!v.networkDriving&&!wanted.Contains(v.networkData.m_uid)).ToArray()) {
                current=vehicle;ReleaseVehicle();
            }
            foreach(var data in records) {
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
        var velocity=data.GetVec3(VehicleNetworkProtocol.Velocity,Vector3.zero);
        int sequence=data.GetInt("nfvu2_motion_sequence",-1),epoch=data.GetInt("nfvu2_motion_epoch");long driver=data.GetLong(VehicleNetworkProtocol.Driver);
        bool first=current.networkMotion==null;
        if(first){current.networkMotion=new VehicleRemoteMotion();current.networkPresentation=new VehicleRemoteMotion.Presentation();}
        if(first||epoch!=current.networkMotionEpoch||sequence!=current.networkObservedSequence||driver!=current.networkObservedDriver||target!=current.networkLastPosition||rotation!=current.networkLastRotation||velocity!=current.networkLastVelocity) {
            bool reset=first||epoch!=current.networkMotionEpoch||driver!=current.networkObservedDriver||
                VehicleRemoteMotion.RequiresReset(Vector3.Distance(target,current.networkLastPosition),current.networkLastVelocity.magnitude,velocity.magnitude,Time.unscaledTime-current.networkSampleTime);
            string prefix=VehicleMotionTelemetry.Prefix;
            var detail=new VehicleRemoteMotion.Presentation{Steering=data.GetFloat(VehicleNetworkProtocol.Steering),
                Fold=data.GetFloat(prefix+"fold"),Pitch=data.GetFloat(prefix+"pitch"),Roll=data.GetFloat(prefix+"roll"),Thrust=data.GetFloat(prefix+"thrust"),
                Rpm=data.GetFloat(prefix+"rpm"),Load=data.GetFloat(prefix+"load"),Throttle=data.GetFloat(prefix+"throttle")};
            for(int i=0;i<4;i++){detail.Wheels[i]=data.GetVec3(prefix+"wheel"+i,wheels[i].transform.localPosition);detail.Rotations[i]=data.GetQuaternion(prefix+"rotation"+i,Quaternion.identity);detail.WheelRpm[i]=data.GetFloat(prefix+"wheelrpm"+i);}
            current.networkMotion.Push(Time.unscaledTime,target,rotation,velocity,reset,detail);
            current.networkMotionEpoch=epoch;current.networkObservedSequence=sequence;current.networkObservedDriver=driver;
            current.networkLastPosition=target;current.networkLastRotation=rotation;current.networkLastVelocity=velocity;
            current.networkSampleTime=Time.unscaledTime;
        }
        current.networkMotion.Evaluate(Time.unscaledTime,out var renderedPosition,out var renderedRotation);
        current.networkMotion.EvaluatePresentation(Time.unscaledTime,current.networkPresentation);
        var presentation=current.networkPresentation;
        car.transform.SetPositionAndRotation(renderedPosition,renderedRotation);
        TickWorkshop();TickExhaustCustomization();wheelSizeVisuals?.Apply(RimSetting.Value);
        for(int i=0;i<4;i++)UpdateWheelVisual(i);
        bool active=data.GetLong(VehicleNetworkProtocol.Driver)!=0;
        string key=VehicleMotionTelemetry.Prefix;bool flying=data.GetBool(key+"flying"),running=data.GetBool(key+"running",VehiclePersistence.WasDriven(data)&&keepRunning.Value);
        float throttle=presentation.Throttle,fold=presentation.Fold,thrust=presentation.Thrust;
        bool pulse=data.GetBool(key+"backfire");bool backfire=pulse&&!current.networkObservedBackfire;current.networkObservedBackfire=pulse;
        ApplyNeonSettings();vehicleNeon?.Tick(active||running,Time.unscaledDeltaTime);
        vehicleLighting?.SetHeadlights(headlightPreset.Value,headlightStrength.Value);
        vehicleLighting?.Tick(active||running,active&&data.GetBool(key+"brake"),Vector3.Dot(velocity,car.transform.forward),throttle,Time.unscaledDeltaTime,false);
        nitroEffects?.Tick(data.GetBool(VehicleNetworkProtocol.Boost),data.GetBool(key+"purge"),backfire,!active,Time.unscaledDeltaTime);
        current.flightEffects?.TickFlight(fold,thrust,audioBlocked,originalWheels);
        current.flightAudio?.TickFlight(flying,fold,thrust,audioBlocked,soundVolume.Value);
        var sound=new EngineSoundSnapshot{Rpm=active?presentation.Rpm:current.Profile.sound.minRpm,Load=active?presentation.Load:0,
            Gear=data.GetInt(key+"gear"),Shifting=data.GetBool(key+"shifting"),Speed=velocity.magnitude,ForwardSpeed=Vector3.Dot(velocity,car.transform.forward),
            WheelRpm=(Mathf.Abs(presentation.WheelRpm[2])+Mathf.Abs(presentation.WheelRpm[3]))*.5f,Contact=data.GetFloat(key+"contact"),Slip=data.GetFloat(key+"slip")};
        vehicleAudio?.SetEngineStage(PerformanceTuning.Stage(engineStage.Value));
        vehicleAudio?.Tick(active,throttle,data.GetBool(key+"brake"),data.GetBool(key+"horn"),audioBlocked,data.GetInt(key+"groundmode")==0,soundVolume.Value,data.GetBool(key+"burnout"),!active&&running,flying,true,sound);
        effectsAudio?.Tick(data.GetBool(VehicleNetworkProtocol.Boost),data.GetBool(key+"purge"),backfire,!active||audioBlocked,soundVolume.Value);
        ApplyShowroomSettings();
        showroom?.Tick(Time.unscaledDeltaTime,velocity.magnitude>.5f||flying,doorStyle.Value,active||running,trunkNeonEnabled.Value,trunkNeonPulsing.Value,NeonColors[Mathf.Max(0,System.Array.IndexOf(ColorNames,trunkNeonColor.Value))]);
        // Once the local player releases the seat this observer branch owns
        // presentation. Keep ticking radio so its cabin mix becomes spatial.
        vehicleRadio?.Tick(false,audioBlocked,radioVolume.Value,radioEnabled.Value,active||running,
            showroom?.State.Progress[1]??0,showroom?.State.Progress[0]??0,showroom?.State.Progress[3]??0,radioAutoPlay.Value);
        TickShowroomBass();
        current.interior?.Tick(presentation.Steering,windowTint.Value,false);
        TickRemoteCabin();
    }
}
}
