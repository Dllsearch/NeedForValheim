using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace NeedForValheim {
// Native projection geometry stays separate from the four body-vinyl layers.
public sealed class VehicleDecals:IDisposable {
    readonly GameObject car;readonly ModelData data;readonly DecalArtworkCatalog artwork;
    readonly Func<ModelPart,Transform,Material,Transform> create;
    readonly Dictionary<int,Material> materials=new Dictionary<int,Material>();readonly Dictionary<int,Texture2D> textures=new Dictionary<int,Texture2D>();
    readonly Dictionary<string,List<GameObject>> built=new Dictionary<string,List<GameObject>>();
    readonly Dictionary<string,int> last=new Dictionary<string,int>();int lastKit=-2;
    Material[] templates;Texture2D[] unused;
    public ModelDecalSlot[] Slots=>data.decalSlots;
    public DecalArtwork[] Artwork=>artwork.entries;
    public VehicleDecals(GameObject root,ModelData geometry,DecalArtworkCatalog logos,Func<ModelPart,Transform,Material,Transform> factory){car=root;data=geometry;artwork=logos;create=factory;}
    public IEnumerable<int> LoadSteps(MonoBehaviour local,Action<string> log){foreach(var step in VehicleMaterials.CreateForModelSteps(local,data,log,(m,t)=>{templates=m;unused=t;}))yield return step;}
    public Material ArtworkMaterial(int id) {
        if(materials.TryGetValue(id,out var material))return material;
        var entry=artwork.entries.First(e=>e.id==id);var rgba=VinylCatalog.Unpack(entry.data,entry.width*entry.height*4);var pixels=new byte[rgba.Length];
        for(int row=0;row<entry.height;row++)Array.Copy(rgba,row*entry.width*4,pixels,(entry.height-1-row)*entry.width*4,entry.width*4);
        var texture=new Texture2D(entry.width,entry.height,TextureFormat.RGBA32,true){name=entry.name,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=4};textures[id]=texture;
        texture.SetPixelData<byte>(pixels,0);texture.Apply(true,true);
        material=new Material(templates[0]){name="MW_Decal_"+entry.name,renderQueue=3100};materials[id]=material;
        foreach(var property in new[]{"_MainTex","_BaseMap","_Albedo"})if(material.HasTexture(property))material.SetTexture(property,texture);
        return material;
    }
    public void Tick(IReadOnlyDictionary<string,int> selected,int kit) {
        int Choice(string key)=>selected.TryGetValue(key,out var value)?value:0;
        if(lastKit==kit&&Slots.All(slot=>last.TryGetValue(slot.key,out var value)&&value==Choice(slot.key)))return;
        foreach(var nodes in built.Values)foreach(var node in nodes)node.SetActive(false);
        foreach(var slot in Slots) {
            if(!selected.TryGetValue(slot.key,out var id)||id==0)continue;
            var art=artwork.entries.FirstOrDefault(e=>e.id==id&&e.kind==slot.kind);if(art==null)continue;
            var candidates=data.parts.Where(p=>p.option==slot.key).ToArray();
            int geometryKit=candidates.Any(p=>p.sourceKit==kit)?kit:0;
            string key=slot.key+":"+geometryKit;
            if(!built.TryGetValue(key,out var nodes)) {
                nodes=new List<GameObject>();built[key]=nodes;
                foreach(var part in candidates.Where(p=>p.sourceKit<0||p.sourceKit==geometryKit))nodes.Add(create(part,car.transform,ArtworkMaterial(id)).gameObject);
            }
            foreach(var node in nodes){node.GetComponent<Renderer>().sharedMaterial=ArtworkMaterial(id);node.SetActive(true);}
        }
        lastKit=kit;foreach(var slot in Slots)last[slot.key]=Choice(slot.key);
    }
    public void Dispose(){foreach(var nodes in built.Values)foreach(var node in nodes)if(node!=null)UnityEngine.Object.Destroy(node);foreach(var m in materials.Values)UnityEngine.Object.Destroy(m);foreach(var t in textures.Values)UnityEngine.Object.Destroy(t);if(templates!=null)foreach(var m in templates)UnityEngine.Object.Destroy(m);if(unused!=null)foreach(var t in unused)UnityEngine.Object.Destroy(t);built.Clear();materials.Clear();textures.Clear();}
}
}
