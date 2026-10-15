using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace NeedForValheim {
// Developer audit only. Separate PhysX scene, no player/world/input involved.
internal static class VehicleSuspensionAudit {
 public sealed class Result {public string id;public int cases;public float uncorrectedLift,maxHubError,maxBodyError;}
 public static IEnumerable<int> Check(CarProfile profile,Action<Result> done) {
  var scene=SceneManager.CreateScene("MW suspension "+profile.id,new CreateSceneParameters(LocalPhysicsMode.Physics3D));
  var physics=scene.GetPhysicsScene();var root=new GameObject("Suspension test");root.SetActive(false);SceneManager.MoveGameObjectToScene(root,scene);
  var ground=new GameObject("Suspension floor");SceneManager.MoveGameObjectToScene(ground,scene);
  var rig=profile.rig;var result=new Result{id=profile.id};
  try {
   var floor=ground.AddComponent<BoxCollider>();floor.center=new Vector3(0,-.5f,0);floor.size=new Vector3(30,1,30);
   var hull=root.AddComponent<BoxCollider>();hull.center=new Vector3(rig.boxCenterX,rig.boxCenterY,rig.boxCenterZ);hull.size=new Vector3(rig.boxSizeX,rig.boxSizeY,rig.boxSizeZ);
   var body=root.AddComponent<Rigidbody>();body.mass=rig.mass;body.constraints=RigidbodyConstraints.FreezePositionX|RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;
   var wheels=new WheelCollider[4];
   for(int i=0;i<4;i++) {
    var node=new GameObject("Suspension wheel "+i);node.transform.SetParent(root.transform,false);
    node.transform.localPosition=new Vector3((i%2==0?-1:1)*rig.TrackWidth(i),rig.WheelHeight(i),i<2?rig.frontAxleZ:-rig.rearAxleZ);
    wheels[i]=node.AddComponent<WheelCollider>();wheels[i].radius=rig.WheelRadius(i);wheels[i].brakeTorque=6000;
   }
   var handling=new VehicleHandling(body,wheels,true);MwChassisTuning.Configure(body,wheels);var fit=new VehicleRideHeight(body,wheels,true);
   root.transform.position=Vector3.up*.5f;root.SetActive(true);
   for(int step=0;step<400;step++){physics.Simulate(.02f);if(step%20==0)yield return 0;}
   result.uncorrectedLift=body.position.y;
   foreach(int mode in new[]{0,1,2,0})foreach(float cm in new[]{0f,12f,0f}) {
    if(mode==2)handling.SetDriftMode();else handling.SetTerrainMode(mode==0);
    MwChassisTuning.Configure(body,wheels);fit.Apply(cm);
    for(int step=0;step<400;step++){physics.Simulate(.02f);if(step%20==0)yield return 0;}
    for(int i=0;i<4;i++) {
     if(!wheels[i].GetGroundHit(out var hit))throw new InvalidOperationException(profile.id+" suspension lost floor contact");
     wheels[i].GetWorldPose(out var point,out var rotation);
     float error=Math.Abs(root.transform.InverseTransformPoint(point).y-rig.WheelHeight(i)-cm*.01f);
     result.maxHubError=Math.Max(result.maxHubError,error);
    }
    result.maxBodyError=Math.Max(result.maxBodyError,Math.Abs(body.position.y+cm*.01f));result.cases++;
   }
   if(result.maxHubError>.015f||result.maxBodyError>.015f)throw new InvalidOperationException(profile.id+" static suspension error: hub="+result.maxHubError+", body="+result.maxBodyError);
   done(result);
  }finally{root.SetActive(false);ground.SetActive(false);UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(ground);SceneManager.UnloadSceneAsync(scene);}
 }
}
}
