using System;
using System.IO;
using System.Linq;
using NeedForValheim;
class Program {
    static int checks;
    static void Check(bool valid,string text){if(!valid)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    sealed class Car {public int ticks;public float distance;}
    static void Main(string[] args) {
        var empty=new Car();var fleet=new VehicleFleet<Car>(empty);var first=new Car{distance=4};var second=new Car{distance=1};
        fleet.Add(first);fleet.Current=first;fleet.Add(second);
        Check(fleet.Count==2&&fleet.Contains(first)&&fleet.Current==first,"adding second car retains first car and selection");
        Check(fleet.Nearest(c=>c.distance)==second,"nearer second car selected beside two cars");
        second.distance=9;Check(fleet.Nearest(c=>c.distance)==first,"walking to first car changes nearest vehicle");
        fleet.Visit(()=>fleet.Current.ticks++);
        Check(first.ticks==1&&second.ticks==1&&fleet.Current==first,"both cars tick independently and driver context restores");
        try{fleet.Visit(()=>{if(fleet.Current==second)throw new InvalidOperationException();});}catch(InvalidOperationException){}
        Check(fleet.Current==first,"tick exception restores selection");
        fleet.Remove(second);Check(fleet.Count==1&&fleet.Contains(first)&&fleet.Current==first,"removing second car preserves first car");
        fleet.Add(second);fleet.Add(new Car());fleet.Add(new Car());
        bool full=false;try{fleet.Add(new Car());}catch(InvalidOperationException){full=true;}
        Check(full&&fleet.Count==4,"capacity refuses fifth without removing existing cars");
        bool duplicate=false;try{fleet.Add(first);}catch(ArgumentException){duplicate=true;}
        Check(duplicate&&fleet.Count==4,"duplicate instance cannot be owned twice");
        fleet.Visit(()=>fleet.Remove(fleet.Current));Check(fleet.Count==0,"world cleanup visits every car despite removals");
        Check(fleet.Nearest(c=>c.distance)==null,"empty garage has no nearest car");
        var observers=new VehicleFleet<Car>(new Car()){Limit=8};
        for(int i=0;i<8;i++)observers.Add(new Car());
        bool capped=false;try{observers.Add(new Car());}catch(InvalidOperationException){capped=true;}
        Check(observers.Count==8&&capped,"observer capacity can expand while retaining a hard rendering bound");
        var spinner=new SpinnerMotion();spinner.Step(.1f,0);Check(spinner.Angle==0&&spinner.Rpm==0,"spinner rests before driving");
        for(int i=0;i<40;i++)spinner.Step(.05f,200);
        Check(spinner.Rpm>100&&spinner.Rpm<200,"spinner gains speed with bearing inertia");
        float initial=spinner.Rpm;for(int i=0;i<20;i++)spinner.Step(.05f,0);
        Check(spinner.Rpm>initial*.7f&&spinner.Rpm<initial,"spinner continues for seconds after tyre stops");
        float angle=spinner.Angle;spinner.Step(float.NaN,500);Check(spinner.Angle==angle,"invalid time cannot corrupt spinner");
        for(int i=0;i<600;i++)spinner.Step(.1f,0);Check(spinner.Rpm==0,"spinner eventually settles at rest");
        for(int i=0;i<60;i++)spinner.Step(.05f,-180);Check(spinner.Rpm<0&&Math.Abs(spinner.Angle)<360,"reverse drives spinner backwards with bounded phase");
        Console.WriteLine(checks+" fleet and spinner checks passed.");
    }
}
