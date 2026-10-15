using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using NeedForValheim;
class Program {
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static void Frames(CabinTransition motion,int frames,bool blocked=false){for(int i=0;i<frames;i++)motion.Step(.02f,blocked,0,false);}
    static void Main(string[] args) {
        var motion=new CabinTransition();motion.Arrive();Frames(motion,200,true);
        Check(motion.TimeInPhase==0,"doorless entry pauses with focus/menu");
        Frames(motion,200);Check(motion.CanDrive&&motion.SeatBlend==1,"cars without animated doors still seat the actual driver");
        Check(motion.BeginExit(),"doorless exit request accepted");Frames(motion,150);
        Check(motion.Phase==CabinPhase.Complete&&motion.SeatBlend==0,"doorless exit completes outside");
        motion.CancelExit();Check(motion.CanDrive,"blocked doorless exit can return to seat");
        var pose=new CabinControlPose();pose.Step(.1f,1,0,false,3,true);
        Check(pose.Shift==0,"initial engine gear does not spuriously shift");
        pose.Step(.1f,1,0,false,4,true);Check(pose.Shift>.1f&&pose.Accelerator>0,"real gear change moves lever hand and pedal");
        for(int i=0;i<10;i++)pose.Step(.1f,0,1,true,4,true);
        Check(pose.Shift==0&&pose.Handbrake==1&&pose.Brake==1&&pose.Accelerator==0,"hand returns after shift; braking and handbrake respond independently");
        pose.Step(.1f,0,0,false,4,false);Check(pose.Handbrake<1&&pose.Brake<1,"controls release during exit");
        float previous=pose.Handbrake;pose.Step(float.NaN,1,1,true,0,true);Check(pose.Handbrake==previous,"invalid time cannot corrupt pose");
        for(int i=0;i<10;i++)pose.Step(.1f,float.NaN,float.PositiveInfinity,false,4,true);
        Check(pose.Accelerator==0&&pose.Brake==0,"invalid input cannot corrupt limbs");
        var rows=new List<string>{"car,evidence,seatX,seatY,seatZ,ceiling,exitSide,driverScale"};
        var fits=new Dictionary<string,CabinFit>();
        var catalog=CarCatalog.Load(args[0]);
        foreach(var profile in catalog.Profiles) {
            var model=ModelFile.Load(Path.Combine(args[0],profile.assets.modelFile));var fit=CabinFit.FromModel(profile,model);fits.Add(profile.id,fit);
            Check(fit.seatX<-.15f&&fit.exitSide<fit.seatX-.35f,profile.id+" driver and exit are on physical left");
            Check(fit.seatY>=.2f&&fit.seatY+.74f*fit.driverScale/.85f<=fit.headCeiling+.001f,profile.id+" pelvis/head clearance coherent");
            Check(fit.seatZ>fit.rearEnd+.5f&&fit.seatZ+.8f<fit.frontEnd,profile.id+" pelvis/pedals fit original vehicle length");
            Check(fit.evidence!="chassis bounds fallback",profile.id+" original glazing supplies cabin measurements");
            rows.Add(string.Join(",",new[]{profile.id,fit.evidence}.Concat(new[]{fit.seatX,fit.seatY,fit.seatZ,fit.headCeiling,fit.exitSide,fit.driverScale}.Select(v=>v.ToString("F4",CultureInfo.InvariantCulture)))));
        }
        Check(fits.Count==76,"all shipped U2 and MW cars checked");
        Check(fits["mw-cemtr"].seatZ>1&&fits["mw-garb"].seatY>1,"commercial vehicles sit in front cab, not centre of cargo body");
        Check(fits["h2"].seatY>fits["mw-elise"].seatY+.35f,"SUV and low sports-car seats have distinct heights");
        Check(CabinFit.Stock350Z().seatY==.35f&&CabinFit.Stock350Z().seatZ==-.25f,"separate350Z interior retains its proven measured seat");
        File.WriteAllLines(args[1],rows);
        Console.WriteLine(checks+" cabin fitting/controls checks passed across "+fits.Count+" original models (no Unity runtime).");
    }
}
