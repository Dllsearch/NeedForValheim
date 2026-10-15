using System;
using System.Linq;
using UnityEngine;
namespace NeedForValheim {
internal sealed partial class VehicleInterior {
    Transform controlRoot,accelerator,brakePedal,clutch,gearLever,handLever;
    Material controlMaterial;
    public Vector3 ShiftGrip=>controlRoot.TransformPoint(new Vector3(.23f,.17f,.28f));
    public Vector3 HandbrakeGrip=>handLever.TransformPoint(new Vector3(0,.5f,0));
    public Vector3 RightFoot=>accelerator.TransformPoint(new Vector3(0,0,-.02f));
    public Vector3 LeftFoot=>clutch.TransformPoint(new Vector3(0,0,-.02f));
    public Vector3 BrakeFoot=>brakePedal.TransformPoint(new Vector3(0,0,-.02f));
    // Native imported exterior/cockpit is retained. These small local controls
    // supply moving targets when a car has no separately rigged interior.
    public static VehicleInterior CreateFallback(GameObject car,CarProfile profile,ModelData model) {
        var result=new VehicleInterior();
        try {
            result.fit=CabinFit.FromModel(profile,model);result.fittedControls=true;
            var wheel=new GameObject("NFVU2_FittedSteering");result.objects.Add(wheel);
            result.steering=wheel.transform;result.steering.SetParent(car.transform,false);
            result.steering.localPosition=result.SeatPosition+new Vector3(0,.38f,.55f);
            result.CreateControls(car);
            var mesh=result.SteeringMesh();result.meshes.Add(mesh);
            wheel.AddComponent<MeshFilter>().sharedMesh=mesh;
            wheel.AddComponent<MeshRenderer>().sharedMaterial=result.controlMaterial;
            result.SetupFallbackGlass(car,model);
            return result;
        } catch {result.Dispose();throw;}
    }
    public void CreateControls(GameObject car) {
        if(controlRoot!=null)return;
        var root=new GameObject("NFVU2_DriverControls");objects.Add(root);controlRoot=root.transform;
        controlRoot.SetParent(car.transform,false);controlRoot.localPosition=SeatPosition;
        var reference=car.GetComponentsInChildren<MeshRenderer>(true).Select(r=>r.sharedMaterial).FirstOrDefault(m=>m!=null&&m.shader!=null&&m.shader.isSupported);
        if(reference==null)throw new InvalidOperationException("Cabin control material unavailable");
        controlMaterial=new Material(reference){name="NFVU2_CabinControls"};
        foreach(var name in new[]{"_MainTex","_BaseMap"})if(controlMaterial.HasTexture(name))controlMaterial.SetTexture(name,Texture2D.whiteTexture);
        foreach(var name in new[]{"_Color","_BaseColor"})if(controlMaterial.HasColor(name))controlMaterial.SetColor(name,new Color(.045f,.05f,.055f,1));
        controlMaterial.DisableKeyword("_EMISSION");
        foreach(var name in new[]{"_EmissionColor","_EmissiveColor"})if(controlMaterial.HasColor(name))controlMaterial.SetColor(name,Color.black);
        if(controlMaterial.HasFloat("_Glossiness"))controlMaterial.SetFloat("_Glossiness",.32f);
        accelerator=Control("Accelerator",new Vector3(.09f,-.16f,.70f),new Vector3(.065f,.12f,.025f));
        brakePedal=Control("Brake",new Vector3(-.015f,-.16f,.70f),new Vector3(.085f,.08f,.025f));
        clutch=Control("Clutch",new Vector3(-.12f,-.16f,.70f),new Vector3(.075f,.08f,.025f));
        gearLever=Control("Gear",new Vector3(.23f,.07f,.28f),new Vector3(.03f,.16f,.03f));
        handLever=Control("Handbrake",new Vector3(.24f,-.02f,.05f),new Vector3(.03f,.16f,.03f));
    }
    Transform Control(string name,Vector3 position,Vector3 size) {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);objects.Add(obj);obj.name="NFVU2_"+name;
        var collider=obj.GetComponent<Collider>();collider.enabled=false;UnityEngine.Object.Destroy(collider);
        obj.transform.SetParent(controlRoot,false);obj.transform.localPosition=position;obj.transform.localScale=size;
        obj.GetComponent<Renderer>().sharedMaterial=controlMaterial;return obj.transform;
    }
    public void ApplyControls(CabinControlPose pose) {
        if(controlRoot==null)return;
        accelerator.localRotation=Quaternion.Euler(pose.Accelerator*24,0,0);
        brakePedal.localRotation=Quaternion.Euler(pose.Brake*24,0,0);
        clutch.localRotation=Quaternion.Euler(pose.Shift*24,0,0);
        // Gear pattern is visual; engine audio owns the real automatic shift.
        gearLever.localRotation=Quaternion.Euler(pose.Gear%2==0?12:-12,0,pose.Gear<0?-12:pose.Gear%3*8-8);
        handLever.localRotation=Quaternion.Euler(-pose.Handbrake*38,0,0);
    }
    Mesh SteeringMesh() {
        const int segments=48,sides=6;
        var vertices=new Vector3[segments*sides];var indices=new int[segments*sides*6];
        var rotation=Quaternion.FromToRotation(Vector3.forward,WheelAxis);
        for(int i=0;i<segments;i++)for(int j=0;j<sides;j++) {
            float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides,r=.18f+.014f*Mathf.Cos(b);
            vertices[i*sides+j]=rotation*new Vector3(r*Mathf.Cos(a),r*Mathf.Sin(a),.014f*Mathf.Sin(b));
            int k=(i*sides+j)*6,next=(i+1)%segments,side=(j+1)%sides;
            indices[k]=i*sides+j;indices[k+1]=next*sides+j;indices[k+2]=i*sides+side;
            indices[k+3]=i*sides+side;indices[k+4]=next*sides+j;indices[k+5]=next*sides+side;
        }
        var mesh=new Mesh{name="NFVU2_FittedSteeringRing"};mesh.vertices=vertices;mesh.triangles=indices;
        mesh.uv=new Vector2[vertices.Length];mesh.colors=Enumerable.Repeat(Color.white,vertices.Length).ToArray();
        mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    void SetupFallbackGlass(GameObject car,ModelData model) {
        // MW already has its own transparent glazing/tint implementation.
        // U2's original windows use this known shared texture; opaque paint stays intact.
        var names=model.materials.Where(m=>CabinFit.IsGlazing(model,m)&&!(m.name??"").StartsWith("mw_window_",StringComparison.OrdinalIgnoreCase)).Select(m=>m.name).ToArray();
        var renderers=car.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.sharedMaterial!=null&&names.Any(n=>r.sharedMaterial.name.Contains(n))).ToArray();
        if(renderers.Length==0)return;
        var shader=Shader.Find("Standard");if(shader==null||!shader.isSupported)return;
        glass=new Material(shader){name="NFVU2_FittedCabinGlass",renderQueue=3000};
        glass.SetOverrideTag("RenderType","Transparent");glass.SetFloat("_Mode",2);glass.SetFloat("_SrcBlend",5);glass.SetFloat("_DstBlend",10);glass.SetFloat("_ZWrite",0);
        glass.EnableKeyword("_ALPHABLEND_ON");glass.SetColor("_Color",new Color(.12f,.16f,.18f,.18f));glass.SetFloat("_Glossiness",.8f);
        foreach(var r in renderers){glassOriginals.Add(Tuple.Create<Renderer,Material>(r,r.sharedMaterial));r.sharedMaterial=glass;}
    }
    void DisposeControls(){if(controlMaterial!=null)UnityEngine.Object.Destroy(controlMaterial);controlMaterial=null;}
}
}
