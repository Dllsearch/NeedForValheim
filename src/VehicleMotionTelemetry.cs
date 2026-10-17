using System;
using UnityEngine;
namespace NeedForValheim {
// Presentation and fuel accompany each accepted motion sample. Native wheel
// poses are expressed in the car frame before visual camber/mirroring/folding.
internal sealed class VehicleMotionTelemetry {
    public const string Prefix="nfvu2_motion_";
    public float Rpm,Load,Throttle,Charge=1,Fold,Pitch,Roll,Thrust,Contact,Slip;
    public int Gear=1,GroundMode;
    public bool Braking,Horn,Burnout,Purge,Backfire,Flying,Running,Shifting,Landing;
    public readonly float[] WheelRpm=new float[4];
    public readonly Vector3[] Wheels=new Vector3[4];
    public readonly Quaternion[] Rotations={Quaternion.identity,Quaternion.identity,Quaternion.identity,Quaternion.identity};
    public void Write(ZPackage p) {
        p.Write(Rpm);p.Write(Load);p.Write(Throttle);p.Write(Charge);p.Write(Gear);
        p.Write(Braking);p.Write(Horn);p.Write(Burnout);p.Write(Purge);p.Write(Backfire);p.Write(Flying);p.Write(Running);p.Write(Shifting);p.Write(Landing);p.Write(GroundMode);
        p.Write(Fold);p.Write(Pitch);p.Write(Roll);p.Write(Thrust);p.Write(Contact);p.Write(Slip);
        for(int i=0;i<4;i++){p.Write(Wheels[i]);p.Write(Rotations[i]);p.Write(WheelRpm[i]);}
    }
    public static VehicleMotionTelemetry Read(ZPackage p) {
        var t=new VehicleMotionTelemetry{Rpm=p.ReadSingle(),Load=p.ReadSingle(),Throttle=p.ReadSingle(),Charge=p.ReadSingle(),Gear=p.ReadInt(),
            Braking=p.ReadBool(),Horn=p.ReadBool(),Burnout=p.ReadBool(),Purge=p.ReadBool(),Backfire=p.ReadBool(),Flying=p.ReadBool(),Running=p.ReadBool(),Shifting=p.ReadBool(),Landing=p.ReadBool(),GroundMode=p.ReadInt(),
            Fold=p.ReadSingle(),Pitch=p.ReadSingle(),Roll=p.ReadSingle(),Thrust=p.ReadSingle(),Contact=p.ReadSingle(),Slip=p.ReadSingle()};
        for(int i=0;i<4;i++){t.Wheels[i]=p.ReadVector3();t.Rotations[i]=p.ReadQuaternion();t.WheelRpm[i]=p.ReadSingle();}return t;
    }
    static bool Range(float v,float low,float high)=>!float.IsNaN(v)&&!float.IsInfinity(v)&&v>=low&&v<=high;
    public bool Valid() {
        if(GroundMode<0||GroundMode>2||Landing&&!Flying||!Range(Rpm,0,20000)||!Range(Load,0,1)||!Range(Throttle,-1,1)||!Range(Charge,0,1)||Gear< -1||Gear>16||
            !Range(Fold,0,1)||!Range(Pitch,-40,40)||!Range(Roll,-40,40)||!Range(Thrust,0,1)||!Range(Contact,0,1)||!Range(Slip,0,20))return false;
        for(int i=0;i<4;i++) {
            var v=Wheels[i];var q=Rotations[i];float norm=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;
            if(!Range(WheelRpm[i],-20000,20000)||!Range(v.x,-12,12)||!Range(v.y,-12,12)||!Range(v.z,-12,12)||!Range(q.x,-1.1f,1.1f)||!Range(q.y,-1.1f,1.1f)||!Range(q.z,-1.1f,1.1f)||!Range(q.w,-1.1f,1.1f)||!Range(norm,.5f,1.5f))return false;
        }return true;
    }
    public void Store(ZDO d) {
        d.Set(Prefix+"rpm",Rpm);d.Set(Prefix+"load",Load);d.Set(Prefix+"throttle",Throttle);d.Set(Prefix+"charge",Charge);d.Set(Prefix+"gear",Gear);
        d.Set(Prefix+"brake",Braking);d.Set(Prefix+"horn",Horn);d.Set(Prefix+"burnout",Burnout);d.Set(Prefix+"purge",Purge);d.Set(Prefix+"backfire",Backfire);
        d.Set(Prefix+"landing",Landing);d.Set(Prefix+"groundmode",GroundMode);d.Set(Prefix+"flying",Flying);d.Set(Prefix+"running",Running);d.Set(Prefix+"shifting",Shifting);
        d.Set(Prefix+"fold",Fold);d.Set(Prefix+"pitch",Pitch);d.Set(Prefix+"roll",Roll);d.Set(Prefix+"thrust",Thrust);d.Set(Prefix+"contact",Contact);d.Set(Prefix+"slip",Slip);
        for(int i=0;i<4;i++){d.Set(Prefix+"wheel"+i,Wheels[i]);d.Set(Prefix+"rotation"+i,Rotations[i]);d.Set(Prefix+"wheelrpm"+i,WheelRpm[i]);}
    }
    public static void ReleaseEffects(ZDO d) {
        foreach(var key in new[]{"horn","burnout","purge","backfire"})d.Set(Prefix+key,false);
        d.Set(Prefix+"throttle",0f);d.Set(Prefix+"thrust",0f);d.Set(VehicleNetworkProtocol.Boost,false);
    }
    public static void StopEffects(ZDO d){ReleaseEffects(d);d.Set(Prefix+"running",false);}
}
}
