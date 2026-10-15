using System;
using System.Collections.Generic;
using UnityEngine;
using NeedForValheim;
class Program {
    static int checks;
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static KeyValuePair<GameObject,string> Entry(GameObject car,string name)=>new KeyValuePair<GameObject,string>(car,name);
    static void Main(){
        var map=Minimap.instance=new Minimap();
        var a=new GameObject();a.transform.position=new Vector3(1,2,3);
        var b=new GameObject();b.transform.position=new Vector3(10,20,30);
        var markers=new VehicleMapMarkers();
        markers.Tick(new[]{Entry(a,"BMW M3"),Entry(b,"Nissan 350Z")},true);
        Check(map.Pins.Count==2,"every owned vehicle");
        Check(map.Pins.TrueForAll(p=>!p.save),"temporary pins never saved");
        Check(map.Pins[0].name=="BMW M3"&&map.Pins[1].name=="Nissan 350Z","model labels");
        var pin=map.Pins[0];var icon=pin.m_icon;
        Check(icon!=null,"distinct car icon");
        Check(pin.m_pos.x==1&&pin.m_pos.z==3,"initial position");
        a.transform.position=new Vector3(40,0,60);
        markers.Tick(new[]{Entry(a,"BMW M3"),Entry(b,"Nissan 350Z")},true);
        Check(map.Pins.Count==2&&ReferenceEquals(pin,map.Pins[0]),"movement does not duplicate pins");
        Check(pin.m_pos.x==40&&pin.m_pos.z==60,"pin follows moving car");
        markers.Remove(a);
        Check(map.Pins.Count==1&&map.Pins[0].name=="Nissan 350Z","despawn removes exact pin");
        markers.Tick(Array.Empty<KeyValuePair<GameObject,string>>(),true);
        Check(map.Pins.Count==0,"world cleanup removes stale pins");
        markers.Tick(new[]{Entry(b,"Nissan 350Z")},true);
        var next=Minimap.instance=new Minimap();
        markers.Tick(new[]{Entry(b,"Nissan 350Z")},true);
        Check(map.Pins.Count==0&&next.Pins.Count==1,"map scene replacement");
        markers.Tick(new[]{Entry(b,"Nissan 350Z")},false);
        Check(next.Pins.Count==0,"option off removes pins");
        markers.Tick(new[]{Entry(b,"Nissan 350Z")},true);
        Check(next.Pins.Count==1,"option on restores pins");
        markers.Dispose();
        Check(next.Pins.Count==0,"plugin shutdown cleanup");
        Check(icon.destroyed,"owned sprite cleanup");
        markers.Dispose();
        Check(next.Pins.Count==0,"idempotent cleanup");
        Minimap.instance=null;markers=new VehicleMapMarkers();
        markers.Tick(new[]{Entry(a,"BMW M3")},true);markers.Dispose();
        Check(true,"safe before map exists");
        Console.WriteLine("Vehicle map lifecycle: "+checks+" checks passed (native API adapter stub).");
    }
}
