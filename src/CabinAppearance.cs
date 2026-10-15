using System;
namespace NeedForValheim {
// Shared tuning for the visible cabin only; never changes wheel physics or character scale outside it.
public static class CabinAppearance {
    public const float DriverScale=.85f;
    public const float HeadCeiling=1.09f;
    public static float SteeringDegrees(float wheelAngle)=>Finite(wheelAngle)?Math.Max(-180,Math.Min(180,wheelAngle*4)):0;
    public static float Tint(float value)=>Finite(value)?Math.Max(0,Math.Min(1,value)):.16f;
    public static float WindowAlpha(float tint)=>.04f+.94f*Tint(tint);
    // Reserve space above the head bone for the scalp/hair, in car-local metres.
    public static float RoofLowering(float headY,float visualScale)=>RoofLowering(headY,visualScale,HeadCeiling);
    public static float RoofLowering(float headY,float visualScale,float ceiling)=>Finite(headY)&&Finite(visualScale)&&Finite(ceiling)?
        Math.Max(0,headY+.18f*Math.Abs(visualScale)-ceiling):0;
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
