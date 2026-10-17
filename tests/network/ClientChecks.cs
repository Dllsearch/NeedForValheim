using System;
using System.Collections.Generic;
using System.Linq;
using NeedForValheim;
static class ClientChecks {
 static int checks;static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static int Request(ZRoutedRpc.Sent sent){sent.Package.SetPos(0);sent.Package.ReadByte();sent.Package.ReadInt();return sent.Package.ReadInt();}
 static ZPackage Reply(int id,bool ok=true,int version=VehicleNetworkProtocol.Version){var p=new ZPackage();p.Write((byte)128);p.Write(version);p.Write(id);p.Write(ok);p.Write("result");p.Write(new ZDOID(123));return p;}
 public static void Run() {
  var logs=new List<string>();var client=new VehicleNetworkClient(logs.Add);var rpc=ZRoutedRpc.instance;
  client.Tick();int hello=Request(rpc.sent.Last());
  Check(!client.Ready&&rpc.sent.Last().Peer==88&&rpc.sent.Last().Route==VehicleNetworkProtocol.Rpc,"hello targets native server request route");
  rpc.Deliver(99,VehicleNetworkProtocol.ReplyRpc,Reply(hello));Check(!client.Ready,"spoofed server reply rejected");
  rpc.Deliver(88,VehicleNetworkProtocol.ReplyRpc,Reply(hello,version:99));Check(!client.Ready,"incompatible protocol reply rejected");
  rpc.Deliver(88,VehicleNetworkProtocol.ReplyRpc,new ZPackage());Check(!client.Ready&&logs.Count==1,"truncated reply rejected without consuming pending hello");
  rpc.Deliver(88,VehicleNetworkProtocol.ReplyRpc,Reply(hello));Check(client.Ready,"matching server version completes handshake");
  int callbacks=0;bool success=false;client.Send(VehicleNetworkProtocol.Command.Claim,new ZDOID(123),null,(ok,m,id)=>{callbacks++;success=ok;});
  int claim=Request(rpc.sent.Last());rpc.Deliver(88,VehicleNetworkProtocol.ReplyRpc,Reply(claim));rpc.Deliver(88,VehicleNetworkProtocol.ReplyRpc,Reply(claim));
  Check(callbacks==1&&success,"duplicate replies complete a request only once");
  client.Send(VehicleNetworkProtocol.Command.Claim,new ZDOID(123),null,(ok,m,id)=>{callbacks++;success=ok;});int timed=Request(rpc.sent.Last());
  UnityEngine.Time.realtimeSinceStartupAsDouble=6;rpc.Deliver(88,VehicleNetworkProtocol.ReplyRpc,Reply(timed));
  Check(callbacks==2&&!success,"late reply cannot grant an expired request");
  client.Tick();int heartbeat=Request(rpc.sent.Last());Check(client.Ready,"ready client continues server heartbeat");
  UnityEngine.Time.realtimeSinceStartupAsDouble=12;client.Tick();Check(!client.Ready,"missing server heartbeat revokes readiness");
  rpc.Deliver(88,VehicleNetworkProtocol.ReplyRpc,Reply(heartbeat));Check(!client.Ready,"timed-out heartbeat cannot reconnect through a stale reply");
  int latest=Request(rpc.sent.Last());rpc.Deliver(88,VehicleNetworkProtocol.ReplyRpc,Reply(latest));Check(client.Ready,"new valid heartbeat recovers connection");
  client.Send(VehicleNetworkProtocol.Command.Remove,new ZDOID(123),null,(ok,m,id)=>{callbacks++;success=ok;});
  ZDOMan.instance=new ZDOMan();client.Tick();Check(callbacks==3&&!success&&!client.Ready,"world switch cancels pending requests and readiness");
  int next=Request(rpc.sent.Last());rpc.Deliver(88,VehicleNetworkProtocol.ReplyRpc,Reply(next));Check(client.Ready,"new world requires its own server handshake");
  ZNet.IsSinglePlayer=true;client.Tick();Check(!client.Ready&&client.Server==0,"singleplayer clears network session without native requests");
  ZNet.IsSinglePlayer=false;ZNet.instance.host=true;client.Tick();
  int hostHello=Request(rpc.sent.Last());Check(rpc.sent.Last().Peer==ZNet.GetUID(),"host handshake targets its own server UID");
  rpc.Deliver(ZNet.GetUID(),VehicleNetworkProtocol.ReplyRpc,Reply(hostHello));Check(client.Ready,"host reply uses a separate route from server requests");
  int denied=0;
  for(int i=0;i<17;i++)client.Send(VehicleNetworkProtocol.Command.Claim,new ZDOID(i),null,(ok,m,id)=>{if(!ok)denied++;});
  Check(denied==1,"pending request count bounded to sixteen");
  client.Reset("test finished");Check(denied==17&&!client.Ready,"reset resolves each pending callback exactly once");
  Check(rpc.routes.Count==1,"world changes do not duplicate reply route registration");
  Console.WriteLine(checks+" client transport checks passed with native API doubles.");
 }
}
