using System;
using System.Linq;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;
[BepInPlugin("local.nfvu2.smoke","NFVU2 isolated smoke","1.0.0")]
public sealed class NativeServerSmoke:BaseUnityPlugin {
 float started,readyAt=-1;
 GameObject hullUnderTest;int hullCleanupFrame=-1;
 readonly FieldInfo generated=typeof(ZoneSystem).GetField("m_locationsGenerated",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
 void Awake(){started=Time.realtimeSinceStartup;Logger.LogInfo("NFVU2 native smoke watchdog started");}
 const long FixtureOwner=4611686000123L;
 void CheckVehicleSave() {
  var candidates=ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.String,"nfvu2_profile_v2".GetStableHashCode()).Select(id=>ZDOMan.instance.GetZDO(id)).Where(d=>d!=null&&d.GetLong("nfvu2_creator_v1")==FixtureOwner).ToArray();
  if(candidates.Length>1)throw new Exception("Duplicate native vehicle records");
  if(candidates.Length==1) {
   var d=candidates[0];var position=d.GetVec3("nfvu2_pose_v2",Vector3.zero);
   if(d.GetString("nfvu2_profile_v2")!="350z"||position!=new Vector3(25,26,27)||d.GetFloat("nfvu2_motion_charge")!=.42f||d.GetString("nfvu2_build_v2")!="{\"Styling/Paint\":\"3\"}")throw new Exception("Vehicle snapshot changed across native save/reload");
   var inventory=new Inventory("NFVU2 smoke",null,8,4);inventory.Load(new ZPackage(d.GetByteArray(ZDOVars.s_items,null)));
   var items=inventory.GetAllItems();if(items.Count!=1||items[0].m_stack!=3||items[0].m_shared.m_name!="$item_wood")throw new Exception("Native cargo items changed across restart");
   Logger.LogInfo("NFVU2_NATIVE_VEHICLE_RELOAD_OK id="+d.m_uid);File.WriteAllText(Path.Combine(Paths.GameRootPath,"native-vehicle-reload-ok.txt"),d.m_uid.ToString());return;
  }
  var root=UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("piece_chest"),new Vector3(25,26,27),Quaternion.identity);
  var data=root.GetComponent<ZNetView>().GetZDO();if(data==null)throw new Exception("Native cargo anchor has no ZDO");
  var assembly=BepInEx.Bootstrap.Chainloader.PluginInfos["local.needforvalheim.prototype"].Instance.GetType().Assembly;
  assembly.GetType("NeedForValheim.VehiclePersistence").GetMethod("Save",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{data,"350z",root.transform,0,true,"{\"Styling/Paint\":\"3\"}"});
  data.Set("nfvu2_creator_v1",FixtureOwner);data.Set("nfvu2_motion_charge",.42f);
  var cargo=root.GetComponent<Container>();if(!cargo.GetInventory().AddItem(ZNetScene.instance.GetPrefab("Wood"),3))throw new Exception("Native wood item could not be added");
  typeof(Container).GetMethod("Save",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(cargo,null);
  if(data.GetByteArray(ZDOVars.s_items,null)==null)throw new Exception("Native inventory bytes not stored");
  Logger.LogInfo("NFVU2_NATIVE_VEHICLE_CREATED id="+data.m_uid);File.WriteAllText(Path.Combine(Paths.GameRootPath,"native-vehicle-created.txt"),data.m_uid.ToString());
 }
 void CheckNativeHull() {
  var plugin=BepInEx.Bootstrap.Chainloader.PluginInfos["local.needforvalheim.prototype"].Instance;
  var assembly=plugin.GetType().Assembly;
  var root=UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("piece_chest"),new Vector3(30,100,30),Quaternion.identity);
  var view=root.GetComponent<ZNetView>();var data=view.GetZDO();if(data==null)throw new Exception("Native hull fixture has no anchor");
  assembly.GetType("NeedForValheim.VehiclePersistence").GetMethod("Save",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{data,"350z",root.transform,0,false,"{}"});
  assembly.GetType("NeedForValheim.Server.ServerCargoLoad").GetMethod("Hide",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.GetComponent<Container>()});
  var collisions=plugin.GetType().GetField("collisions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(plugin);var type=collisions.GetType();
  type.GetField("nextScan",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(collisions,0f);
  type.GetMethod("Tick").Invoke(collisions,null);type.GetMethod("FixedTick").Invoke(collisions,null);
  hullUnderTest=UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Where(b=>b.name=="NFVU2_ServerHull").Select(b=>b.gameObject).FirstOrDefault(g=>Vector3.Distance(g.transform.position,root.transform.position)<1);
  if(hullUnderTest==null)throw new Exception("Production server hull not created for native anchor");
  var box=hullUnderTest.GetComponent<BoxCollider>();var body=hullUnderTest.GetComponent<Rigidbody>();
  if(box==null||!box.enabled||box.size.x<1||!body.isKinematic||body.mass<100||hullUnderTest.GetComponentsInChildren<Renderer>(true).Length!=0)throw new Exception("Invalid asset-free server hull");
  Physics.SyncTransforms();
  if(!box.Raycast(new Ray(hullUnderTest.transform.position+Vector3.up*10,Vector3.down),out var hit,20))throw new Exception("Native hull does not participate in physics queries");
  Logger.LogInfo("NFVU2_NATIVE_HULL_QUERY_OK");
  view.Destroy();type.GetField("nextScan",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(collisions,0f);type.GetMethod("Tick").Invoke(collisions,null);hullCleanupFrame=Time.frameCount;
 }
 void Update(){
  bool ready=ZNet.instance!=null&&ZNet.instance.IsServer()&&ZDOMan.instance!=null&&ZNetScene.instance!=null&&ZoneSystem.instance!=null&&ZNetScene.instance.GetPrefab("piece_chest")!=null&&generated!=null&&(bool)generated.GetValue(ZoneSystem.instance);
  if(ready&&readyAt<0){readyAt=Time.realtimeSinceStartup;Logger.LogInfo("NFVU2_NATIVE_GENERATED_WORLD_READY; dedicated="+ZNet.instance.IsDedicated());File.WriteAllText(Path.Combine(Paths.GameRootPath,"native-world-ready.txt"),"Native server world and cargo prefab ready");try{CheckVehicleSave();CheckNativeHull();}catch(Exception e){Logger.LogError("NFVU2_NATIVE_VEHICLE_FAIL "+e);File.WriteAllText(Path.Combine(Paths.GameRootPath,"native-vehicle-fail.txt"),e.ToString());}}
  if(hullCleanupFrame>=0&&Time.frameCount>hullCleanupFrame+2){
   if(hullUnderTest==null)Logger.LogInfo("NFVU2_NATIVE_HULL_CLEANUP_OK");
   else Logger.LogError("NFVU2_NATIVE_HULL_CLEANUP_FAILED");
   hullCleanupFrame=-1;
  }
  if(readyAt>=0&&Time.realtimeSinceStartup-readyAt>10||Time.realtimeSinceStartup-started>180){Logger.LogInfo("NFVU2 smoke watchdog requests clean shutdown");Application.Quit();enabled=false;}
 }
}
