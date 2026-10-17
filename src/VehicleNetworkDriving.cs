using System;
using System.Linq;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    bool networkSpawnPending;
    bool OwnNetworkVehicle(VehicleRuntime vehicle)=>vehicle.networkData!=null&&LocalPlayer() is Player local&&(vehicle.networkData.GetLong(VehicleNetworkProtocol.Creator)==local.GetPlayerID()||networkClient?.OwnedVehicles.Any(v=>v.Id.Equals(vehicle.networkData.m_uid))==true);
    bool NetworkSessionActive=>ZNet.instance!=null&&!ZNet.IsSinglePlayer;
    void RequestNetworkDriver(MonoBehaviour local) {
        var vehicle=current;var data=vehicle.networkData;
        if(data==null||vehicle.networkClaimPending||!NetworkObserversEnabled||Vector3.Distance(local.transform.position,VehiclePersistence.Location(data))>6)return;
        if(data.GetVec3(VehicleNetworkProtocol.Velocity,Vector3.zero).magnitude>.5f){ShowVehicleWarning("Wait until the vehicle stops.");return;}
        vehicle.networkClaimPending=true;var world=ZDOMan.instance;
        networkClient.Send(VehicleNetworkProtocol.Command.Claim,data.m_uid,null,(ok,message,id)=>{
            vehicle.networkClaimPending=false;
            if(!ok){ShowVehicleWarning(message);return;}
            var previous=current;
            try {
                current=vehicle;
                if(world!=ZDOMan.instance||!NetworkObserversEnabled||!fleet.Contains(vehicle)||local!=LocalPlayer()||fleet.Any(v=>v.driving)||SpawnBusy||
                    Vector3.Distance(local.transform.position,vehicle.car.transform.position)>6||!ParkingTerrainLoaded()) {
                    networkClient.Send(VehicleNetworkProtocol.Command.Release,id,null,null);return;
                }
                vehicle.networkDriving=true;vehicle.networkClaimTime=Time.unscaledTime;vehicle.networkSequence=0;vehicle.networkPublishTime=0;
                vehicleNitro?.State.RestoreCharge(data.GetFloat(VehicleMotionTelemetry.Prefix+"charge",1));
                current.flight?.Nitro.RestoreCharge(data.GetFloat(VehicleMotionTelemetry.Prefix+"charge",1));
                body.position=VehiclePersistence.Location(data);body.rotation=VehiclePersistence.Orientation(data);
                body.isKinematic=false;current.parkingFrozen=false;
                foreach(var wheel in wheels)wheel.enabled=true;
                int mode=Mathf.Clamp(data.GetInt(VehicleMotionTelemetry.Prefix+"groundmode"),0,2);
                vehicle.networkSettings.SetRuntime(terrainMode,mode==0);
                vehicle.networkSettings.SetRuntime(driftMode,mode==2&&vehicle.Profile.capabilities.drift);
                ApplyDriveMode();
                // Reclaiming a previously flown instance must pass normal entry
                // while grounded, then restore the suspended flight controller.
                current.flight?.Dispose();current.flight=new VehicleFlight(body,wheels,vehicleNitro?.State);
                Enter(local);
                if(driving&&data.GetBool(VehicleMotionTelemetry.Prefix+"flying"))current.flight.Resume(data.GetBool(VehicleMotionTelemetry.Prefix+"landing"),data.GetFloat(VehicleMotionTelemetry.Prefix+"fold",1));
                if(!driving)StopNetworkDriving(false);else previous=vehicle;
            }catch(Exception error){
                if(driving)Exit();else StopNetworkDriving(false);
                Logger.LogWarning("Network driver entry failed: "+error.Message);
            }finally {current=fleet.Contains(previous)?previous:fleet.FirstOrDefault()??new VehicleRuntime();}
        });
    }
    void TickNetworkDriving() {
        var vehicle=fleet.FirstOrDefault(v=>v.networkDriving);if(vehicle==null)return;
        var previous=current;
        try {
            current=vehicle;var data=vehicle.networkData;
            bool gone=!NetworkObserversEnabled||ZDOMan.instance==null||ZDOMan.instance.GetZDO(data.m_uid)!=data||!VehiclePersistence.IsVehicle(data);
            bool revoked=Time.unscaledTime-vehicle.networkClaimTime>2&&data.GetLong(VehicleNetworkProtocol.Driver)!=ZNet.GetUID();
            if(gone||revoked||!driving){if(driving)Exit();else StopNetworkDriving();ShowVehicleWarning("Vehicle connection lost; character control restored.");return;}
            vehicle.networkWheelPhase=(vehicle.networkWheelPhase+wheels[0].rpm*6*Time.unscaledDeltaTime)%360;
            if(Time.unscaledTime>=vehicle.networkPublishTime){vehicle.networkPublishTime=Time.unscaledTime+.05f;PublishNetworkState();}
        } finally {current=fleet.Contains(previous)?previous:fleet.FirstOrDefault()??new VehicleRuntime();}
    }
    void PublishNetworkState() {
        if(!current.networkDriving||current.networkData==null||body==null||!NetworkObserversEnabled)return;
        var vehicle=current;
        networkClient.Send(VehicleNetworkProtocol.Command.State,vehicle.networkData.m_uid,p=>WriteNetworkMotion(vehicle,p),null);
    }
    void WriteDriverSeatBlend(float blend) {
        var actor=player!=null?player.GetComponent<ZNetView>()?.GetZDO():null;
        if(actor!=null&&ZDOMan.instance!=null&&ZDOMan.instance.GetZDO(actor.m_uid)==actor&&actor.IsOwner())actor.Set(VehicleNetworkProtocol.SeatBlend,Mathf.Clamp01(blend));
    }
    void WriteNetworkMotion(VehicleRuntime vehicle,ZPackage p) {
        WriteDriverSeatBlend(driving?cabinDriver?.Motion.SeatBlend??1:0);
        p.Write(vehicle.networkSequence++);p.Write(car.transform.position);p.Write(car.transform.rotation);p.Write(body.linearVelocity);
        p.Write(wheels[0].steerAngle);p.Write(vehicle.networkWheelPhase);p.Write(ActiveNitro!=null&&ActiveNitro.Boosting);
        var motion=new VehicleMotionTelemetry{Rpm=vehicleAudio?.Rpm??0,Load=vehicleAudio?.Load??0,Gear=vehicleAudio?.Gear??1,Shifting=vehicleAudio?.IsShifting==true,
            Throttle=throttle,Braking=brake>0,Horn=honking,Burnout=burnoutHeld,Purge=vehicleNitro?.State.Purging==true,Backfire=vehicle.networkBackfire,
            Charge=ActiveNitro?.Charge??1,Flying=Flying,Landing=Flying&&current.flight.State.Landing,GroundMode=driftMode.Value?2:terrainMode.Value?0:1,Running=driving||ParkedRunning,Fold=Flying?current.flight.State.Blend:0,
            Pitch=current.flight?.WheelPitch??0,Roll=current.flight?.WheelRoll??0,Thrust=current.flight?.Thrust??0};
        for(int i=0;i<4;i++) {
            Vector3 pos;Quaternion rot;if(Flying)current.flight.WheelPose(i,out pos,out rot);else wheels[i].GetWorldPose(out pos,out rot);
            motion.Wheels[i]=car.transform.InverseTransformPoint(pos);motion.Rotations[i]=Quaternion.Inverse(car.transform.rotation)*rot;motion.WheelRpm[i]=wheels[i].rpm;
            if(wheels[i].enabled&&wheels[i].GetGroundHit(out var hit)) {
                motion.Contact+=.25f;float slip=Mathf.Max(Mathf.Abs(hit.sidewaysSlip),Mathf.Abs(hit.forwardSlip));
                if(!float.IsNaN(slip)&&!float.IsInfinity(slip))motion.Slip=Mathf.Max(motion.Slip,Mathf.Clamp(slip,0,20));
            }
        }
        motion.Write(p);
        vehicle.networkBackfire=false;
    }
    void StopNetworkDriving(bool publishFinal=true) {
        if(!current.networkDriving)return;
        WriteDriverSeatBlend(0);
        var data=current.networkData;
        if(data!=null&&NetworkObserversEnabled)networkClient.Send(VehicleNetworkProtocol.Command.Release,data.m_uid,publishFinal?(Action<ZPackage>)(p=>WriteNetworkMotion(current,p)):null,null);
        current.networkDriving=false;
        if(body!=null){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;current.parkingFrozen=true;}
        if(wheels!=null)foreach(var wheel in wheels)wheel.enabled=false;

    }
    void RequestNetworkSpawn(MonoBehaviour local) {
        if(!NetworkObserversEnabled||SpawnBusy||driving||local==null)return;
        var profile=SelectedProfile;
        if(!FindSpawnPoint(local,profile,out var origin)){ShowVehicleWarning("No clear spawn location remains.");return;}
        var rotation=SpawnRotation(local);var previous=current;string setup;
        try {current=new VehicleRuntime{Profile=profile};setup=CaptureWorldBuild();}finally {current=previous;}
        networkSpawnPending=true;CloseVehicleMenu();var world=ZDOMan.instance;
        networkClient.Send(VehicleNetworkProtocol.Command.Spawn,ZDOID.None,p=>{p.Write(profile.id);p.Write(setup);p.Write(origin);p.Write(rotation);},(ok,message,id)=>{
            networkSpawnPending=false;if(world!=ZDOMan.instance)return;
            ShowVehicleWarning(ok?"Vehicle saved on server; loading its model.":message);
        });
    }
    void RecoverNetworkVehicle(ZDOID id,string profileId) {
        if(!NetworkObserversEnabled||SpawnBusy||driving||!(LocalPlayer() is Player local))return;
        var profile=vehicleCatalog.Profiles.FirstOrDefault(p=>p.id==profileId);if(profile==null)return;
        if(!FindSpawnPoint(local,profile,out var position)){ShowVehicleWarning("No clear ground nearby for recovery.");return;}
        var rotation=SpawnRotation(local);var world=ZDOMan.instance;
        networkClient.Send(VehicleNetworkProtocol.Command.Recover,id,p=>{p.Write(position);p.Write(rotation);},(ok,message,target)=>{
            if(world!=ZDOMan.instance)return;
            if(ok){networkDeferred.Remove(id);networkScanTime=0;}
            if(ok)foreach(var vehicle in fleet.Where(v=>v.networkData!=null&&v.networkData.m_uid.Equals(id)).ToArray()) {
                var selected=current;
                try {current=vehicle;ReleaseVehicle();}
                finally {current=fleet.Contains(selected)?selected:fleet.FirstOrDefault()??new VehicleRuntime();}
            }
            ShowVehicleWarning(message);
        });
    }
    void RemoveDistantNetworkVehicle(NetworkVehicleSummary saved) {
        if(!NetworkObserversEnabled||SpawnBusy)return;var world=ZDOMan.instance;
        networkClient.Send(VehicleNetworkProtocol.Command.Remove,saved.Id,null,(ok,message,id)=>{if(world==ZDOMan.instance)ShowVehicleWarning(message);});
    }
    void RequestNetworkRemoval(VehicleRuntime vehicle) {
        if(vehicle?.networkData==null||!NetworkObserversEnabled||SpawnBusy)return;
        var previous=current;
        try {
            current=vehicle;
            if(driving) {
                if(player==null||!FindCabinExit(player,out var safe,true)){ShowVehicleWarning("No safe ground nearby. The vehicle has not been removed.");return;}
                Exit(safe);
            }
        } finally {current=previous;}
        var world=ZDOMan.instance;var data=vehicle.networkData;
        networkClient.Send(VehicleNetworkProtocol.Command.Remove,data.m_uid,null,(ok,message,id)=>{
            if(world!=ZDOMan.instance)return;
            if(ok&&fleet.Contains(vehicle)) {
                var selected=current;try {current=vehicle;ReleaseVehicle();}finally {current=fleet.Contains(selected)?selected:fleet.FirstOrDefault()??new VehicleRuntime();}
            }
            ShowVehicleWarning(message);
        });
    }
}
}
