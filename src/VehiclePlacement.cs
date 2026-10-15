using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    Quaternion SpawnRotation(MonoBehaviour local) {
        if(TryGarageStationSpawnAnchor(local,out _,out var stationYaw))return stationYaw;
        // The garage temporarily orbits the native camera around the parked car.
        // Use the saved world view in that case, never the showroom's reverse view.
        // Player.transform.forward can also lag behind the actual look direction.
        var view=GameCamera.instance;
        var direction=menuOpen&&menuCamera==view?showroomCameraRotation*Vector3.forward:view!=null?view.transform.forward:local is Character actor?actor.GetLookDir():local.transform.forward;
        direction.y=0;
        if(direction.sqrMagnitude<.01f)direction=local is Character player?player.GetLookDir():local.transform.forward;
        direction.y=0;
        return Quaternion.LookRotation(direction.sqrMagnitude>.01f?direction:Vector3.forward);
    }
    // Stations can supply their marked pad centre and yaw through this same path.
    bool FindSpawnPoint(Vector3 origin,Quaternion rotation,CarProfile profile,out Vector3 result,bool markedPad=false) {
        var rig=profile.rig;
        float near=Mathf.Max(5.5f,rig.boxSizeZ*.5f+3);
        var half=new Vector3(rig.boxSizeX*.5f+rig.clearanceX,rig.boxSizeY*.5f+rig.clearanceY,rig.boxSizeZ*.5f+rig.clearanceZ);
        for(int row=0;row<3;row++)foreach(float angle in new[]{0f,-15f,15f,-28f,28f}) {
            var point=origin+rotation*Quaternion.Euler(0,angle,0)*Vector3.forward*((markedPad?0:near)+row*2.5f);
            if(!SpawnSupport(point,rotation,profile,out var root))continue;
            var centre=root+rotation*new Vector3(rig.boxCenterX,rig.boxCenterY,rig.boxCenterZ);
            if(Physics.CheckBox(centre,half,rotation,~0,QueryTriggerInteraction.Ignore))continue;
            result=root;return true;
        }
        result=Vector3.zero;return false;
    }
    bool SpawnSupport(Vector3 point,Quaternion rotation,CarProfile profile,out Vector3 root) {
        root=Vector3.zero;
        var zones=ZoneSystem.instance;
        var scene=ZNetScene.instance;
        if(zones==null||scene==null||!scene.IsAreaReady(point))return false;
        float low=float.PositiveInfinity,high=float.NegativeInfinity;
        var rig=profile.rig;
        // All wheel contacts must have solid support; a centre ray alone allows
        // spawning across cliffs, unloaded tile edges, and narrow roof ridges.
        for(int i=-1;i<4;i++) {
            var sample=point+(i<0?Vector3.zero:rotation*new Vector3((i%2==0?-1:1)*rig.TrackWidth(i),0,i<2?rig.frontAxleZ:-rig.rearAxleZ));
            if(!zones.IsZoneLoaded(sample)||!Physics.Raycast(sample+Vector3.up*4,Vector3.down,out var hit,8,~0,QueryTriggerInteraction.Ignore))return false;
            if(hit.normal.y<.7f||hit.collider.GetComponentInParent<Character>()!=null||hit.collider.GetComponentInParent<VehicleInteraction>()!=null)return false;
            if(hit.rigidbody!=null&&!hit.rigidbody.isKinematic)return false;
            if(hit.point.y<Floating.GetLiquidLevel(hit.point)+.12f||zones.IsLava(hit.point))return false;
            low=Mathf.Min(low,hit.point.y);high=Mathf.Max(high,hit.point.y);
        }
        if(high-low>.65f||Mathf.Abs(high-point.y)>3)return false;
        root=new Vector3(point.x,high+rig.spawnLift,point.z);return true;
    }
}
}
