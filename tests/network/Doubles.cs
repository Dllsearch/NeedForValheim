using System;
using System.Collections.Generic;
using System.IO;
namespace UnityEngine {public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}public static class Time {public static double realtimeSinceStartupAsDouble;}}
public struct ZDOID {public long Value;public static ZDOID None=>new ZDOID();public ZDOID(long value){Value=value;}}
public class ZDOMan {public static ZDOMan instance=new ZDOMan();}
public class ZNetPeer {public long m_uid;}
public class ZNet {
 public static ZNet instance=new ZNet();public static bool IsSinglePlayer;public static long GetUID()=>10;
 public bool host;public ZNetPeer peer=new ZNetPeer{m_uid=88};public bool IsServer()=>host;public ZNetPeer GetServerPeer()=>peer;
}
public class ZRoutedRpc {
 public static ZRoutedRpc instance=new ZRoutedRpc();
 public sealed class Sent {public long Peer;public string Route;public ZPackage Package;}
 public readonly Dictionary<string,Action<long,ZPackage>> routes=new Dictionary<string,Action<long,ZPackage>>();
 public readonly List<Sent> sent=new List<Sent>();
 public void Register<T>(string name,Action<long,T> receive){routes.Add(name,(sender,p)=>receive(sender,(T)(object)p));}
 public void InvokeRoutedRPC(long peer,string route,params object[] args){sent.Add(new Sent{Peer=peer,Route=route,Package=(ZPackage)args[0]});}
 public void Deliver(long sender,string route,ZPackage p){p.SetPos(0);routes[route](sender,p);}
}
public class ZPackage {
 readonly MemoryStream stream=new MemoryStream();BinaryReader reader;BinaryWriter writer;
 public ZPackage(){reader=new BinaryReader(stream);writer=new BinaryWriter(stream);}
 public void Write(byte v)=>writer.Write(v);public void Write(int v)=>writer.Write(v);public void Write(bool v)=>writer.Write(v);public void Write(string v)=>writer.Write(v);public void Write(ZDOID v)=>writer.Write(v.Value);
 public UnityEngine.Vector3 ReadVector3()=>new UnityEngine.Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());public long ReadLong()=>reader.ReadInt64();
 public byte ReadByte()=>reader.ReadByte();public int ReadInt()=>reader.ReadInt32();public bool ReadBool()=>reader.ReadBoolean();public string ReadString()=>reader.ReadString();public ZDOID ReadZDOID()=>new ZDOID(reader.ReadInt64());
 public int Size()=>(int)stream.Length;public int GetPos()=>(int)stream.Position;public void SetPos(int value){stream.Position=value;}
}
