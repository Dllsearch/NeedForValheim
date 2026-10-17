using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
namespace NeedForValheim {
// Optional original-part library, with lazy mesh creation and atomic swaps.
// Browsing parts never moves the car, touches its colliders or respawns cargo.
public sealed class VehicleStyling : IDisposable {
    readonly GameObject car;
    readonly ModelData original,catalog;
    readonly Transform[] wheels,brakes;
    readonly Func<ModelPart,Transform,Transform> create;
    readonly Dictionary<string,List<GameObject>> built=new Dictionary<string,List<GameObject>>();
    readonly Dictionary<string,int> active=new Dictionary<string,int>();
    readonly Dictionary<string,ModelStyleOption[]> available=new Dictionary<string,ModelStyleOption[]>();
    readonly Dictionary<string,List<GameObject>> originals=new Dictionary<string,List<GameObject>>();
    readonly List<PaintSurface> painted=new List<PaintSurface>();
    readonly List<Tuple<Material,Color>> glass=new List<Tuple<Material,Color>>();
    int lastGlassColour=-1,lastGlassShade=-1;
    readonly List<GameObject> spoilerSupports=new List<GameObject>();
    readonly List<GameObject> owned=new List<GameObject>();
    IEnumerator<int> pending;
    string pendingCategory;
    int pendingId;
    Material[] materials;
    Texture2D[] textures;
    readonly VehicleVinylRenderer vinyls;
    readonly Color originalBodyColour;
    int paintRevision;
    float carbonMetal=.08f,carbonGloss=.45f;
    int lastPaint=-1,lastFinish=-1,lastRim=-1,lastVinyl=-1,lastVinylColor=-1;
    bool lastTrim;
    readonly bool factoryWide;
    sealed class PaintSurface {public Material material;public Color color;public Texture texture;public float metal,gloss;public bool rim,trim,vinyl,carbon;}
    public Material[] Materials=>materials;
    public bool Busy=>pending!=null;
    public bool HasCatalog=>catalog!=null;
    public VehicleVinylRenderer Vinyls=>vinyls;
    public int ActiveBodykit=>active.TryGetValue("Bodykit",out var bodykit)?bodykit:0;
    public ModelStyleOption ActiveOption(string category)=>available.TryGetValue(category,out var options)?options.FirstOrDefault(o=>o.id==(active.TryGetValue(category,out var selected)?selected:0)):null;
    public VehicleStyling(GameObject vehicle,ModelData model,ModelData styles,Material[] baseMaterials,Transform[] wheelRoots,Func<ModelPart,Transform,Transform> factory,Transform[] brakeRoots=null) {
        car=vehicle;original=model;catalog=styles;wheels=wheelRoots;brakes=brakeRoots;create=factory;
        factoryWide=model.parts.Any(p=>p.group=="body"&&p.name.Contains("_KITW"));
        foreach(var category in new[]{"Bodykit","Front","Rear","Skirts","Hood","Spoiler","Roof","Brakes","Rims"})
            available[category]=new[]{new ModelStyleOption{category=category,id=0,title="Factory"}}.Concat(styles?.styleOptions.Where(o=>o.category==category)??Enumerable.Empty<ModelStyleOption>()).ToArray();
        var supports=new HashSet<string>(model.parts.Where(p=>p.optionGroup=="spoiler-support").Select(p=>p.name));
        foreach(var node in car.GetComponentsInChildren<Transform>(true))if(supports.Contains(node.name)){spoilerSupports.Add(node.gameObject);node.gameObject.SetActive(false);}
        RememberMaterials(model,baseMaterials);
        originalBodyColour=OriginalBodyColour();
        vinyls=new VehicleVinylRenderer(model.preset);
        if(styles!=null) {
            var nodes=car.GetComponentsInChildren<Transform>(true);
            foreach(var replacement in styles.styleReplacements) {
                int split=replacement.IndexOf('|');if(split<1)continue;
                string category=replacement.Substring(0,split),name=replacement.Substring(split+1);
                if(!originals.TryGetValue(category,out var list))originals[category]=list=new List<GameObject>();
                list.AddRange(nodes.Where(t=>t.name==name).Select(t=>t.gameObject));
            }
        }
    }
    public IEnumerable<int> LoadSteps(MonoBehaviour local,Action<string> log) {
        if(catalog==null)yield break;
        foreach(var step in VehicleMaterials.CreateForModelSteps(local,catalog,log,(m,t)=>{materials=m;textures=t;}))yield return step;
        RememberMaterials(catalog,materials);
    }
    void RememberMaterials(ModelData data,Material[] table) {
        if(table==null||data.materials==null)return;
        for(int i=0;i<table.Length;i++) {
            var description=data.materials[i];var m=table[i];
            string name=description.name??"";
            bool carbon=description.texture>=0&&IsCarbon(data.textures[description.texture].name);
            if(carbon) {
                carbonMetal=description.metallic;carbonGloss=description.smoothness;
            }
            bool wheelSurface=data.parts.Any(p=>p.material==i&&(p.group=="wheel"||p.group=="spinner"));
            bool rim=wheelSurface&&(description.rimPaint||name.StartsWith("wheel_",StringComparison.Ordinal)||(name.Contains("_wheel_")&&description.metallic>.4f));
            bool trim=data.parts.Any(p=>p.material==i&&p.group=="body")&&(name.StartsWith("wheel_chrome_",StringComparison.Ordinal)||new[]{"fd102a92_","54949afd_","2e65e067_","22719fa9_"}.Any(prefix=>name.StartsWith(prefix,StringComparison.Ordinal)));
            if(name.StartsWith("mw_window",StringComparison.Ordinal))glass.Add(Tuple.Create(m,m.HasColor("_Color")?m.GetColor("_Color"):Color.white));
            if(!description.bodyPaint&&!rim&&!trim)continue;
            painted.Add(new PaintSurface{material=m,color=m.HasColor("_Color")?m.GetColor("_Color"):Color.white,texture=m.HasTexture("_MainTex")?m.GetTexture("_MainTex"):null,
                metal=m.HasFloat("_Metallic")?m.GetFloat("_Metallic"):description.metallic,gloss=m.HasFloat("_Glossiness")?m.GetFloat("_Glossiness"):description.smoothness,rim=rim,trim=trim,carbon=carbon,
                vinyl=description.bodyPaint&&!rim&&!trim&&(description.texture<0||!IsCarbon(data.textures[description.texture].name))});
        }
    }
    static bool IsCarbon(string name)=>name!=null&&(name.IndexOf("carbon",StringComparison.OrdinalIgnoreCase)>=0||name.Contains("721aff7c")||name.Contains("b57aef2e"));
    public void SetWindowTint(int colour,int shade) {
        colour=Math.Max(0,Math.Min(colour,Plugin.PaintPaletteSize-1));shade=Math.Max(0,Math.Min(shade,4));
        if(colour==lastGlassColour&&shade==lastGlassShade)return;
        foreach(var surface in glass) {
            var tint=colour==0?surface.Item2:Plugin.PaintColors[colour];tint.a=shade==0?surface.Item2.a:new[]{0f,.2f,.45f,.7f,.9f}[shade];
            foreach(var property in new[]{"_Color","_BaseColor"})if(surface.Item1.HasColor(property))surface.Item1.SetColor(property,tint);
        }lastGlassColour=colour;lastGlassShade=shade;
    }
    public ModelStyleOption[] Options(string category)=>available[category];
    public bool Supports(string category,int bodykit) {
        if(!Options("Bodykit").Any(o=>o.id==bodykit))bodykit=0;
        if(category!="Front"&&category!="Rear"&&category!="Skirts")return true;
        return bodykit==1||bodykit==0&&!factoryWide;
    }
    int Target(Dictionary<string,int> desired,string category) {
        int bodykit=Options("Bodykit").Any(o=>o.id==desired["Bodykit"])?desired["Bodykit"]:0;
        int target=Supports(category,bodykit)?desired[category]:0;
        if(bodykit==1&&target==0&&(category=="Front"||category=="Rear"||category=="Skirts"))target=1;
        return Options(category).Any(o=>o.id==target)?target:0;
    }
    public void Tick(Dictionary<string,int> desired,int paint,int finish,int rim,int vinyl,int colour,bool paintTrim,VinylLayerState[] layers=null) {
        if(catalog!=null) {
            if(pending!=null&&Target(desired,pendingCategory)!=pendingId){pending.Dispose();pending=null;}
            if(pending==null)foreach(var pair in desired) {
                int target=Target(desired,pair.Key);
                int selected=active.TryGetValue(pair.Key,out var value)?value:0;
                if(target==selected)continue;
                pendingCategory=pair.Key;pendingId=target;pending=Build(pair.Key,target).GetEnumerator();break;
            }
            if(pending!=null) {
                var timer=Stopwatch.StartNew();
                do {if(!pending.MoveNext()){pending.Dispose();pending=null;lastPaint=-1;paintRevision++;break;}}while(timer.Elapsed.TotalMilliseconds<2);
            }
        }
        layers=layers??new[]{new VinylLayerState{design=vinyl,primary=colour}};
        if(vinyls.Tick())lastPaint=-1;
        var baseColour=paint==0?originalBodyColour:Plugin.PaintColors[Mathf.Clamp(paint,0,Plugin.PaintPaletteSize-1)];
        var previous=vinyls.Texture;vinyls.Request(layers,baseColour,paintRevision,VinylGeometry);
        if(previous!=vinyls.Texture)lastPaint=-1;
        ApplyPaint(paint,finish,rim,vinyl,colour,paintTrim);
    }
    Color OriginalBodyColour(){
        var surface=painted.FirstOrDefault(s=>s.vinyl);if(surface==null)return Color.white;
        var descriptor=original.materials.FirstOrDefault(m=>m.bodyPaint&&m.texture>=0&&!IsCarbon(original.textures[m.texture].name));
        if(descriptor==null)return surface.color;
        var rgba=original.textures[descriptor.texture].rgba;var counts=new Dictionary<int,int>();
        for(int i=0;i<rgba.Length;i+=64){int rgb=rgba[i]<<16|rgba[i+1]<<8|rgba[i+2];counts[rgb]=counts.TryGetValue(rgb,out var n)?n+1:1;}
        int dominant=counts.OrderByDescending(p=>p.Value).First().Key;
        return new Color((dominant>>16&255)/255f*surface.color.r,(dominant>>8&255)/255f*surface.color.g,(dominant&255)/255f*surface.color.b);
    }
    VinylPoint[][] VinylGeometry(){
        var parts=original.parts.Concat(catalog?.parts??Array.Empty<ModelPart>()).GroupBy(p=>p.name).ToDictionary(g=>g.Key,g=>g.First());
        var triangles=new List<VinylPoint[]>();
        foreach(var filter in car.GetComponentsInChildren<MeshFilter>(true)){
            var renderer=filter.GetComponent<Renderer>();if(!PaintPartVisible(filter.transform)||renderer==null||!painted.Any(s=>s.vinyl&&s.material==renderer.sharedMaterial)||!parts.TryGetValue(filter.name,out var p)||p.uv==null)continue;
            for(int i=0;i<p.indices.Length;i+=3){var triangle=new VinylPoint[3];for(int j=0;j<3;j++){int v=p.indices[i+j];var point=p.vertices[v];var uv=p.uv[v];var normal=p.normals?[v];triangle[j]=new VinylPoint{x=point.x,y=point.y,z=point.z,u=uv.u,v=uv.v,nx=normal?.x??0,ny=normal?.y??1,nz=normal?.z??0};}triangles.Add(triangle);}
        }return triangles.ToArray();
    }
    bool PaintPartVisible(Transform part){for(var node=part;node!=null&&node!=car.transform;node=node.parent)if(!node.gameObject.activeSelf)return false;return true;}
    IEnumerable<int> Build(string category,int id) {
        string key=category+":"+id;
        if(id!=0&&!built.TryGetValue(key,out _)) {
            var list=new List<GameObject>();bool complete=false;
            try {
            foreach(var part in catalog.parts.Where(p=>p.option==key)) {
                if(category=="Rims"||category=="Brakes")for(int i=0;i<wheels.Length;i++) {
                    if(!ModelFile.ForAxle(part,i))continue;
                    var parent=category=="Brakes"&&!part.rotatesWithWheel?brakes?[i]:wheels[i];
                    if(parent==null)throw new InvalidOperationException("Brake attachment roots unavailable");
                    var node=create(part,parent);node.gameObject.SetActive(false);list.Add(node.gameObject);owned.Add(node.gameObject);yield return 0;
                }
                else {
                    Transform parent=car.transform;
                    // Imported hinges remain owned by the existing showroom system.
                    if(!string.IsNullOrEmpty(part.movingPart))parent=car.transform.Find("Vehicle_Motion_"+part.movingPart)??parent;
                    var node=create(part,parent);node.gameObject.SetActive(false);list.Add(node.gameObject);owned.Add(node.gameObject);yield return 0;
                }
            }
            complete=true;built[key]=list;
            }finally {if(!complete)foreach(var node in list){owned.Remove(node);if(node!=null)UnityEngine.Object.Destroy(node);}}
        }
        active[category]=id;
        foreach(var node in spoilerSupports)node.SetActive(active.TryGetValue("Spoiler",out var selectedSpoiler)&&selectedSpoiler!=0);
        // Union all replacement sets: a kit cannot resurrect another category's mesh.
        foreach(var node in originals.Values.SelectMany(v=>v).Distinct()) {
            bool hidden=originals.Any(pair=>active.TryGetValue(pair.Key,out var selection)&&selection!=0&&pair.Value.Contains(node));
            node.SetActive(!hidden);
        }
        foreach(var pair in built) {
            int split=pair.Key.IndexOf(':');string group=pair.Key.Substring(0,split);int option=int.Parse(pair.Key.Substring(split+1));
            bool show=active.TryGetValue(group,out var selection)&&selection==option&&Supports(group,active.TryGetValue("Bodykit",out var body)?body:0);
            foreach(var node in pair.Value)node.SetActive(show);
        }
    }
    void ApplyPaint(int paint,int finish,int rim,int vinyl,int colour,bool paintTrim) {
        paint=Mathf.Clamp(paint,0,Plugin.PaintPaletteSize-1);rim=Mathf.Clamp(rim,0,Plugin.PaintPaletteSize-1);colour=Mathf.Clamp(colour,0,Plugin.PaintPaletteSize-1);
        if(paint==lastPaint&&finish==lastFinish&&rim==lastRim&&vinyl==lastVinyl&&colour==lastVinylColor&&paintTrim==lastTrim)return;
        foreach(var surface in painted) {
            var material=surface.material;int choice=surface.rim?rim:surface.trim&&!paintTrim?0:paint;
            var color=surface.carbon||choice==0?surface.color:Plugin.PaintColors[choice];
            if(surface.vinyl&&vinyl!=0&&paint==0)color=originalBodyColour;
            if(surface.vinyl&&vinyls.Texture!=null)color=Color.white;
            foreach(var property in new[]{"_Color","_BaseColor"})if(material.HasColor(property))material.SetColor(property,color);
            if(surface.rim)foreach(var property in new[]{"_MainTex","_BaseMap","_Albedo"})if(material.HasTexture(property))material.SetTexture(property,rim==0?surface.texture:Texture2D.whiteTexture);
            if(surface.trim)foreach(var property in new[]{"_MainTex","_BaseMap","_Albedo"})if(material.HasTexture(property))material.SetTexture(property,choice==0?surface.texture:Texture2D.whiteTexture);
            if(!surface.rim&&!surface.trim) {
                float metal=surface.carbon||finish==0?surface.metal:finish==2?.85f:.05f,gloss=surface.carbon||finish==0?surface.gloss:finish==3?.15f:.9f;
                if(surface.vinyl) {
                    metal=Mathf.Lerp(metal,carbonMetal,vinyls.CarbonAmount);
                    gloss=Mathf.Lerp(gloss,carbonGloss,vinyls.CarbonAmount);
                }
                if(material.HasFloat("_Metallic"))material.SetFloat("_Metallic",metal);
                foreach(var property in new[]{"_Glossiness","_Smoothness","_GlossMapScale"})if(material.HasFloat(property))material.SetFloat(property,gloss);
                foreach(var property in new[]{"_MainTex","_BaseMap","_Albedo"})if(material.HasTexture(property))material.SetTexture(property,surface.vinyl&&vinyls.Texture!=null?vinyls.Texture:vinyl==0?surface.texture:surface.vinyl?Texture2D.whiteTexture:surface.texture);
            }
        }
        lastPaint=paint;lastFinish=finish;lastRim=rim;lastVinyl=vinyl;lastVinylColor=colour;lastTrim=paintTrim;
    }
    public void Dispose() {
        pending?.Dispose();pending=null;
        foreach(var node in owned)if(node!=null)UnityEngine.Object.Destroy(node);owned.Clear();
        foreach(var surface in painted)if(surface.material!=null&&surface.material.HasTexture("_MainTex"))surface.material.SetTexture("_MainTex",surface.texture);
        vinyls.Dispose();
        if(materials!=null)foreach(var m in materials)if(m!=null)UnityEngine.Object.Destroy(m);
        if(textures!=null)foreach(var t in textures)if(t!=null)UnityEngine.Object.Destroy(t);
    }
}
}
