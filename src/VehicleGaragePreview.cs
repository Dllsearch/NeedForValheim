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
    Material garageAccentMaterial,garageWallMaterial;
    GameObject garageU2Set,garageMwSet;
    ReflectionProbe garageProbe;
    Light[] garageStageLights;
    bool garageDesignMw;
    readonly List<Material> garageSceneryMaterials=new List<Material>();
    Renderer[] garageSceneryRenderers;
    readonly List<Renderer> garageHiddenScenery=new List<Renderer>();
    StagedWork garagePreviewWork;
    CancellationTokenSource garagePreviewCancellation;
    Task<Tuple<ModelData,ModelData,ModelData,DecalArtworkCatalog>> garagePreviewRead;
    float garageYaw=145,garagePitch=14,garageZoom=7;
    float garageHubHeight,garageGroundLift;
    Vector3 garageFocus;
    bool garageFocusReady;
    string garagePreviewStatus="";
    CarProfile FrontendProfile=>frontendProfile??SelectedProfile;
    bool GaragePreviewReady=>garagePreview!=null&&garagePreview.car!=null&&garagePreview.car.activeSelf&&garagePreviewWork==null;
    static bool PreviewTab(int tab)=>tab==13||tab==14||tab==15||tab==16||tab==5||tab==6||tab==7||tab==2||tab==3||tab==4||tab==12;
    void BeginGarageFrontend() {
        try {
            EnsureGarageStage();
            SelectFrontendProfile(CanAccessCurrent(LocalPlayer())?current.Profile:SelectedProfile,true);
        }catch(Exception error){DisposeGarageFrontend();garagePreviewStatus="Showroom unavailable: "+error.Message;Logger.LogWarning(garagePreviewStatus);}
    }
    void EnsureGarageStage() {
        if(garageStage!=null&&garageStageCamera!=null&&garageFrame!=null&&garageStageMaterial!=null){garageStage.SetActive(true);ApplyGarageDesign();return;}
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
        var strip=Surface("Garage light strips",new Color(.12f,.65f,.85f),true);garageAccentMaterial=strip;
        garageWallMaterial=Surface("Garage concrete walls",new Color(.24f,.31f,.34f));
        foreach(var property in new[]{"_MainTex","_BaseMap","_Albedo"})if(garageWallMaterial.HasTexture(property))garageWallMaterial.SetTextureScale(property,new Vector2(4,2));
        var steel=Surface("Garage steel frames",new Color(.12f,.15f,.17f));
        if(steel.HasFloat("_Metallic"))steel.SetFloat("_Metallic",.75f);
        var white=Surface("Workshop softboxes",new Color(.8f,.9f,1),true);
        foreach(var property in new[]{"_MainTex","_BaseMap","_Albedo"})if(strip.HasTexture(property)){strip.SetTexture(property,Texture2D.whiteTexture);strip.SetTextureScale(property,Vector2.one);}
        void Cube(string name,Vector3 position,Vector3 scale,Material material=null,Transform parent=null) {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(cube.GetComponent<Collider>());cube.name=name;cube.layer=GarageLayer;cube.transform.SetParent(parent??garageStage.transform,false);cube.transform.localPosition=position;cube.transform.localScale=scale;cube.GetComponent<Renderer>().sharedMaterial=material??garageStageMaterial;
        }
        Cube("Garage floor",new Vector3(0,-.1f,0),new Vector3(32,.2f,32));
        foreach(float side in new[]{-1f,1f}) {
            Cube("Garage end wall",new Vector3(0,3.5f,side*14),new Vector3(28,7,.25f),garageWallMaterial);
            Cube("Garage side wall",new Vector3(side*14,3.5f,0),new Vector3(.25f,7,28),garageWallMaterial);
        }
        Cube("Garage ceiling",new Vector3(0,7.1f,0),new Vector3(28,.2f,28),garageWallMaterial);
        for(int i=-3;i<=3;i++){Cube("Steel pillar",new Vector3(i*4,3,8.7f),new Vector3(.15f,6,.2f),steel);Cube("Floor seam",new Vector3(i*3,.002f,0),new Vector3(.018f,.003f,30),steel);}
        foreach(float side in new[]{-1f,1f}) {
            Cube("Garage light rail",new Vector3(0,2.4f,side*13.7f),new Vector3(24,.08f,.04f),strip);
            Cube("Side light rail",new Vector3(side*13.7f,2.4f,0),new Vector3(.04f,.08f,24),strip);
            for(int i=0;i<6;i++)Cube("Wall column",new Vector3(side*13.6f,3.4f,-12+i*4.8f),new Vector3(.2f,6.8f,.2f),steel);
        }
        Cube("Parking rail left",new Vector3(-3,.025f,0),new Vector3(.035f,.015f,7),strip);
        Cube("Parking rail right",new Vector3(3,.025f,0),new Vector3(.035f,.015f,7),strip);
        garageU2Set=new GameObject("Underground tuning studio");garageU2Set.transform.SetParent(garageStage.transform,false);
        garageMwSet=new GameObject("Most Wanted warehouse");garageMwSet.transform.SetParent(garageStage.transform,false);
        foreach(float x in new[]{-6f,6f}) {
            Cube("Dyno bay frame",new Vector3(x,2.8f,6.8f),new Vector3(.3f,5.6f,.3f),steel,garageU2Set.transform);
            Cube("Studio wall panel",new Vector3(x*.7f,2.9f,8.3f),new Vector3(2.8f,3.6f,.12f),steel,garageU2Set.transform);
            Cube("Studio panel light",new Vector3(x*.7f,4.6f,8.18f),new Vector3(2.5f,.14f,.06f),white,garageU2Set.transform);
            Cube("Tool cabinet",new Vector3(x, .7f,6.6f),new Vector3(1.4f,1.4f,.7f),steel);
            for(int i=0;i<4;i++)Cube("Drawer handle",new Vector3(x,.35f+i*.24f,6.22f),new Vector3(.8f,.035f,.035f),strip);
        }
        Cube("Studio overhead truss",new Vector3(0,5.5f,6.8f),new Vector3(12.2f,.3f,.3f),steel,garageU2Set.transform);
        for(int i=0;i<6;i++)Cube("Studio ceiling light",new Vector3(-5+i*2,5.25f,2),new Vector3(.7f,.08f,3.5f),white,garageU2Set.transform);
        Cube("Warehouse rolling door",new Vector3(0,2.3f,13.5f),new Vector3(6.4f,4.6f,.14f),steel,garageMwSet.transform);
        for(int i=0;i<19;i++)Cube("Door rib",new Vector3(0,.2f+i*.24f,13.39f),new Vector3(6.3f,.035f,.05f),garageWallMaterial,garageMwSet.transform);
        for(int i=0;i<7;i++) {
            Cube("Warehouse roof beam",new Vector3(-9+i*3,5.7f,0),new Vector3(.15f,.3f,19),steel,garageMwSet.transform);
            foreach(float side in new[]{-1f,1f})Cube("Warehouse window",new Vector3(side*13.7f,4.3f,-7+i*2.2f),new Vector3(.08f,1.3f,1.5f),white,garageMwSet.transform);
        }
        // Reuse scene materials and simple meshes: no external prop downloads or
        // world colliders. Keep the central orbit and vehicle silhouette clear.
        var rubber=Surface("Workshop tyre rubber",new Color(.025f,.028f,.03f));
        var red=Surface("Workshop cabinet red",new Color(.42f,.045f,.035f));
        var cardboard=Surface("Warehouse packing crates",new Color(.36f,.24f,.12f));
        void Cylinder(string name,Vector3 position,Vector3 scale,Material material,Transform parent) {
            var node=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(node.GetComponent<Collider>());
            node.name=name;node.layer=GarageLayer;node.transform.SetParent(parent,false);
            node.transform.localPosition=position;node.transform.localScale=scale;
            node.GetComponent<Renderer>().sharedMaterial=material;
        }
        foreach(float side in new[]{-1f,1f}) {
            var studio=garageU2Set.transform;
            Cube("Red workshop bench",new Vector3(side*6, .9f,4.8f),new Vector3(2.4f,1.8f,.75f),red,studio);
            Cube("Steel worktop",new Vector3(side*6,1.85f,4.8f),new Vector3(2.55f,.12f,.9f),steel,studio);
            for(int row=0;row<3;row++)Cube("Workbench drawer",new Vector3(side*6,.4f+row*.45f,4.39f),new Vector3(1.7f,.035f,.04f),white,studio);
            for(int tyre=0;tyre<4;tyre++)Cylinder("Spare tyre stack",new Vector3(side*7.8f,.17f+tyre*.32f,5.1f),new Vector3(.95f,.15f,.95f),rubber,studio);
            Cube("Wheel rack top",new Vector3(side*8,2.5f,8),new Vector3(2.3f,.12f,.8f),red,studio);
            Cube("Wheel rack shelf",new Vector3(side*8,1.2f,8),new Vector3(2.3f,.12f,.8f),red,studio);
            foreach(float edge in new[]{-1f,1f})Cube("Wheel rack upright",new Vector3(side*8+edge,1.4f,8),new Vector3(.12f,2.8f,.8f),red,studio);
            var warehouse=garageMwSet.transform;
            for(int box=0;box<3;box++) {
                var at=new Vector3(side*(6.4f+box*.9f),.55f,7.8f+box*.55f);
                Cube("Packing crate",at,new Vector3(.85f,1.1f,.85f),cardboard,warehouse);
                Cube("Crate strap",at+new Vector3(0,.56f,0),new Vector3(.06f,.015f,.86f),steel,warehouse);
            }
            Cylinder("Workshop gas cylinder",new Vector3(side*8.8f,.9f,6.5f),new Vector3(.42f,.9f,.42f),steel,warehouse);
            Cylinder("Cylinder valve",new Vector3(side*8.8f,1.86f,6.5f),new Vector3(.12f,.06f,.12f),red,warehouse);
            for(int tyre=0;tyre<3;tyre++)Cylinder("Warehouse tyre stack",new Vector3(side*5.7f,.18f+tyre*.34f,8.5f),new Vector3(1,.16f,1),rubber,warehouse);
        }
        void Sign(GameObject parent,Texture2D texture) {
            if(texture==null)return;
            var material=Surface("Original garage wall artwork",Color.white);
            foreach(var property in new[]{"_MainTex","_BaseMap","_Albedo"})if(material.HasTexture(property)){material.SetTexture(property,texture);material.SetTextureScale(property,Vector2.one);}
            material.SetInt("_Cull",0);material.SetFloat("_Mode",1);material.SetFloat("_Cutoff",.5f);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;
            var sign=GameObject.CreatePrimitive(PrimitiveType.Quad);Destroy(sign.GetComponent<Collider>());sign.name="Original garage logo";sign.layer=GarageLayer;
            sign.transform.SetParent(parent.transform,false);sign.transform.localPosition=new Vector3(0,5.1f,8.25f);sign.transform.localScale=new Vector3(6,1.2f,1);sign.GetComponent<Renderer>().sharedMaterial=material;
        }
        if(menuAssets!=null){menuAssets.Frontend.TryGetValue("logo",out var u2Logo);Sign(garageU2Set,u2Logo);menuAssets.MwFrontend.TryGetValue("wordmark",out var mwLogo);Sign(garageMwSet,mwLogo);}
        // Capture scenery before the preview car is created. Vehicle parts must
        // never be hidden by the camera's scenery visibility pass.
        garageSceneryRenderers=garageStage.GetComponentsInChildren<Renderer>(true);
        garageStageLights=new Light[3];
        for(int i=0;i<3;i++) {
            var lamp=new GameObject("Garage softbox");lamp.transform.SetParent(garageStage.transform,false);lamp.transform.localPosition=new Vector3(i==0?-4:i==1?4:0,5,i==2?-5:1);
            var light=lamp.AddComponent<Light>();garageStageLights[i]=light;light.type=LightType.Point;light.range=32;light.intensity=i==2?1.6f:2.4f;light.cullingMask=1<<GarageLayer;
        }
        var cameraObject=new GameObject("NFVU2 Garage Camera");cameraObject.transform.SetParent(garageStage.transform,false);
        garageStageCamera=cameraObject.AddComponent<Camera>();garageStageCamera.cullingMask=1<<GarageLayer;garageStageCamera.clearFlags=CameraClearFlags.SolidColor;garageStageCamera.backgroundColor=new Color(.04f,.075f,.09f);garageStageCamera.fieldOfView=42;garageStageCamera.nearClipPlane=.1f;garageStageCamera.farClipPlane=70;
        garageStageCamera.useOcclusionCulling=false;
        garageStageCamera.renderingPath=RenderingPath.Forward;
        garageFrame=new RenderTexture((int)GaragePreviewRect.width,(int)GaragePreviewRect.height,24){name="NFVU2 Garage Preview"};garageFrame.Create();garageStageCamera.targetTexture=garageFrame;
        var reflection=new GameObject("Showroom reflections");reflection.transform.SetParent(garageStage.transform,false);reflection.transform.localPosition=Vector3.up*2;
        var probe=reflection.AddComponent<ReflectionProbe>();garageProbe=probe;probe.mode=UnityEngine.Rendering.ReflectionProbeMode.Realtime;probe.refreshMode=UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;probe.cullingMask=1<<GarageLayer;probe.resolution=128;probe.size=new Vector3(28,12,28);probe.boxProjection=true;ApplyGarageDesign(true);
    }
    void ApplyGarageDesign(bool force=false) {
        if(garageU2Set==null||garageMwSet==null||(!force&&garageDesignMw==IsMwFrontend))return;
        garageDesignMw=IsMwFrontend;garageU2Set.SetActive(!garageDesignMw);garageMwSet.SetActive(garageDesignMw);
        Color accent=garageDesignMw?new Color(.95f,.7f,.23f):new Color(.12f,.65f,.85f);
        foreach(var property in new[]{"_Color","_BaseColor"}) {
            if(garageAccentMaterial.HasColor(property))garageAccentMaterial.SetColor(property,accent);
            if(garageWallMaterial.HasColor(property))garageWallMaterial.SetColor(property,garageDesignMw?new Color(.38f,.33f,.24f):new Color(.24f,.31f,.34f));
            if(garageStageMaterial.HasColor(property))garageStageMaterial.SetColor(property,garageDesignMw?new Color(.56f,.52f,.42f):new Color(.7f,.73f,.76f));
        }
        VehicleMaterials.SetEmission(garageAccentMaterial,accent*2);
        for(int i=0;i<garageStageLights.Length;i++)garageStageLights[i].color=garageDesignMw?(i==0?new Color(1,.95f,.83f):i==1?new Color(1,.82f,.6f):Color.white):(i==0?new Color(.8f,.9f,1):i==1?new Color(1,.94f,.86f):Color.white);
        garageStageCamera.backgroundColor=garageDesignMw?new Color(.075f,.065f,.04f):new Color(.04f,.075f,.09f);
        garageProbe?.RenderProbe();
    }
    void SelectFrontendProfile(CarProfile profile,bool force=false) {
        int source=profile.sourceGame==CarCatalog.MostWanted?1:0;
        if(garageSource!=source){garageSource=source;garagePage=0;}
        int index=Array.FindIndex(vehicleCatalog.ForSource(profile.sourceGame),p=>p.id==profile.id);
        if(index>=0)garagePage=index/GaragePageSize;
        var target=NetworkSessionActive?(current.networkData!=null&&current.Profile.id==profile.id?current:fleet.FirstOrDefault(v=>v.Profile.id==profile.id&&OwnNetworkVehicle(v))):null;
        if(!force&&frontendProfile!=null&&frontendProfile.id==profile.id&&garagePreview?.networkPreviewOf==target)return;
        DisposeGarageCar();frontendProfile=profile;garageYaw=145;garagePitch=14;garageZoom=profile.capabilities.suvTuning?8.5f:7;garageFocusReady=false;
        garagePreviewStatus="LOADING  /  "+profile.displayName;
        garagePreview=new VehicleRuntime{Profile=profile,networkPreviewOf=target,networkSettings=target?.networkSettings};garagePreviewCancellation=new CancellationTokenSource();var token=garagePreviewCancellation.Token;
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
        garageHubHeight=model.previewHubHeight>0?model.previewHubHeight:rig.wheelHeight;
        // Fitted models retain their original hub/body relationship. Raise the
        // entire preview, rather than pushing only the wheels into the arches.
        float bottom=float.PositiveInfinity;
        for(int axle=0;axle<2;axle++)foreach(var part in model.parts.Where(p=>p.group=="wheel"&&ModelFile.ForAxle(p,axle*2)))
            foreach(var vertex in part.vertices)bottom=Mathf.Min(bottom,garageHubHeight+rig.WheelHeight(axle*2)-rig.wheelHeight+wheelVisualLift+vertex.y);
        garageGroundLift=float.IsInfinity(bottom)?0:Mathf.Max(0,.02f-bottom);
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
        nitroEffects=VehicleNitroEffects.Create(car,model,message=>Logger.LogInfo("Garage effects: "+message));
        foreach(var particle in car.GetComponentsInChildren<ParticleSystem>(true)){var settings=particle.main;settings.useUnscaledTime=true;}
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
        ApplyGarageDesign();
        var saved=current;
        try {
            current=garagePreview;
            if(garagePreviewWork!=null) {
                if(!garagePreviewWork.Tick(3)){garagePreviewWork=null;garagePreviewCancellation.Dispose();garagePreviewCancellation=null;}
            }
            if(GaragePreviewReady) {
                TickWorkshop();wheelSizeVisuals?.Apply(RimSetting.Value);TickExhaustCustomization();
                bool effectPage=menuTab==16&&(showcasePage==2||showcasePage==3);
                bool previewEffects=effectPage&&Application.isFocused;
                nitroEffects?.Tick(previewEffects&&garageEffectMode==1,previewEffects&&garageEffectMode==2,previewEffects&&garageEffectMode==3&&Time.unscaledTime%1.8f<.2f,!previewEffects,Time.unscaledDeltaTime);
                ApplyNeonSettings();if(vehicleNeon!=null){vehicleNeon.PreviewGroundY=garageStage.transform.position.y+.012f;vehicleNeon.Tick(true,Time.unscaledDeltaTime);}
                vehicleLighting?.SetHeadlights(headlightPreset.Value,headlightStrength.Value);vehicleLighting?.Tick(true,false,0,0,Time.unscaledDeltaTime,false);
                ApplyShowroomSettings();
                int color=Math.Max(0,Array.IndexOf(ColorNames,trunkNeonColor.Value));
                showroom?.Tick(Time.unscaledDeltaTime,false,doorStyle.Value,false,trunkNeonEnabled.Value,trunkNeonPulsing.Value,NeonColors[color]);
                var rig=current.Profile.rig;
                float lowered=WheelFitMath.Sanitize(lowering.Value,0,12,0)*.01f;
                car.transform.localPosition=new Vector3(0,garageGroundLift-lowered,0);
                for(int i=0;i<4;i++) {
                    float side=i%2==0?-1:1;float camber=(i<2?frontCamber:rearCamber).Value;
                    visuals[i].localPosition=new Vector3(side*(BodykitTrack(i,out var widthScale)+(i<2?frontWheelOffset:rearWheelOffset).Value*.01f),garageHubHeight+rig.WheelHeight(i)-rig.wheelHeight+wheelVisualLift+lowered,i<2?rig.frontAxleZ+frontWheelVisualOffset:-rig.rearAxleZ+rearWheelVisualOffset);
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
            bool wheelFocus=GaragePreviewReady&&(menuTab==5||menuTab==14&&(workshopTab==2||workshopTab==1&&StyleCategories[styleCategory]=="Brakes"));
            var target=wheelFocus?garagePreview.visuals[0].position:garageStage.transform.position+Vector3.up*(FrontendProfile.capabilities.suvTuning?1.15f:.75f);
            garageFocus=garageFocusReady?Vector3.Lerp(garageFocus,target,1-Mathf.Exp(-Time.unscaledDeltaTime*7)):target;garageFocusReady=true;
            var pivot=garageFocus;
            var offset=Quaternion.Euler(garagePitch,garageYaw,0)*Vector3.back*garageZoom*(wheelFocus?.45f:1);
            garageStageCamera.transform.SetPositionAndRotation(pivot+offset,Quaternion.LookRotation(-offset)*Quaternion.Euler(0,4,0));
            UpdateGarageSceneryVisibility(pivot,wheelFocus);
        }
    }
    void ClearGarageSceneryVisibility() {
        foreach(var renderer in garageHiddenScenery)if(renderer!=null)renderer.forceRenderingOff=false;
        garageHiddenScenery.Clear();
    }
    void UpdateGarageSceneryVisibility(Vector3 focus,bool wheelFocus) {
        ClearGarageSceneryVisibility();
        if(garageSceneryRenderers==null||garageStageCamera==null)return;
        var camera=garageStageCamera.transform;
        float width=wheelFocus?.22f:Mathf.Max(.65f,FrontendProfile.rig.boxSizeX*.4f);
        float height=wheelFocus?.12f:.35f;
        foreach(var renderer in garageSceneryRenderers) {
            if(renderer==null||!renderer.enabled||renderer.forceRenderingOff||!renderer.gameObject.activeInHierarchy)continue;
            var bounds=renderer.bounds;bounds.Expand(.08f);
            for(int sample=0;sample<5;sample++) {
                var point=focus+(sample==1?camera.right*width:sample==2?-camera.right*width:sample==3?Vector3.up*height:sample==4?-Vector3.up*height:Vector3.zero);
                var direction=point-camera.position;float distance=direction.magnitude;
                if(distance<.01f)continue;
                if(bounds.IntersectRay(new Ray(camera.position,direction/distance),out var hit)&&hit<distance-.15f) {
                    renderer.forceRenderingOff=true;garageHiddenScenery.Add(renderer);break;
                }
            }
        }
    }
    void GarageCameraInput(Vector2 degrees,float scroll) {garageYaw+=degrees.x;garagePitch=Mathf.Clamp(garagePitch-degrees.y,3,65);garageZoom=Mathf.Clamp(garageZoom-scroll*.5f,4,12);}
    void EndGarageFrontend(){ClearGarageSceneryVisibility();DisposeGarageCar();if(garageStage!=null)garageStage.SetActive(false);}
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
        ClearGarageSceneryVisibility();garageSceneryRenderers=null;
        garageU2Set=garageMwSet=null;garageProbe=null;garageStageLights=null;garageAccentMaterial=garageWallMaterial=null;
    }
}
}
