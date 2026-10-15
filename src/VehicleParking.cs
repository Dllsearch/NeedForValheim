using UnityEngine;
namespace NeedForValheim {
// Per-car state is destroyed with its owner. Loaded object readiness is more
// expensive than the tile/contact checks and only sampled while held.
sealed class VehicleParkingState:MonoBehaviour {
    internal float ReadySince=-1,NextAreaCheck;
    internal bool AreaReady;
}
public partial class Plugin {
    bool UpdateParkedVehicle() {
        var local=LocalPlayer();
        var delta=local!=null?body.position-local.transform.position:Vector3.one*1000;delta.y=0;float distance=delta.magnitude;
        bool loaded=ParkingTerrainLoaded();
        bool supported=loaded&&ParkingSupported();
        var state=car.GetComponent<VehicleParkingState>();
        if(state==null)state=car.AddComponent<VehicleParkingState>();
        if(current.parkingFrozen) {
            float now=Time.unscaledTime;
            if(now>=state.NextAreaCheck) {
                state.AreaReady=loaded&&ZNetScene.instance!=null&&ZNetScene.instance.IsAreaReady(body.position);
                state.NextAreaCheck=now+.25f;
            }
            bool ready=supported&&state.AreaReady;
            if(!ready)state.ReadySince=-1;
            else if(state.ReadySince<0)state.ReadySince=now;
            if(!VehicleParkingRules.Thaw(driving,distance,ready,state.ReadySince<0?0:now-state.ReadySince))return true;
            body.isKinematic=false;body.WakeUp();current.parkingFrozen=false;
            state.ReadySince=-1;
            Logger.LogInfo("Parking resumed: "+current.Profile.id);
        }
        if(supported&&!Flying){current.parkPosition=body.position;current.parkRotation=body.rotation;current.parkSupportTime=Time.unscaledTime;}
        if(VehicleParkingRules.Freeze(driving,Flying,distance,current.parkPosition.y-body.position.y,Time.unscaledTime-current.parkSupportTime,loaded)) {
            body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            body.isKinematic=true;body.position=current.parkPosition;body.rotation=current.parkRotation;
            current.parkingFrozen=true;
            state.ReadySince=-1;state.AreaReady=false;state.NextAreaCheck=0;
            Logger.LogInfo("Parking held: "+current.Profile.id+" (distance="+distance.ToString("0")+" m; terrain="+supported+")");
            return true;
        }
        return false;
    }
    bool ParkingTerrainLoaded() {
        var zones=ZoneSystem.instance;
        if(zones==null||!zones.IsZoneLoaded(body.position))return false;
        foreach(var wheel in wheels)if(wheel!=null&&!zones.IsZoneLoaded(wheel.transform.position))return false;
        return true;
    }
    bool ParkingSupported() {
        foreach(var wheel in wheels) {
            if(wheel==null)return false;
            bool found=false;
            // Include constructed floors/bridges, but never our own colliders or
            // the Viking. Kinematic parked wheel colliders have no GetGroundHit.
            foreach(var hit in Physics.RaycastAll(wheel.transform.position+Vector3.up,Vector3.down,wheel.radius+wheel.suspensionDistance+1.65f,~0,QueryTriggerInteraction.Ignore)) {
                if(hit.collider.transform.IsChildOf(car.transform)||hit.collider.GetComponentInParent<Character>()!=null)continue;
                if(hit.rigidbody!=null&&!hit.rigidbody.isKinematic)continue;
                if(hit.normal.y<.45f)continue;
                found=true;break;
            }
            if(!found)return false;
        }
        return wheels.Length>=4;
    }
}
}
