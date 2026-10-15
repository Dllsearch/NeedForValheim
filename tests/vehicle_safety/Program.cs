using System;using NeedForValheim;using UnityEngine;
class SafetyTests {
 static int checks;
 static void Check(bool pass,string name){if(!pass)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static void Main(){
  var p=new Plugin();var rig=new CarProfile();var local=new MonoBehaviour();local.transform.forward=Vector3.back;
  GameCamera.instance=new GameCamera();GameCamera.instance.transform.forward=Vector3.forward;
  World.Reset();Check(p.Place(local,rig,out var pos),"camera-forward view finds a grounded placement despite backward character facing");Check(pos.z>5&&Math.Abs(pos.x)<.01,"first spawn is visible centred ahead");
  GameCamera.instance.transform.forward=Vector3.right;World.Reset();Check(p.Place(local,rig,out pos)&&pos.x>5&&Math.Abs(pos.z)<.001,"orbiting view controls placement instead of character yaw");
  p.menuOpen=true;p.menuCamera=GameCamera.instance;p.showroomCameraRotation=Quaternion.Euler(0,0,0);GameCamera.instance.transform.forward=Vector3.back;World.Reset();Check(p.Place(local,rig,out pos)&&pos.z>5,"garage orbit cannot reverse the saved world spawn view");p.menuOpen=false;
  GameCamera.instance.transform.forward=Vector3.forward;World.Reset();World.Block=(c)=>true;Check(!p.Place(local,rig,out pos),"completely blocked forward fan fails instead of teleporting behind");Check(World.MaxSampleZ<13&&World.MinSampleZ>0,"bounded obstruction search never walks behind or far around player");
  World.Reset();World.Block=c=>Math.Abs(c.x)<1;Check(p.Place(local,rig,out pos)&&pos.z>0&&Math.Abs(pos.x)>1,"side fallback remains inside visible forward fan");
  World.Reset();World.Zone=q=>q.z<=5.5f;Check(!p.Place(local,rig,out pos),"one unloaded wheel tile rejects a centre-supported spawn");
  World.Reset();World.Height=q=>q.z*.5f;Check(!p.Place(local,rig,out pos),"cliff straddling wheel footprint is rejected");
  World.Reset();World.Water=1;Check(!p.Place(local,rig,out pos),"underwater terrain is not treated as a safe spawn");
  World.Reset();World.Ready=false;Check(!p.Place(local,rig,out pos),"terrain generation alone does not bypass object streaming readiness");
  World.Reset();World.CarUnder=true;Check(!p.Place(local,rig,out pos),"existing vehicle roof cannot become spawn support");
  World.Reset();World.CharacterUnder=true;Check(!p.Place(local,rig,out pos),"player or monster cannot become spawn support");
  World.Reset();World.DynamicUnder=true;Check(!p.Place(local,rig,out pos),"moving physical objects cannot become spawn support");
  World.Reset();World.Slope=.6f;Check(!p.Place(local,rig,out pos),"steep footprint is rejected");
  World.Reset();World.Lava=true;Check(!p.Place(local,rig,out pos),"lava is rejected");
  World.Reset();Check(p.Pad(new Vector3(10,0,20),Quaternion.Euler(0,90,0),rig,out pos)&&pos.x==10&&pos.z==20,"garage marked pad starts at requested centre");
  World.Reset();World.Height=q=>.4f;Check(p.Place(local,rig,out pos)&&Math.Abs(pos.y-.75f)<.001,"supported platform spawn keeps profile lift");
  Check(VehicleParkingRules.Freeze(false,false,0,0,0,false),"parked car freezes immediately when terrain unloads");
  Check(!VehicleParkingRules.Freeze(true,false,0,0,0,false),"occupied car continues streaming through zone boundaries");
  Check(!VehicleParkingRules.Freeze(false,true,200,8,4,false),"flight control retains responsibility for flying cars");
  Check(VehicleParkingRules.Freeze(false,false,81,0,0,true),"remote car freezes before loaded tile eviction");
  Check(!VehicleParkingRules.Thaw(true,0,false,10),"entering a held car cannot thaw absent ground");
  Check(!VehicleParkingRules.Thaw(false,20,true,.29f),"a single returning terrain frame cannot prematurely thaw");
  Check(VehicleParkingRules.Thaw(false,20,true,.3f),"stable loaded close support releases parking");
  Check(!VehicleParkingRules.Thaw(false,61,true,3),"parking hysteresis avoids remote freeze/thaw churn");
  Check(VehicleParkingRules.Freeze(false,false,20,1.1f,.1f,true),"unoccupied falling car restores supported pose before large drop");
  Check(!VehicleParkingRules.Freeze(false,false,20,0,.2f,true),"short near-player airborne motion is allowed");
  World.Reset();var parked=new Plugin();for(int i=0;i<4;i++)parked.wheels[i].transform.position=new Vector3(i%2==0?-.8f:.8f,.7f,i<2?1.3f:-1.3f);Time.unscaledTime=0;parked.body.position=new Vector3(0,.35f,0);Check(!parked.Park(),"supported near car records safe physical parking pose");
  World.Zone=q=>false;parked.body.position=new Vector3(0,-.1f,0);Time.unscaledTime=.02f;Check(parked.Park()&&parked.body.isKinematic&&parked.body.position.y==.35f,"actual parking guard restores pose before missing tile can drop car");
  World.Zone=q=>true;World.Ready=false;Time.unscaledTime=1;Check(parked.Park()&&parked.body.isKinematic,"actual held car waits for object loading after terrain returns");
  World.Ready=true;Time.unscaledTime=2;Check(parked.Park(),"actual held car waits for stable support instead of first ready frame");
  Time.unscaledTime=2.31f;Check(!parked.Park()&&!parked.body.isKinematic&&parked.body.Wakes==1,"actual guard thaws exactly after stable loaded support");
  World.Zone=q=>false;Time.unscaledTime=3;parked.Park();parked.driving=true;Time.unscaledTime=4;Check(parked.Park()&&parked.body.isKinematic,"driver entry cannot bypass held missing terrain in real parking path");
  parked.driving=false;World.Zone=q=>true;World.Ready=true;World.Support=q=>q.z>0;Time.unscaledTime=5;Check(parked.Park(),"actual held car does not thaw when one axle lacks collider support");
  Check(VehicleExitRules.Active(10,10.8f,0,0),"grounded exit starts short landing grace");
  Check(!VehicleExitRules.Active(10.8f,10.8f,0,0),"exit grace expires even without contact");
  Check(!VehicleExitRules.Active(10.1f,10.8f,4.1f,0),"moving away immediately ends exit protection");
  Check(!VehicleExitRules.Active(10.1f,10.8f,1,-.81f),"a real drop beyond exit ledge keeps native fall damage");
  Check(!VehicleExitRules.Active(10.1f,10.8f,1,.81f),"jumping away does not retain exit fall reset");
  Console.WriteLine(checks+" vehicle safety checks passed.");
 }
}
