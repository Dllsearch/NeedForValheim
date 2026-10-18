using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeedForValheim {
public sealed class VehicleNeonMount : MonoBehaviour { public ModelNeonSegment[] Segments; }
public sealed class VehicleNeon : IDisposable {
    const int Columns = 7, Rows = 13;
    static readonly string[] TintProperties = { "_Color", "_TintColor", "_BaseColor" };
    readonly Transform car;
    readonly List<Light> lights = new List<Light>();
    readonly RaycastHit[] hits = new RaycastHit[24];
    readonly Vector3[] vertices = new Vector3[Columns * Rows];
    readonly bool[] grounded = new bool[Columns * Rows];
    readonly List<int> triangles = new List<int>();
    readonly int groundMask;
    float mountY=.08f,halfWidth=.7f,halfLength=1.55f,poolX,poolZ,poolWidth=2.7f,poolLength=5.2f;
    GameObject projection;
    Mesh mesh;
    MeshRenderer renderer;
    Material material;
    Texture2D texture;
    Material tubeMaterial;
    readonly List<LineRenderer> tubes=new List<LineRenderer>();
    public float? PreviewGroundY {get;set;}
    readonly Action<string> log;
    bool reportedGround;
    float phase, sampleIn,mountIn;
    readonly List<Vector3[]> authoredSegments=new List<Vector3[]>();
    readonly List<Vector3[]> fallbackSegments=new List<Vector3[]>();
    bool disposed;
    public bool Enabled { get; set; } = true;
    public bool Pulsing { get; set; }
    public Color Color { get; set; } = new Color(.12f, 1, .3f);
    VehicleNeon(Transform car, Action<string> log) {
        this.log = log;
        this.car = car;
        groundMask = LayerMask.GetMask("terrain", "piece", "static_solid", "Default");
        if (groundMask == 0) groundMask = ~0;
    }
    public static VehicleNeon Create(GameObject car, ModelData model, Action<string> log) {
        var result = new VehicleNeon(car.transform, log);
        result.FitUnderbody(model);
        try {
            {
                result.texture = FallbackMask();
                try { result.material = VehicleMaterials.CreateGlowMaterial(result.texture); }
                catch (InvalidOperationException e) { log?.Invoke("Vehicle neon projection unavailable: " + e.Message + "; ground lights remain active"); }
                if (result.material != null) {
                    result.projection = new GameObject("Vehicle_GroundedNeonMask");
                    result.mesh = new Mesh { name = "Vehicle_TerrainNeon" }; result.mesh.MarkDynamic();
                    var uv = new Vector2[Columns * Rows]; var colors = new Color[uv.Length]; var normals = new Vector3[uv.Length];
                    for (int row = 0; row < Rows; row++) for (int col = 0; col < Columns; col++) {
                        int i = row * Columns + col;
                        uv[i] = new Vector2((float)row / (Rows - 1), (float)col / (Columns - 1));
                        colors[i] = UnityEngine.Color.white; normals[i] = Vector3.up;
                    }
                    result.mesh.vertices = result.vertices; result.mesh.uv = uv; result.mesh.colors = colors; result.mesh.normals = normals;
                    result.projection.AddComponent<MeshFilter>().sharedMesh = result.mesh;
                    result.renderer = result.projection.AddComponent<MeshRenderer>(); result.renderer.sharedMaterial = result.material;
                    result.renderer.shadowCastingMode = ShadowCastingMode.Off; result.renderer.receiveShadows = false; result.renderer.enabled = false;
                    result.tubeMaterial=new Material(result.material){name="Vehicle neon tubes",mainTexture=Texture2D.whiteTexture};
                    if(result.tubeMaterial.HasTexture("_EmissionMap"))result.tubeMaterial.SetTexture("_EmissionMap",Texture2D.whiteTexture);
                    float x=result.halfWidth,z=result.halfLength,y=result.mountY;
                    foreach(var endpoints in new[]{new[]{new Vector3(-x,y,-z*.87f),new Vector3(-x,y,z*.87f)},new[]{new Vector3(x,y,-z*.87f),new Vector3(x,y,z*.87f)},new[]{new Vector3(-x*.93f,y,z),new Vector3(x*.93f,y,z)},new[]{new Vector3(-x*.93f,y,-z),new Vector3(x*.93f,y,-z)}}) {
                        result.fallbackSegments.Add(endpoints);
                        var tube=new GameObject("Vehicle neon tube").AddComponent<LineRenderer>();tube.transform.SetParent(car.transform,false);
                        tube.useWorldSpace=false;tube.positionCount=2;tube.SetPositions(endpoints);tube.widthMultiplier=.025f;
                        tube.sharedMaterial=result.tubeMaterial;tube.shadowCastingMode=ShadowCastingMode.Off;tube.receiveShadows=false;tube.enabled=false;result.tubes.Add(tube);
                    }
                }
            }
            foreach (var position in new[] { new Vector3(-result.halfWidth, result.mountY, 0), new Vector3(result.halfWidth, result.mountY, 0), new Vector3(0, result.mountY, result.halfLength), new Vector3(0, result.mountY, -result.halfLength) }) {
                var obj = new GameObject("Vehicle_NeonLight"); obj.transform.SetParent(car.transform, false); obj.transform.localPosition = position;
                obj.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var light = obj.AddComponent<Light>(); result.lights.Add(light);
                light.type = LightType.Spot; light.range = 1.6f; light.spotAngle = 145; light.shadows = LightShadows.None; light.enabled = false;
            }
            log?.Invoke("Vehicle neon: " + "continuous underbody mask" + "; shader=" + (result.material == null ? "unavailable" : result.material.shader.name));
            return result;
        } catch { result.Dispose(); throw; }
    }
    void UpdateMounts() {
        if(tubeMaterial==null)return;
        authoredSegments.Clear();
        foreach(var mount in car.GetComponentsInChildren<VehicleNeonMount>()) {
            if(mount.Segments==null||!mount.gameObject.activeInHierarchy)continue;
            foreach(var segment in mount.Segments) {
                var a=segment.start;var b=segment.end;
                authoredSegments.Add(new[]{car.InverseTransformPoint(mount.transform.TransformPoint(new Vector3(a.x,a.y,a.z))),car.InverseTransformPoint(mount.transform.TransformPoint(new Vector3(b.x,b.y,b.z)))});
            }
        }
        if(authoredSegments.Count==0)authoredSegments.AddRange(fallbackSegments);
        while(tubes.Count<authoredSegments.Count) {
            var tube=new GameObject("Vehicle source neon tube").AddComponent<LineRenderer>();tube.transform.SetParent(car,false);
            tube.useWorldSpace=false;tube.positionCount=2;tube.widthMultiplier=.025f;tube.sharedMaterial=tubeMaterial;
            tube.shadowCastingMode=ShadowCastingMode.Off;tube.receiveShadows=false;tubes.Add(tube);
        }
        for(int i=0;i<tubes.Count;i++) {
            tubes[i].gameObject.SetActive(i<authoredSegments.Count);
            if(i<authoredSegments.Count)tubes[i].SetPositions(authoredSegments[i]);
        }
        // The light pool must follow the same fitted/source footprint as the
        // visible tubes, including asymmetric bumpers and selected widebody.
        float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minZ=float.PositiveInfinity,maxZ=float.NegativeInfinity;
        foreach(var segment in authoredSegments)foreach(var point in segment) {
            minX=Mathf.Min(minX,point.x);maxX=Mathf.Max(maxX,point.x);
            minZ=Mathf.Min(minZ,point.z);maxZ=Mathf.Max(maxZ,point.z);
        }
        if(authoredSegments.Count==0)return;
        poolX=(minX+maxX)*.5f;poolZ=(minZ+maxZ)*.5f;
        poolWidth=Mathf.Max(.5f,maxX-minX+.7f);poolLength=Mathf.Max(1,maxZ-minZ+.7f);
        var centres=new Vector3[4];var weights=new float[4];
        foreach(var segment in authoredSegments) {
            var midpoint=(segment[0]+segment[1])*.5f;
            bool side=Mathf.Abs(midpoint.x-poolX)/Mathf.Max(.1f,maxX-minX)>Mathf.Abs(midpoint.z-poolZ)/Mathf.Max(.1f,maxZ-minZ);
            int index=side?(midpoint.x<poolX?0:1):(midpoint.z>poolZ?2:3);
            float weight=Mathf.Max(.01f,Vector3.Distance(segment[0],segment[1]));
            centres[index]+=midpoint*weight;weights[index]+=weight;
        }
        for(int i=0;i<lights.Count;i++)if(weights[i]>0)lights[i].transform.localPosition=centres[i]/weights[i];
    }
    void FitUnderbody(ModelData model) {
        // Source body vertices share the chassis coordinate space. The origin
        // is not the floor (especially on MW cars); wheel bounds are unrelated.
        // Ignore optional catalog meshes and movable hood/trunk/spoiler parts.
        float bottom=float.PositiveInfinity,width=0,length=0;
        if(model?.parts!=null)foreach(var part in model.parts) {
            if(part==null||part.group!="body"||!string.IsNullOrEmpty(part.optionGroup)||!string.IsNullOrEmpty(part.movingPart)||part.vertices==null)continue;
            foreach(var v in part.vertices) {
                width=Mathf.Max(width,Mathf.Abs(v.x));length=Mathf.Max(length,Mathf.Abs(v.z));
                bottom=Mathf.Min(bottom,v.y);
            }
        }
        if(float.IsInfinity(bottom)||width<.3f||length<.5f)return;
        mountY=bottom-.015f;
        halfWidth=width*.78f;halfLength=length*.68f;
    }
    public void Tick(bool driving, float deltaTime) {
        if (disposed || car == null) return;
        mountIn-=Mathf.Max(0,deltaTime);
        if(mountIn<=0){mountIn=.15f;UpdateMounts();}
        bool active = Enabled && driving;
        phase = (phase + Mathf.Clamp(deltaTime, 0, .2f) * 2.5f) % (Mathf.PI * 2);
        float intensity = Pulsing ? .25f + .75f * (.5f + .5f * Mathf.Sin(phase)) : 1;
        foreach (var light in lights) { light.enabled = active; light.color = Color; light.intensity = intensity * 1.3f; }
        foreach(var tube in tubes){tube.enabled=active;tube.startColor=tube.endColor=new Color(Color.r*intensity,Color.g*intensity,Color.b*intensity,1);}
        if (renderer == null) return;
        projection.layer=car.gameObject.layer;
        renderer.enabled = active;
        if (!active) { sampleIn = 0; return; }
        var tint = new Color(Color.r * intensity, Color.g * intensity, Color.b * intensity, .8f);
        foreach (var property in TintProperties) if (material.HasColor(property)) material.SetColor(property, tint);
        if(material.HasColor("_EmissionColor"))material.SetColor("_EmissionColor",new Color(tint.r,tint.g,tint.b,1));
        if(tubeMaterial!=null && tubeMaterial.HasColor("_EmissionColor"))tubeMaterial.SetColor("_EmissionColor",new Color(tint.r,tint.g,tint.b,1));
        sampleIn -= Mathf.Max(0, deltaTime);
        if (sampleIn > 0) return;
        sampleIn = .06f;
        UpdateGround();
    }
    void UpdateGround() {
        // Independent world transform keeps the pool on the sampled ground
        // between updates, instead of making the terrain rotate with the car.
        projection.transform.position = car.position;
        for (int row = 0; row < Rows; row++) for (int col = 0; col < Columns; col++) {
            int index = row * Columns + col;
            var point = car.TransformPoint(new Vector3(poolX+((float)col / (Columns - 1) - .5f) * poolWidth, 0, poolZ+((float)row / (Rows - 1) - .5f) * poolLength));
            var origin = point + Vector3.up * .9f;
            if(PreviewGroundY.HasValue){grounded[index]=true;vertices[index]=new Vector3(point.x,PreviewGroundY.Value+.025f,point.z)-car.position;continue;}
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, hits, 2.4f, groundMask, QueryTriggerInteraction.Ignore);
            float distance = float.MaxValue; int best = -1;
            for (int j = 0; j < count; j++) {
                var hit = hits[j];
                if (hit.collider == null || hit.collider.transform.IsChildOf(car) || Vector3.Dot(hit.normal, Vector3.up) < .35f || hit.distance >= distance) continue;
                best = j; distance = hit.distance;
            }
            grounded[index] = best >= 0;
            vertices[index] = best >= 0 ? hits[best].point + hits[best].normal * .025f - car.position : point - car.position;
        }
        triangles.Clear();
        for (int row = 0; row < Rows - 1; row++) for (int col = 0; col < Columns - 1; col++) {
            int a = row * Columns + col, b = a + Columns;
            AddTriangle(a, b, a + 1); AddTriangle(a + 1, b, b + 1);
        }
        mesh.vertices = vertices; mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
        if (!reportedGround) {
            reportedGround = true;
            log?.Invoke("Vehicle neon projection: " + triangles.Count / 3 + " ground triangles; layer=" + projection.layer + "; preview=" + PreviewGroundY.HasValue);
        }
    }
    static Texture2D FallbackMask() {
        // MW models do not necessarily provide a neon atlas. Use a continuous
        // underbody pool instead of silently degrading to four spot lights.
        var mask=new Texture2D(128,64,TextureFormat.RGBA32,false){name="NFVU2 neon fallback",wrapMode=TextureWrapMode.Clamp};
        var pixels=new Color[128*64];
        for(int y=0;y<64;y++)for(int x=0;x<128;x++) {
            float u=Mathf.Abs((x+.5f)/128-.5f),v=Mathf.Abs((y+.5f)/64-.5f);
            float d=Mathf.Min(Mathf.Sqrt(Mathf.Pow(u-.34f,2)+Mathf.Pow(Mathf.Max(0,v-.26f),2)),Mathf.Sqrt(Mathf.Pow(v-.26f,2)+Mathf.Pow(Mathf.Max(0,u-.34f),2)));
            float glow=Mathf.Exp(-d*d/ .009f);pixels[y*128+x]=new Color(glow,glow,glow,glow);
        }
        mask.SetPixels(pixels);mask.Apply(false,true);return mask;
    }
    void AddTriangle(int a, int b, int c) {
        if (!grounded[a] || !grounded[b] || !grounded[c]) return;
        // Do not stretch a glowing sheet across steps, cliffs, or missing hits.
        if (Mathf.Abs(vertices[a].y - vertices[b].y) > .65f || Mathf.Abs(vertices[a].y - vertices[c].y) > .65f || Mathf.Abs(vertices[b].y - vertices[c].y) > .65f) return;
        triangles.Add(a); triangles.Add(b); triangles.Add(c);
    }
    public void Dispose() {
        if (disposed) return; disposed = true;
        foreach (var light in lights) if (light != null) UnityEngine.Object.Destroy(light.gameObject);
        foreach(var tube in tubes)if(tube!=null)UnityEngine.Object.Destroy(tube.gameObject);
        tubes.Clear();
        if (projection != null) UnityEngine.Object.Destroy(projection);
        if (mesh != null) UnityEngine.Object.Destroy(mesh);
        if (material != null) UnityEngine.Object.Destroy(material);
        if(tubeMaterial!=null)UnityEngine.Object.Destroy(tubeMaterial);
        if (texture != null) UnityEngine.Object.Destroy(texture);
        lights.Clear();
    }
}
}
