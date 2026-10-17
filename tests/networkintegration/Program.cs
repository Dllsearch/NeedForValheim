using System;
using System.Collections.Generic;
using System.Linq;
using NeedForValheim;
using UnityEngine;
class Program {
 static bool clearPlacement=true;static int checks;static ZNet server;static VehicleNetworkServer broker;static List<string> logs=new List<string>();
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
 static void As(ZNet net,Action action){var old=ZNet.instance;try{ZNet.instance=net;action();}finally{ZNet.instance=old;}}
 static bool NativeOpen(Container container,long peer,long player){bool allowed=false;As(server,()=>allowed=broker.AllowsNativeCargoOpen(container,peer,player));return allowed;}
 static void Wait(double seconds=.3){Time.realtimeSinceStartupAsDouble+=seconds;As(server,()=>broker.Tick());}
 static (ZNet,VehicleNetworkClient) Client(long uid,long player,Vector3 position) {
  var character=ZDOMan.instance.Create(position);character.Set(ZDOVars.s_playerID,player);var peer=new ZNetPeer{m_uid=uid,m_playerID=0,m_characterID=character.m_uid};server.peers[uid]=peer;
  var net=new ZNet{uid=uid,serverPeer=new ZNetPeer{m_uid=10}};ZRoutedRpc.instance.endpoints[uid]=net;
  var client=new VehicleNetworkClient(logs.Add);As(net,()=>client.Tick());Check(client.Ready,"client "+uid+" completes serialized native-style hello/reply");return(net,client);
 }
 static (bool,ZDOID) Send((ZNet net,VehicleNetworkClient client) c,VehicleNetworkProtocol.Command command,ZDOID id,Action<ZPackage> payload=null) {
  bool completed=false,ok=false;ZDOID result=ZDOID.None;
  As(c.net,()=>c.client.Send(command,id,payload,(success,message,target)=>{completed=true;ok=success;result=target;}));
  if(command!=VehicleNetworkProtocol.Command.State)Check(completed,"command "+command+" receives correlated serialized reply");return(ok,result);
 }
 static ZDO Spawn((ZNet,VehicleNetworkClient) client,string profile,Vector3 position) {
  Wait();var result=Send(client,VehicleNetworkProtocol.Command.Spawn,ZDOID.None,p=>{p.Write(profile);p.Write("{}");p.Write(position);p.Write(Quaternion.identity);});
  Check(result.Item1,"server accepts available nearby catalog profile");return ZDOMan.instance.GetZDO(result.Item2);
 }
 static void State((ZNet,VehicleNetworkClient) c,ZDOID id,int sequence,Vector3 position,float speed=10) {
  Send(c,VehicleNetworkProtocol.Command.State,id,p=>{p.Write(sequence);p.Write(position);p.Write(Quaternion.identity);p.Write(new Vector3(speed,0,0));p.Write(12f);p.Write(90f);p.Write(true);new VehicleMotionTelemetry{Rpm=4500,Load=.8f,Charge=.5f,Gear=3,Running=true}.Write(p);});
 }
 static void Main(string[] args) {
  var catalog=CarCatalog.Load(args[0]);server=new ZNet{uid=10,server=true};ZRoutedRpc.instance.endpoints[10]=server;int hidden=0;
  broker=new VehicleNetworkServer(catalog,logs.Add,c=>hidden++,(profile,pos,rot)=>clearPlacement);As(server,()=>broker.Tick());
  var first=Client(22,2200,Vector3.zero);var second=Client(33,3300,Vector3.zero);
  server.peers[22].m_playerID=0;
  Check(first.Item2.Ready,"native player-ID peer cache is not required for protocol readiness");
  var car=Spawn(first,"350z",new Vector3(1,1,0));
  Check(car!=null&&car.Persistent&&car.GetLong(VehicleNetworkProtocol.Creator)==2200&&hidden==1,"spawn creates one persistent server-owned cargo identity");
  Wait();Check(Send(first,VehicleNetworkProtocol.Command.Setup,car.m_uid,p=>{p.Write(0);p.Write("{\"Styling/Paint\":\"3\",\"Showroom/TrunkLayout\":\"0\",\"Driving/TerrainMode\":\"false\"}");}).Item1,"parked owner can publish setup");
  Check(car.GetInt(VehicleNetworkProtocol.SetupRevision)==1&&VehiclePersistence.Settings(car).Contains("Styling/Paint"),"server stores snapshot with a new revision");
  Check(car.GetInt(VehicleMotionTelemetry.Prefix+"groundmode")==1,"explicit parked road-mode setup becomes the next driver handover mode");
  Wait();Check(!Send(second,VehicleNetworkProtocol.Command.Setup,car.m_uid,p=>{p.Write(1);p.Write("{}");}).Item1,"foreign driver cannot overwrite vehicle setup");
  Wait();Check(!Send(first,VehicleNetworkProtocol.Command.Setup,car.m_uid,p=>{p.Write(0);p.Write("{}");}).Item1&&car.GetInt(VehicleNetworkProtocol.SetupRevision)==1,"stale setup revision cannot replace a newer snapshot");
  Wait();Check(Send(first,VehicleNetworkProtocol.Command.Claim,car.m_uid).Item1&&car.GetLong(VehicleNetworkProtocol.Driver)==22,"server grants the first driver");
  Check(!Send(second,VehicleNetworkProtocol.Command.Claim,car.m_uid).Item1,"second peer cannot claim the occupied car");
  Wait();Check(!Send(first,VehicleNetworkProtocol.Command.Recover,car.m_uid,p=>{p.Write(new Vector3(2,1,0));p.Write(Quaternion.identity);}).Item1,"recovery cannot teleport a vehicle occupied by its creator");
  Wait(.1);State(first,car.m_uid,0,new Vector3(2,1,0));
  Check(VehiclePersistence.Location(car).x==2&&car.GetBool(VehicleNetworkProtocol.Boost),"holder state reaches pose and effect metadata through both production handlers");
  Check(car.GetPosition().y==.15f&&car.GetPosition().z<0,"cargo anchor follows grounded trunk while car pose stays exact");
  Wait(.1);State(second,car.m_uid,0,new Vector3(5,1,0));Check(VehiclePersistence.Location(car).x==2,"foreign state cannot move an occupied car");
  Wait(.1);State(first,car.m_uid,0,new Vector3(6,1,0));Check(VehiclePersistence.Location(car).x==2,"duplicate state sequence cannot move the car");
  Wait(.1);State(first,car.m_uid,1,new Vector3(1000,1,0));Check(VehiclePersistence.Location(car).x==2,"teleport packet rejected before persistent pose writes");
  Wait(.1);var originalCharacter=server.peers[22].m_characterID;server.peers[22].m_characterID=ZDOMan.instance.Create(Vector3.zero).m_uid;
  State(first,car.m_uid,1,new Vector3(3,1,0));Check(VehiclePersistence.Location(car).x==2,"a replacement character cannot publish motion through the old driver grant");server.peers[22].m_characterID=originalCharacter;
  var motion=new VehicleMotionTelemetry{Rpm=5200,Load=.7f,Gear=4,Charge=.49f,Flying=true,Landing=true,GroundMode=1,Fold=.6f,Pitch=12,Roll=-8,Thrust=.8f,Running=true,Horn=true};
  motion.Wheels[0]=new Vector3(-1,-.2f,1.6f);motion.Rotations[0]=Quaternion.Euler(0,18,0);motion.WheelRpm[0]=450;
  Wait(.1);Send(first,VehicleNetworkProtocol.Command.State,car.m_uid,p=>{p.Write(1);p.Write(new Vector3(2,1,0));p.Write(Quaternion.identity);p.Write(new Vector3(10,0,0));p.Write(12f);p.Write(90f);p.Write(true);motion.Write(p);});
  Check(car.GetFloat(VehicleMotionTelemetry.Prefix+"rpm")==5200&&car.GetInt(VehicleMotionTelemetry.Prefix+"gear")==4&&car.GetFloat(VehicleMotionTelemetry.Prefix+"fold")==.6f,"engine and flight telemetry survives the full serialized handler path");
  Check(car.GetBool(VehicleMotionTelemetry.Prefix+"landing")&&car.GetInt(VehicleMotionTelemetry.Prefix+"groundmode")==1,"landing and ground mode survive the serialized handoff metadata");
  var invalidMode=new VehicleMotionTelemetry{GroundMode=3};Check(!invalidMode.Valid(),"unsupported ground mode is rejected before state mutation");
  Check(!new VehicleMotionTelemetry{Landing=true,Flying=false}.Valid(),"landing cannot be reported by a grounded controller");
  Check(car.GetVec3(VehicleMotionTelemetry.Prefix+"wheel0",Vector3.zero).y==-.2f&&car.GetFloat(VehicleMotionTelemetry.Prefix+"wheelrpm0")==450,"individual wheel suspension and spin telemetry retained");
  motion.Charge=1;Wait(.05);Send(first,VehicleNetworkProtocol.Command.State,car.m_uid,p=>{p.Write(2);p.Write(new Vector3(3,1,0));p.Write(Quaternion.identity);p.Write(new Vector3(10,0,0));p.Write(12f);p.Write(90f);p.Write(true);motion.Write(p);});
  Check(VehiclePersistence.Location(car).x==2&&car.GetFloat(VehicleMotionTelemetry.Prefix+"charge")==.49f,"driver handoff cannot immediately refill a partially depleted tank");
  Wait();Check(!Send(first,VehicleNetworkProtocol.Command.Setup,car.m_uid,p=>{p.Write(1);p.Write("{}");}).Item1,"moving driver's setup edits are rejected");
  Wait(.3);Check(!Send(first,VehicleNetworkProtocol.Command.Remove,car.m_uid).Item1,"owner cannot remove an occupied car");
  Check(Send(first,VehicleNetworkProtocol.Command.Release,car.m_uid).Item1&&car.GetLong(VehicleNetworkProtocol.Driver)==0,"release bypasses command throttle and frees driver immediately");
  car.cargoStacks=19;Wait();Check(!Send(first,VehicleNetworkProtocol.Command.Setup,car.m_uid,p=>{p.Write(1);p.Write("{\"Showroom/TrunkLayout\":\"2\"}");}).Item1,"overfull cargo blocks a capacity-reducing setup");
  Check(!car.GetBool(VehicleMotionTelemetry.Prefix+"horn")&&!car.GetBool(VehicleNetworkProtocol.Boost),"release clears transient horn and boost state");
  Check(car.cargoStacks==19&&car.GetInt(VehicleNetworkProtocol.SetupRevision)==1&&VehiclePersistence.Trunk(car)==0,"rejected resize preserves cargo and committed setup");
  car.cargoStacks=3;Wait();Check(Send(first,VehicleNetworkProtocol.Command.Setup,car.m_uid,p=>{p.Write(1);p.Write("{\"Showroom/TrunkLayout\":\"2\"}");}).Item1,"legal trunk resize can be committed");
  Check(car.GetInt(VehicleNetworkProtocol.SetupRevision)==2&&VehiclePersistence.Trunk(car)==2&&ZNetScene.instance.FindInstance(car.m_uid).GetComponent<Container>().GetInventory().GetHeight()==2,"accepted resize updates native capacity and snapshot revision together");
  var nativeCargo=ZNetScene.instance.FindInstance(car.m_uid).GetComponent<Container>();
  Check(!NativeOpen(nativeCargo,22,2200),"native cargo-open RPC cannot bypass the custom access grant");
  Wait();Check(Send(first,VehicleNetworkProtocol.Command.CargoOpen,car.m_uid).Item1,"parked creator receives cargo access");
  Check(NativeOpen(nativeCargo,22,2200)&&!NativeOpen(nativeCargo,33,2200),"native open validates both sender connection and persistent player");
  Wait();Check(!Send(first,VehicleNetworkProtocol.Command.Recover,car.m_uid,p=>{p.Write(new Vector3(2,1,0));p.Write(Quaternion.identity);}).Item1,"recovery cannot move an open native cargo inventory");
  car.SetOwner(22);Wait(.2);Check(car.Owner==22,"maintenance preserves native client ownership during cargo access");
  Wait();Check(!Send(second,VehicleNetworkProtocol.Command.CargoOpen,car.m_uid).Item1,"foreign cargo request cannot steal access");
  Wait();Check(!Send(second,VehicleNetworkProtocol.Command.Claim,car.m_uid).Item1,"vehicle cannot be driven while cargo access is active");
  Wait();Check(!Send(first,VehicleNetworkProtocol.Command.Setup,car.m_uid,p=>{p.Write(2);p.Write("{}");}).Item1,"setup cannot mutate capacity while a remote inventory is open");
  Wait();Check(!Send(first,VehicleNetworkProtocol.Command.Remove,car.m_uid).Item1,"cargo access protects the vehicle from removal");
  byte[] finalItems={0,1,127,128,255};string finalHash=VehicleNetworkProtocol.InventoryHash(finalItems);bool closed=false;
  As(first.Item1,()=>first.Item2.Send(VehicleNetworkProtocol.Command.CargoClose,car.m_uid,p=>p.Write(finalHash),(ok,message,id)=>{closed=ok;}));
  Wait(.2);Check(!closed&&car.Owner==22,"close does not reclaim ownership before final native item bytes arrive");
  car.inventoryBytes=finalItems;Wait(.2);
  Check(closed&&car.Owner==10&&car.GetLong(VehicleNetworkProtocol.CargoPeer)==0&&car.GetString(VehicleNetworkProtocol.CargoCommit)==finalHash&&car.inventoryBytes.SequenceEqual(finalItems),"matching native inventory snapshot completes close without cloning item bytes");
  Wait();Check(Send(first,VehicleNetworkProtocol.Command.CargoOpen,car.m_uid).Item1,"cargo can reopen after a completed handoff");
  Check(Send(first,VehicleNetworkProtocol.Command.CargoClose,car.m_uid,p=>p.Write("")).Item1&&car.Owner==10&&car.GetLong(VehicleNetworkProtocol.CargoPeer)==0,"unused native cargo access can cancel without waiting for a stale inventory hash");
  Wait();Check(Send(first,VehicleNetworkProtocol.Command.CargoOpen,car.m_uid).Item1,"cargo access can restart after unused grant cancellation");
  car.SetOwner(22);server.peers[22].ready=false;Wait(.2);
  Check(car.Owner==10&&car.GetLong(VehicleNetworkProtocol.CargoPeer)==0&&car.inventoryBytes.SequenceEqual(finalItems),"disconnect recovers cargo ownership while retaining last replicated inventory");server.peers[22].ready=true;
  Wait();Check(Send(second,VehicleNetworkProtocol.Command.Claim,car.m_uid).Item1,"second driver can acquire after release");
  Check(Send(second,VehicleNetworkProtocol.Command.Release,car.m_uid).Item1,"new holder releases its own lease");
  Wait();Check(!Send(second,VehicleNetworkProtocol.Command.Remove,car.m_uid).Item1&&VehiclePersistence.IsVehicle(car),"driver permissions do not transfer creator deletion rights");
  Wait();Check(!Send(second,VehicleNetworkProtocol.Command.Recover,car.m_uid,p=>{p.Write(new Vector3(2,1,0));p.Write(Quaternion.identity);}).Item1,"foreign driver cannot recover the creator vehicle");
  Wait();Check(!Send(first,VehicleNetworkProtocol.Command.Recover,car.m_uid,p=>{p.Write(new Vector3(1000,1,0));p.Write(Quaternion.identity);}).Item1,"recovery rejects positions far from the authenticated character");
  clearPlacement=false;Wait();Check(!Send(first,VehicleNetworkProtocol.Command.Recover,car.m_uid,p=>{p.Write(new Vector3(2,1,0));p.Write(Quaternion.identity);}).Item1,"server rejects unsupported or obstructed recovery ground");clearPlacement=true;
  car.Set(VehicleMotionTelemetry.Prefix+"flying",true);car.Set(VehicleMotionTelemetry.Prefix+"fold",1f);
  var settingsBeforeRecovery=VehiclePersistence.Settings(car);var chargeBeforeRecovery=car.GetFloat(VehicleMotionTelemetry.Prefix+"charge");var itemsBeforeRecovery=car.inventoryBytes;
  Wait();Check(Send(first,VehicleNetworkProtocol.Command.Recover,car.m_uid,p=>{p.Write(new Vector3(2,1,0));p.Write(Quaternion.identity);}).Item1,"owner recovers the vacant vehicle at validated nearby ground");
  Check(VehiclePersistence.Location(car).x==2&&!car.GetBool(VehicleMotionTelemetry.Prefix+"flying")&&car.GetFloat(VehicleMotionTelemetry.Prefix+"fold")==0,"recovery grounds the original persistent vehicle");
  Check(VehiclePersistence.Settings(car)==settingsBeforeRecovery&&car.GetFloat(VehicleMotionTelemetry.Prefix+"charge")==chargeBeforeRecovery&&car.inventoryBytes.SequenceEqual(itemsBeforeRecovery),"recovery preserves setup fuel and native inventory bytes without a new identity");
  car.cargoStacks=3;Wait();Check(Send(first,VehicleNetworkProtocol.Command.Remove,car.m_uid).Item1,"creator can remove the vacant car");
  Check(!VehiclePersistence.IsVehicle(car)&&!car.deleted&&car.cargoStacks==3,"removal retains original cargo record and inventory");
  clearPlacement=false;Wait();Check(!Send(first,VehicleNetworkProtocol.Command.Spawn,ZDOID.None,p=>{p.Write("350z");p.Write("{}");p.Write(new Vector3(1,1,0));p.Write(Quaternion.identity);}).Item1,"server independently rejects an unsupported spawn before creating an anchor");clearPlacement=true;
  var profiles=catalog.Profiles.Take(5).ToArray();var owned=new List<ZDO>();
  foreach(var profile in profiles.Take(4))owned.Add(Spawn(first,profile.id,new Vector3(1,1,0)));
  Wait();var fifth=Send(first,VehicleNetworkProtocol.Command.Spawn,ZDOID.None,p=>{p.Write(profiles[4].id);p.Write("{}");p.Write(new Vector3(1,1,0));p.Write(Quaternion.identity);});
  Check(!fifth.Item1,"server garage limit includes all four persistent owned records");
  Wait(1.1);Check(Send(first,VehicleNetworkProtocol.Command.Roster,ZDOID.None).Item1&&first.Item2.OwnedVehicles.Count==4,"server roster includes all four owned cars independently of client zone visibility");
  Check(first.Item2.OwnedVehicles.All(v=>owned.Any(d=>d.m_uid.Equals(v.Id))),"roster identities refer to the original persistent cargo records");
  var another=Spawn(second,profiles[0].id,new Vector3(1,1,0));Check(another.GetLong(VehicleNetworkProtocol.Creator)==3300,"another player may own the same model independently");
  Wait(1.1);Send(second,VehicleNetworkProtocol.Command.Roster,ZDOID.None);
  Check(second.Item2.OwnedVehicles.Count==1&&second.Item2.OwnedVehicles[0].Id.Equals(another.m_uid),"roster cannot expose another player's garage ownership");
  Wait();var duplicate=Send(second,VehicleNetworkProtocol.Command.Spawn,ZDOID.None,p=>{p.Write(profiles[0].id);p.Write("{}");p.Write(new Vector3(1,1,0));p.Write(Quaternion.identity);});Check(!duplicate.Item1,"duplicate model rejected within one owner garage");
  Wait();Check(Send(first,VehicleNetworkProtocol.Command.Claim,owned[0].m_uid).Item1,"owned car can be driven after a later request");
  var respawnedCharacter=ZDOMan.instance.Create(Vector3.zero);respawnedCharacter.Set(ZDOVars.s_playerID,2200L);server.peers[22].m_characterID=respawnedCharacter.m_uid;Wait(.2);
  Check(owned[0].GetLong(VehicleNetworkProtocol.Driver)==0&&owned[0].GetZDOID(VehicleNetworkProtocol.DriverCharacter).Equals(ZDOID.None),"respawn character replacement revokes the old seat during server maintenance");
  Wait();Check(Send(first,VehicleNetworkProtocol.Command.Claim,owned[0].m_uid).Item1,"replacement character can request a fresh driver grant");
  server.peers[22].ready=false;Wait(.2);Check(owned[0].GetLong(VehicleNetworkProtocol.Driver)==0,"disconnect maintenance clears persistent driver marker");
  Wait();Check(Send(second,VehicleNetworkProtocol.Command.Claim,owned[0].m_uid).Item1,"disconnected driver does not strand the seat");
  Wait(4.1);Check(owned[0].GetLong(VehicleNetworkProtocol.Driver)==0,"missing movement heartbeat releases a silent driver");
  Wait();Check(Send(second,VehicleNetworkProtocol.Command.Claim,owned[0].m_uid).Item1,"authorized driver can be used for malformed-payload dispatch checks");
  Wait();var malformed=new ZPackage();malformed.Write((byte)VehicleNetworkProtocol.Command.State);malformed.Write(VehicleNetworkProtocol.Version);malformed.Write(999);malformed.Write(owned[0].m_uid);
  As(second.Item1,()=>ZRoutedRpc.instance.InvokeRoutedRPC(10,VehicleNetworkProtocol.Rpc,malformed));Check(logs.Any(l=>l.StartsWith("Rejected malformed vehicle RPC")),"truncated state handled without crashing routed dispatcher");
  ZRoutedRpc.instance=new ZRoutedRpc();ZRoutedRpc.instance.endpoints[10]=server;broker=new VehicleNetworkServer(catalog,logs.Add,c=>hidden++,(profile,pos,rot)=>clearPlacement);Wait();
  var rejoined=Client(55,2200,Vector3.zero);Wait();Check(Send(rejoined,VehicleNetworkProtocol.Command.Claim,owned[0].m_uid).Item1,"new connection can drive its persistent player's retained vehicle");
  Wait(1.1);Send(rejoined,VehicleNetworkProtocol.Command.Roster,ZDOID.None);Check(rejoined.Item2.OwnedVehicles.Count==4,"server roster survives connection UID changes");
  Wait();Check(Send(rejoined,VehicleNetworkProtocol.Command.Release,owned[0].m_uid).Item1,"rejoined driver can release");
  Check(Send(rejoined,VehicleNetworkProtocol.Command.Remove,owned[0].m_uid).Item1,"deletion ownership survives peer UID change");
  Check(rejoined.Item2.OwnedVehicles.Count==3&&rejoined.Item2.WasRemoved(owned[0].m_uid),"successful removal immediately updates roster and suppresses stale visual respawn");
  Console.WriteLine(checks+" serialized client/server integration checks passed with native API doubles. No Unity scene, sockets, disk world save or avatar control executed.");
 }
}
