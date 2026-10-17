using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
namespace NeedForValheim.Server {
// Dedicated-server entry point. This assembly deliberately excludes client code
// and embedded audio; persistent identity/profile formats are shared with it.
[BepInPlugin("local.needforvalheim.prototype","NeedForValheim server","0.28.0")]
public sealed class ServerPlugin : BaseUnityPlugin {
    Harmony patches;VehicleNetworkServer network;VehicleServerCollisions collisions;
    internal static ServerPlugin Instance;
    internal bool AllowsCargo(Container container,long peer,long player)=>network?.AllowsNativeCargoOpen(container,peer,player)==true;
    void Awake() {
        if(!Application.isBatchMode) {Logger.LogError("This is the dedicated-server payload. Install the client payload for gameplay.");enabled=false;return;}
        var catalog=CarCatalog.Load(Path.GetDirectoryName(Info.Location));
        Instance=this;collisions=new VehicleServerCollisions(catalog);
        network=new VehicleNetworkServer(catalog,message=>Logger.LogWarning(message),ServerCargoLoad.Hide,VehicleNetworkPlacement.Clear);
        patches=new Harmony("local.needforvalheim.server");
        patches.PatchAll(typeof(ServerCargoLoad));patches.PatchAll(typeof(ServerCargoProtection));patches.PatchAll(typeof(ServerNativeCargoOpen));
        Logger.LogInfo("NFVU2 server: "+catalog.Profiles.Count+" profiles; no client model, texture, audio or frontend assets loaded. Development network prototype; native multiplayer acceptance is pending.");
    }
    void Update(){network?.Tick();collisions?.Tick();}
    void FixedUpdate(){collisions?.FixedTick();}
    void OnDestroy(){collisions?.Dispose();patches?.UnpatchSelf();if(Instance==this)Instance=null;}
    internal static bool PersistentCargo(Component component)=>component!=null&&VehiclePersistence.IsVehicle(component.GetComponent<ZNetView>()?.GetZDO());
}
[HarmonyPatch(typeof(Container),"Awake")]
internal static class ServerCargoLoad {
    static void Postfix(Container __instance){Hide(__instance);}
    internal static void Hide(Container container) {
        if(container==null||!ServerPlugin.PersistentCargo(container))return;
        var visibility=container.GetComponent<ServerCargoVisibility>()??container.gameObject.AddComponent<ServerCargoVisibility>();
        visibility.Hide();
    }

}
[HarmonyPatch]
internal static class ServerCargoProtection {
    static IEnumerable<System.Reflection.MethodBase> TargetMethods() {
        foreach(var name in new[]{"UpdateWear","RPC_Damage","RPC_Remove"})yield return AccessTools.Method(typeof(WearNTear),name);
    }
    static bool Prefix(WearNTear __instance)=>!ServerPlugin.PersistentCargo(__instance);
}
}

namespace NeedForValheim.Server {
internal sealed class ServerCargoVisibility : MonoBehaviour {
    Renderer[] renderers;Collider[] colliders;bool[] visible,solid;WearNTear wear;bool roof,support;
    internal void Hide() {
        if(renderers!=null)return;
        renderers=GetComponentsInChildren<Renderer>(true);colliders=GetComponentsInChildren<Collider>(true);
        visible=new bool[renderers.Length];solid=new bool[colliders.Length];
        for(int i=0;i<renderers.Length;i++){visible[i]=renderers[i].enabled;renderers[i].enabled=false;}
        for(int i=0;i<colliders.Length;i++){solid[i]=colliders[i].enabled;colliders[i].enabled=false;}
        wear=GetComponent<WearNTear>();if(wear!=null){roof=wear.m_noRoofWear;support=wear.m_noSupportWear;wear.m_noRoofWear=wear.m_noSupportWear=false;}
    }
    void Update() {
        if(ServerPlugin.PersistentCargo(this)) {
            var data=GetComponent<ZNetView>()?.GetZDO();if(data!=null)transform.SetPositionAndRotation(data.GetPosition(),data.GetRotation());return;
        }
        if(renderers==null)return;
        for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].enabled=visible[i];
        for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=solid[i];
        if(wear!=null){wear.m_noRoofWear=roof;wear.m_noSupportWear=support;}
        var container=GetComponent<Container>();if(container!=null){container.m_height=4;container.GetInventory().SetHeight(4);}
        renderers=null;colliders=null;Destroy(this);
    }
}
}

namespace NeedForValheim.Server {
[HarmonyPatch(typeof(Container),"RPC_RequestOpen")]
internal static class ServerNativeCargoOpen {
    static bool Prefix(Container __instance,long __0,long __1)=>!ServerPlugin.PersistentCargo(__instance)||ServerPlugin.Instance?.AllowsCargo(__instance,__0,__1)==true;
}
}
