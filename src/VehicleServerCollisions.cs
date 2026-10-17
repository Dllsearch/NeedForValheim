using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace NeedForValheim {
// Asset-free server hulls exist only for native loaded anchors. A host suppresses
// the proxy whenever its client already supplies that car's physical body.
internal sealed class VehicleServerCollisions : IDisposable {
    sealed class Hull {public ZDO Data;public GameObject Root;public Rigidbody Body;public BoxCollider Collider;}
    readonly Dictionary<ZDOID,Hull> hulls=new Dictionary<ZDOID,Hull>();
    readonly CarCatalog catalog;readonly Func<ZDO,bool> clientBody;
    PhysicsMaterial material;ZDOMan world;ZNetScene scene;float nextScan;
    public VehicleServerCollisions(CarCatalog catalog,Func<ZDO,bool> clientBody=null){this.catalog=catalog;this.clientBody=clientBody;}
    public void Tick() {
        if(ZNet.instance==null||!ZNet.instance.IsServer()||ZDOMan.instance==null||ZNetScene.instance==null){Clear();return;}
        if(world!=ZDOMan.instance||scene!=ZNetScene.instance){Clear();world=ZDOMan.instance;scene=ZNetScene.instance;nextScan=0;}
        if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+.2f;
        var ids=ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.String,VehiclePersistence.Model.GetStableHashCode())
            .Concat(ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Int,VehiclePersistence.LegacyModel.GetStableHashCode())).Distinct();
        var wanted=new HashSet<ZDOID>();
        foreach(var id in ids) {
            var data=world.GetZDO(id);
            if(!VehiclePersistence.IsVehicle(data)||!VehiclePersistence.ValidPose(data)||scene.FindInstance(id)==null)continue;
            var profile=catalog.Profiles.FirstOrDefault(p=>p.id==VehiclePersistence.Profile(data));if(profile==null)continue;
            wanted.Add(id);if(hulls.TryGetValue(id,out var existing)){if(existing.Root!=null)continue;hulls.Remove(id);}
            if(material==null)material=new PhysicsMaterial("Vehicle server hull"){dynamicFriction=.08f,staticFriction=.08f,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum};
            var root=new GameObject("NFVU2_ServerHull");root.transform.SetPositionAndRotation(VehiclePersistence.Location(data),VehiclePersistence.Orientation(data));
            var box=root.AddComponent<BoxCollider>();var rig=profile.rig;box.center=new Vector3(rig.boxCenterX,rig.boxCenterY,rig.boxCenterZ);box.size=new Vector3(rig.boxSizeX,rig.boxSizeY,rig.boxSizeZ);box.sharedMaterial=material;
            var body=root.AddComponent<Rigidbody>();body.mass=rig.mass;body.isKinematic=true;body.useGravity=false;body.interpolation=RigidbodyInterpolation.Interpolate;
            hulls[id]=new Hull{Data=data,Root=root,Body=body,Collider=box};
        }
        foreach(var id in hulls.Keys.Where(id=>!wanted.Contains(id)).ToArray()){UnityEngine.Object.Destroy(hulls[id].Root);hulls.Remove(id);}
    }
    public void FixedTick() {
        foreach(var hull in hulls.Values) {
            if(hull.Root==null||hull.Collider==null||hull.Body==null)continue;
            bool active=world==ZDOMan.instance&&scene==ZNetScene.instance&&scene!=null&&VehiclePersistence.IsVehicle(hull.Data)&&VehiclePersistence.ValidPose(hull.Data)&&scene.FindInstance(hull.Data.m_uid)!=null&&!(clientBody?.Invoke(hull.Data)??false);
            hull.Collider.enabled=active;if(!active)continue;
            hull.Body.MovePosition(VehiclePersistence.Location(hull.Data));hull.Body.MoveRotation(VehiclePersistence.Orientation(hull.Data));
        }
    }
    void Clear(){foreach(var hull in hulls.Values)if(hull.Root!=null)UnityEngine.Object.Destroy(hull.Root);hulls.Clear();world=null;scene=null;}
    public void Dispose(){Clear();if(material!=null)UnityEngine.Object.Destroy(material);material=null;}
}
}
