using System;
using UnityEngine;
using NeedForValheim;
namespace UnityEngine {
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
  public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a*(1-t)+b*t;
 }
 public struct Quaternion {
  public float x,y,z,w;
  public Quaternion(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}
  public static Quaternion identity=>new Quaternion(0,0,0,1);
  static System.Numerics.Quaternion N(Quaternion q)=>new System.Numerics.Quaternion(q.x,q.y,q.z,q.w);
  static Quaternion U(System.Numerics.Quaternion q)=>new Quaternion(q.X,q.Y,q.Z,q.W);
  public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>U(System.Numerics.Quaternion.Slerp(N(a),N(b),t));
  public static Quaternion Inverse(Quaternion q)=>U(System.Numerics.Quaternion.Inverse(N(q)));
  public static Quaternion operator *(Quaternion a,Quaternion b)=>U(N(a)*N(b));
 }
 public static class Mathf {public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));public static float Clamp01(float v)=>Clamp(v,0,1);}
}
class RemoteMotionTests {
 static int checks;
 static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;}
 static void Near(VehicleRemoteMotion m,float time,float expected,string label){m.Evaluate(time,out var p,out var r);if(Math.Abs(p.x-expected)>.001f)throw new Exception(label+": "+p.x);checks++;}
 static void Main(){
  var m=new VehicleRemoteMotion();var q=Quaternion.identity;var v=new Vector3(10,0,0);
  m.Push(0,new Vector3(0,0,0),q,v,true);
  Near(m,0,0,"initial pose does not drift backwards");
  m.Push(.1f,new Vector3(1,0,0),q,v,false);
  Near(m,.17f,.5f,"render between snapshots instead of jumping to latest");
  m.Push(.23f,new Vector3(2.3f,0,0),q,v,false);
  Near(m,.3f,1.8f,"irregular packet interval remains continuous");
  Near(m,.4f,2.8f,"short gap extrapolates from latest velocity");
  Near(m,3,3.3f,"packet loss cannot extrapolate indefinitely");
  m.Push(4,new Vector3(100,0,0),q,new Vector3(),true);
  Near(m,4,100,"recovery resets the buffer immediately");
  m.Push(4,new Vector3(999,0,0),q,v,false);
  Near(m,4,100,"duplicate sample time does not divide by zero or replace pose");
  for(int i=1;i<=100;i++)m.Push(4+i*.05f,new Vector3(100+i*.5f,0,0),q,v,false);
  Near(m,9,148.8f,"bounded buffer retains recent continuous history");
  var channel=new VehicleRemoteMotion();var early=new VehicleRemoteMotion.Presentation{Fold=0,Rpm=1000,Steering=0};
  var late=new VehicleRemoteMotion.Presentation{Fold=1,Rpm=5000,Steering=20};
  early.Wheels[0]=new Vector3(0,0,0);late.Wheels[0]=new Vector3(0,2,0);late.WheelRpm[0]=600;
  channel.Push(0,new Vector3(),q,v,true,early);channel.Push(.1f,new Vector3(1,0,0),q,v,false,late);
  var shown=new VehicleRemoteMotion.Presentation();channel.Evaluate(.17f,out var body,out var rotation);channel.EvaluatePresentation(.17f,shown);
  if(Math.Abs(body.x-.5f)>.001f||Math.Abs(shown.Wheels[0].y-1)>.001f||Math.Abs(shown.Fold-.5f)>.001f||Math.Abs(shown.Rpm-3000)>.01f||Math.Abs(shown.Steering-10)>.001f||Math.Abs(shown.WheelRpm[0]-300)>.001f)throw new Exception("presentation must use the same interpolation time as the chassis");checks++;
  channel.EvaluatePresentation(2,shown);if(shown.Fold!=1||shown.Rpm!=5000)throw new Exception("packet loss must hold the last presentation instead of overshooting");checks++;
  channel.Push(3,new Vector3(),q,new Vector3(),true,early);channel.EvaluatePresentation(3,shown);if(shown.Fold!=0||shown.Rpm!=1000)throw new Exception("recovery must reset continuous effect history");checks++;
  var jittered=new VehicleRemoteMotion();float[] arrivals={0,.1f,.4f,.5f,.6f,.9f,1,1.1f,1.4f,1.5f};
  int packet=0;float previousPosition=0,peakStep=0;
  for(int frame=0;frame<=90;frame++) {
   float now=frame*.02f;
   while(packet<arrivals.Length&&arrivals[packet]<=now+.00001f){float at=arrivals[packet++];jittered.Push(at,new Vector3(at*10,0,0),q,v,packet==1);}
   jittered.Evaluate(now,out var p,out var facing);peakStep=Math.Max(peakStep,Math.Abs(p.x-previousPosition));previousPosition=p.x;
  }
  if(peakStep>.4f)throw new Exception("packet-gap recovery jumps by "+peakStep+" metres in one 20 ms render frame");checks++;
  var stopped=new VehicleRemoteMotion();stopped.Push(0,new Vector3(),q,v,true);stopped.Push(.1f,new Vector3(1,0,0),q,v,false);
  float previousStop=0,stopJump=0;
  for(int frame=0;frame<=70;frame++) {
   float now=frame*.02f;if(frame==20)stopped.Push(now,new Vector3(1.3f,0,0),q,new Vector3(),false);
   stopped.Evaluate(now,out var p,out var facing);stopJump=Math.Max(stopJump,Math.Abs(p.x-previousStop));previousStop=p.x;
  }
  if(stopJump>.35f||Math.Abs(previousStop-1.3f)>.001f)throw new Exception("late stop sample must converge without a snap or indefinite drift; peak "+stopJump);checks++;
  Check(!VehicleRemoteMotion.RequiresReset(18,40,40,.5f)&&VehicleRemoteMotion.RequiresReset(40,0,0,.5f),
   "legitimate fast travel across a packet gap is distinct from an unexplained teleport");
  Check(VehicleRemoteMotion.RequiresReset(15,30,30,0)&&VehicleRemoteMotion.RequiresReset(15,30,30,float.NaN),
   "missing or invalid timing cannot grant an unbounded discontinuity allowance");
  var sparse=new VehicleRemoteMotion();int sparsePacket=0;float previousSparse=0,sparseStep=0;
  for(int frame=0;frame<=200;frame++) {
   float now=frame*.02f;
   while(sparsePacket*.3f<=now+.00001f){float at=sparsePacket++*.3f;sparse.Push(at,new Vector3(at*10,0,0),q,v,sparsePacket==1);}
   sparse.Evaluate(now,out var p,out var facing);sparseStep=Math.Max(sparseStep,Math.Abs(p.x-previousSparse));previousSparse=p.x;
  }
  Check(sparse.InterpolationDelay>.25f&&sparse.InterpolationDelay<=.35f&&sparseStep<.4f&&previousSparse>32&&previousSparse<40,
   "sparse native updates build bounded interpolation headroom and keep rendering forward");
  var yaw90=new Quaternion(0,(float)Math.Sin(Math.PI/4),0,(float)Math.Cos(Math.PI/4));
  var turn=new VehicleRemoteMotion();turn.Push(0,new Vector3(),q,new Vector3(),true);turn.Push(.1f,new Vector3(),yaw90,new Vector3(),false);
  turn.Evaluate(.17f,out var turnPosition,out var halfway);
  Check(Math.Abs(halfway.y-Math.Sin(Math.PI/8))<.001,"orientation interpolates with actual quaternion math instead of a no-op test double");
  var lateTurn=new VehicleRemoteMotion();lateTurn.Push(0,new Vector3(),q,v,true);lateTurn.Push(.1f,new Vector3(1,0,0),q,v,false);
  for(int frame=0;frame<20;frame++)lateTurn.Evaluate(frame*.02f,out var p,out var r);
  lateTurn.Push(.4f,new Vector3(1.3f,0,0),yaw90,new Vector3(),false);lateTurn.Evaluate(.4f,out var correctedPosition,out var correctedRotation);
  Check(2*Math.Acos(Math.Min(1,Math.Abs(correctedRotation.w)))*180/Math.PI<15,"late turning packet reconciles orientation without a one-frame angular snap");
  lateTurn.Evaluate(1.4f,out var finalPosition,out var finalRotation);
  Check(Math.Abs(finalPosition.x-1.3f)<.001&&Math.Abs(finalRotation.y-yaw90.y)<.001,"late stopping turn reaches the exact final pose after correction ends");
  lateTurn.Push(2,new Vector3(100,0,0),q,new Vector3(),true);lateTurn.Evaluate(2,out var recoveredPosition,out var recoveredRotation);
  Check(recoveredPosition.x==100&&recoveredRotation.w==1&&lateTurn.InterpolationDelay==.12f,"driver/recovery reset discards adaptive delay and all extrapolation correction");
  Console.WriteLine("Replay peaks: gaps="+peakStep+" m, stop="+stopJump+" m, sparse="+sparseStep+" m per 20 ms frame.");
  Console.WriteLine(checks+" remote position buffer checks passed. Quaternion interpolation and rendered motion need Unity acceptance.");
 }
}
