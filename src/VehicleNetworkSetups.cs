using System.Linq;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    float networkNextSetupCommand;
    VehicleRuntime CreateNetworkRuntime(CarProfile profile,ZDO data) {
        var vehicle=new VehicleRuntime{Profile=profile,networkData=data};
        if(data!=null) {
            vehicle.networkSetupText=VehiclePersistence.Settings(data);vehicle.networkSetupRevision=data.GetInt(VehicleNetworkProtocol.SetupRevision);
            vehicle.networkSettings=new NetworkVehicleSettings(vehicle.networkSetupText,()=>uiBuilding,()=>CanEditNetworkVehicle(vehicle),new System.Collections.Generic.Dictionary<string,string>{{profile.ConfigPrefix+"Showroom/TrunkLayout",VehiclePersistence.Trunk(data).ToString(System.Globalization.CultureInfo.InvariantCulture)}});
        }
        return vehicle;
    }
    bool CanEditNetworkVehicle(VehicleRuntime vehicle) {
        if(vehicle==null||!NetworkObserversEnabled||vehicle.networkData==null||vehicle.networkSetupInFlight||!OwnNetworkVehicle(vehicle)||LocalPlayer()==null||vehicle.car==null)return false;
        var data=vehicle.networkData;
        float speed=vehicle.networkDriving?vehicle.body.linearVelocity.magnitude:data.GetVec3(VehicleNetworkProtocol.Velocity,Vector3.zero).magnitude;
        long driver=data.GetLong(VehicleNetworkProtocol.Driver);
        return VehiclePersistence.IsVehicle(data)&&Vector3.Distance(LocalPlayer().transform.position,vehicle.car.transform.position)<=10&&speed<.5f&&(driver==0||driver==ZNet.GetUID());
    }
    void TickNetworkSetups() {
        if(!NetworkObserversEnabled)return;
        foreach(var vehicle in fleet.Where(v=>v.networkData!=null&&v.networkSettings!=null).ToArray()) {
            var data=vehicle.networkData;int revision=data.GetInt(VehicleNetworkProtocol.SetupRevision);
            if(vehicle.networkSetupInFlight)continue;
            if(revision>vehicle.networkSetupRevision) {
                try {
                    vehicle.networkSettings.Replace(VehiclePersistence.Settings(data));vehicle.networkSetupRevision=revision;
                    vehicle.networkSetupText=VehiclePersistence.Settings(data);vehicle.networkSetupAcknowledgedVersion=vehicle.networkSettings.Version;
                }catch(System.Exception error){Logger.LogWarning("Network setup update rejected: "+error.Message);}
            }
            long version=vehicle.networkSettings.Version;
            if(version==vehicle.networkSetupAcknowledgedVersion)continue;
            if(!CanEditNetworkVehicle(vehicle))continue;
            if(version!=vehicle.networkSetupEditVersion){vehicle.networkSetupEditVersion=version;vehicle.networkSetupSendTime=Time.unscaledTime+.4f;continue;}
            if(Time.unscaledTime<vehicle.networkSetupSendTime||Time.unscaledTime<networkNextSetupCommand)continue;
            string snapshot=vehicle.networkSettings.Snapshot();int expected=vehicle.networkSetupRevision;var world=ZDOMan.instance;
            vehicle.networkSetupInFlight=true;networkNextSetupCommand=Time.unscaledTime+.3f;
            networkClient.Send(VehicleNetworkProtocol.Command.Setup,data.m_uid,p=>{p.Write(expected);p.Write(snapshot);},(ok,message,id)=>{
                vehicle.networkSetupInFlight=false;if(world!=ZDOMan.instance)return;
                bool committed=ok||data.GetInt(VehicleNetworkProtocol.SetupRevision)>expected&&VehiclePersistence.Settings(data)==snapshot;
                if(committed){vehicle.networkSetupRevision=expected+1;vehicle.networkSetupText=snapshot;}
                else {
                    vehicle.networkSettings.Replace(vehicle.networkSetupText);
                    if(vehicle.networkData==current.networkData||garagePreview?.networkPreviewOf==vehicle)workshopMessage=message;
                }
                vehicle.networkSetupAcknowledgedVersion=version;
            });
        }
    }
}
}
