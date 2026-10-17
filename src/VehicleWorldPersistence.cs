using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    ZDOMan vehicleWorld;
    readonly List<ZDOID> savedVehicleIds=new List<ZDOID>();
    readonly HashSet<ZDOID> deferredVehicles=new HashSet<ZDOID>();
    ZDO restoringVehicle;
    IEnumerable<ZDO> SavedWorldVehicles=>vehicleWorld!=null&&vehicleWorld==ZDOMan.instance
        ?savedVehicleIds.Select(vehicleWorld.GetZDO).Where(VehiclePersistence.IsVehicle):Enumerable.Empty<ZDO>();
    int WorldVehicleCount=>NetworkSessionActive?(networkClient?.OwnedVehicles.Count??0):SavedWorldVehicles.Count()+fleet.Count(v=>v.storage==null||!VehiclePersistence.IsVehicle(v.storage.Data));
    bool SavedWorldProfile(string id)=>SavedWorldVehicles.Any(d=>VehiclePersistence.Profile(d)==id);
    void TickWorldVehicles(MonoBehaviour local) {
        if(NetworkSessionActive||Multiplayer()||ZDOMan.instance==null||ZNetScene.instance==null)return;
        if(vehicleWorld!=ZDOMan.instance) {
            vehicleWorld=ZDOMan.instance;savedVehicleIds.Clear();deferredVehicles.Clear();
            foreach(var id in ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.String,VehiclePersistence.Model.GetStableHashCode()))
                if(VehiclePersistence.IsVehicle(vehicleWorld.GetZDO(id)))savedVehicleIds.Add(id);
            foreach(var id in ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Int,VehiclePersistence.LegacyModel.GetStableHashCode()))
                if(!savedVehicleIds.Contains(id)&&VehiclePersistence.IsVehicle(vehicleWorld.GetZDO(id)))savedVehicleIds.Add(id);
        }
        if(SpawnBusy||driving||local is Player live&&(live.IsDead()||live.IsTeleporting()))return;
        var saved=SavedWorldVehicles.FirstOrDefault(d=>!deferredVehicles.Contains(d.m_uid)&&!fleet.Any(v=>v.storage?.Data==d)&&ZNetScene.instance.FindInstance(d.m_uid)!=null);
        if(saved==null)return;
        try {
            if(!VehiclePersistence.ValidPose(saved))throw new InvalidOperationException("Invalid saved vehicle pose");
            var profile=vehicleCatalog.Profiles.FirstOrDefault(p=>p.id==VehiclePersistence.Profile(saved));
            if(profile==null)throw new InvalidOperationException("Saved vehicle profile is not installed: "+VehiclePersistence.Profile(saved));
            restoringVehicle=saved;StartVehicleLoad(local,profile);
        }catch(Exception error){deferredVehicles.Add(saved.m_uid);restoringVehicle=null;Logger.LogWarning("Vehicle restore deferred; cargo retained: "+error.Message);}
    }
    void RemoveDistantWorldVehicle(ZDO data) {
        if(SpawnBusy||NetworkSessionActive||Multiplayer()||vehicleWorld!=ZDOMan.instance||!SavedWorldVehicles.Contains(data)||fleet.Any(v=>v.storage?.Data==data))return;
        try {
            if(!data.IsOwner())data.SetOwner(ZDOMan.GetSessionID());
            VehicleStorage.RemoveSaved(data);
            deferredVehicles.Remove(data.m_uid);
            workshopMessage="Saved vehicle removed. Any cargo remains in a chest at its location.";
        }catch(Exception error){Logger.LogError("Saved vehicle removal: "+error);workshopMessage="Saved vehicle could not be removed; see the log.";}
    }
    string CaptureWorldBuild()=>JsonConvert.SerializeObject(CurrentBuildSettings().ToDictionary(e=>e.Definition.Section+"/"+e.Definition.Key,e=>e.GetSerializedValue()));
    void RestoreWorldBuild(ZDO data) {
        var text=VehiclePersistence.Settings(data);
        if(text.Length>131072)throw new InvalidOperationException("Oversized world vehicle setup");
        var values=JsonConvert.DeserializeObject<Dictionary<string,string>>(text);
        if(values==null||values.Count>512||values.Any(v=>v.Key.Length>256||v.Value==null||v.Value.Length>16384))throw new InvalidOperationException("Invalid world vehicle setup");
        RestoreMissingVinylSettings(values);RestoreMissingDecalSettings(values);
        var entries=CurrentBuildSettings().ToDictionary(e=>e.Definition.Section+"/"+e.Definition.Key);
        var before=entries.ToDictionary(e=>e.Key,e=>e.Value.GetSerializedValue());
        bool autoSave=Config.SaveOnConfigSet;Config.SaveOnConfigSet=false;
        try {foreach(var value in values)if(entries.TryGetValue(value.Key,out var entry))entry.SetSerializedValue(value.Value);}
        catch {foreach(var entry in entries)entry.Value.SetSerializedValue(before[entry.Key]);throw;}
        finally {Config.SaveOnConfigSet=autoSave;}
    }
    void SaveWorldVehicle() {
        if(car==null||current.storage==null||vehicleWorld!=ZDOMan.instance||NetworkSessionActive||Multiplayer())return;
        var data=current.storage.Data;
        if(!data.IsOwner())return;
        current.storage.Tick();
        VehiclePersistence.Save(data,current.Profile.id,car.transform,current.storage.Layout,hasBeenDriven,CaptureWorldBuild());
        data.Set(VehicleMotionTelemetry.Prefix+"charge",ActiveNitro?.Charge??1);
        if(!savedVehicleIds.Contains(data.m_uid))savedVehicleIds.Add(data.m_uid);
    }
    internal void CaptureWorldVehicles() {
        if(NetworkSessionActive||Multiplayer())return;
        var selected=current;
        try {foreach(var vehicle in fleet) {
            current=vehicle;
            try {SaveWorldVehicle();}catch(Exception error){Logger.LogError("Vehicle world snapshot "+vehicle.Profile.id+": "+error);}
        }}
        finally {current=selected;}
    }
    internal bool ProtectWorldVehicle(Component component)=>!Multiplayer()&&VehiclePersistence.IsVehicle(component.GetComponent<ZNetView>()?.GetZDO());
    internal void PrepareWorldVehicle(Container container){if(ProtectWorldVehicle(container))VehicleStorage.Hide(container.gameObject);}
    internal void ShutdownWorldVehicles(){FlushNetworkCargo();CaptureWorldVehicles();CleanupAll();}
}
[HarmonyPatch(typeof(ZDOMan),"PrepareSave")]
internal static class WorldVehicleSave {static void Prefix(){Plugin.ControlsOwner?.CaptureWorldVehicles();}}
[HarmonyPatch(typeof(Game),"Shutdown")]
internal static class WorldVehicleShutdown {static void Prefix(){Plugin.ControlsOwner?.ShutdownWorldVehicles();}}
[HarmonyPatch(typeof(Container),"Awake")]
internal static class WorldVehicleCargoLoad {static void Postfix(Container __instance){Plugin.ControlsOwner?.PrepareWorldVehicle(__instance);}}
[HarmonyPatch]
internal static class WorldVehicleCargoProtection {
    static IEnumerable<System.Reflection.MethodBase> TargetMethods() {
        foreach(var method in new[]{"UpdateWear","RPC_Damage","RPC_Remove"})yield return AccessTools.Method(typeof(WearNTear),method);
    }
    static bool Prefix(WearNTear __instance)=>Plugin.ControlsOwner?.ProtectWorldVehicle(__instance)!=true;
}
}

namespace NeedForValheim {
[HarmonyLib.HarmonyPatch(typeof(Container),"RPC_RequestOpen")]
internal static class NativeNetworkCargoOpen {
    static bool Prefix(Container __instance,long __0,long __1) {
        var data=__instance.GetComponent<ZNetView>()?.GetZDO();
        if(!VehiclePersistence.IsVehicle(data)||ZNet.instance==null||ZNet.IsSinglePlayer)return true;
        return Plugin.ControlsOwner?.AllowsNativeCargo(__instance,__0,__1)==true;
    }
}
public partial class Plugin {
    internal bool AllowsNativeCargo(Container container,long peer,long player)=>ZNet.instance!=null&&ZNet.instance.IsServer()&&hostNetwork?.AllowsNativeCargoOpen(container,peer,player)==true;
}
}
