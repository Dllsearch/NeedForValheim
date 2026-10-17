using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace NeedForValheim {
// Separate reply route also permits a host to act as a client on the same router.
internal sealed class NetworkVehicleSummary {public ZDOID Id;public string Profile;public Vector3 Position;public long Driver;}
internal sealed class VehicleNetworkClient {
    sealed class Request {public VehicleNetworkProtocol.Command Command;public double Deadline;public Action<bool,string,ZDOID> Complete;}
    readonly Dictionary<int,Request> pending=new Dictionary<int,Request>();
    readonly Action<string> log;
    readonly List<NetworkVehicleSummary> roster=new List<NetworkVehicleSummary>();
    readonly HashSet<ZDOID> removed=new HashSet<ZDOID>();
    readonly Queue<ZDOID> removedOrder=new Queue<ZDOID>();
    double nextRoster;
    public IReadOnlyList<NetworkVehicleSummary> OwnedVehicles=>roster;
    public bool WasRemoved(ZDOID id)=>removed.Contains(id);
    public void RefreshRoster(){nextRoster=0;}
    ZRoutedRpc router;ZDOMan world;long server;int serial;double nextHello;
    public bool Ready {get;private set;}
    public long Server=>server;
    public VehicleNetworkClient(Action<string> log){this.log=log;}
    public void Tick() {
        var net=ZNet.instance;
        if(net==null||ZNet.IsSinglePlayer||ZDOMan.instance==null||ZRoutedRpc.instance==null){Reset("Vehicle connection closed");return;}
        long target=net.IsServer()?ZNet.GetUID():net.GetServerPeer()?.m_uid??0;
        if(world!=ZDOMan.instance||server!=target||router!=ZRoutedRpc.instance) {
            Reset("Vehicle connection changed");world=ZDOMan.instance;server=target;
            if(router!=ZRoutedRpc.instance){router=ZRoutedRpc.instance;router.Register<ZPackage>(VehicleNetworkProtocol.ReplyRpc,Receive);}
        }
        double now=Time.realtimeSinceStartupAsDouble;
        foreach(var item in pending.Where(v=>v.Value.Deadline<=now).ToArray()) {
            pending.Remove(item.Key);Complete(item.Value,false,"Vehicle request timed out",ZDOID.None);
        }
        if(Ready&&now>=nextRoster) {
            nextRoster=now+3;Send(VehicleNetworkProtocol.Command.Roster,ZDOID.None,null,null);
        }
        if(server!=0&&now>=nextHello) {
            nextHello=now+3;
            Send(VehicleNetworkProtocol.Command.Hello,ZDOID.None,null,(ok,message,id)=>{Ready=ok;});
        }
    }
    public void Send(VehicleNetworkProtocol.Command command,ZDOID id,Action<ZPackage> write,Action<bool,string,ZDOID> complete) {
        if(server==0||router==null||world!=ZDOMan.instance||!Ready&&command!=VehicleNetworkProtocol.Command.Hello){complete?.Invoke(false,"Compatible NFVU2 server is not ready",id);return;}
        bool reply=command!=VehicleNetworkProtocol.Command.State;
        if(reply&&pending.Count>=16){complete?.Invoke(false,"Too many pending vehicle requests",id);return;}
        int request=serial==int.MaxValue?serial=1:++serial;
        var p=new ZPackage();p.Write((byte)command);p.Write(VehicleNetworkProtocol.Version);p.Write(request);
        if(command!=VehicleNetworkProtocol.Command.Hello)p.Write(id);
        try {
            write?.Invoke(p);if(p.Size()>VehicleNetworkProtocol.MaxPacket)throw new InvalidOperationException("Vehicle packet exceeds size limit");
            if(reply)pending.Add(request,new Request{Command=command,Deadline=Time.realtimeSinceStartupAsDouble+5,Complete=complete});
            router.InvokeRoutedRPC(server,VehicleNetworkProtocol.Rpc,p);
        }catch(Exception error) {
            pending.Remove(request);complete?.Invoke(false,"Vehicle request could not be sent",id);log?.Invoke("Vehicle send: "+error.GetType().Name);
        }
    }
    void Receive(long sender,ZPackage p) {
        if(sender!=server||server==0||world!=ZDOMan.instance||p==null||p.Size()>1024)return;
        try {
            var kind=(VehicleNetworkProtocol.Command)p.ReadByte();
            if((kind!=VehicleNetworkProtocol.Command.Reply&&kind!=VehicleNetworkProtocol.Command.RosterReply)||p.ReadInt()!=VehicleNetworkProtocol.Version)return;
            int request=p.ReadInt();
            if(!pending.TryGetValue(request,out var waiting))return;
            if(waiting.Deadline<=Time.realtimeSinceStartupAsDouble){pending.Remove(request);Complete(waiting,false,"Vehicle request timed out",ZDOID.None);return;}
            if(kind==VehicleNetworkProtocol.Command.RosterReply) {
                if(waiting.Command!=VehicleNetworkProtocol.Command.Roster)return;
                int count=p.ReadInt();if(count<0||count>4)return;
                var entries=new List<NetworkVehicleSummary>();var ids=new HashSet<ZDOID>();
                for(int i=0;i<count;i++) {
                    var id=p.ReadZDOID();string profile=p.ReadString();var position=p.ReadVector3();long driver=p.ReadLong();
                    if(profile.Length==0||profile.Length>64||!Finite(position.x)||!Finite(position.y)||!Finite(position.z)||!ids.Add(id))return;
                    if(!removed.Contains(id))entries.Add(new NetworkVehicleSummary{Id=id,Profile=profile,Position=position,Driver=driver});
                }
                if(p.GetPos()!=p.Size())return;
                roster.Clear();roster.AddRange(entries);pending.Remove(request);Complete(waiting,true,"Garage synchronized",ZDOID.None);return;
            }
            if(waiting.Command==VehicleNetworkProtocol.Command.Roster)return;
            bool ok=p.ReadBool();string message=p.ReadString();var target=p.ReadZDOID();
            if(message.Length>256||p.GetPos()!=p.Size())return;
            pending.Remove(request);
            if(ok&&waiting.Command==VehicleNetworkProtocol.Command.Remove) {
                roster.RemoveAll(v=>v.Id.Equals(target));
                if(removed.Add(target)){removedOrder.Enqueue(target);if(removedOrder.Count>128)removed.Remove(removedOrder.Dequeue());}
            }
            if(ok&&(waiting.Command==VehicleNetworkProtocol.Command.Spawn||waiting.Command==VehicleNetworkProtocol.Command.Remove||waiting.Command==VehicleNetworkProtocol.Command.Claim||waiting.Command==VehicleNetworkProtocol.Command.Recover))RefreshRoster();
            Complete(waiting,ok,message,target);
        }catch(Exception error){log?.Invoke("Rejected vehicle reply: "+error.GetType().Name);}
    }
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    void Complete(Request request,bool ok,string message,ZDOID id) {
        try {request.Complete?.Invoke(ok,message,id);}catch(Exception error){log?.Invoke("Vehicle callback: "+error.GetType().Name);}
    }
    public void Reset(string message) {
        Ready=false;world=null;server=0;nextHello=nextRoster=0;roster.Clear();removed.Clear();removedOrder.Clear();
        var requests=pending.Values.ToArray();pending.Clear();
        foreach(var request in requests)Complete(request,false,message,ZDOID.None);
    }
}
}
