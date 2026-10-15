using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
namespace NeedForValheim {
// Only stale fall history and contacts with the just-exited car are affected.
// Attacks, projectiles, status effects and subsequent genuine falls stay native.
public sealed class VehicleExitSafety:MonoBehaviour {
    static readonly FieldInfo AirAltitude=AccessTools.Field(typeof(Character),"m_maxAirAltitude");
    static readonly FieldInfo FallTimer=AccessTools.Field(typeof(Character),"m_fallTimer");
    readonly List<Collider[]> ignored=new List<Collider[]>();
    Player owner;
    Vector3 exit;
    float until;
    internal static void Install(Harmony harmony)=>harmony.PatchAll(typeof(NativeExitGroundContact));
    internal static void Arm(Player player,GameObject vehicle) {
        if(player==null)return;
        var guard=player.GetComponent<VehicleExitSafety>();
        if(guard==null)guard=player.gameObject.AddComponent<VehicleExitSafety>();
        guard.Restore();guard.owner=player;guard.exit=player.transform.position;guard.until=Time.time+.8f;
        ResetFall(player);
        if(vehicle==null)return;
        foreach(var actor in player.GetComponentsInChildren<Collider>(true))foreach(var hull in vehicle.GetComponentsInChildren<Collider>(true)) {
            if(actor==null||hull==null||actor.isTrigger||hull.isTrigger||Physics.GetIgnoreCollision(actor,hull))continue;
            Physics.IgnoreCollision(actor,hull,true);guard.ignored.Add(new[]{actor,hull});
        }
    }
    static void ResetFall(Character player) {
        AirAltitude?.SetValue(player,player.transform.position.y);
        FallTimer?.SetValue(player,0f);
    }
    bool Active=>owner!=null&&VehicleExitRules.Active(Time.time,until,(owner.transform.position-exit).sqrMagnitude,owner.transform.position.y-exit.y);
    void FixedUpdate(){if(!Active)Restore();}
    void Restore() {
        foreach(var pair in ignored)if(pair[0]!=null&&pair[1]!=null)Physics.IgnoreCollision(pair[0],pair[1],false);
        ignored.Clear();owner=null;
    }
    void OnDisable()=>Restore();
    void OnDestroy()=>Restore();
    internal void BeforeLanding() {
        if(!Active){Restore();return;}
        ResetFall(owner);
    }
}
[HarmonyPatch(typeof(Character),"UpdateGroundContact")]
static class NativeExitGroundContact {
    static void Prefix(Character __instance) {
        if(__instance is Player player)player.GetComponent<VehicleExitSafety>()?.BeforeLanding();
    }
}
}
