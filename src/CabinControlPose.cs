using System;
namespace NeedForValheim {
// Input/automatic-transmission animation only: does not operate the drivetrain.
public sealed class CabinControlPose {
    public float Accelerator {get;private set;}
    public float Brake {get;private set;}
    public float Handbrake {get;private set;}
    public float Shift {get;private set;}
    public int Gear {get;private set;}
    bool initialized;
    float shiftTime;
    public void Step(float dt,float throttle,float brake,bool handbrake,int gear,bool active) {
        if(!Finite(dt)||dt<=0)return;
        dt=Math.Min(dt,.1f);
        if(!initialized){Gear=gear;initialized=true;}
        if(active&&gear!=Gear){shiftTime=.65f;Gear=gear;}
        if(!active)shiftTime=0;
        shiftTime=Math.Max(0,shiftTime-dt);
        // Reach the lever, shift, then bring the hand back to the rotating wheel.
        Shift=shiftTime<=0?0:(float)Math.Sin(Math.PI*(.65f-shiftTime)/.65f);
        Accelerator=Move(Accelerator,active?Clamp(Math.Abs(throttle)):0,dt*6);
        Brake=Move(Brake,active?Clamp(brake):0,dt*8);
        Handbrake=Move(Handbrake,active&&handbrake?1:0,dt*7);
    }
    static float Move(float value,float target,float speed)=>value<target?Math.Min(value+speed,target):Math.Max(value-speed,target);
    static float Clamp(float value)=>Finite(value)?Math.Max(0,Math.Min(1,value)):0;
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
