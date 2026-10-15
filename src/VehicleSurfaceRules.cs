using System;
namespace NeedForValheim {
public enum VehicleGroundSurface { Hard, Grass, Dirt, Mud, Snow, Ash }
// These rules affect appearance only; tyre grip and suspension stay independent.
public static class VehicleSurfaceRules {
    static float Clamp(float value,float low,float high)=>float.IsNaN(value)||float.IsInfinity(value)?low:Math.Max(low,Math.Min(high,value));
    public static float DustRate(float speed,float slip,VehicleGroundSurface surface,bool wet,bool submerged,bool blocked,bool grounded) {
        if(blocked||!grounded||wet||submerged||surface==VehicleGroundSurface.Hard||surface==VehicleGroundSurface.Mud)return 0;
        speed=Clamp(Math.Abs(speed),0,80);slip=Clamp(Math.Abs(slip),0,2);
        if(speed<=2.5f)return 0;
        float density=surface==VehicleGroundSurface.Grass?.32f:surface==VehicleGroundSurface.Snow?.7f:1;
        return Math.Min(24,(speed-2.5f)*1.2f*(1+slip*.8f))*density;
    }
    public static float DirtStep(float amount,float seconds,float metres,int contacts,VehicleGroundSurface surface,bool rain,bool submerged,bool enabled) {
        if(!enabled)return 0;
        amount=Clamp(amount,0,.75f);seconds=Clamp(seconds,0,.25f);metres=Clamp(metres,0,20);contacts=Math.Max(0,Math.Min(4,contacts));
        float wash=submerged?.22f:rain?.014f:0;
        float soil=surface==VehicleGroundSurface.Mud?1.6f:surface==VehicleGroundSurface.Dirt?1:surface==VehicleGroundSurface.Grass?.45f:surface==VehicleGroundSurface.Ash?.65f:0;
        return Clamp(amount+metres*(contacts/4f)*soil*.0014f-seconds*wash,0,.75f);
    }
}
}
