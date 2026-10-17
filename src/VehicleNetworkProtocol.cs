using System;
using System.Collections.Generic;
namespace NeedForValheim {
// Connection-scoped leases are never persisted as peer IDs: reconnects receive
// new IDs. A stopped/missing driver loses authority after a short timeout.
internal sealed class VehicleNetworkLeases {
    internal sealed class Lease {public long Peer,Player;public double Expires,LastUpdate;public int Sequence;}
    readonly Dictionary<string,Lease> leases=new Dictionary<string,Lease>();
    public const double Timeout=4;
    public bool Claim(string id,long peer,long player,double now) {
        if(peer==0||player==0||string.IsNullOrEmpty(id)||!Finite(now))return false;
        foreach(var pair in leases)if(pair.Key!=id&&pair.Value.Peer==peer&&pair.Value.Expires>now)return false;
        if(leases.TryGetValue(id,out var old)&&old.Expires>now&&old.Peer!=peer)return false;
        if(old!=null&&old.Peer==peer&&old.Expires>now){old.Expires=now+Timeout;return true;}
        leases[id]=new Lease{Peer=peer,Player=player,Expires=now+Timeout,LastUpdate=now,Sequence=-1};return true;
    }
    public bool HasPeer(long peer,double now){foreach(var lease in leases.Values)if(lease.Peer==peer&&lease.Expires>now)return true;return false;}
    public bool Active(string id,double now)=>leases.TryGetValue(id,out var lease)&&lease.Expires>now;
    public double Elapsed(string id,long peer,double now)=>leases.TryGetValue(id,out var lease)&&lease.Peer==peer?Math.Max(0,now-lease.LastUpdate):0;
    public bool HeldBy(string id,long peer,double now)=>leases.TryGetValue(id,out var lease)&&lease.Peer==peer&&lease.Expires>now;
    public bool Accept(string id,long peer,int sequence,double now,double distance,double speed) {
        if(!leases.TryGetValue(id,out var lease)||lease.Peer!=peer||lease.Expires<=now||sequence<=lease.Sequence||!Finite(now)||now<lease.LastUpdate||!Finite(distance)||distance<0||!Finite(speed)||speed<0||speed>180)return false;
        // Generous enough for driving/flight and ordinary packet loss, but never
        // permit a single state update to teleport across the map.
        if(distance>8+180*Math.Min(Timeout,now-lease.LastUpdate))return false;
        lease.Sequence=sequence;lease.LastUpdate=now;lease.Expires=now+Timeout;return true;
    }
    public void Release(string id,long peer){if(leases.TryGetValue(id,out var lease)&&lease.Peer==peer)leases.Remove(id);}
    public void Expire(double now,Func<long,bool> connected) {
        var remove=new List<string>();foreach(var pair in leases)if(pair.Value.Expires<=now||!connected(pair.Value.Peer))remove.Add(pair.Key);
        foreach(var id in remove)leases.Remove(id);
    }
    public void Clear()=>leases.Clear();
    static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
}
internal static class VehicleNetworkProtocol {
    public const int Version=3,MaxPacket=140000;
    public const string Rpc="NFVU2.Vehicle.v1",ReplyRpc="NFVU2.Vehicle.v1.Reply";
    public static string InventoryHash(byte[] bytes) {
        using(var hash=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes??Array.Empty<byte>())).Replace("-","").ToLowerInvariant();
    }
    public const string DriverCharacter="nfvu2_driver_character_v1",SeatBlend="nfvu2_seat_blend_v1";
    public const string CargoPeer="nfvu2_cargo_peer_v1",CargoCommit="nfvu2_cargo_commit_v1";
    public const string SetupRevision="nfvu2_setup_revision_v1";
    public const string Creator="nfvu2_creator_v1",Driver="nfvu2_driver_v1",Velocity="nfvu2_velocity_v1",Steering="nfvu2_steering_v1",Wheels="nfvu2_wheels_v1",Boost="nfvu2_boost_v1";
    public enum Command:byte {Hello=0,Spawn=1,Claim=2,State=3,Release=4,Remove=5,Roster=6,Setup=7,CargoOpen=8,CargoKeep=9,CargoClose=10,Recover=11,Reply=128,RosterReply=129}
}
}
