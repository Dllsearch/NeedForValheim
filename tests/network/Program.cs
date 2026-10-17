using System;
using NeedForValheim;
class Program {
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static void Main() {
  var leases=new VehicleNetworkLeases();
  Check(!leases.Claim("car",0,1,0)&&!leases.Claim("car",1,0,0)&&!leases.Claim("",1,1,0),"invalid driver identities rejected");
  Check(!leases.Claim("car",1,1,double.NaN),"invalid lease clock rejected");
  Check(leases.Claim("car",11,101,0),"first driver receives lease");
  Check(!leases.Claim("car",22,202,1),"second driver cannot steal occupied vehicle");
  Check(!leases.Claim("other",11,101,1),"one connection cannot drive two vehicles");
  Check(!leases.Accept("car",22,0,1,1,10),"other driver's state rejected");
  Check(leases.Accept("car",11,0,1,10,10),"valid driver state accepted");
  Check(!leases.Accept("car",11,0,1.1,1,10)&&!leases.Accept("car",11,-1,1.1,1,10),"duplicate and reordered packets rejected");
  Check(!leases.Accept("car",11,1,.5,1,10),"backwards clock cannot extend authority");
  Check(!leases.Accept("car",11,1,1.1,100,10),"implausible per-update teleport rejected");
  Check(!leases.Accept("car",11,1,1.1,1,181),"out-of-range velocity rejected");
  Check(!leases.Accept("car",11,1,1.1,double.NaN,10)&&!leases.Accept("car",11,1,1.1,1,double.PositiveInfinity),"nonfinite movement rejected");
  Check(leases.Accept("car",11,1,1.1,1,10),"rejected packets do not consume sequence");
  Check(leases.Claim("car",11,101,2)&&!leases.Accept("car",11,0,2.1,1,10),"claim retry preserves accepted sequence");
  leases.Release("car",22);Check(leases.HeldBy("car",11,2.2),"nonholder release cannot eject driver");
  leases.Release("car",11);Check(!leases.Active("car",2.2),"holder release frees seat immediately");
  Check(leases.Claim("car",22,202,2.3)&&leases.Accept("car",22,0,2.4,1,10),"next driver gets fresh sequence after release");
  leases.Expire(2.5,id=>false);Check(!leases.Active("car",2.5),"disconnected driver loses lease without waiting for timeout");
  Check(leases.Claim("car",33,303,3)&&!leases.HeldBy("car",33,7),"silent driver expires at timeout boundary");
  Check(!leases.Accept("car",33,0,7,1,10)&&leases.Claim("car",44,404,7),"expired driver cannot publish and a new driver can take over");
  leases.Clear();Check(!leases.Active("car",7),"world switch clears connection-scoped leases");
  Check(VehicleNetworkProtocol.Version==3&&VehicleNetworkProtocol.MaxPacket<150000,"bounded versioned wire protocol");
  ClientChecks.Run();
  Console.WriteLine(checks+" network authority checks passed. No real peers or Unity transport executed.");
 }
}
