using System;
using UnityEngine;
namespace NeedForValheim {
// One vanilla cargo ZDO is the durable identity of both the car and its items.
internal static class VehiclePersistence {
    public const string LegacyModel="nfvu2_vehicle_v1";
    public const string Model="nfvu2_profile_v2";
    const string Position="nfvu2_pose_v2",Rotation="nfvu2_rotation_v2",Build="nfvu2_build_v2",Layout="nfvu2_layout_v2",Driven="nfvu2_driven_v2";
    public static bool IsLegacy(ZDO data)=>data!=null&&(data.GetInt(LegacyModel)==1||data.GetInt(LegacyModel)==2);
    public static bool IsVehicle(ZDO data)=>data!=null&&(!string.IsNullOrEmpty(data.GetString(Model))||IsLegacy(data));
    public static string Profile(ZDO data)=>!string.IsNullOrEmpty(data.GetString(Model))?data.GetString(Model):data.GetInt(LegacyModel)==2?"h2":CarCatalog.StockId;
    public static Vector3 Location(ZDO data)=>data.GetVec3(Position,data.GetVec3("nfvu2_position",data.GetPosition()));
    public static Quaternion Orientation(ZDO data)=>data.GetQuaternion(Rotation,data.GetQuaternion("nfvu2_rotation",data.GetRotation()));
    public static string Settings(ZDO data)=>string.IsNullOrEmpty(data.GetString(Model))?"{}":data.GetString(Build);
    public static int Trunk(ZDO data)=>VehicleStorageRules.Layout(string.IsNullOrEmpty(data.GetString(Model))?data.GetInt("nfvu2_layout"):data.GetInt(Layout));
    public static bool WasDriven(ZDO data)=>string.IsNullOrEmpty(data.GetString(Model))?data.GetBool("nfvu2_driven"):data.GetBool(Driven);
    public static bool ValidPose(ZDO data) {
        var p=Location(data);var q=Orientation(data);
        return Finite(p.x)&&Finite(p.y)&&Finite(p.z)&&Math.Abs(p.x)<=20000&&Math.Abs(p.z)<=20000&&Math.Abs(p.y)<=10000&&
            Finite(q.x)&&Finite(q.y)&&Finite(q.z)&&Finite(q.w)&&q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w>.5f&&q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w<1.5f;
    }
    static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
    public static void Save(ZDO data,string profile,Transform car,int layout,bool driven,string settings) {
        if(string.IsNullOrEmpty(profile)||profile.Length>64||settings==null||settings.Length>131072)throw new InvalidOperationException("Invalid persistent vehicle record");
        data.Persistent=true;data.Set(Position,car.position);data.Set(Rotation,car.rotation);
        data.Set(Layout,layout);data.Set(Driven,driven);data.Set(Build,settings);
        // Publish the discriminator last, after the snapshot is complete.
        data.Set(Model,profile);data.Set(LegacyModel,0);
    }
    public static void UpdateSettings(ZDO data,string settings,int layout) {
        if(settings==null||settings.Length>131072)throw new InvalidOperationException("Invalid vehicle setup size");
        data.Set(Build,settings);data.Set(Layout,VehicleStorageRules.Layout(layout));
        data.Set(VehicleNetworkProtocol.SetupRevision,data.GetInt(VehicleNetworkProtocol.SetupRevision)+1);
    }
    public static void UpgradeLegacy(ZDO data) {
        if(!IsLegacy(data)||!string.IsNullOrEmpty(data.GetString(Model)))return;
        string profile=Profile(data);var position=Location(data);var rotation=Orientation(data);int layout=Trunk(data);bool driven=WasDriven(data);
        data.Set(Position,position);data.Set(Rotation,rotation);data.Set(Layout,layout);data.Set(Driven,driven);data.Set(Build,"{}");
        data.Set(Model,profile);data.Set(LegacyModel,0);
    }
    public static void Forget(ZDO data){data.Set(Model,"");data.Set(LegacyModel,0);}
}
}
