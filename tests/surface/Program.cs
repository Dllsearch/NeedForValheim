using System;
using NeedForValheim;
class Program {
    static int checks;
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static void Main(){
        foreach(VehicleGroundSurface surface in Enum.GetValues(typeof(VehicleGroundSurface))) {
            Check(VehicleSurfaceRules.DustRate(30,1,surface,false,false,true,true)==0,"blocked");
            Check(VehicleSurfaceRules.DustRate(30,1,surface,false,false,false,false)==0,"airborne");
            Check(VehicleSurfaceRules.DustRate(30,1,surface,true,false,false,true)==0,"rain");
            Check(VehicleSurfaceRules.DustRate(30,1,surface,false,true,false,true)==0,"water");
            Check(VehicleSurfaceRules.DustRate(2.5f,1,surface,false,false,false,true)==0,"parked");
            for(int speed=0;speed<=400;speed+=4) {
                float rate=VehicleSurfaceRules.DustRate(speed,100,surface,false,false,false,true);
                Check(rate>=0&&rate<=24,"particle budget");
            }
        }
        Check(VehicleSurfaceRules.DustRate(20,.1f,VehicleGroundSurface.Hard,false,false,false,true)==0,"constructed floors");
        Check(VehicleSurfaceRules.DustRate(20,.1f,VehicleGroundSurface.Mud,false,false,false,true)==0,"mud has no dry dust");
        Check(VehicleSurfaceRules.DustRate(20,.1f,VehicleGroundSurface.Dirt,false,false,false,true)>VehicleSurfaceRules.DustRate(20,.1f,VehicleGroundSurface.Grass,false,false,false,true),"grass lighter than dirt");
        Check(VehicleSurfaceRules.DustRate(20,1,VehicleGroundSurface.Dirt,false,false,false,true)>VehicleSurfaceRules.DustRate(20,0,VehicleGroundSurface.Dirt,false,false,false,true),"slip raises dust");
        float dirt=0;
        for(int i=0;i<5000;i++)dirt=VehicleSurfaceRules.DirtStep(dirt,.1f,2,4,VehicleGroundSurface.Mud,false,false,true);
        Check(dirt==.75f,"dirt capped");
        Check(VehicleSurfaceRules.DirtStep(dirt,.1f,20,4,VehicleGroundSurface.Hard,false,false,false)==0,"disable restores clean");
        float full=VehicleSurfaceRules.DirtStep(0,.1f,10,4,VehicleGroundSurface.Dirt,false,false,true);
        float half=VehicleSurfaceRules.DirtStep(0,.1f,10,2,VehicleGroundSurface.Dirt,false,false,true);
        Check(Math.Abs(full-half*2)<.000001f,"contact weighted deposition");
        Check(VehicleSurfaceRules.DirtStep(.2f,.1f,10,0,VehicleGroundSurface.Dirt,false,false,true)==.2f,"airborne no deposition");
        Check(VehicleSurfaceRules.DirtStep(.2f,.1f,10,4,VehicleGroundSurface.Snow,false,false,true)==.2f,"snow no brown grime");
        Check(VehicleSurfaceRules.DirtStep(.2f,.1f,0,4,VehicleGroundSurface.Hard,false,false,true)==.2f,"stationary no deposition");
        float rain=dirt,water=dirt;
        for(int i=0;i<600;i++){
            rain=VehicleSurfaceRules.DirtStep(rain,.1f,0,0,VehicleGroundSurface.Hard,true,false,true);
            water=VehicleSurfaceRules.DirtStep(water,.1f,0,0,VehicleGroundSurface.Hard,false,true,true);
            Check(water<=rain,"water washes faster");
        }
        Check(rain==0&&water==0,"rain and water eventually clean");
        foreach(float value in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity}) {
            float amount=VehicleSurfaceRules.DirtStep(value,value,value,20,VehicleGroundSurface.Dirt,false,false,true);
            Check(!float.IsNaN(amount)&&!float.IsInfinity(amount)&&amount>=0&&amount<=.75f,"invalid inputs bounded");
            float rate=VehicleSurfaceRules.DustRate(value,value,VehicleGroundSurface.Dirt,false,false,false,true);
            Check(!float.IsNaN(rate)&&!float.IsInfinity(rate)&&rate>=0&&rate<=24,"invalid dust inputs bounded");
        }
        Console.WriteLine("Vehicle surface rules: "+checks+" checks passed.");
    }
}
