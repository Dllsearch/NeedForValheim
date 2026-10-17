using System;
using System.Linq;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    bool CanOpenStorage=>LocalPlayer() is Player local&&!local.IsDead()&&!local.IsTeleporting()&&!local.IsAttached()&&
        CanAccessCurrent(local)&&(!NetworkSessionActive||OwnNetworkVehicle(current)&&current.networkData.GetLong(VehicleNetworkProtocol.Driver)==0&&current.networkData.GetVec3(VehicleNetworkProtocol.Velocity,Vector3.zero).magnitude<=.5f&&networkCargo==null&&!networkCargoRequestPending)&&!Multiplayer()&&!driving&&!Flying&&!HordeVehicle&&body!=null&&body.linearVelocity.magnitude<=.5f&&
        Vector3.Distance(local.transform.position,car.transform.TransformPoint(new Vector3(0,.5f,-current.Profile.rig.trunkApproachZ)))<3.5f;
    void OpenVehicleStorage() {
        if(!CanOpenStorage)return;
        if(NetworkSessionActive){RequestNetworkStorage();return;}
        try {
            if(current.storage==null)current.storage=new VehicleStorage(car,current.Profile,trunkLayout.Value);
            CloseVehicleMenu();
            if(current.storage.Open(Tr("Storage")+" · "+VehicleName)) {
                if(showroom!=null)showroom.State.Open[3]=true;
            } else ShowVehicleWarning("Storage is currently unavailable.");
        } catch(Exception e){Logger.LogError("Vehicle storage: "+e);ShowVehicleWarning("Storage is currently unavailable.");}
    }
    void DrawStorageButton(Rect rect) {
        if(NetworkSessionActive){GUI.enabled=CanOpenStorage;if(MenuButton(rect,"Open storage"))OpenVehicleStorage();GUI.enabled=true;return;}
        int capacity=current.storage?.Capacity??VehicleStorageRules.Capacity(current.Profile,trunkLayout.Value);
        int count=current.storage?.Count??0;
        GUI.enabled=CanOpenStorage;
        if(MenuButton(rect,Tr("Open storage")+" · "+(count<0?"?":count.ToString())+" / "+capacity))OpenVehicleStorage();
        GUI.enabled=true;
    }
    bool SetTrunkLayout(int layout) {
        layout=VehicleStorageRules.Layout(layout);
        if(!CanSetTrunkLayout(layout)) {
            ShowVehicleWarning("Remove excess cargo and close storage before selecting this layout.");return false;
        }
        foreach(var v in fleet)if(v.Profile.id==current.Profile.id&&v.Profile.capabilities.showroom&&v.storage!=null)v.storage.Resize(layout);
        trunkLayout.Value=layout;return true;
    }
    bool CanSetTrunkLayout(int layout)=>fleet.All(v=>v.Profile.id!=current.Profile.id||!v.Profile.capabilities.showroom||v.storage==null||v.storage.CanResize(VehicleStorageRules.Layout(layout)));
    void TickVehicleStorage() {
        if(current.storage==null)return;
        current.storage.Tick();
        if(current.Profile.capabilities.showroom&&current.storage.Layout!=trunkLayout.Value&&!SetTrunkLayout(trunkLayout.Value))trunkLayout.Value=current.storage.Layout;
    }
}
}
