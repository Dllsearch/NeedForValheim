using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    static readonly MethodInfo CargoLoad=AccessTools.Method(typeof(Container),"Load"),CargoSave=AccessTools.Method(typeof(Container),"Save"),CargoRows=AccessTools.Method(typeof(Container),"UpdateRows");
    static readonly FieldInfo CargoTitle=AccessTools.Field(typeof(Inventory),"m_name"),CargoInUse=AccessTools.Field(typeof(Container),"m_inUse");
    sealed class NetworkCargoView {
        public Container Container;public ZDO Data;public ZDOMan World;public VehicleRuntime Vehicle;
        public bool Seen,Closing,KeepPending,ClosePending;public float Deadline,NextKeep,NextClose;public string Hash,Title;
    }
    NetworkCargoView networkCargo;bool networkCargoRequestPending;
    void RequestNetworkStorage() {
        if(!CanOpenStorage||!NetworkObserversEnabled||networkCargo!=null||networkCargoRequestPending||current.networkData==null)return;
        var vehicle=current;var data=vehicle.networkData;var world=ZDOMan.instance;
        var root=ZNetScene.instance?.FindInstance(data.m_uid);var container=root!=null?root.GetComponent<Container>():null;
        if(container==null){ShowVehicleWarning("Cargo is still loading.");return;}
        networkCargoRequestPending=true;
        networkClient.Send(VehicleNetworkProtocol.Command.CargoOpen,data.m_uid,null,(ok,message,id)=>{
            networkCargoRequestPending=false;if(world!=ZDOMan.instance)return;
            if(!ok){ShowVehicleWarning(message);return;}
            if(!NetworkObserversEnabled||container==null||LocalPlayer() is not Humanoid local||Vector3.Distance(local.transform.position,data.GetPosition())>4) {
                networkClient.Send(VehicleNetworkProtocol.Command.CargoClose,id,p=>p.Write(""),null);return;
            }
            var view=new NetworkCargoView{Container=container,Data=data,World=world,Vehicle=vehicle,Deadline=Time.unscaledTime+5,NextKeep=Time.unscaledTime+1,Title=CargoTitle.GetValue(container.GetInventory()) as string};
            networkCargo=view;CloseVehicleMenu();
            try {
                PrepareNetworkCargo(container);CargoTitle.SetValue(container.GetInventory(),Tr("Storage")+" / "+vehicle.Profile.displayName);
                container.Interact(local,false,false);
            }catch(Exception error){Logger.LogWarning("Network cargo opening: "+error.Message);BeginCargoClose(view);}
        });
    }
    void PrepareNetworkCargo(Container container) {
        var data=container.GetComponent<ZNetView>()?.GetZDO();if(data==null)return;
        container.transform.SetPositionAndRotation(data.GetPosition(),data.GetRotation());
        var profile=vehicleCatalog.Get(VehiclePersistence.Profile(data));
        container.m_height=VehicleStorageRules.Rows(profile,VehiclePersistence.Trunk(data));
        CargoLoad.Invoke(container,null);CargoRows.Invoke(container,null);
    }
    internal bool PrepareCargoResponse(Container container,long sender,bool granted) {
        if(!NetworkSessionActive||!VehiclePersistence.IsVehicle(container.GetComponent<ZNetView>()?.GetZDO()))return true;
        if(!granted)return true;
        var view=networkCargo;
        if(view==null||view.Container!=container||view.Closing||view.World!=ZDOMan.instance||!NetworkObserversEnabled||sender!=networkClient.Server)return false;
        PrepareNetworkCargo(container);return true;
    }
    void BeginCargoClose(NetworkCargoView view) {
        if(view.Closing)return;
        if(view.Container!=null&&view.Container.IsInUse()&&InventoryGui.instance!=null&&Player.m_localPlayer!=null)InventoryGui.instance.Hide();
        if(view.World!=ZDOMan.instance){ClearNetworkCargo(view);return;}
        var native=view.Container!=null?view.Container.GetComponent<ZNetView>():null;
        if(view.Seen&&native!=null&&native.IsOwner())CargoSave.Invoke(view.Container,null);
        view.Hash=!view.Seen&&(native==null||!native.IsOwner())?"":VehicleNetworkProtocol.InventoryHash(view.Data.GetByteArray(ZDOVars.s_items,null));
        view.Closing=true;view.NextClose=0;
        ForceCargoSend(view);
    }
    void ForceCargoSend(NetworkCargoView view) {
        if(view.World!=ZDOMan.instance||view.Data==null||ZNet.instance==null||ZNet.instance.IsServer())return;
        if(view.Container!=null&&view.Container.GetComponent<ZNetView>()?.IsOwner()==true)ZDOMan.instance.ForceSendZDO(networkClient.Server,view.Data.m_uid);
    }
    void ClearNetworkCargo(NetworkCargoView view) {
        if(networkCargo!=view)return;
        if(view.World==ZDOMan.instance&&view.Container!=null) {
            if(view.Container.IsInUse()&&InventoryGui.instance!=null&&Player.m_localPlayer!=null)InventoryGui.instance.Hide();
            // SetInUse ignores calls after ownership loss. Clear only the local
            // GUI state; never serialize an inventory from a nonowner here.
            CargoInUse.SetValue(view.Container,false);
            if(view.Title!=null)CargoTitle.SetValue(view.Container.GetInventory(),view.Title);
            if(view.Vehicle?.showroom!=null)view.Vehicle.showroom.State.Open[3]=false;
        }
        networkCargo=null;
    }
    void FlushNetworkCargo() {
        var view=networkCargo;if(view==null)return;
        try {BeginCargoClose(view);TickNetworkCargo();}
        catch(Exception error){Logger.LogWarning("Cargo shutdown snapshot: "+error.Message);}
    }
    void DisposeNetworkCargo() {
        var view=networkCargo;
        try {FlushNetworkCargo();}
        finally {if(view!=null)ClearNetworkCargo(view);networkCargoRequestPending=false;}
    }
    void TickNetworkCargo() {
        var view=networkCargo;if(view==null)return;
        if(view.World!=ZDOMan.instance){ClearNetworkCargo(view);return;}
        if(view.Container==null||!NetworkObserversEnabled||!VehiclePersistence.IsVehicle(view.Data)) {
            if(view.Container!=null&&view.Container.IsInUse()&&InventoryGui.instance!=null&&Player.m_localPlayer!=null)InventoryGui.instance.Hide();
            if(view.Container!=null&&view.Container.GetComponent<ZNetView>()?.IsOwner()==true&&view.Seen){CargoSave.Invoke(view.Container,null);ForceCargoSend(view);}
            ClearNetworkCargo(view);return;
        }
        float now=Time.unscaledTime;
        if(view.Container.IsInUse()) {
            view.Seen=true;if(view.Vehicle?.showroom!=null)view.Vehicle.showroom.State.Open[3]=true;
        }
        else if(view.Seen||now>=view.Deadline)BeginCargoClose(view);
        if(view.Closing&&view.Hash.Length>0&&view.Data.GetLong(VehicleNetworkProtocol.CargoPeer)==0&&view.Data.GetString(VehicleNetworkProtocol.CargoCommit)==view.Hash) {ClearNetworkCargo(view);return;}
        if(now>=view.NextKeep&&!view.KeepPending) {
            view.KeepPending=true;view.NextKeep=now+1;
            networkClient.Send(VehicleNetworkProtocol.Command.CargoKeep,view.Data.m_uid,null,(ok,message,id)=>{
                view.KeepPending=false;if(networkCargo!=view)return;
                if(!ok){ShowVehicleWarning(message);ClearNetworkCargo(view);}
            });
        }
        if(networkCargo!=view||!view.Closing||view.ClosePending||now<view.NextClose)return;
        view.ClosePending=true;ForceCargoSend(view);
        networkClient.Send(VehicleNetworkProtocol.Command.CargoClose,view.Data.m_uid,p=>p.Write(view.Hash),(ok,message,id)=>{
            view.ClosePending=false;if(networkCargo!=view)return;
            if(ok){ClearNetworkCargo(view);return;}
            var native=view.Container!=null?view.Container.GetComponent<ZNetView>():null;
            if(native?.IsOwner()!=true&&view.Data.GetLong(VehicleNetworkProtocol.CargoPeer)==0){ShowVehicleWarning(message);ClearNetworkCargo(view);return;}
            view.NextClose=Time.unscaledTime+1;
            if(view.Hash.Length==0&&native?.IsOwner()==true)view.Hash=VehicleNetworkProtocol.InventoryHash(view.Data.GetByteArray(ZDOVars.s_items,null));
        });
    }
}
[HarmonyPatch(typeof(Container),"RPC_OpenResponse")]
internal static class NativeNetworkCargoResponse {
    static bool Prefix(Container __instance,long __0,bool __1)=>Plugin.ControlsOwner?.PrepareCargoResponse(__instance,__0,__1)!=false;
}
}
