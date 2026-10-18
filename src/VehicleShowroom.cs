using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeedForValheim {
public sealed class VehicleShowroom : IDisposable {
    public readonly ShowroomState State=new ShowroomState();
    public ModelTrunkLayout[] Layouts=>model.trunkLayouts;
    readonly ModelData model;
    readonly GameObject car;
    readonly Func<ModelPart,Transform,Transform> create;
    readonly Transform[] moving=new Transform[4];
    readonly List<GameObject> equipment=new List<GameObject>();
    readonly List<SubwooferVisual>[] woofers=new List<SubwooferVisual>[6];
    readonly List<Renderer> strips=new List<Renderer>();
    readonly List<Material> ownedMaterials=new List<Material>();
    readonly Dictionary<string,ModelAnimation> animations;
    readonly Dictionary<string,ModelAudioComponent[]> choices;
    readonly Dictionary<ModelPart,GameObject> variants=new Dictionary<ModelPart,GameObject>();
    readonly int[] selected={-1,-1,-1,-1,-1,-1};
    readonly Transform equipmentRoot;
    readonly Light trunkLight;
    int layout=-1,engine=-1;
    float phase;
    public VehicleShowroom(GameObject car,ModelData model,Func<ModelPart,Transform,Transform> create,Material template) {
        this.car=car;this.model=model;this.create=create;
        animations=model.animations.ToDictionary(a=>a.name);
        choices=model.audioComponents.GroupBy(c=>c.size).ToDictionary(g=>g.Key,g=>g.ToArray());
        try {
        string[] roles={"leftDoor","rightDoor","hood","trunk"};
        for(int i=0;i<4;i++) {
            var root=new GameObject("Vehicle_Motion_"+roles[i]);root.transform.SetParent(car.transform,false);moving[i]=root.transform;
        }
        foreach(var part in model.parts.Where(p=>p.group=="body")) {
            var node=car.transform.Find(part.name);if(node==null)continue;
            int index=Array.IndexOf(roles,part.movingPart);
            if(index>=0)node.SetParent(moving[index],false);
            if(part.optionGroup=="trunk"||part.optionGroup=="engine")variants.Add(part,node.gameObject);
        }
        var audioRoot=new GameObject("Vehicle_TrunkEquipment");audioRoot.transform.SetParent(car.transform,false);equipmentRoot=audioRoot.transform;
        var neon=new Material(template){name="Vehicle_TrunkNeon"};ownedMaterials.Add(neon);
        foreach(var property in new[]{"_MainTex","_BaseMap","_Albedo"})if(neon.HasTexture(property))neon.SetTexture(property,Texture2D.whiteTexture);
        var floor=model.parts.Where(p=>p.optionGroup=="trunk"&&p.option=="2").SelectMany(p=>p.indices.Select(i=>p.vertices[i])).ToArray();
        float halfWidth=.59f,height=.68f,centerZ=-1.43f,depth=.72f;
        if(model.schemaVersion>=3&&floor.Length>0) {
            halfWidth=Mathf.Max(.1f,(floor.Max(p=>p.x)-floor.Min(p=>p.x))*.42f);
            height=floor.Max(p=>p.y)+.025f;
            centerZ=(floor.Max(p=>p.z)+floor.Min(p=>p.z))*.5f;
            depth=Mathf.Max(.1f,(floor.Max(p=>p.z)-floor.Min(p=>p.z))*.8f);
        }
        for(int i=0;i<3;i++) {
            var strip=GameObject.CreatePrimitive(PrimitiveType.Cube);UnityEngine.Object.Destroy(strip.GetComponent<Collider>());
            strip.name="Vehicle_TrunkNeonStrip";strip.transform.SetParent(equipmentRoot,false);
            strip.transform.localPosition=model.schemaVersion<3?(i<2?new Vector3(i==0?-.59f:.59f,.68f,-1.43f):new Vector3(0,.57f,-1.89f)):(i<2?new Vector3(i==0?-halfWidth:halfWidth,height,centerZ):new Vector3(0,height,centerZ-depth*.5f));
            strip.transform.localScale=i<2?new Vector3(.012f,.012f,depth):new Vector3(model.schemaVersion<3?1.03f:halfWidth*2,.012f,.012f);
            var renderer=strip.GetComponent<Renderer>();renderer.sharedMaterial=neon;strips.Add(renderer);
        }
        var lamp=new GameObject("Vehicle_TrunkGlow");lamp.transform.SetParent(equipmentRoot,false);lamp.transform.localPosition=model.schemaVersion<3?new Vector3(0,.85f,-1.35f):new Vector3(0,height+.15f,centerZ);
        trunkLight=lamp.AddComponent<Light>();trunkLight.type=LightType.Point;trunkLight.range=1.2f;trunkLight.shadows=LightShadows.None;trunkLight.enabled=false;
        }catch {Dispose();throw;}
    }
    public ModelAudioComponent[] Choices(int layoutIndex,int slot) {
        var sockets=Layouts[Mathf.Clamp(layoutIndex,0,Layouts.Length-1)].sockets;
        if(slot<0||slot>=sockets.Length)return new ModelAudioComponent[0];
        return choices[sockets[slot].size];
    }
    public void Configure(int trunkLayout,int engineStyle,int[] options) {
        trunkLayout=Mathf.Clamp(trunkLayout,0,Layouts.Length-1);engineStyle=Mathf.Clamp(engineStyle,0,2);
        bool newLayout=layout!=trunkLayout;
        if(newLayout||engine!=engineStyle) {
            layout=trunkLayout;engine=engineStyle;
            foreach(var entry in variants)entry.Value.SetActive(entry.Key.option==(entry.Key.optionGroup=="trunk"?layout:engine).ToString());
        }
        var sockets=Layouts[layout].sockets;
        for(int i=0;i<6;i++) {
            int choice=i<sockets.Length?Mathf.Clamp(options[i],0,Choices(layout,i).Length-1):-1;
            if(!newLayout&&selected[i]==choice)continue;
            selected[i]=choice;
            if(woofers[i]!=null){foreach(var woofer in woofers[i])woofer.Dispose();woofers[i]=null;}
            while(equipment.Count<=i)equipment.Add(null);
            if(equipment[i]!=null){equipment[i].SetActive(false);UnityEngine.Object.Destroy(equipment[i]);equipment[i]=null;}
            if(choice<0)continue;
            var component=Choices(layout,i)[choice];var socket=sockets[i];
            var root=new GameObject("Vehicle_AudioSlot"+i);root.transform.SetParent(equipmentRoot,false);
            root.transform.localPosition=V(socket.position);root.transform.localRotation=Quaternion.LookRotation(V(socket.forward),V(socket.up));equipment[i]=root;
            var parts=model.parts.Where(p=>p.group=="audio"&&p.option==component.id).ToArray();
            float radius=component.kind=="speaker"?parts.SelectMany(p=>p.vertices).Max(v=>Mathf.Max(Mathf.Abs(v.x),Mathf.Abs(v.y))):0;
            if(radius>0)woofers[i]=new List<SubwooferVisual>();
            foreach(var part in parts) {
                var node=create(part,root.transform);
                if(radius>0)woofers[i].Add(new SubwooferVisual(node,part,radius));
            }
        }
    }
    public void Tick(float dt,bool movingCar,int doorStyle,bool powered,bool neonEnabled,bool pulsing,Color color) {
        if(!SupportsDoorStyle(doorStyle))doorStyle=0;
        State.Step(dt,movingCar,doorStyle);
        string style=new[]{"REG","SUICIDE","SCISSOR"}[State.DoorStyle];
        string[] tracks={"LEFT_DOOR_"+style,"RIGHT_DOOR_"+style,"HOOD_REG","TRUNK_REG"};
        for(int i=0;i<4;i++) {
            if(!animations.TryGetValue(tracks[i],out var track)||track.frames.Length<2)continue;
            float position=State.Progress[i]*(track.frames.Length-1);
            int a=Mathf.FloorToInt(position),b=Mathf.Min(a+1,track.frames.Length-1);float t=position-a;
            var rotation=Quaternion.Slerp(Q(track.frames[a].rotation),Q(track.frames[b].rotation),t);
            var offset=Vector3.Lerp(V(track.frames[a].offset),V(track.frames[b].offset),t);var pivot=V(track.pivot);
            moving[i].localRotation=rotation;moving[i].localPosition=pivot+offset-rotation*pivot;
        }
        phase=(phase+Mathf.Max(0,dt)*2.8f)%(Mathf.PI*2);
        bool active=neonEnabled&&(powered||State.Progress[3]>.01f);
        float gain=active?(pulsing?.3f+.7f*(.5f+.5f*Mathf.Sin(phase)):1):0;
        foreach(var strip in strips)strip.enabled=active;
        var material=ownedMaterials[0];
        foreach(var property in new[]{"_Color","_BaseColor"})if(material.HasColor(property))material.SetColor(property,color);
        VehicleMaterials.SetEmission(material,color*gain*2);
        trunkLight.enabled=active&&State.Progress[3]>.01f;trunkLight.color=color;trunkLight.intensity=gain*State.Progress[3]*1.4f;
    }
    static Vector3 V(ModelVertex v)=>new Vector3(v.x,v.y,v.z);
    public bool SupportsMotion(int part) {
        if(part<0||part>=4||moving[part]==null||moving[part].childCount==0)return false;
        string name=new[]{"LEFT_DOOR_REG","RIGHT_DOOR_REG","HOOD_REG","TRUNK_REG"}[part];
        return animations.TryGetValue(name,out var track)&&track.frames.Length>1;
    }
    public bool SupportsDoorStyle(int style) {
        if(style<0||style>2)return false;
        string suffix=new[]{"REG","SUICIDE","SCISSOR"}[style];
        return animations.ContainsKey("LEFT_DOOR_"+suffix)&&animations.ContainsKey("RIGHT_DOOR_"+suffix);
    }
    public void TickSubwoofers(float offset) {
        foreach(var slot in woofers)if(slot!=null)foreach(var woofer in slot)woofer.Tick(offset);
    }
    static Quaternion Q(ModelRotation q)=>new Quaternion(q.x,q.y,q.z,q.w);
    public void Dispose() {
        foreach(var slot in woofers)if(slot!=null)foreach(var woofer in slot)woofer.Dispose();
        // Meshes/materials of imported parts belong to Plugin, as before.
        foreach(var root in moving)if(root!=null){root.localPosition=Vector3.zero;root.localRotation=Quaternion.identity;}
        foreach(var material in ownedMaterials)if(material!=null)UnityEngine.Object.Destroy(material);
        ownedMaterials.Clear();
        if(equipmentRoot!=null)UnityEngine.Object.Destroy(equipmentRoot.gameObject);
    }
}
}
