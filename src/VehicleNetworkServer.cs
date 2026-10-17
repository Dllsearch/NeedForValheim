using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
namespace NeedForValheim {
internal sealed class VehicleNetworkServer {
    readonly CarCatalog catalog;
    readonly Action<string> log;
    readonly Action<Container> hide;
    readonly Func<CarProfile,Vector3,Quaternion,bool> placement;
    readonly VehicleNetworkLeases leases=new VehicleNetworkLeases();
    readonly VehicleNetworkLeases cargoLeases=new VehicleNetworkLeases();
    sealed class CargoClose {public long Peer;public int Request;public string Hash;}
    readonly Dictionary<ZDOID,CargoClose> cargoClosing=new Dictionary<ZDOID,CargoClose>();
    readonly List<ZDOID> records=new List<ZDOID>();
    readonly Dictionary<long,double> requests=new Dictionary<long,double>(),updates=new Dictionary<long,double>(),queries=new Dictionary<long,double>();
    ZRoutedRpc router;ZDOMan world;double nextMaintenance;
    public VehicleNetworkServer(CarCatalog catalog,Action<string> log,Action<Container> hide,Func<CarProfile,Vector3,Quaternion,bool> placement=null){this.catalog=catalog;this.log=log;this.hide=hide;this.placement=placement;}
    public void Tick() {
        if(ZNet.instance==null||!ZNet.instance.IsServer())return;
        if(router!=ZRoutedRpc.instance&&ZRoutedRpc.instance!=null){router=ZRoutedRpc.instance;router.Register<ZPackage>(VehicleNetworkProtocol.Rpc,Receive);}
        if(world!=ZDOMan.instance) {
            world=ZDOMan.instance;nextMaintenance=0;records.Clear();leases.Clear();cargoLeases.Clear();cargoClosing.Clear();requests.Clear();updates.Clear();queries.Clear();
            if(world!=null)foreach(var id in ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.String,VehiclePersistence.Model.GetStableHashCode()))
                if(VehiclePersistence.IsVehicle(world.GetZDO(id)))records.Add(id);
            if(world!=null)foreach(var id in ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Int,VehiclePersistence.LegacyModel.GetStableHashCode()))
                if(!records.Contains(id)&&VehiclePersistence.IsVehicle(world.GetZDO(id)))records.Add(id);
        }
        double now=Time.realtimeSinceStartupAsDouble;
        if(now<nextMaintenance)return;nextMaintenance=now+.1;
        foreach(var peer in requests.Keys.Where(id=>!Connected(id)).ToArray())requests.Remove(peer);
        foreach(var peer in updates.Keys.Where(id=>!Connected(id)).ToArray())updates.Remove(peer);
        foreach(var peer in queries.Keys.Where(id=>!Connected(id)).ToArray())queries.Remove(peer);
        if(world!=null)records.RemoveAll(id=>!VehiclePersistence.IsVehicle(world.GetZDO(id)));
        foreach(var pair in cargoClosing.ToArray()) {
            var data=world?.GetZDO(pair.Key);var close=pair.Value;
            if(!VehiclePersistence.IsVehicle(data)){cargoClosing.Remove(pair.Key);continue;}
            if(VehicleNetworkProtocol.InventoryHash(data.GetByteArray(ZDOVars.s_items,null))!=close.Hash)continue;
            cargoLeases.Release(pair.Key.ToString(),close.Peer);cargoClosing.Remove(pair.Key);
            data.Set(VehicleNetworkProtocol.CargoCommit,close.Hash);data.SetOwner(ZDOMan.GetSessionID());data.Set(VehicleNetworkProtocol.CargoPeer,0L);
            Reply(close.Peer,close.Request,true,"Cargo saved and closed",pair.Key);
        }
        cargoLeases.Expire(now,Connected);
        leases.Expire(now,Connected);
        if(world==null)return;
        foreach(var id in records) {
            var data=world.GetZDO(id);if(!VehiclePersistence.IsVehicle(data))continue;
            bool cargoActive=cargoLeases.Active(id.ToString(),now);
            if(!cargoActive&&!data.IsOwner())data.SetOwner(ZDOMan.GetSessionID());
            if(!cargoActive&&data.GetLong(VehicleNetworkProtocol.CargoPeer)!=0) {
                data.Set(VehicleNetworkProtocol.CargoPeer,0L);
                if(cargoClosing.TryGetValue(id,out var closing)){Reply(closing.Peer,closing.Request,false,"Cargo connection ended; last replicated inventory retained",id);cargoClosing.Remove(id);}
            }
            long driver=data.GetLong(VehicleNetworkProtocol.Driver);
            if(driver!=0&&(!leases.Active(id.ToString(),now)||!DriverCharacterMatches(data,driver))){leases.Release(id.ToString(),driver);data.Set(VehicleNetworkProtocol.Driver,0L);data.Set(VehicleNetworkProtocol.DriverCharacter,ZDOID.None);data.Set(VehicleNetworkProtocol.Velocity,Vector3.zero);VehicleMotionTelemetry.StopEffects(data);}
        }
    }
    void Reply(long peer,int request,bool ok,string message,ZDOID id) {
        var p=new ZPackage();p.Write((byte)VehicleNetworkProtocol.Command.Reply);p.Write(VehicleNetworkProtocol.Version);p.Write(request);p.Write(ok);p.Write(message);p.Write(id);
        router.InvokeRoutedRPC(peer,VehicleNetworkProtocol.ReplyRpc,p);
    }
    void Receive(long sender,ZPackage p) {
        if(ZNet.instance==null||!ZNet.instance.IsServer()||world==null||world!=ZDOMan.instance||p==null||p.Size()>VehicleNetworkProtocol.MaxPacket)return;
        int request=0;
        try {
            var peer=ZNet.instance.GetPeer(sender);
            bool host=Host(sender);
            if(!host&&(peer==null||!peer.IsReady()))return;
            var characterId=host?ZNet.instance.LocalPlayerCharacterID:peer.m_characterID;
            var command=(VehicleNetworkProtocol.Command)p.ReadByte();int version=p.ReadInt();request=p.ReadInt();
            if(version!=VehicleNetworkProtocol.Version){Reply(sender,request,false,"Vehicle protocol version mismatch",ZDOID.None);return;}
            if(command==VehicleNetworkProtocol.Command.Hello){Reply(sender,request,true,"NFVU2 server protocol "+VehicleNetworkProtocol.Version,ZDOID.None);return;}
            double now=Time.realtimeSinceStartupAsDouble;
            // State has its own 30 Hz cap; commands are limited to 4 per second.
            double interval=command==VehicleNetworkProtocol.Command.State?1.0/30:command==VehicleNetworkProtocol.Command.Roster?1:.25;
            var limiter=command==VehicleNetworkProtocol.Command.State?updates:command==VehicleNetworkProtocol.Command.Roster?queries:requests;
            bool urgent=command==VehicleNetworkProtocol.Command.Release||command==VehicleNetworkProtocol.Command.CargoClose||command==VehicleNetworkProtocol.Command.CargoKeep;
            if(!urgent&&limiter.TryGetValue(sender,out var previous)&&now-previous<interval){if(command!=VehicleNetworkProtocol.Command.State)Reply(sender,request,false,"Vehicle command rate exceeded",ZDOID.None);return;}
            if(!urgent)limiter[sender]=now;
            var id=p.ReadZDOID();
            var character=world.GetZDO(characterId);
            long player=host?Player.m_localPlayer.GetPlayerID():character?.GetLong(ZDOVars.s_playerID,0L)??0;
            if(character==null||player==0){Reply(sender,request,false,"Player character data is still loading",id);return;}
            if(command==VehicleNetworkProtocol.Command.Roster) {
                var owned=records.Select(world.GetZDO).Where(d=>VehiclePersistence.IsVehicle(d)&&d.GetLong(VehicleNetworkProtocol.Creator)==player).Take(4).ToArray();
                var reply=new ZPackage();reply.Write((byte)VehicleNetworkProtocol.Command.RosterReply);reply.Write(VehicleNetworkProtocol.Version);reply.Write(request);reply.Write(owned.Length);
                foreach(var record in owned){reply.Write(record.m_uid);reply.Write(VehiclePersistence.Profile(record));reply.Write(VehiclePersistence.Location(record));reply.Write(record.GetLong(VehicleNetworkProtocol.Driver));}
                router.InvokeRoutedRPC(sender,VehicleNetworkProtocol.ReplyRpc,reply);return;
            }
            if(command==VehicleNetworkProtocol.Command.Spawn){Spawn(sender,player,character.GetPosition(),request,p);return;}
            var data=world.GetZDO(id);
            if(!VehiclePersistence.IsVehicle(data)){Reply(sender,request,false,"Vehicle no longer exists",id);return;}
            string key=id.ToString();
            if(command==VehicleNetworkProtocol.Command.CargoOpen) {
                var root=ZNetScene.instance.FindInstance(id);var container=root!=null?root.GetComponent<Container>():null;
                bool allowed=data.GetLong(VehicleNetworkProtocol.Creator)==player&&!leases.Active(key,now)&&!leases.HasPeer(sender,now)&&
                    !cargoClosing.ContainsKey(id)&&data.GetVec3(VehicleNetworkProtocol.Velocity,Vector3.zero).magnitude<.5f&&
                    Vector3.Distance(character.GetPosition(),data.GetPosition())<=4&&container!=null&&!container.IsInUse();
                bool ok=allowed&&cargoLeases.Claim(key,sender,player,now);
                if(ok)data.Set(VehicleNetworkProtocol.CargoPeer,sender);
                Reply(sender,request,ok,ok?"Cargo access granted":"Park your own vehicle nearby and close other cargo first",id);return;
            }
            if(command==VehicleNetworkProtocol.Command.CargoKeep) {
                bool ok=cargoLeases.HeldBy(key,sender,now)&&cargoLeases.Claim(key,sender,player,now);
                Reply(sender,request,ok,ok?"Cargo access renewed":"Cargo access expired",id);return;
            }
            if(command==VehicleNetworkProtocol.Command.CargoClose) {
                string hash=p.ReadString();
                if(hash.Length==0&&data.IsOwner()&&cargoLeases.HeldBy(key,sender,now)) {
                    cargoLeases.Release(key,sender);cargoClosing.Remove(id);data.Set(VehicleNetworkProtocol.CargoPeer,0L);
                    Reply(sender,request,true,"Unused cargo access released",id);return;
                }
                if(hash.Length!=64||hash.Any(c=>!((c>='0'&&c<='9')||(c>='a'&&c<='f')))||!cargoLeases.HeldBy(key,sender,now)){Reply(sender,request,false,"Invalid or expired cargo close",id);return;}
                cargoClosing[id]=new CargoClose{Peer=sender,Request=request,Hash=hash};cargoLeases.Claim(key,sender,player,now);return;
            }
            if(command==VehicleNetworkProtocol.Command.Recover) {
                var position=p.ReadVector3();var rotation=p.ReadQuaternion();
                var profile=catalog.Profiles.FirstOrDefault(v=>v.id==VehiclePersistence.Profile(data));
                bool allowed=data.GetLong(VehicleNetworkProtocol.Creator)==player&&!leases.Active(key,now)&&!cargoLeases.Active(key,now)&&
                    !cargoLeases.HasPeer(sender,now)&&!leases.HasPeer(sender,now)&&profile!=null&&Pose(position,rotation)&&
                    Vector3.Distance(character.GetPosition(),position)<=10&&p.GetPos()==p.Size();
                if(!allowed||placement==null||!placement(profile,position,rotation)){Reply(sender,request,false,"Recovery requires a vacant owned vehicle and clear nearby ground",id);return;}
                var nativeRoot=ZNetScene.instance?.FindInstance(id);var container=nativeRoot!=null?nativeRoot.GetComponent<Container>():null;
                if(container!=null&&container.IsInUse()){Reply(sender,request,false,"Close the cargo before recovery",id);return;}
                data.Set("nfvu2_pose_v2",position);data.Set("nfvu2_rotation_v2",rotation);
                data.SetPosition(position+rotation*new Vector3(0,.5f,-profile.rig.trunkApproachZ));data.SetRotation(rotation);
                data.Set(VehicleNetworkProtocol.Driver,0L);data.Set(VehicleNetworkProtocol.DriverCharacter,ZDOID.None);
                data.Set(VehicleNetworkProtocol.Velocity,Vector3.zero);VehicleMotionTelemetry.StopEffects(data);
                foreach(var field in new[]{"flying","landing"})data.Set(VehicleMotionTelemetry.Prefix+field,false);
                foreach(var field in new[]{"fold","pitch","roll"})data.Set(VehicleMotionTelemetry.Prefix+field,0f);
                Reply(sender,request,true,"Vehicle recovered; setup and cargo retained",id);return;
            }
            if(command==VehicleNetworkProtocol.Command.Claim) {
                bool nearby=Vector3.Distance(character.GetPosition(),VehiclePersistence.Location(data))<=6;
                bool supported=catalog.Profiles.Any(v=>v.id==VehiclePersistence.Profile(data));
                bool unowned=data.GetLong(VehicleNetworkProtocol.Creator)==0;
                bool room=!unowned||records.Select(world.GetZDO).Count(d=>VehiclePersistence.IsVehicle(d)&&d.GetLong(VehicleNetworkProtocol.Creator)==player)<4;
                bool ok=nearby&&supported&&room&&!cargoLeases.Active(key,now)&&!cargoLeases.HasPeer(sender,now)&&leases.Claim(key,sender,player,now);
                if(ok) {
                    VehiclePersistence.UpgradeLegacy(data);
                    if(unowned)data.Set(VehicleNetworkProtocol.Creator,player);
                    data.Set(VehicleNetworkProtocol.Driver,sender);data.Set(VehicleNetworkProtocol.DriverCharacter,characterId);
                }
                Reply(sender,request,ok,ok?"Driver granted":"Vehicle is occupied or out of reach",id);return;
            }
            if(command==VehicleNetworkProtocol.Command.State) {
                if(data.GetZDOID(VehicleNetworkProtocol.DriverCharacter).Equals(characterId))ApplyMotion(data,sender,now,p);return;
            }
            if(command==VehicleNetworkProtocol.Command.Release) {
                if(leases.HeldBy(key,sender,now)){if(p.GetPos()<p.Size()&&data.GetZDOID(VehicleNetworkProtocol.DriverCharacter).Equals(characterId)){try{ApplyMotion(data,sender,now,p);}catch(Exception error){log?.Invoke("Final motion ignored: "+error.GetType().Name);}}leases.Release(key,sender);data.Set(VehicleNetworkProtocol.Driver,0L);data.Set(VehicleNetworkProtocol.DriverCharacter,ZDOID.None);data.Set(VehicleNetworkProtocol.Velocity,Vector3.zero);VehicleMotionTelemetry.ReleaseEffects(data);}
                Reply(sender,request,true,"Driver released",id);return;
            }
            if(command==VehicleNetworkProtocol.Command.Setup) {
                int expected=p.ReadInt();string setup=p.ReadString();
                bool allowed=!cargoLeases.Active(key,now)&&data.GetLong(VehicleNetworkProtocol.Creator)==player&&Vector3.Distance(character.GetPosition(),VehiclePersistence.Location(data))<=10&&
                    (!leases.Active(key,now)||leases.HeldBy(key,sender,now))&&data.GetVec3(VehicleNetworkProtocol.Velocity,Vector3.zero).magnitude<.5f;
                if(!allowed){Reply(sender,request,false,"Park your own vehicle nearby before changing its setup",id);return;}
                if(expected!=data.GetInt(VehicleNetworkProtocol.SetupRevision)){Reply(sender,request,false,"Vehicle setup changed on server. Refresh and try again",id);return;}
                var profile=catalog.Profiles.First(v=>v.id==VehiclePersistence.Profile(data));
                var settings=ReadSetup(setup);
                int layout=VehiclePersistence.Trunk(data);
                if(settings.TryGetValue(profile.ConfigPrefix+"Showroom/TrunkLayout",out var next)&&(!int.TryParse(next,out layout)||layout<0||layout>3))throw new System.IO.InvalidDataException("Invalid trunk layout");
                var root=ZNetScene.instance.FindInstance(id);var cargo=root!=null?root.GetComponent<Container>():null;
                if(cargo==null||cargo.IsInUse()){Reply(sender,request,false,"Cargo is loading or in use. Close it before changing the setup",id);return;}
                typeof(Container).GetMethod("Load",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance).Invoke(cargo,null);
                var inventory=cargo.GetInventory();
                if(!VehicleStorageRules.CanResize(profile,layout,inventory.NrOfItems(),false)){Reply(sender,request,false,"Remove excess cargo before changing the trunk layout",id);return;}
                int rows=VehicleStorageRules.Rows(profile,layout);
                if(inventory.GetHeight()!=rows) {
                    var items=inventory.GetAllItems();for(int i=0;i<items.Count;i++)items[i].m_gridPos=new Vector2i(i%inventory.GetWidth(),i/inventory.GetWidth());
                    cargo.m_height=rows;inventory.SetHeight(rows);inventory.m_onChanged?.Invoke();
                }
                settings[profile.ConfigPrefix+"Showroom/TrunkLayout"]=layout.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if(SavedGroundMode(profile,settings)!=SavedGroundMode(profile,ReadSetup(VehiclePersistence.Settings(data))))
                    data.Set(VehicleMotionTelemetry.Prefix+"groundmode",SavedGroundMode(profile,settings));
                VehiclePersistence.UpdateSettings(data,JsonConvert.SerializeObject(settings),layout);
                Reply(sender,request,true,"Vehicle setup saved",id);return;
            }
            if(command==VehicleNetworkProtocol.Command.Remove) {
                var cargoRoot=ZNetScene.instance?.FindInstance(id);var container=cargoRoot!=null?cargoRoot.GetComponent<Container>():null;
                bool ok=!cargoLeases.Active(key,now)&&data.GetLong(VehicleNetworkProtocol.Creator)==player&&!leases.Active(key,now)&&(container==null||!container.IsInUse());
                if(ok){VehiclePersistence.Forget(data);data.Set(VehicleNetworkProtocol.Driver,0L);data.Set(VehicleNetworkProtocol.DriverCharacter,ZDOID.None);}
                Reply(sender,request,ok,ok?"Vehicle removed; cargo retained":"Only the owner can remove a parked vehicle",id);return;
            }
            Reply(sender,request,false,"Unknown vehicle command",id);
        }catch(Exception error){log?.Invoke("Rejected malformed vehicle RPC: "+error.GetType().Name);}
    }
    void Spawn(long sender,long player,Vector3 character,int request,ZPackage p) {
        string profileId=p.ReadString(),setup=p.ReadString();var position=p.ReadVector3();var rotation=p.ReadQuaternion();
        var profile=catalog.Profiles.FirstOrDefault(v=>v.id==profileId);
        if(profile==null||!Pose(position,rotation)||Vector3.Distance(character,position)>10||setup.Length>131072){Reply(sender,request,false,"Invalid vehicle spawn",ZDOID.None);return;}
        if(placement==null||!placement(profile,position,rotation)){Reply(sender,request,false,"Vehicle spawn requires clear supported ground",ZDOID.None);return;}
        var settings=ReadSetup(setup);int layout=0;
        if(settings.TryGetValue(profile.ConfigPrefix+"Showroom/TrunkLayout",out var trunk)&&(!int.TryParse(trunk,out layout)||layout<0||layout>3)){Reply(sender,request,false,"Invalid trunk layout",ZDOID.None);return;}
        settings[profile.ConfigPrefix+"Showroom/TrunkLayout"]=layout.ToString(System.Globalization.CultureInfo.InvariantCulture);setup=JsonConvert.SerializeObject(settings);
        var owned=records.Select(world.GetZDO).Where(d=>VehiclePersistence.IsVehicle(d)&&d.GetLong(VehicleNetworkProtocol.Creator)==player).ToArray();
        if(owned.Length>=4||owned.Any(d=>VehiclePersistence.Profile(d)==profileId)){Reply(sender,request,false,"Garage limit or duplicate vehicle",ZDOID.None);return;}
        var prefab=ZNetScene.instance?.GetPrefab(profile.capabilities.showroom?"piece_chest":"piece_chest_blackmetal");
        if(prefab==null){Reply(sender,request,false,"Cargo prefab unavailable",ZDOID.None);return;}
        var root=UnityEngine.Object.Instantiate(prefab,position,rotation);var view=root.GetComponent<ZNetView>();var data=view?.GetZDO();
        if(data==null){UnityEngine.Object.Destroy(root);Reply(sender,request,false,"World anchor unavailable",ZDOID.None);return;}
        try {
            data.Set(VehicleNetworkProtocol.Creator,player);VehiclePersistence.Save(data,profileId,root.transform,layout,false,setup);
            data.Set(VehicleMotionTelemetry.Prefix+"groundmode",SavedGroundMode(profile,settings));
            data.SetOwner(ZDOMan.GetSessionID());records.Add(data.m_uid);
            hide?.Invoke(root.GetComponent<Container>());
            Reply(sender,request,true,"Vehicle spawned",data.m_uid);
        }catch {view.Destroy();throw;}
    }
    bool ApplyMotion(ZDO data,long sender,double now,ZPackage p) {
        string key=data.m_uid.ToString();
        int sequence=p.ReadInt();var position=p.ReadVector3();var rotation=p.ReadQuaternion();var velocity=p.ReadVector3();
        float steering=p.ReadSingle(),wheel=p.ReadSingle();bool boost=p.ReadBool();
        var telemetry=VehicleMotionTelemetry.Read(p);
        if(!telemetry.Valid()||p.GetPos()!=p.Size())return false;
        if(telemetry.Charge>data.GetFloat(VehicleMotionTelemetry.Prefix+"charge",1)+.05f+.2f*(float)leases.Elapsed(key,sender,now))return false;
        if(!Pose(position,rotation)||!Finite(velocity.x)||!Finite(velocity.y)||!Finite(velocity.z)||!Finite(steering)||!Finite(wheel)||Mathf.Abs(steering)>90||Mathf.Abs(wheel)>1000000)return false;
        if(!leases.Accept(key,sender,sequence,now,Vector3.Distance(position,VehiclePersistence.Location(data)),velocity.magnitude))return false;
        telemetry.Store(data);
        data.Set("nfvu2_pose_v2",position);data.Set("nfvu2_rotation_v2",rotation);
        var profile=catalog.Profiles.First(v=>v.id==VehiclePersistence.Profile(data));
        var cargo=position+rotation*new Vector3(0,.5f,-profile.rig.trunkApproachZ);
        if(ZoneSystem.instance!=null&&ZoneSystem.instance.GetGroundHeight(cargo,out var ground))cargo.y=ground+.15f;
        data.SetPosition(cargo);data.SetRotation(Quaternion.Euler(0,rotation.eulerAngles.y,0));
        data.Set(VehicleNetworkProtocol.Velocity,velocity);data.Set(VehicleNetworkProtocol.Steering,steering);data.Set(VehicleNetworkProtocol.Wheels,wheel);data.Set(VehicleNetworkProtocol.Boost,boost);data.Set("nfvu2_driven_v2",true);
        return true;
    }
    public bool AllowsNativeCargoOpen(Container container,long sender,long player) {
        var data=container?.GetComponent<ZNetView>()?.GetZDO();var peer=ZNet.instance?.GetPeer(sender);
        bool identity=Host(sender)?Player.m_localPlayer.GetPlayerID()==player:peer!=null&&peer.IsReady()&&world?.GetZDO(peer.m_characterID)?.GetLong(ZDOVars.s_playerID,0L)==player;
        return world==ZDOMan.instance&&data!=null&&identity&&VehiclePersistence.IsVehicle(data)&&cargoLeases.HeldBy(data.m_uid.ToString(),sender,Time.realtimeSinceStartupAsDouble);
    }
    bool DriverCharacterMatches(ZDO data,long peer) {
        var actor=Host(peer)?ZNet.instance.LocalPlayerCharacterID:ZNet.instance.GetPeer(peer)?.m_characterID??ZDOID.None;
        return !actor.Equals(ZDOID.None)&&data.GetZDOID(VehicleNetworkProtocol.DriverCharacter).Equals(actor)&&world.GetZDO(actor)!=null;
    }
    static int SavedGroundMode(CarProfile profile,Dictionary<string,string> settings) {
        bool terrain=true,drift=false;
        if(settings.TryGetValue(profile.ConfigPrefix+"Driving/TerrainMode",out var road)&&bool.TryParse(road,out var t))terrain=t;
        if(settings.TryGetValue(profile.ConfigPrefix+"Driving/DriftMode",out var sliding)&&bool.TryParse(sliding,out var d))drift=d;
        return drift&&profile.capabilities.drift?2:terrain?0:1;
    }
    static Dictionary<string,string> ReadSetup(string setup) {
        if(setup==null||setup.Length>131072)throw new System.IO.InvalidDataException("Invalid setup size");
        Dictionary<string,string> values;
        using(var reader=new JsonTextReader(new System.IO.StringReader(setup))){reader.MaxDepth=4;values=JsonSerializer.Create(new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,CheckAdditionalContent=true}).Deserialize<Dictionary<string,string>>(reader);}
        if(values==null||values.Count>512||values.Any(v=>v.Key.Length>256||v.Value==null||v.Value.Length>16384))throw new System.IO.InvalidDataException("Invalid setup values");
        return values;
    }
    static bool Host(long peer)=>ZNet.instance!=null&&ZNet.instance.IsServer()&&!ZNet.instance.IsDedicated()&&peer==ZNet.GetUID()&&Player.m_localPlayer!=null;
    static bool Connected(long peer)=>Host(peer)||ZNet.instance.GetPeer(peer)?.IsReady()==true;
    static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
    static bool Pose(Vector3 p,Quaternion q)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z)&&Mathf.Abs(p.x)<=20000&&Mathf.Abs(p.z)<=20000&&Mathf.Abs(p.y)<=10000&&Finite(q.x)&&Finite(q.y)&&Finite(q.z)&&Finite(q.w)&&q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w>.5f&&q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w<1.5f;
}
}
