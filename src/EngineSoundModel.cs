using System;

namespace NeedForValheim {
// A gearbox for sound only. It never writes to the vehicle's physics.
public struct EngineSoundSnapshot {
    public float Rpm,Load,Speed,ForwardSpeed,WheelRpm,Contact,Slip;public int Gear;public bool Shifting;
}
public sealed class EngineSoundModel {
    public static readonly int[] Bands={1400,2100,2800,3500,4300,5100,5900,6500};
    public static readonly int[] SuvBands={1000,1500,2000,2500,3000,3600,4200,4782};
    readonly float[] ratios;
    readonly float wheelRadius,upShiftRpm,downShiftRpm,minRpm,maxRpm;
    public EngineSoundModel(CarSound sound,float wheelRadius) {
        ratios=sound.gearRatios;this.wheelRadius=wheelRadius;
        upShiftRpm=sound.upShiftRpm;downShiftRpm=sound.downShiftRpm;minRpm=sound.minRpm;maxRpm=sound.maxRpm;Rpm=minRpm;
    }
    public EngineSoundModel(bool suv=false):this(suv?CarCatalog.BuiltInH2().sound:CarCatalog.Nissan350Z().sound,
        suv?CarCatalog.BuiltInH2().rig.wheelRadius:CarCatalog.Nissan350Z().rig.wheelRadius) {}
    public float Rpm {get;private set;}=950;
    public float Load {get;private set;}
    public float Level {get;private set;}
    public float ShiftGain {get;private set;}=1;
    public int Gear {get;private set;}=1;
    public bool IsShifting => shiftRemaining>0;
    float shiftRemaining,cooldown;
    static float Move(float a,float b,float step)=>Math.Abs(b-a)<=step?b:a+Math.Sign(b-a)*step;
    static float Safe(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:value;
    public void Step(float dt,bool driving,float forwardSpeed,float wheelRpm,float throttle,bool brake,bool terrain=true,bool burnout=false) {
        dt=DrivingMath.Clamp(Safe(dt),0,.1f);forwardSpeed=Safe(forwardSpeed);wheelRpm=Safe(wheelRpm);throttle=DrivingMath.Clamp(Safe(throttle),-1,1);
        Level=Move(Level,driving?1:0,dt*3);
        cooldown=Math.Max(0,cooldown-dt);shiftRemaining=Math.Max(0,shiftRemaining-dt);
        float speed=Math.Abs(forwardSpeed),rollingRpm=speed*60/(2*(float)Math.PI*wheelRadius);
        float rpmAtWheel=Math.Max(rollingRpm,Math.Min(Math.Abs(wheelRpm),rollingRpm+180));
        bool reverse=forwardSpeed<-.5f || (speed<.5f && throttle<0);
        if(!driving){Gear=1;shiftRemaining=cooldown=0;}
        else if(reverse)Gear=-1;
        else if(Gear<1 || speed<.5f)Gear=1;
        // Compress the audible gears for short Valheim runs. This final drive is
        // acoustic only: neither wheel torque nor actual vehicle speed changes.
        float acousticDrive=3.54f*(terrain?2.4f:1.65f);
        float rawRpm=rpmAtWheel*(Gear<0?3.44f:ratios[Gear-1])*(Gear<0?3.54f:acousticDrive);
        if(driving && !burnout && Gear>0 && speed>2 && cooldown==0) {
            // Wheelspin can raise the note, but cannot race through the gears
            // while the car barely moves or a wheel is airborne.
            float shiftRpm=rollingRpm*ratios[Gear-1]*acousticDrive;
            if(shiftRpm>upShiftRpm && Gear<ratios.Length){Gear++;shiftRemaining=.14f;cooldown=.45f;}
            else if(shiftRpm<downShiftRpm && Gear>1){Gear--;shiftRemaining=.12f;cooldown=.45f;}
            rawRpm=rpmAtWheel*ratios[Gear-1]*acousticDrive;
        }
        float input=driving&&!DrivingMath.DirectionBrake(forwardSpeed,throttle)?Math.Abs(throttle):0;
        float target=driving?Math.Max(minRpm+input*(brake?3300:1700),rawRpm):minRpm;
        if(driving&&burnout){Gear=1;shiftRemaining=cooldown=0;input=1;target=Math.Max(5000,rawRpm);}
        target=DrivingMath.Clamp(target,minRpm,maxRpm);
        Rpm=Move(Rpm,target,dt*(target>Rpm?6000:9500));
        Load=Move(Load,input,dt*5);
        ShiftGain=Move(ShiftGain,shiftRemaining>0?.32f:1,dt*12);
    }
    public void Observe(float dt,bool running,float rpm,float load,int gear,bool shifting) {
        dt=DrivingMath.Clamp(Safe(dt),0,.1f);Level=Move(Level,running?1:0,dt*3);
        Rpm=DrivingMath.Clamp(Safe(rpm),minRpm,maxRpm);Load=running?DrivingMath.Clamp(Safe(load),0,1):0;
        Gear=Math.Max(-1,Math.Min(ratios.Length-1,gear));shiftRemaining=shifting?.1f:0;
        ShiftGain=Move(ShiftGain,shifting?.32f:1,dt*12);
    }
    // Equal-power blending: only the two surrounding recordings are audible.
    public static float BandGain(float rpm,int index)=>BandGain(rpm,index,Bands);
    public static float BandGain(float rpm,int index,int[] bands) {
        if(index<0 || index>=bands.Length)return 0;
        rpm=DrivingMath.Clamp(Safe(rpm),bands[0],bands[bands.Length-1]);
        float gain;
        if(rpm<=bands[index])gain=index==0?1:(rpm-bands[index-1])/(bands[index]-bands[index-1]);
        else gain=index==bands.Length-1?1:(bands[index+1]-rpm)/(bands[index+1]-bands[index]);
        return (float)Math.Sqrt(DrivingMath.Clamp(gain,0,1));
    }
}
}
