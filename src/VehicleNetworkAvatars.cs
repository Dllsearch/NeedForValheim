using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    sealed class RemoteCabin {
        public ZDOID Actor;public VehicleRuntime Vehicle;public Player Player;public VehicleCabinDriver Pose;
    }
    readonly Dictionary<ZDOID,RemoteCabin> remoteCabins=new Dictionary<ZDOID,RemoteCabin>();
    readonly Dictionary<VehicleRuntime,RemoteCabin> vehicleCabins=new Dictionary<VehicleRuntime,RemoteCabin>();
    readonly HashSet<ZDOID> failedRemoteCabins=new HashSet<ZDOID>();
    void ReleaseRemoteCabin(VehicleRuntime vehicle) {
        if(!vehicleCabins.TryGetValue(vehicle,out var record))return;
        vehicleCabins.Remove(vehicle);if(remoteCabins.TryGetValue(record.Actor,out var currentPose)&&currentPose==record)remoteCabins.Remove(record.Actor);
        record.Pose?.Dispose();vehicle.interior?.SetDriverWindowOpen(false);
    }
    void TickRemoteCabin() {
        var vehicle=current;var data=vehicle.networkData;
        if(vehicle.interior==null||data==null||data.GetLong(VehicleNetworkProtocol.Driver)==0||vehicle.networkDriving){ReleaseRemoteCabin(vehicle);return;}
        var actor=data.GetZDOID(VehicleNetworkProtocol.DriverCharacter);var root=ZNetScene.instance?.FindInstance(actor);var avatar=root!=null?root.GetComponent<Player>():null;
        if(avatar==null||avatar==Player.m_localPlayer){ReleaseRemoteCabin(vehicle);return;}
        if(vehicleCabins.TryGetValue(vehicle,out var record)&&(record.Player!=avatar||!record.Actor.Equals(actor))){ReleaseRemoteCabin(vehicle);record=null;}
        if(record==null) {
            if(failedRemoteCabins.Contains(actor))return;
            try {
                // Restore a prior car's layer before capturing the same avatar
                // for another car, including delayed ZDO role updates.
                if(remoteCabins.TryGetValue(actor,out var previous))ReleaseRemoteCabin(previous.Vehicle);
                record=new RemoteCabin{Actor=actor,Player=avatar,Vehicle=vehicle,Pose=new VehicleCabinDriver(avatar,car.transform,vehicle.interior,avatar.transform.position)};
                remoteCabins[actor]=record;vehicleCabins[vehicle]=record;
            }catch(Exception error){failedRemoteCabins.Add(actor);Logger.LogWarning("Remote cabin pose unavailable: "+error.Message);return;}
        }
        var actorData=avatar.GetComponent<ZNetView>()?.GetZDO();float blend=actorData?.GetFloat(VehicleNetworkProtocol.SeatBlend)??0;
        string key=VehicleMotionTelemetry.Prefix;
        try {
            vehicle.interior.SetDriverWindowOpen(blend>.5f);
            record.Pose.SetControls(data.GetFloat(key+"throttle"),data.GetBool(key+"brake")?1:0,false,data.GetInt(key+"gear"),data.GetFloat(VehicleNetworkProtocol.Steering)/32);
            record.Pose.TickObserver(Time.unscaledDeltaTime,blend,showroom?.State);
        }catch(Exception error){ReleaseRemoteCabin(vehicle);failedRemoteCabins.Add(actor);Logger.LogWarning("Remote cabin pose stopped: "+error.Message);}
    }
    void DisposeRemoteCabins() {
        foreach(var record in new List<RemoteCabin>(vehicleCabins.Values))ReleaseRemoteCabin(record.Vehicle);
        failedRemoteCabins.Clear();
    }
}
}
