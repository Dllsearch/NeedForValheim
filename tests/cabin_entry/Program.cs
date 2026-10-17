using System;
using NeedForValheim;
class Program {
    static int checks;
    static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;}
    static void Main() {
        var stuck=new CabinApproach();bool recover=false;
        for(int i=0;i<60;i++)recover|=stuck.Step(.02f,2,false);
        Check(recover,"A stationary approach recovers within one second");
        var paused=new CabinApproach();bool safe=true;for(int i=0;i<1000;i++)safe&=!paused.Step(.1f,2,true);
        Check(safe,"Menus/focus do not exhaust the approach timeout");
        var walking=new CabinApproach();safe=true;for(int i=0;i<200;i++)safe&=!walking.Step(.02f,8-i*.03f,false);
        Check(safe,"A progressing walk is retained");
        walking.NextWaypoint();for(int i=0;i<110;i++)recover=walking.Step(.02f,5-i*.03f,false);
        Check(recover,"Even a moving route has a six-second limit across waypoints");
        var invalid=new CabinApproach();safe=true;for(int i=0;i<100;i++)safe&=!invalid.Step(float.NaN,2,false);
        Check(safe,"Invalid time cannot trigger a teleport");
        var motion=new CabinTransition();motion.Arrive();for(int i=0;i<200;i++)motion.Step(.02f,false,0,false);
        Check(motion.CanDrive,"Models without animated doors finish entry");
        var closedDoor=new CabinTransition();closedDoor.Arrive();
        for(int i=0;i<240;i++)closedDoor.Step(.02f,false,0,true);
        Check(closedDoor.DoorTimedOut&&!closedDoor.CanDrive,"A door forced shut cannot strand entry indefinitely");
        var focusLost=new CabinTransition();focusLost.Arrive();
        for(int i=0;i<1000;i++)focusLost.Step(.1f,true,0,true);
        Check(!focusLost.DoorTimedOut&&focusLost.TimeInPhase==0,"Paused door animation does not consume the timeout");
        var exit=new CabinTransition();exit.Arrive();
        for(int i=0;i<200;i++)exit.Step(.02f,false,1,true);
        for(int i=0;i<50;i++)exit.Step(.02f,false,0,true);
        Check(exit.CanDrive&&exit.BeginExit(),"Normal animated entry still reaches the seat");
        for(int i=0;i<240;i++)exit.Step(.02f,false,0,true);
        Check(exit.DoorTimedOut,"An exit door forced shut is bounded too");
        exit.CancelExit();
        Check(exit.CanDrive&&!exit.DoorTimedOut,"Cancelled exit restores seated control and clears its timeout");
        Console.WriteLine(checks+" cabin entry timing checks passed; collision and animation require Unity acceptance.");
    }
}
