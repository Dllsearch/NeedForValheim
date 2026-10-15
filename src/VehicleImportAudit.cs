using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
namespace NeedForValheim {
// Explicit developer launch only: NFVU2_MW_AUDIT_REPORT=<report.json>.
// Runs in an isolated distant scene without loading or changing a player/world.
internal static class VehicleImportAudit {
 sealed class CarResult {public string id,error;public int options,meshes,audioSources,decalSlots,decalArt;public bool passed;}
 sealed class Report {public string unity,assembly,status,scene,command;public bool complete;public List<CarResult> cars=new List<CarResult>();public List<VehicleSuspensionAudit.Result> suspension=new List<VehicleSuspensionAudit.Result>();}
 public static IEnumerator Run(MonoBehaviour host,CarCatalog catalog,string reportPath,Action<string> log) {
  if(string.IsNullOrWhiteSpace(reportPath))yield break;
  reportPath=Path.GetFullPath(reportPath);Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
  var report=new Report{unity=Application.unityVersion,assembly=typeof(Plugin).Assembly.Location,command=Environment.CommandLine,status="waiting_for_3d_assets"};
  File.WriteAllText(reportPath,JsonConvert.SerializeObject(report,Formatting.Indented));
  float deadline=Time.realtimeSinceStartup+180;
  while(!VehicleMaterials.TemplateReady) {
   if(Time.realtimeSinceStartup>deadline){report.status="3D scene unavailable; inspect Steam/Player.log";File.WriteAllText(reportPath,JsonConvert.SerializeObject(report,Formatting.Indented));yield break;}
   yield return new WaitForSecondsRealtime(1);
  }
  report.scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;report.status="testing";
  foreach(var profile in catalog.ForSource(CarCatalog.MostWanted)) {
   var result=new CarResult{id=profile.id};report.cars.Add(result);
   using(var work=StagedWork.Optional(CheckAll(host,profile,result,report.suspension,Path.GetDirectoryName(reportPath),log),error=>{result.error=error.ToString();log("MW_AUDIT_FAIL "+profile.id+" "+error);}).GetEnumerator()) {
    var budget=System.Diagnostics.Stopwatch.StartNew();
    while(work.MoveNext())if(work.Current<0||budget.Elapsed.TotalMilliseconds>=12){yield return null;budget.Restart();}
   }
   result.passed=result.error==null;File.WriteAllText(reportPath,JsonConvert.SerializeObject(report,Formatting.Indented));
   log("MW_AUDIT "+profile.id+" passed="+result.passed+" options="+result.options);yield return null;
  }
  report.complete=true;report.status=report.cars.All(c=>c.passed)?"passed":"failed";File.WriteAllText(reportPath,JsonConvert.SerializeObject(report,Formatting.Indented));
  log("MW_AUDIT_COMPLETE "+report.cars.Count(c=>c.passed)+"/"+report.cars.Count);
 }
 static IEnumerable<int> CheckAll(MonoBehaviour host,CarProfile profile,CarResult result,List<VehicleSuspensionAudit.Result> suspension,string output,Action<string> log) {
  foreach(var step in VehicleSuspensionAudit.Check(profile,value=>suspension.Add(value)))yield return step;
  foreach(var step in CheckCar(host,profile,result,output,log))yield return step;
 }
 static IEnumerable<int> CheckCar(MonoBehaviour host,CarProfile profile,CarResult result,string output,Action<string> log) {
  string folder=Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
  var loading=Task.Run(()=>SpawnAssets.Load(folder,profile,new SpawnAssetCache(),CancellationToken.None));
  while(!loading.IsCompleted)yield return -1;var assets=loading.GetAwaiter().GetResult();
  if(assets.Warnings.Count>0)throw new InvalidDataException(string.Join("; ",assets.Warnings));
  var root=new GameObject("MW audit "+profile.id);root.transform.position=new Vector3(0,-5000,0);
  var meshes=new List<Mesh>();Material[] materials=null;Texture2D[] textures=null;VehicleStyling styles=null;VehicleAudio audio=null;VehicleNitroEffects effects=null;VehicleDecals nativeDecals=null;
  RenderTexture target=null;Texture2D frame=null;
  try {
   foreach(var step in VehicleMaterials.CreateForModelSteps(host,assets.Model,log,(m,t)=>{materials=m;textures=t;}))yield return step;
   if(materials.Any(m=>m.shader==null||!m.shader.isSupported))throw new InvalidOperationException("Unsupported runtime shader");
   int wheelCount=profile.rig.extraRearAxleOffset>0?6:4;var wheels=new Transform[wheelCount];var brakes=new Transform[wheelCount];var colliders=new WheelCollider[4];
   var rb=root.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
   for(int i=0;i<wheelCount;i++) {
    var wheel=new GameObject("Audit wheel "+i);wheel.transform.SetParent(root.transform,false);wheels[i]=wheel.transform;
    wheel.transform.localPosition=new Vector3((i%2==0?-1:1)*profile.rig.TrackWidth(i),profile.rig.WheelHeight(i),i<2?profile.rig.frontAxleZ:-profile.rig.rearAxleZ+(i>=4?profile.rig.extraRearAxleOffset:0));
    wheel.transform.localRotation=Quaternion.Euler(0,i%2==0?180:0,0);
    var brake=new GameObject("Audit caliper "+i);brake.transform.SetParent(wheel.transform,false);brakes[i]=brake.transform;
    if(i<4){colliders[i]=wheel.AddComponent<WheelCollider>();colliders[i].radius=profile.rig.WheelRadius(i);colliders[i].enabled=false;}
   }
   Transform Create(ModelPart p,Transform parent,ModelData model,Material[] table) {
    var node=new GameObject(p.name);node.layer=31;node.transform.SetParent(parent,false);
    var mesh=new Mesh{name=p.name};meshes.Add(mesh);
    mesh.vertices=p.vertices.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray();mesh.normals=p.normals.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray();mesh.uv=p.uv.Select(v=>new Vector2(v.u,v.v)).ToArray();mesh.colors=Enumerable.Repeat(Color.white,p.vertices.Length).ToArray();mesh.triangles=p.indices;
    VehicleMesh.AddBackfaces(mesh);mesh.RecalculateTangents();mesh.RecalculateBounds();
    if(mesh.vertexCount!=p.vertices.Length*2)throw new InvalidDataException("Incomplete Unity mesh");
    node.AddComponent<MeshFilter>().sharedMesh=mesh;node.AddComponent<MeshRenderer>().sharedMaterial=table[p.material];return node.transform;
   }
   foreach(var part in assets.Model.parts) {
    if(part.group=="body")Create(part,root.transform,assets.Model,materials);
    else for(int i=0;i<wheelCount;i++)if(ModelFile.ForAxle(part,i))Create(part,part.group=="brake"&&!part.rotatesWithWheel?brakes[i]:wheels[i],assets.Model,materials);
    yield return 0;
   }
   audio=new VehicleAudio(profile);foreach(var step in audio.LoadSteps(root,rb,colliders,log,assets.Audio))yield return step;
   for(int stage=0;stage<=3;stage++) {
    audio.SetEngineStage(stage);for(int tick=0;tick<4;tick++){audio.Tick(true,.5f,false,false,false,true,0);yield return -1;}
    int voices=root.GetComponentsInChildren<AudioSource>(true).Count(s=>s.clip!=null&&(s.clip.name.Contains("_power_")||s.clip.name.Contains("_coast_"))&&s.isPlaying);
    if(voices!=profile.sound.rpmBands.Length*2)throw new InvalidOperationException("Wrong active engine voice count at stage "+stage+": "+voices);
   }
   result.audioSources=root.GetComponentsInChildren<AudioSource>(true).Length;
   if(profile.capabilities.turbo&&result.audioSources<profile.sound.rpmBands.Length*2*profile.assets.engineStageFolders.Length+7)throw new InvalidDataException("Required turbo dependency was not loaded");
   effects=VehicleNitroEffects.Create(root,assets.Model,log);if(effects==null)throw new InvalidOperationException("Nitro construction failed");
   styles=new VehicleStyling(root,assets.Model,assets.Styling,materials,wheels,(p,parent)=>Create(p,parent,assets.Styling,styles.Materials),brakes);
   foreach(var step in styles.LoadSteps(host,log))yield return step;
   var selectedDecals=new Dictionary<string,int>();
   if(assets.Decals!=null) {
    nativeDecals=new VehicleDecals(root,assets.Decals,assets.DecalArtwork,(p,parent,material)=>Create(p,parent,assets.Decals,new[]{material}));
    foreach(var step in nativeDecals.LoadSteps(host,log))yield return step;
    foreach(var slot in nativeDecals.Slots)selectedDecals[slot.key]=nativeDecals.Artwork.First(e=>e.kind==slot.kind&&e.title.StartsWith("AEM",StringComparison.Ordinal)).id;
    if(profile.id=="mw-a3")foreach(var art in nativeDecals.Artwork){var material=nativeDecals.ArtworkMaterial(art.id);if(material.mainTexture.width!=art.width||material.mainTexture.height!=art.height)throw new InvalidDataException("Wrong native decal texture dimensions");result.decalArt++;yield return 0;}
    nativeDecals.Tick(selectedDecals,0);result.decalSlots=nativeDecals.Slots.Length;
   }
   var selected=new Dictionary<string,int>();foreach(var category in new[]{"Bodykit","Front","Rear","Skirts","Hood","Spoiler","Roof","Brakes","Rims"})selected[category]=0;
   foreach(var option in assets.Styling.styleOptions) {
    selected[option.category]=option.id;int frames=0;
    do {styles.Tick(selected,0,0,0,0,0,false);if(++frames>300)throw new TimeoutException("Styling option did not activate: "+option.category+":"+option.id);yield return 0;}while(styles.Busy||styles.ActiveOption(option.category)?.id!=option.id);
    if(!root.GetComponentsInChildren<MeshRenderer>(false).Any(r=>r.name.StartsWith("Garage_"+option.category+"_"+option.id+"_",StringComparison.Ordinal)))throw new InvalidOperationException("Activated option has no visible renderer");
    if(nativeDecals!=null) {
     int decalKit=styles.ActiveOption("Bodykit")?.sourceKit??0;nativeDecals.Tick(selectedDecals,Math.Max(0,decalKit));
     int visible=root.GetComponentsInChildren<MeshRenderer>(false).Count(r=>r.name.Contains("_DECAL_"));
     if(visible!=nativeDecals.Slots.Length)throw new InvalidOperationException("Native decal projection slots did not follow the active bodykit");
    }
    result.options++;
   }
   var kit=styles.ActiveOption("Bodykit");
   for(int i=0;i<wheelCount;i++) {
    float scale=i<2?(kit?.frontTyreScale??1):(kit?.rearTyreScale??1);float offset=i<2?(kit?.frontWheelOffset??0):(kit?.rearWheelOffset??0);float width=i<2?profile.rig.tyreWidth:profile.rig.rearTyreWidth;
    var position=wheels[i].localPosition;position.x=(i%2==0?-1:1)*WheelFitMath.StylingTrack(profile.rig.TrackWidth(i),width,offset,scale);wheels[i].localPosition=position;wheels[i].localScale=new Vector3(scale,1,1);
   }
   styles.SetWindowTint(11,4);
   foreach(var material in materials.Where(m=>m.name.StartsWith("Vehicle_mw_window",StringComparison.Ordinal)))if(material.HasColor("_Color")&&material.GetColor("_Color").a<.89f)throw new InvalidOperationException("Window tint did not apply");
   result.meshes=meshes.Count;
   var cameraObject=new GameObject("Audit camera");cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.17f,.2f);camera.nearClipPlane=.1f;camera.farClipPlane=40;
   camera.orthographic=true;camera.orthographicSize=Math.Max(2.3f,profile.rig.boxSizeZ*.65f);camera.transform.localPosition=new Vector3(-6,3,6);camera.transform.LookAt(root.transform.position+Vector3.up*profile.rig.boxCenterY);
   var lamp=new GameObject("Audit lamp");lamp.transform.SetParent(root.transform,false);lamp.transform.localPosition=new Vector3(-3,6,4);var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.intensity=6;light.range=30;light.cullingMask=1<<31;
   target=new RenderTexture(640,400,24);camera.targetTexture=target;target.Create();camera.Render();
   var previous=RenderTexture.active;try{RenderTexture.active=target;frame=new Texture2D(640,400,TextureFormat.RGBA32,false);frame.ReadPixels(new Rect(0,0,640,400),0,0);frame.Apply();File.WriteAllBytes(Path.Combine(output,profile.id+".png"),frame.EncodeToPNG());}finally{RenderTexture.active=previous;}
  }finally {
   audio?.Dispose();effects?.Dispose();nativeDecals?.Dispose();styles?.Dispose();UnityEngine.Object.Destroy(root);
   foreach(var mesh in meshes)UnityEngine.Object.Destroy(mesh);if(materials!=null)foreach(var m in materials)UnityEngine.Object.Destroy(m);if(textures!=null)foreach(var t in textures)UnityEngine.Object.Destroy(t);
   if(frame!=null)UnityEngine.Object.Destroy(frame);if(target!=null){target.Release();UnityEngine.Object.Destroy(target);}
  }
 }
}
}
