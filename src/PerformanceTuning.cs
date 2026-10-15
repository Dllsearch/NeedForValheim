using System;
namespace NeedForValheim {
// NFVU2 upgrade packages. Stage zero is exactly the established controller.
public static class PerformanceTuning {
    public static int Stage(int value)=>Math.Max(0,Math.Min(3,value));
    public static float Torque(int engine,int turbo,int transmission)=>(1+.12f*Stage(engine))*(1+.10f*Stage(turbo))*(1+.035f*Stage(transmission));
    public static float Speed(int engine,int turbo,int transmission)=>1+.04f*Stage(engine)+.04f*Stage(turbo)+.06f*Stage(transmission);
    public static float Grip(int tyres)=>1+.08f*Stage(tyres);
    public static float Brakes(int brakes)=>1+.2f*Stage(brakes);
    public static float Capacity(int nitro)=>1+.25f*Stage(nitro);
    public static float Boost(int nitro)=>1+.1f*Stage(nitro);
}
}
