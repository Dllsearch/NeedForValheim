using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace NeedForValheim {
// Native pins are temporary, never written to world/map saves or shared tables.
public sealed class VehicleMapMarkers : IDisposable {
    readonly Dictionary<GameObject,Minimap.PinData> pins=new Dictionary<GameObject,Minimap.PinData>();
    Minimap map;Texture2D texture;Sprite icon;
    void CreateIcon() {
        const int size=32;texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="NFVU2_CarMapIcon",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
            bool body=x>=3&&x<=28&&y>=11&&y<=18,roof=y>=19&&y<=23&&x>=8+(y-19)/2&&x<=24-(y-19)/2;
            bool wheel=(x-9)*(x-9)+(y-10)*(y-10)<=13||(x-24)*(x-24)+(y-10)*(y-10)<=13;
            bool window=y>=19&&y<=21&&x>=12&&x<=21;
            pixels[y*size+x]=window?new Color(.06f,.12f,.15f,1):body||roof?new Color(.25f,.95f,1,1):wheel?new Color(.05f,.08f,.1f,1):Color.clear;
        }
        texture.SetPixels(pixels);texture.Apply(false,true);
        icon=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);icon.name="NFVU2_CarMapIcon";
    }
    public void Tick(IEnumerable<KeyValuePair<GameObject,string>> vehicles,bool enabled) {
        var next=enabled?Minimap.instance:null;
        if(map!=next){Clear();map=next;}
        if(map==null)return;
        var present=new HashSet<GameObject>();
        foreach(var entry in vehicles) {
            var car=entry.Key;if(car==null)continue;present.Add(car);
            if(!pins.TryGetValue(car,out var pin)) {
                if(icon==null)CreateIcon();
                // Player is the native, always-visible dynamic pin category; our sprite/name remain distinct.
                pin=map.AddPin(car.transform.position,Minimap.PinType.Player,entry.Value,false,false);
                pin.m_icon=icon;pins.Add(car,pin);
            }
            pin.m_pos=car.transform.position;
        }
        foreach(var car in pins.Keys.Where(c=>c==null||!present.Contains(c)).ToArray())Remove(car);
    }
    public void Remove(GameObject car) {
        if(ReferenceEquals(car,null)||!pins.TryGetValue(car,out var pin))return;
        if(map!=null)map.RemovePin(pin);pins.Remove(car);
    }
    void Clear(){if(map!=null)foreach(var pin in pins.Values)map.RemovePin(pin);pins.Clear();}
    public void Dispose(){Clear();map=null;if(icon!=null)UnityEngine.Object.Destroy(icon);if(texture!=null)UnityEngine.Object.Destroy(texture);icon=null;texture=null;}
}
}
