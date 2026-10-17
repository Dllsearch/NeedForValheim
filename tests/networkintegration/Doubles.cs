using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace UnityEngine {
 public static class Time {public static double realtimeSinceStartupAsDouble;}
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 zero=>new Vector3();public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
 }
 public struct Quaternion {
  public float x,y,z,w;public static Quaternion identity=>new Quaternion{w=1};
  public static Quaternion Euler(float x,float y,float z){double a=y*Math.PI/360;return new Quaternion{y=(float)Math.Sin(a),w=(float)Math.Cos(a)};}
  public Vector3 eulerAngles=>new Vector3(0,(float)(Math.Atan2(2*w*y,1-2*y*y)*180/Math.PI),0);
  public static Vector3 operator *(Quaternion q,Vector3 p){double a=q.eulerAngles.y*Math.PI/180;return new Vector3((float)(p.x*Math.Cos(a)+p.z*Math.Sin(a)),p.y,(float)(p.z*Math.Cos(a)-p.x*Math.Sin(a)));}
 }
 public static class Mathf {public static float Abs(float v)=>Math.Abs(v);}
 public class Transform {public Vector3 position;public Quaternion rotation=Quaternion.identity;}
 public class Component {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();}
 public class GameObject {
  public Transform transform=new Transform();public Dictionary<Type,Component> parts=new Dictionary<Type,Component>();
  public T GetComponent<T>() where T:Component=>parts.TryGetValue(typeof(T),out var c)?(T)c:null;
  public T Add<T>(T c) where T:Component {parts[typeof(T)]=c;c.gameObject=this;return c;}
 }
 public static class Object {
  public static GameObject Instantiate(GameObject prefab,Vector3 p,Quaternion q){var root=new GameObject();root.transform.position=p;root.transform.rotation=q;var data=ZDOMan.instance.Create(p);root.Add(new ZNetView{data=data});root.Add(new Container());ZNetScene.instance.roots[data.m_uid]=root;return root;}
  public static void Destroy(GameObject root){root?.GetComponent<ZNetView>()?.Destroy();}
 }
}
public struct ZDOID : IEquatable<ZDOID> {
 public long Value;public ZDOID(long value){Value=value;}public static ZDOID None=>new ZDOID();public bool Equals(ZDOID other)=>Value==other.Value;
 public override bool Equals(object obj)=>obj is ZDOID other&&Equals(other);public override int GetHashCode()=>Value.GetHashCode();public override string ToString()=>Value.ToString();
}
public static class HashExtensions {public static int GetStableHashCode(this string text){unchecked{int result=5381;foreach(var c in text)result=result*33+c;return result;}}}
public class ZDO {
 public ZDOID m_uid;public bool Persistent,deleted;public int cargoStacks;public byte[] inventoryBytes;
 public byte[] GetByteArray(int key,byte[] fallback)=>inventoryBytes??fallback;
 public long Owner=>owner;
 public readonly Dictionary<string,object> values=new Dictionary<string,object>();
 UnityEngine.Vector3 position;UnityEngine.Quaternion rotation=UnityEngine.Quaternion.identity;long owner=10;
 public void Set(string key,object value){values[key]=value;}public string GetString(string key)=>values.TryGetValue(key,out var v)?(string)v:"";
 public float GetFloat(string key,float fallback=0)=>values.TryGetValue(key,out var v)?(float)v:fallback;
 public void Set(int key,long value)=>values["native:"+key]=value;
 public long GetLong(int key,long fallback)=>values.TryGetValue("native:"+key,out var value)?(long)value:fallback;
 public ZDOID GetZDOID(string key)=>values.TryGetValue(key,out var v)?(ZDOID)v:ZDOID.None;
 public int GetInt(string key)=>values.TryGetValue(key,out var v)?(int)v:0;public long GetLong(string key)=>values.TryGetValue(key,out var v)?(long)v:0;
 public bool GetBool(string key)=>values.TryGetValue(key,out var v)&&(bool)v;
 public UnityEngine.Vector3 GetVec3(string key,UnityEngine.Vector3 fallback)=>values.TryGetValue(key,out var v)?(UnityEngine.Vector3)v:fallback;
 public UnityEngine.Quaternion GetQuaternion(string key,UnityEngine.Quaternion fallback)=>values.TryGetValue(key,out var v)?(UnityEngine.Quaternion)v:fallback;
 public void SetPosition(UnityEngine.Vector3 v){position=v;}public UnityEngine.Vector3 GetPosition()=>position;
 public void SetRotation(UnityEngine.Quaternion v){rotation=v;}public UnityEngine.Quaternion GetRotation()=>rotation;
 public bool IsOwner()=>owner==ZDOMan.GetSessionID();public void SetOwner(long value){owner=value;}
}
public static class ZDOVars {public const int s_items=1,s_playerID=2;}
public class ZDOMan {
 public static ZDOMan instance=new ZDOMan();long serial;public Dictionary<ZDOID,ZDO> records=new Dictionary<ZDOID,ZDO>();
 public static long GetSessionID()=>10;
 public ZDO Create(UnityEngine.Vector3 position){var d=new ZDO{m_uid=new ZDOID(++serial)};d.SetPosition(position);records[d.m_uid]=d;return d;}
 public ZDO GetZDO(ZDOID id)=>records.TryGetValue(id,out var d)&&!d.deleted?d:null;
}
public static class ZDOExtraData {
 public enum Type {String,Int}
 public static ZDOID[] GetAllZDOIDsWithHash(Type type,int hash)=>ZDOMan.instance.records.Values.Where(d=>!d.deleted&&d.values.Any(p=>p.Key.GetStableHashCode()==hash&&(type==Type.String?p.Value is string:p.Value is int))).Select(d=>d.m_uid).ToArray();
}
public class ZNetView:UnityEngine.Component {public ZDO data;public ZDO GetZDO()=>data;public void Destroy(){data.deleted=true;ZNetScene.instance.roots.Remove(data.m_uid);}}
public struct Vector2i {public int x,y;public Vector2i(int x,int y){this.x=x;this.y=y;}}
public class CargoItem {public Vector2i m_gridPos;}
public class Inventory {
 public int rows=4;public List<CargoItem> items=new List<CargoItem>();public Action m_onChanged;
 public int NrOfItems()=>items.Count;public int GetHeight()=>rows;public void SetHeight(int value){rows=value;}public int GetWidth()=>6;public List<CargoItem> GetAllItems()=>items;
}
public class Container:UnityEngine.Component {
 public bool busy;public int m_height=4;public Inventory inventory=new Inventory();public bool IsInUse()=>busy;public Inventory GetInventory()=>inventory;
 void Load(){int count=GetComponent<ZNetView>().data.cargoStacks;if(inventory.items.Count!=count){inventory.items.Clear();for(int i=0;i<count;i++)inventory.items.Add(new CargoItem());}}
}
public class ZNetScene {
 public static ZNetScene instance=new ZNetScene();public Dictionary<ZDOID,UnityEngine.GameObject> roots=new Dictionary<ZDOID,UnityEngine.GameObject>();
 public UnityEngine.GameObject GetPrefab(string name)=>new UnityEngine.GameObject();
 public UnityEngine.GameObject FindInstance(ZDOID id)=>roots.TryGetValue(id,out var root)?root:null;
}
public class ZoneSystem {public static ZoneSystem instance=new ZoneSystem();public bool GetGroundHeight(UnityEngine.Vector3 p,out float height){height=0;return true;}}
public class Player {public static Player m_localPlayer;public long id;public long GetPlayerID()=>id;}
public class ZNetPeer {public long m_uid,m_playerID;public ZDOID m_characterID;public bool ready=true;public bool IsReady()=>ready;}
public class ZNet {
 public static ZNet instance;public static bool IsSinglePlayer;public long uid;public bool server,dedicated=true;public ZDOID LocalPlayerCharacterID;
 public Dictionary<long,ZNetPeer> peers=new Dictionary<long,ZNetPeer>();public ZNetPeer serverPeer;
 public static long GetUID()=>instance.uid;public bool IsServer()=>server;public bool IsDedicated()=>dedicated;
 public ZNetPeer GetPeer(long uid)=>peers.TryGetValue(uid,out var p)?p:null;public ZNetPeer GetServerPeer()=>serverPeer;
}
public class ZRoutedRpc {
 public static ZRoutedRpc instance=new ZRoutedRpc();public readonly Dictionary<long,ZNet> endpoints=new Dictionary<long,ZNet>();
 readonly Dictionary<(long,string),Action<long,ZPackage>> handlers=new Dictionary<(long,string),Action<long,ZPackage>>();
 public int sent;public void Register<T>(string route,Action<long,T> callback){handlers.Add((ZNet.GetUID(),route),(sender,p)=>callback(sender,(T)(object)p));}
 public void InvokeRoutedRPC(long target,string route,params object[] args) {
  sent++;long sender=ZNet.GetUID();if(!endpoints.TryGetValue(target,out var endpoint)||!handlers.TryGetValue((target,route),out var handler))return;
  var previous=ZNet.instance;try {ZNet.instance=endpoint;handler(sender,new ZPackage(((ZPackage)args[0]).GetArray()));}finally {ZNet.instance=previous;}
 }
}
public class ZPackage {
 readonly MemoryStream stream;readonly BinaryReader reader;readonly BinaryWriter writer;
 public ZPackage(){stream=new MemoryStream();reader=new BinaryReader(stream);writer=new BinaryWriter(stream);}
 public ZPackage(byte[] bytes):this(){writer.Write(bytes);stream.Position=0;}
 public void Write(byte v)=>writer.Write(v);public void Write(int v)=>writer.Write(v);public void Write(long v)=>writer.Write(v);public void Write(bool v)=>writer.Write(v);public void Write(float v)=>writer.Write(v);public void Write(string v)=>writer.Write(v);public void Write(ZDOID v)=>writer.Write(v.Value);
 public void Write(UnityEngine.Vector3 v){Write(v.x);Write(v.y);Write(v.z);}public void Write(UnityEngine.Quaternion v){Write(v.x);Write(v.y);Write(v.z);Write(v.w);}
 public long ReadLong()=>reader.ReadInt64();
 public byte ReadByte()=>reader.ReadByte();public int ReadInt()=>reader.ReadInt32();public bool ReadBool()=>reader.ReadBoolean();public float ReadSingle()=>reader.ReadSingle();public string ReadString()=>reader.ReadString();public ZDOID ReadZDOID()=>new ZDOID(reader.ReadInt64());
 public UnityEngine.Vector3 ReadVector3()=>new UnityEngine.Vector3(ReadSingle(),ReadSingle(),ReadSingle());
 public UnityEngine.Quaternion ReadQuaternion()=>new UnityEngine.Quaternion{x=ReadSingle(),y=ReadSingle(),z=ReadSingle(),w=ReadSingle()};
 public int Size()=>(int)stream.Length;public int GetPos()=>(int)stream.Position;public void SetPos(int pos){stream.Position=pos;}public byte[] GetArray()=>stream.ToArray();
}
