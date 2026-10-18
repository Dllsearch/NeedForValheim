using System;
using NeedForValheim;
using UnityEngine;
namespace UnityEngine {
    public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
    public class Transform {public Vector3 position;}
    public class MonoBehaviour {public bool enabled=true;public Transform transform=new Transform();}
}
public class ZNet {
    public static ZNet instance;
    public Vector3 Reference;public int Calls;
    public void SetReferencePosition(Vector3 point){Reference=point;Calls++;}
}
class StreamingTests {
    static void Check(bool value,string label){if(!value)throw new Exception(label);Console.WriteLine("PASS "+label);}
    static void Main() {
        Check(VehicleWorldStreaming.KeepObserver(159,false,true),"nearby native anchor permits initial observer load");
        Check(!VehicleWorldStreaming.KeepObserver(161,false,true),"new observer waits outside the inner radius");
        Check(VehicleWorldStreaming.KeepObserver(170,true,true),"existing observer survives the hysteresis band");
        Check(VehicleWorldStreaming.KeepObserver(170,true,false),"temporary native anchor unloading does not flicker an existing visual");
        Check(!VehicleWorldStreaming.KeepObserver(181,true,true),"distant visual releases outside the outer radius");
        Check(!VehicleWorldStreaming.KeepObserver(150,false,false),"new visual waits for the native anchor");
        Check(!VehicleWorldStreaming.KeepObserver(float.NaN,true,true),"invalid distance cannot retain a visual");
        var driver=new MonoBehaviour{enabled=false};var net=new ZNet();ZNet.instance=net;
        for(int i=0;i<1000;i++) {
            driver.transform.position=new Vector3(i*4,1+i*.1f,-i*3);
            VehicleWorldStreaming.Follow(driver);
            if(net.Reference.x!=driver.transform.position.x||net.Reference.y!=driver.transform.position.y||net.Reference.z!=driver.transform.position.z)throw new Exception("Zone centre stopped following the disabled driver");
        }
        Check(net.Calls==1000,"disabled seated driver publishes every frame across many zones");
        driver.transform.position=new Vector3(12,3,14);VehicleWorldStreaming.Follow(driver);
        Check(net.Reference.x==12&&net.Reference.z==14,"exit position restores the native reference immediately");
        int calls=net.Calls;VehicleWorldStreaming.Follow(null);Check(net.Calls==calls,"absent driver does not overwrite world position");
        ZNet.instance=null;VehicleWorldStreaming.Follow(driver);Check(true,"world teardown without ZNet is safe");
        Check(!VehicleParkingRules.Freeze(true,false,300,100,30),"occupied car remains dynamic across zone boundaries");
        Check(!VehicleParkingRules.Freeze(false,true,300,100,30),"flying car remains under flight control");
        Check(VehicleParkingRules.Freeze(false,false,81,0,0),"remote parked car freezes before terrain leaves the loaded area");
        Check(!VehicleParkingRules.Freeze(false,false,20,0,.5f),"short airborne motion is allowed near the player");
        Check(VehicleParkingRules.Freeze(false,false,20,5,0),"falling unoccupied car recovers its last supported pose");
        Check(VehicleParkingRules.Freeze(false,false,20,0,2),"missing terrain holds parked car after a grace period");
        Check(!VehicleParkingRules.Thaw(false,40,false),"returning player does not release parking before terrain reloads");
        Check(!VehicleParkingRules.Thaw(false,75,true),"parking thresholds have hysteresis to avoid repeated freeze/thaw");
        Check(VehicleParkingRules.Thaw(false,40,true),"parking releases after approaching loaded terrain");
        Check(VehicleParkingRules.Thaw(true,0,true),"entering the parked car restores dynamic driving");
        Console.WriteLine("14 streaming/parking checks passed. Native terrain generation requires an in-game drive.");
    }
}
