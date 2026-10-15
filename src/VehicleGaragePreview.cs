using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    const int GarageLayer=30;
    CarProfile frontendProfile;
    VehicleRuntime garagePreview;
    GameObject garageStage;
    Camera garageStageCamera;
    RenderTexture garageFrame;
    Material garageStageMaterial;
    readonly List<Material> garageSceneryMaterials=new List<Material>();
    StagedWork garagePreviewWork;
    CancellationTokenSource garagePreviewCancellation;
    Task<Tuple<ModelData,ModelData,ModelData,DecalArtworkCatalog>> garagePreviewRead;
    float garageYaw=145,garagePitch=14,garageZoom=7;
    string garagePreviewStatus="";
    CarProfile FrontendProfile=>frontendProfile??SelectedProfile;
    bool GaragePreviewReady=>garagePreview!=null&&garagePreview.car!=null&&garagePreview.car.activeSelf&&garagePreviewWork==null;
    static bool PreviewTab(int tab)=>tab==13||tab==14||tab==15||tab==5||tab==6||tab==7||tab==2||tab==3||tab==4||tab==12;
    void BeginGarageFrontend() {
        try {
            EnsureGarageStage();
            SelectFrontendProfile(CanAccessCurrent(LocalPlayer())?current.Profile:SelectedProfile,true);
        }catch(Exception error){DisposeGarageFrontend();garagePreviewStatus="Showroom unavailable: "+error.Message;Logger.LogWarning(garagePreviewStatus);}
    }
    void EnsureGarageStage() {
        if(garageStage!=null&&garageStageCamera!=null&&garageFrame!=null&&garageStageMaterial!=null){garageStage.SetActive(true);return;}
        DisposeGarageFrontend();
        garageStage=new GameObject("NFVU2 Underground Showroom");garageStage.transform.position=new Vector3(0,12000,0);
        // Valheim loads this shader through materials; Shader.Find alone returns null.
        var shader=Resources.FindObjectsOfTypeAll<Material>().Where(m=>m!=null&&m.shader!=null&&m.shader.isSupported&&m.shader.name=="Standard").Select(m=>m.shader).FirstOrDefault();
        if(shader!=null)garageStageMaterial=new Material(shader){name="NFVU2 Showroom concrete",color=new Color(.16f,.19f,.22f)};
        else {var templates=VehicleMaterials.Create(LocalPlayer(),message=>Logger.LogInfo(message));garageStageMaterial=templates[1];Destroy(templates[0]);}
        if(garageStageMaterial.HasFloat("_Metallic"))garageStageMaterial.SetFloat("_Metallic",.08f);
        if(garageStageMaterial.HasFloat("_Glossiness"))garageStageMaterial.SetFloat("_Glossiness",.28f);
        if(menuAssets!=null&&menuAssets.Frontend.TryGetValue("garage_floor",out var floorTexture)) {
            foreach(var property in new[]{"_MainTex","_BaseMap","_Albedo"})if(garageStageMaterial.HasTexture(property)) {
                garageStageMaterial.SetTexture(property,floorTexture);garageStageMaterial.SetTextureScale(property,new Vector2(8,8));
            }
            foreach(var property in new[]{"_Color","_BaseColor"})if(garageStageMaterial.HasColor(property))garageStageMaterial.SetColor(property,new Color(.7f,.73f,.76f));
        }
        Material Surface(string name,Color color,bool emission=false) {
            var material=new Material(garageStageMaterial){name=name};garageSceneryMaterials.Add(material);
            foreach(var property in new[]{"_Color","_BaseColor"})if(material.HasColor(property))material.SetColor(property,color);
            if(emission)VehicleMaterials.SetEmission(material,color*2);
            return material;
        }
        var strip=Surface("Underground cyan strips",new Color(.12f,.65f,.85f),true);
        foreach(var property in new[]{"_MainTex","_BaseMap","_Albedo"})if(strip.HasTexture(property)){strip.SetTexture(property,Texture2D.whiteTexture);strip.SetTextureScale(property,Vector2.one);}
        void Cube(string name,Vector3 position,Vector3 scale,Material material=null) {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(cube.GetComponent<Collider>());cube.name=name;cube.layer=GarageLayer;cube.transform.SetParent(garageStage.transform,false);cube.transform.localPosition=position;cube.transform.localScale=scale;cube.GetComponent<Renderer>().sharedMaterial=material??garageStageMaterial;
        }
        Cube("Garage floor",new Vector3(0,-.1f,0),new Vector3(32,.2f,32));
        Cube("Back wall",new Vector3(0,3,9),new Vector3(26,7,.25f));
        Cube("Side wall",new Vector3(-11,3,0),new Vector3(.25f,7,26));
        for(int i=-3;i<=3;i++){Cube("Steel pillar",new Vector3(i*4,3,8.7f),new Vector3(.15f,6,.2f));Cube("Floor seam",new Vector3(i*3,-.041f,0),new Vector3(.024f,.015f,30));}
        Cube("Garage light rail",new Vector3(0,2.4f,8.5f),new Vector3(24,.08f,.04f),strip);
        Cube("Side light rail",new Vector3(-10.8f,2.4f,0),new Vector3(.04f,.08f,24),strip);
        Cube("Parking rail left",new Vector3(-3,.025f,0),new Vector3(.035f,.015f,7),strip);
        Cube("Parking rail right",new Vector3(3,.025f,0),new Vector3(.035f,.015f,7),strip);
        for(int i=0;i<3;i++) {
            var lamp=new GameObject("Garage softbox");lamp.transform.SetParent(garageStage.transform,false);lamp.transform.localPosition=new Vector3(i==0?-4:i==1?4:0,5,i==2?-5:1);
            var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=14;light.intensity=i==2?2.5f:3.5f;light.color=i==0?new Color(.65f,.82f,1):i==1?new Color(1,.9f,.7f):Color.white;light.cullingMask=1<<GarageLayer;
        }
        var cameraObject=new GameObject("NFVU2 Garage Camera");cameraObject.transform.SetParent(garageStage.transform,false);
        garageStageCamera=cameraObject.AddComponent<Camera>();garageStageCamera.cullingMask=1<<GarageLayer;garageStageCamera.clearFlags=CameraClearFlags.SolidColor;garageStageCamera.backgroundColor=new Color(.04f,.075f,.09f);garageStageCamera.fieldOfView=42;garageStageCamera.nearClipPlane=.1f;garageStageCamera.farClipPlane=70;
        garageStageCamera.useOcclusionCulling=false;
        garageStageCamera.renderingPath=RenderingPath.Forward;
        garageFrame=new RenderTexture(1200,900,24){name="NFVU2 Garage Preview"};garageFrame.Create();garageStageCamera.targetTexture=garageFrame;
        var reflection=new GameObject("Showroom reflections");reflection.transform.SetParent(garageStage.transform,false);reflection.transform.localPosition=Vector3.up*2;
        var probe=reflection.AddComponent<ReflectionProbe>();probe.mode=UnityEngine.Rendering.ReflectionProbeMode.Realtime;probe.refreshMode=UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;probe.cullingMask=1<<GarageLayer;probe.resolution=128;probe.size=new Vector3(28,12,28);probe.boxProjection=true;probe.RenderProbe();
    }
    void SelectFrontendProfile(CarProfile profile,bool force=false) {
        int source=profile.sourceGame==CarCatalog.MostWanted?1:0;
        if(garageSource!=source){garageSource=source;garagePage=0;}
        if(!force&&frontendProfile!=null&&frontendProfile.id==profile.id)return;
        DisposeGarageCar();frontendProfile=profile;garageYaw=145;garagePitch=14;garageZoom=profile.capabilities.suvTuning?8.5f:7;
        garagePreviewStatus="LOADING  /  "+profile.displayName;
        garagePreview=new VehicleRuntime{Profile=profile};garagePreviewCancellation=new CancellationTokenSource();var token=garagePreviewCancellation.Token;
        string folder=Path.GetDirectoryName(Info.Location);var cache=spawnCache;spawnCacheUsed=true;
        garagePreviewRead=Task.Run(()=>{
            var model=cache.Load(Path.Combine(folder,profile.assets.modelFile),ModelFile.Load,token);
            string path=Path.Combine(folder,"styling",profile.id+".json");ModelData styles=null;
            if(File.Exists(path)){styles=ModelFile.Load(path);if(styles.styleOptions==null)styles=null;}
            ModelData decals=null;DecalArtworkCatalog art=null;
            if(!string.IsNullOrEmpty(profile.assets.decalFile)) {
                try {
                    var geometry=cache.Load(Path.Combine(folder,profile.assets.decalFile),ModelFile.Load,token);
                    var artwork=cache.Load(Path.Combine(folder,"decals","mw-logos.json"),DecalArtworkCatalog.Load,token);
                    decals=geometry;art=artwork;
                }catch(OperationCanceledException){throw;}catch(Exception error){Logger.LogWarning("Native decals unavailable in showroom: "+error.Message);}
            }
            token.ThrowIfCancellationRequested();return Tuple.Create(model,styles,decals,art);
        },token);
        garagePreviewRead.ContinueWith(t=>{var ignored=t.Exception;},CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);
        garagePreviewWork=new StagedWork(BuildGarageCar());
    }
    IEnumerable<int> BuildGarageCar() {
        while(!garagePreviewRead.IsCompleted)yield return StagedWork.NextFrame;
        var data=garagePreviewRead.GetAwaiter().GetResult();var model=data.Item1;var rig=current.Profile.rig;
        car=new GameObject("Garage "+current.Profile.id);car.transform.SetParent(garageStage.transform,false);car.SetActive(false);
        originalWheels=model.schemaVersion>=2;wheelVisualLift=model.wheelVisualLift;frontWheelVisualOffset=model.frontWheelVisualOffset;rearWheelVisualOffset=model.rearWheelVisualOffset;
        if(originalWheels)wheelSizeVisuals=new WheelSizeVisuals(model,rig.rimNominal,rig.rimMin,rig.rimMax);
        foreach(var step in VehicleMaterials.CreateForModelSteps(LocalPlayer(),model,message=>Logger.LogInfo(message),(m,t)=>{vehicleMaterials=m;vehicleTextures=t;}))yield return step;
        var paint=vehicleMaterials[0];var dark=vehicleMaterials[Math.Min(1,vehicleMaterials.Length-1)];
        foreach(var part in model.parts.Where(p=>p.group=="body")){AddVisual(part,car.transform,model,paint,dark);yield return 0;}
        visuals=new Transform[4];brakeVisuals=new Transform[4];wheels=new WheelCollider[4];
        for(int i=0;i<4;i++) {
            var root=new GameObject("Garage wheel "+i);root.transform.SetParent(car.transform,false);visuals[i]=root.transform;
            wheels[i]=root.AddComponent<WheelCollider>();wheels[i].enabled=false;wheels[i].radius=rig.WheelRadius(i);
            var brakeRoot=new GameObject("Garage brake "+i);brakeRoot.transform.SetParent(root.transform,false);brakeVisuals[i]=brakeRoot.transform;
            foreach(var part in model.parts.Where(p=>ModelFile.ForAxle(p,i)&&(p.group=="wheel"||p.group=="spinner"))){AddVisual(part,root.transform,model,paint,dark);yield return 0;}
            foreach(var part in model.parts.Where(p=>ModelFile.ForAxle(p,i)&&p.group=="brake")){AddVisual(part,brakeRoot.transform,model,paint,dark);yield return 0;}
        }
        foreach(var step in BuildExtraRearAxle(model,paint,dark))yield return step;
        body=car.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
        if(model.animations!=null){showroom=new VehicleShowroom(car,model,(p,parent)=>AddVisual(p,parent,model,paint,dark),dark);ApplyShowroomSettings();}
        current.exhaustVisuals=new VehicleExhaustVisuals(car,model);
        try{vehicleLighting=VehicleLighting.Create(car,model,message=>Logger.LogInfo(message));vehicleNeon=VehicleNeon.Create(car,model,message=>Logger.LogInfo(message));}catch(Exception error){Logger.LogWarning("Garage light preview: "+error.Message);}
        current.styling=new VehicleStyling(car,model,data.Item2,vehicleMaterials,StylingWheels,(p,parent)=>AddVisual(p,parent,data.Item2,paint,dark,current.styling.Materials),StylingBrakes);
        foreach(var step in current.styling.LoadSteps(LocalPlayer(),message=>Logger.LogInfo(message)))yield return step;
        if(data.Item3!=null) {
            current.decals=new VehicleDecals(car,data.Item3,data.Item4,(part,parent,material)=>AddVisual(part,parent,data.Item3,paint,dark,new[]{material}));
            foreach(var step in current.decals.LoadSteps(LocalPlayer(),message=>Logger.LogInfo(message)))yield return step;
        }
        TickWorkshop();
        foreach(var node in car.GetComponentsInChildren<Transform>(true))node.gameObject.layer=GarageLayer;
        car.SetActive(true);garagePreviewStatus="";
    }
    void TickGarageFrontend() {
        if(!menuOpen||garageStage==null)return;
        var saved=current;
        try {
            current=garagePreview;
            if(garagePreviewWork!=null) {
                if(!garagePreviewWork.Tick(3)){garagePreviewWork=null;garagePreviewCancellation.Dispose();garagePreviewCancellation=null;}
            }
            if(GaragePreviewReady) {
                TickWorkshop();wheelSizeVisuals?.Apply(RimSetting.Value);current.exhaustVisuals?.Apply(exhaustStyle.Value);
                ApplyNeonSettings();vehicleNeon?.Tick(true,Time.unscaledDeltaTime);
                vehicleLighting?.SetHeadlights(headlightPreset.Value,headlightStrength.Value);vehicleLighting?.Tick(true,false,0,0,Time.unscaledDeltaTime,false);
                ApplyShowroomSettings();
                int color=Math.Max(0,Array.IndexOf(ColorNames,trunkNeonColor.Value));
                showroom?.Tick(Time.unscaledDeltaTime,false,doorStyle.Value,false,trunkNeonEnabled.Value,trunkNeonPulsing.Value,NeonColors[color]);
                var rig=current.Profile.rig;
                float lowered=WheelFitMath.Sanitize(lowering.Value,0,12,0)*.01f;
                car.transform.localPosition=new Vector3(0,-lowered,0);
                for(int i=0;i<4;i++) {
                    float side=i%2==0?-1:1;float camber=(i<2?frontCamber:rearCamber).Value;
                    visuals[i].localPosition=new Vector3(side*(BodykitTrack(i,out var widthScale)+(i<2?frontWheelOffset:rearWheelOffset).Value*.01f),rig.WheelHeight(i)+wheelVisualLift+lowered,i<2?rig.frontAxleZ+frontWheelVisualOffset:-rig.rearAxleZ+rearWheelVisualOffset);
                    visuals[i].localScale=new Vector3(widthScale,1,1);
                    visuals[i].localRotation=Quaternion.AngleAxis(-side*camber,Vector3.forward)*Quaternion.Euler(0,i%2==0?180:0,0);
                    UpdateExtraRearPose(i);
                }
                // Lazy styles add transforms after the initial model build.
                foreach(var node in car.GetComponentsInChildren<Transform>(true))node.gameObject.layer=GarageLayer;
                foreach(var light in car.GetComponentsInChildren<Light>(true))light.cullingMask=1<<GarageLayer;
            }
        }catch(Exception error){garagePreviewStatus="Preview unavailable: "+error.Message;Logger.LogWarning(garagePreviewStatus);DisposeGarageCar();}
        finally {current=saved;}
        if(garageStageCamera!=null) {
            var pivot=garageStage.transform.position+Vector3.up*(FrontendProfile.capabilities.suvTuning?1.15f:.75f);
            var offset=Quaternion.Euler(garagePitch,garageYaw,0)*Vector3.back*garageZoom;
            garageStageCamera.transform.SetPositionAndRotation(pivot+offset,Quaternion.LookRotation(-offset)*Quaternion.Euler(0,4,0));
        }
    }
    void GarageCameraInput(Vector2 degrees,float scroll) {garageYaw+=degrees.x;garagePitch=Mathf.Clamp(garagePitch-degrees.y,3,65);garageZoom=Mathf.Clamp(garageZoom-scroll*.5f,4,12);}
    void EndGarageFrontend(){DisposeGarageCar();if(garageStage!=null)garageStage.SetActive(false);}
    void DisposeGarageCar() {
        garagePreviewCancellation?.Cancel();garagePreviewWork?.Dispose();garagePreviewWork=null;
        garagePreviewCancellation?.Dispose();garagePreviewCancellation=null;garagePreviewRead=null;
        if(garagePreview!=null){var saved=current;try{current=garagePreview;ReleaseVehicle();}finally{current=saved;}garagePreview=null;}
    }
    void DisposeGarageFrontend() {
        EndGarageFrontend();if(garageStage!=null)Destroy(garageStage);garageStage=null;
        if(garageFrame!=null){garageFrame.Release();Destroy(garageFrame);}garageFrame=null;
        if(garageStageMaterial!=null)Destroy(garageStageMaterial);garageStageMaterial=null;
        foreach(var material in garageSceneryMaterials)if(material!=null)Destroy(material);garageSceneryMaterials.Clear();
        garageStageCamera=null;
    }
}
}
