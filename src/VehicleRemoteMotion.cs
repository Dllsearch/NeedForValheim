using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeedForValheim {
// Render slightly behind packet arrival so ordinary network gaps interpolate
// between complete poses instead of repeatedly chasing the newest target.
internal sealed class VehicleRemoteMotion {
    internal sealed class Presentation {
        public readonly Vector3[] Wheels=new Vector3[4];
        public readonly Quaternion[] Rotations={Quaternion.identity,Quaternion.identity,Quaternion.identity,Quaternion.identity};
        public readonly float[] WheelRpm=new float[4];
        public float Steering,Fold,Pitch,Roll,Thrust,Rpm,Load,Throttle;
    }
    struct Sample {public float Time;public Vector3 Position,Velocity;public Quaternion Rotation;public Presentation Detail;}
    readonly List<Sample> samples=new List<Sample>();
    float delay=.12f,interval,jitter,renderTime,lastNow;
    bool clockReady,renderStarted;
    bool hasRendered,correcting;
    Vector3 lastPosition,positionCorrection;
    Quaternion lastRotation,rotationCorrection;
    float correctionStart;
    public float InterpolationDelay=>delay;
    public static bool RequiresReset(float distance,float previousSpeed,float speed,float elapsed) {
        if(float.IsNaN(elapsed)||float.IsInfinity(elapsed))elapsed=0;
        float travel=Math.Max(previousSpeed,speed)*Math.Max(0,Math.Min(4,elapsed));
        return distance>12+travel;
    }
    public void Push(float time,Vector3 position,Quaternion rotation,Vector3 velocity,bool reset,Presentation detail=null) {
        if(float.IsNaN(time)||float.IsInfinity(time))return;
        if(reset){samples.Clear();delay=.12f;interval=jitter=0;clockReady=renderStarted=hasRendered=correcting=false;}
        if(samples.Count>0&&time<=samples[samples.Count-1].Time)return;
        bool correct=hasRendered&&samples.Count>0&&renderTime>samples[samples.Count-1].Time;
        if(samples.Count>0) {
            float gap=time-samples[samples.Count-1].Time;
            if(interval==0)interval=gap;
            else {float error=gap-interval;interval+=error*.1f;jitter+=(Math.Abs(error)-jitter)*.1f;}
            delay=Math.Max(.12f,Math.Min(.35f,interval*1.1f+jitter*2));
        }
        samples.Add(new Sample{Time=time,Position=position,Rotation=rotation,Velocity=velocity,Detail=detail});
        if(samples.Count>16)samples.RemoveAt(0);
        if(correct) {
            Pose(renderTime,out var rawPosition,out var rawRotation);
            positionCorrection=lastPosition-rawPosition;rotationCorrection=lastRotation*Quaternion.Inverse(rawRotation);
            correctionStart=time;correcting=true;
        }
    }
    float RenderTime(float now) {
        if(samples.Count==0)return now;
        if(float.IsNaN(now)||float.IsInfinity(now))return clockReady?renderTime:samples[0].Time;
        if(clockReady&&now<=lastNow)return renderTime;
        float desired=now-delay;
        if(!clockReady||!renderStarted) {
            renderTime=Math.Max(samples[0].Time,desired);renderStarted=desired>samples[0].Time;
        }else {
            float dt=now-lastNow;
            float error=desired-(renderTime+dt);
            float rate=1+Math.Max(-.15f,Math.Min(.15f,error*4));
            renderTime+=dt*rate;
        }
        // Freeze the playback clock as well as the pose when extrapolation is
        // exhausted. A new packet then advances from the held time, not from a
        // hidden clock that ran ahead during the gap.
        renderTime=Math.Max(samples[0].Time,Math.Min(renderTime,samples[samples.Count-1].Time+.1f));
        clockReady=true;lastNow=now;return renderTime;
    }
    public void EvaluatePresentation(float now,Presentation result) {
        if(samples.Count==0)return;
        float time=RenderTime(now);var a=samples[0];var b=samples[samples.Count-1];
        if(time<=a.Time)b=a;
        else if(time>=b.Time)a=b;
        else {for(int i=1;i<samples.Count;i++)if(samples[i].Time>=time){a=samples[i-1];b=samples[i];break;}}
        var from=a.Detail;var to=b.Detail;if(from==null||to==null)return;
        float t=b.Time>a.Time?Mathf.Clamp01((time-a.Time)/(b.Time-a.Time)):0;
        for(int i=0;i<4;i++) {
            result.Wheels[i]=Vector3.Lerp(from.Wheels[i],to.Wheels[i],t);
            result.Rotations[i]=Quaternion.Slerp(from.Rotations[i],to.Rotations[i],t);
            result.WheelRpm[i]=Mix(from.WheelRpm[i],to.WheelRpm[i],t);
        }
        result.Steering=Mix(from.Steering,to.Steering,t);result.Fold=Mix(from.Fold,to.Fold,t);
        result.Pitch=Mix(from.Pitch,to.Pitch,t);result.Roll=Mix(from.Roll,to.Roll,t);
        result.Thrust=Mix(from.Thrust,to.Thrust,t);result.Rpm=Mix(from.Rpm,to.Rpm,t);
        result.Load=Mix(from.Load,to.Load,t);result.Throttle=Mix(from.Throttle,to.Throttle,t);
    }
    static float Mix(float a,float b,float t)=>a+(b-a)*t;
    public void Evaluate(float now,out Vector3 position,out Quaternion rotation) {
        if(samples.Count==0){position=new Vector3();rotation=Quaternion.identity;return;}
        if(float.IsNaN(now)||float.IsInfinity(now))now=clockReady?lastNow:samples[0].Time;
        float time=RenderTime(now);
        while(samples.Count>2&&samples[1].Time<=time)samples.RemoveAt(0);
        Pose(time,out position,out rotation);
        if(correcting) {
            float blend=Mathf.Clamp01((now-correctionStart)/.15f);
            position+=positionCorrection*(1-blend);
            rotation=Quaternion.Slerp(rotationCorrection,Quaternion.identity,blend)*rotation;
            if(blend>=1)correcting=false;
        }
        lastPosition=position;lastRotation=rotation;hasRendered=true;
    }
    void Pose(float time,out Vector3 position,out Quaternion rotation) {
        var a=samples[0];var b=samples[samples.Count-1];
        if(time<=a.Time){position=a.Position;rotation=a.Rotation;return;}
        if(time>=b.Time){position=b.Position+b.Velocity*Mathf.Clamp(time-b.Time,0,.1f);rotation=b.Rotation;return;}
        for(int i=1;i<samples.Count;i++)if(samples[i].Time>=time){a=samples[i-1];b=samples[i];break;}
        float blend=Mathf.Clamp01((time-a.Time)/(b.Time-a.Time));
        position=Vector3.Lerp(a.Position,b.Position,blend);rotation=Quaternion.Slerp(a.Rotation,b.Rotation,blend);
    }
}
}
