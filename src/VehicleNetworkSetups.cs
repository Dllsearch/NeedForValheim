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
    bool CanEditNetworkVehicle(VehicleRuntime vehicle)=>NetworkVehicleEditBlock(vehicle)==null;
    string NetworkVehicleEditBlock(VehicleRuntime vehicle) {
        vehicle=vehicle?.networkPreviewOf??vehicle;
        if(!NetworkObserversEnabled)return "Waiting for a compatible vehicle server.";
        if(vehicle?.networkData==null||vehicle.car==null)return "Spawn or recover your vehicle nearby before editing it.";
        if(!OwnNetworkVehicle(vehicle))return "Only the vehicle owner can change its setup.";
        var local=LocalPlayer();if(local==null)return "Player character is not ready.";
        var data=vehicle.networkData;
        if(!VehiclePersistence.IsVehicle(data))return "Vehicle no longer exists.";
        if(Vector3.Distance(local.transform.position,VehiclePersistence.Location(data))>10)return "Move within 10 metres of your vehicle.";
        float speed=vehicle.networkDriving&&vehicle.body!=null?vehicle.body.linearVelocity.magnitude:data.GetVec3(VehicleNetworkProtocol.Velocity,Vector3.zero).magnitude;
        if(speed>=.5f)return "Stop your vehicle before editing it.";
        long driver=data.GetLong(VehicleNetworkProtocol.Driver);
        if(driver!=0&&driver!=ZNet.GetUID())return "Wait until the other driver leaves your vehicle.";
        // Edits made during an outstanding request remain queued by Version;
        // only transport dispatch waits for the acknowledgement, not the UI.
        return null;
    }
    string WorkshopBlockedMessage=>NetworkSessionActive?NetworkVehicleEditBlock(current)??"Park and finish the horde run before changing parts.":"Park and finish the horde run before changing parts.";
    void TickNetworkSetups() {
        if(!NetworkObserversEnabled)return;
        foreach(var vehicle in fleet.Where(v=>v.networkData!=null&&v.networkSettings!=null).ToArray()) {
            var data=vehicle.networkData;int revision=data.GetInt(VehicleNetworkProtocol.SetupRevision);
            if(vehicle.networkSetupInFlight)continue;
            if(revision>vehicle.networkSetupRevision) {
                try {
                    string serverSnapshot=VehiclePersistence.Settings(data);
                    bool pending=vehicle.networkSettings.Rebase(serverSnapshot,vehicle.networkSetupText);
                    vehicle.networkSetupRevision=revision;vehicle.networkSetupText=serverSnapshot;
                    if(!pending)vehicle.networkSetupAcknowledgedVersion=vehicle.networkSettings.Version;
                }catch(System.Exception error){Logger.LogWarning("Network setup update rejected: "+error.Message);}
            }
            long version=vehicle.networkSettings.Version;
            if(version==vehicle.networkSetupAcknowledgedVersion)continue;
            if(!CanEditNetworkVehicle(vehicle))continue;
            if(version!=vehicle.networkSetupEditVersion){vehicle.networkSetupEditVersion=version;vehicle.networkSetupSendTime=Time.unscaledTime+.4f;continue;}
            if(Time.unscaledTime<vehicle.networkSetupSendTime||Time.unscaledTime<networkNextSetupCommand)continue;
            string snapshot=vehicle.networkSettings.Snapshot(),confirmed=vehicle.networkSetupText;int expected=vehicle.networkSetupRevision;var world=ZDOMan.instance;
            vehicle.networkSetupInFlight=true;networkNextSetupCommand=Time.unscaledTime+.3f;
            networkClient.Send(VehicleNetworkProtocol.Command.Setup,data.m_uid,p=>{p.Write(expected);p.Write(snapshot);},(ok,message,id)=>{
                vehicle.networkSetupInFlight=false;if(world!=ZDOMan.instance)return;
                bool committed=ok||data.GetInt(VehicleNetworkProtocol.SetupRevision)>expected&&VehiclePersistence.Settings(data)==snapshot;
                bool pending;
                if(committed){
                    pending=vehicle.networkSettings.Rebase(snapshot,snapshot);
                    vehicle.networkSetupRevision=expected+1;vehicle.networkSetupText=snapshot;
                    if((vehicle.networkData==current.networkData||garagePreview?.networkPreviewOf==vehicle)&&workshopMessage==vehicle.networkSetupMessage)workshopMessage="";
                    vehicle.networkSetupMessage=null;
                }
                else {
                    Logger.LogWarning("Vehicle setup rejected: "+message);
                    string serverSnapshot=data.GetInt(VehicleNetworkProtocol.SetupRevision)>expected?VehiclePersistence.Settings(data):vehicle.networkSetupText;
                    // Conflicts and transport uncertainty do not reject the user's
                    // settings. Rebase against the last acknowledged snapshot so
                    // the submitted changes stay queued. Validation/authority
                    // failures discard only that submission, retaining later edits.
                    bool retry=message==VehicleNetworkProtocol.SetupConflict||message=="Vehicle request timed out"||
                        message=="Vehicle request could not be sent"||message=="Too many pending vehicle requests"||
                        message=="Compatible NFVU2 server is not ready"||message=="Vehicle connection changed"||message=="Vehicle connection closed";
                    pending=vehicle.networkSettings.Rebase(serverSnapshot,retry?confirmed:snapshot);
                    vehicle.networkSetupText=serverSnapshot;vehicle.networkSetupRevision=data.GetInt(VehicleNetworkProtocol.SetupRevision);
                    if(retry&&pending)vehicle.networkSetupSendTime=Time.unscaledTime+1;
                    vehicle.networkSetupMessage=retry&&pending?"Retrying vehicle setup synchronization…":message;
                    if(vehicle.networkData==current.networkData||garagePreview?.networkPreviewOf==vehicle)workshopMessage=vehicle.networkSetupMessage;
                }
                vehicle.networkSetupAcknowledgedVersion=pending?-1:vehicle.networkSettings.Version;
            });
        }
    }
}
}
