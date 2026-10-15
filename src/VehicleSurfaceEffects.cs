using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeedForValheim {
// Bounded world-space tyre dust and reversible car-only paint/rim overrides.
public sealed class VehicleSurfaceEffects : IDisposable {
    readonly GameObject car;readonly Rigidbody body;readonly WheelCollider[] wheels;
    readonly HashSet<string> paintNames=new HashSet<string>(StringComparer.Ordinal);
    readonly List<Surface> surfaces=new List<Surface>();
    readonly ParticleSystem[] dust=new ParticleSystem[4];
    readonly bool[] emitting=new bool[4];
    readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
    GameObject root;Texture2D texture;Material material;
    float nextScan,nextPaint,dirt,wetness;bool disposed,particlesFailed;
    readonly Action<string> log;
    sealed class Surface {public Renderer renderer;public Material material;public int index;public bool applied;}
    public float Dirt=>dirt;
    public VehicleSurfaceEffects(GameObject car,Rigidbody body,WheelCollider[] wheels,ModelData model,ModelData styles,Action<string> log) {
        this.car=car;this.body=body;this.wheels=wheels;this.log=log;
        foreach(var data in new[]{model,styles})if(data?.materials!=null)
            foreach(var m in data.materials)if(m.alphaMode!="blend"&&(m.bodyPaint||m.rimPaint||m.wheelRubber))paintNames.Add("Vehicle_"+m.name);
        // The old non-schema model has no material descriptors.
        if(model!=null&&model.schemaVersion<2)paintNames.Add("Vehicle_Paint");
    }
    void CreateParticles() {
        var shader=Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if(shader==null||!shader.isSupported)throw new InvalidOperationException("Alpha-blended particle shader missing");
        const int size=32;texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="NFVU2_RoadDust",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
            float r=new Vector2(x-15.5f,y-15.5f).magnitude/15.5f;
            pixels[y*size+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r*r),2)*.32f);
        }
        texture.SetPixels(pixels);texture.Apply(false,true);
        material=new Material(shader){name="NFVU2_RoadDust",mainTexture=texture,renderQueue=3000};
        if(material.HasColor("_TintColor"))material.SetColor("_TintColor",Color.white);
        root=new GameObject("NFVU2_RoadDust");root.transform.SetParent(car.transform,false);
        for(int i=0;i<dust.Length;i++) {
            var obj=new GameObject("ContactDust"+i);obj.transform.SetParent(root.transform,false);
            var ps=obj.AddComponent<ParticleSystem>();dust[i]=ps;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=48;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(.45f,.95f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(.1f,.7f);main.startSize=new ParticleSystem.MinMaxCurve(.16f,.35f);
            main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.gravityModifier=-.018f;
            var emission=ps.emission;emission.rateOverTime=0;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=32;shape.radius=.09f;
            var sizeModule=ps.sizeOverLifetime;sizeModule.enabled=true;sizeModule.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,2.4f));
            var fade=new Gradient();fade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.12f),new GradientAlphaKey(0,1)});
            var colour=ps.colorOverLifetime;colour.enabled=true;colour.color=fade;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
    }
    static VehicleGroundSurface Ground(Heightmap terrain,Vector3 point) {
        if(terrain==null)return VehicleGroundSurface.Hard;
        var biome=terrain.GetBiome(point);
        if((biome&(Heightmap.Biome.Mountain|Heightmap.Biome.DeepNorth))!=0)return VehicleGroundSurface.Snow;
        if((biome&Heightmap.Biome.AshLands)!=0)return VehicleGroundSurface.Ash;
        if((biome&Heightmap.Biome.Swamp)!=0||terrain.IsCultivated(point))return VehicleGroundSurface.Mud;
        return terrain.IsCleared(point)?VehicleGroundSurface.Dirt:VehicleGroundSurface.Grass;
    }
    static Color DustColour(VehicleGroundSurface ground)=>ground==VehicleGroundSurface.Snow?new Color(.85f,.9f,.95f,.7f):ground==VehicleGroundSurface.Ash?new Color(.33f,.31f,.3f,.7f):new Color(.57f,.47f,.32f,.7f);
    public void Tick(float dt,bool blocked,bool dustEnabled,bool grimeEnabled,bool wetPaintEnabled) {
        if(disposed||car==null||body==null||wheels==null)return;
        dt=Mathf.Clamp(dt,0,.25f);bool rain=EnvMan.instance!=null&&EnvMan.IsWet();
        WaterVolume volume=null;float water=Floating.GetWaterLevel(car.transform.position,ref volume);
        bool submerged=volume!=null&&car.transform.position.y<water+.1f;
        float speed=body.linearVelocity.magnitude;int contacts=0;var soil=VehicleGroundSurface.Hard;
        bool allowDust=dustEnabled&&!particlesFailed;
        if(allowDust&&root==null)try{CreateParticles();}catch(Exception e){particlesFailed=true;allowDust=false;log?.Invoke("Vehicle dust unavailable: "+e.Message);}
        for(int i=0;i<Math.Min(4,wheels.Length);i++) {
            var wheel=wheels[i];WheelHit hit=default;bool grounded=wheel!=null&&wheel.enabled&&wheel.GetGroundHit(out hit);
            float rate=0;var ground=VehicleGroundSurface.Hard;
            if(grounded) {
                var terrain=hit.collider!=null?hit.collider.GetComponentInParent<Heightmap>():null;
                ground=Ground(terrain,hit.point);
                if(ground!=VehicleGroundSurface.Hard){contacts++;if(ground==VehicleGroundSurface.Mud||soil==VehicleGroundSurface.Hard)soil=ground;}
                rate=VehicleSurfaceRules.DustRate(speed,Mathf.Max(Mathf.Abs(hit.sidewaysSlip),Mathf.Abs(hit.forwardSlip)),ground,rain,submerged,blocked||!allowDust,true);
                if(dust[i]!=null){dust[i].transform.position=hit.point+Vector3.up*.045f;dust[i].transform.rotation=Quaternion.LookRotation(hit.normal,car.transform.forward);var main=dust[i].main;main.startColor=DustColour(ground);}
            }
            if(dust[i]==null)continue;
            var emission=dust[i].emission;emission.rateOverTime=rate;
            bool active=rate>.05f;
            if(blocked||!allowDust){dust[i].Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);emitting[i]=false;}
            else if(active!=emitting[i]){if(active)dust[i].Play(false);else dust[i].Stop(false,ParticleSystemStopBehavior.StopEmitting);emitting[i]=active;}
        }
        dirt=VehicleSurfaceRules.DirtStep(dirt,dt,blocked?0:speed*dt,contacts,soil,rain,submerged,grimeEnabled);
        wetness=Mathf.MoveTowards(wetness,wetPaintEnabled&&(rain||submerged)?1:0,dt*.18f);
        if(Time.unscaledTime>=nextScan){ScanSurfaces();nextScan=Time.unscaledTime+1;}
        if(Time.unscaledTime>=nextPaint){Paint();nextPaint=Time.unscaledTime+.12f;}
    }
    void ScanSurfaces() {
        surfaces.RemoveAll(s=>s.renderer==null);
        foreach(var renderer in car.GetComponentsInChildren<MeshRenderer>(true)) {
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++) {
                var m=materials[i];if(m==null||!paintNames.Contains(m.name)||surfaces.Any(s=>s.renderer==renderer&&s.index==i))continue;
                block.Clear();renderer.GetPropertyBlock(block,i);
                // A different effect already owns this renderer: leave its overrides intact.
                if(!block.isEmpty)continue;
                surfaces.Add(new Surface{renderer=renderer,material=m,index=i});
            }
        }
    }
    void Paint() {
        foreach(var surface in surfaces) {
            if(surface.renderer==null||surface.material==null)continue;
            if(dirt<.001f&&wetness<.001f){if(surface.applied)surface.renderer.SetPropertyBlock(null,surface.index);surface.applied=false;continue;}
            var m=surface.material;block.Clear();surface.renderer.GetPropertyBlock(block,surface.index);
            // Read the live base material every time: paint, vinyl and finish changes remain authoritative.
            foreach(var p in new[]{"_Color","_BaseColor"})if(m.HasColor(p)) {
                var colour=m.GetColor(p);float alpha=colour.a;colour=Color.Lerp(colour,new Color(.3f,.24f,.16f,alpha),dirt*.22f);colour.a=alpha;block.SetColor(p,colour);
            }
            foreach(var p in new[]{"_Glossiness","_Smoothness"})if(m.HasFloat(p))block.SetFloat(p,Mathf.Lerp(m.GetFloat(p)*(1-dirt*.55f),.94f,wetness*.6f));
            surface.renderer.SetPropertyBlock(block,surface.index);surface.applied=true;
        }
    }
    public void Dispose() {
        if(disposed)return;disposed=true;
        foreach(var surface in surfaces)if(surface.renderer!=null&&surface.applied)surface.renderer.SetPropertyBlock(null,surface.index);
        surfaces.Clear();foreach(var ps in dust)if(ps!=null)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(root!=null)UnityEngine.Object.Destroy(root);if(material!=null)UnityEngine.Object.Destroy(material);if(texture!=null)UnityEngine.Object.Destroy(texture);
    }
}
public partial class Plugin {
    ConfigEntry<bool> vehicleMapMarkersEnabled,vehicleDustEnabled,vehicleGrimeEnabled,vehicleWetPaintEnabled;
    readonly VehicleMapMarkers vehicleMapMarkers=new VehicleMapMarkers();
    readonly Dictionary<VehicleRuntime,VehicleSurfaceEffects> vehicleSurfaces=new Dictionary<VehicleRuntime,VehicleSurfaceEffects>();
    void InitializeVehicleWorldFeatures() {
        vehicleMapMarkersEnabled=Config.Bind("Vehicles","MapMarkers",true,"Show temporary vehicle icons and model names on the small and large map. Pins are not saved.");
        vehicleDustEnabled=Config.Bind("Vehicles","RoadDust",true,"Grounded tyres emit bounded, surface-coloured dust. Rain, water and constructed floors suppress dust.");
        vehicleGrimeEnabled=Config.Bind("Vehicles","RoadGrime",true,"Gradual, subtle road grime on car paint/rims. Rain/water wash it away; disabling restores the chosen paint.");
        vehicleWetPaintEnabled=Config.Bind("Vehicles","WetPaint",true,"Optional car-only wet paint sheen in rain/water. No world shaders are changed.");
    }
    void AttachVehicleSurfaceEffects(ModelData model,ModelData styles) {
        if(vehicleSurfaces.TryGetValue(current,out var old))old.Dispose();
        vehicleSurfaces[current]=new VehicleSurfaceEffects(car,body,wheels,model,styles,message=>Logger.LogWarning(message));
    }
    void TickVehicleWorldFeatures() {
        if(vehicleMapMarkersEnabled==null)return;
        vehicleMapMarkers.Tick(fleet.Where(v=>v.car!=null).Select(v=>new KeyValuePair<GameObject,string>(v.car,v.Profile.displayName)),vehicleMapMarkersEnabled.Value);
        foreach(var entry in vehicleSurfaces.ToArray()) {
            var v=entry.Key;
            if(v.car==null||!fleet.Contains(v)){entry.Value.Dispose();vehicleSurfaces.Remove(v);continue;}
            try {entry.Value.Tick(Time.deltaTime,!v.driving||v.parkingFrozen||v.flight!=null&&v.flight.State.Active||audioBlocked||menuOpen||!Application.isFocused,vehicleDustEnabled.Value,vehicleGrimeEnabled.Value,vehicleWetPaintEnabled.Value);}
            catch(Exception e){entry.Value.Dispose();vehicleSurfaces.Remove(v);Logger.LogWarning("Vehicle surface effects disabled: "+e.Message);}
        }
    }
    void ReleaseVehicleWorldFeatures() {
        if(car!=null)vehicleMapMarkers.Remove(car);
        if(vehicleSurfaces.TryGetValue(current,out var effect)){effect.Dispose();vehicleSurfaces.Remove(current);}
    }
    void DisposeVehicleWorldFeatures(){vehicleMapMarkers.Dispose();foreach(var effect in vehicleSurfaces.Values)effect.Dispose();vehicleSurfaces.Clear();}
    void DrawVehicleWorldFeatureOptions(float x,float y,float width) {
        vehicleMapMarkersEnabled.Value=UiToggle(new Rect(x,y,width,28),vehicleMapMarkersEnabled.Value," vehicle on the map");
        vehicleDustEnabled.Value=UiToggle(new Rect(x,y+32,width,28),vehicleDustEnabled.Value," tyre dust by surface");
        vehicleGrimeEnabled.Value=UiToggle(new Rect(x,y+64,width,28),vehicleGrimeEnabled.Value," road grime on the car");
        vehicleWetPaintEnabled.Value=UiToggle(new Rect(x,y+96,width,28),vehicleWetPaintEnabled.Value," Nasser Fahrzeuglack");
    }
}
}
