using UnityEngine;
namespace NeedForValheim {
// Native server checks for requested spawn/recovery positions. No visual assets.
internal static class VehicleNetworkPlacement {
    public static bool Clear(CarProfile profile,Vector3 position,Quaternion rotation) {
        if(profile==null||(rotation*Vector3.up).y<.95f)return false;
#if NFVU2_DEDICATED_SERVER
        // Dedicated ZoneSystem generates remote players' zones in Ghost mode:
        // their floor/obstacle colliders are not a complete local physics scene.
        // The requesting client already ran SpawnSupport and the clearance box.
        // The broker independently checks pose, character distance, ownership
        // and limits. Repeating client physics here rejects valid remote spawns.
        return true;
#else
        var zones=ZoneSystem.instance;var scene=ZNetScene.instance;
        if(zones==null||scene==null||!scene.IsAreaReady(position))return false;
        var rig=profile.rig;float low=float.PositiveInfinity,high=float.NegativeInfinity;
        for(int i=-1;i<4;i++) {
            var sample=position+(i<0?Vector3.zero:rotation*new Vector3((i%2==0?-1:1)*rig.TrackWidth(i),0,i<2?rig.frontAxleZ:-rig.rearAxleZ));
            if(!zones.IsZoneLoaded(sample)||!Physics.Raycast(sample+Vector3.up*4,Vector3.down,out var hit,8,~0,QueryTriggerInteraction.Ignore))return false;
            if(hit.normal.y<.7f||hit.collider==null||hit.collider.GetComponentInParent<Character>()!=null||hit.collider.transform.root.name=="NFVU2_ServerHull")return false;
            if(hit.rigidbody!=null&&!hit.rigidbody.isKinematic)return false;
            if(hit.point.y<Floating.GetLiquidLevel(hit.point)+.12f||zones.IsLava(hit.point))return false;
            low=Mathf.Min(low,hit.point.y);high=Mathf.Max(high,hit.point.y);
        }
        if(high-low>.65f||Mathf.Abs(position.y-(high+rig.spawnLift))>.35f)return false;
        var centre=position+rotation*new Vector3(rig.boxCenterX,rig.boxCenterY,rig.boxCenterZ);
        var half=new Vector3(rig.boxSizeX*.5f+rig.clearanceX,rig.boxSizeY*.5f+rig.clearanceY,rig.boxSizeZ*.5f+rig.clearanceZ);
        return !Physics.CheckBox(centre,half,rotation,~0,QueryTriggerInteraction.Ignore);
#endif
    }
}
}
