using System.Collections.Generic;
namespace UnityEngine {
public class Object {public bool destroyed;public static void Destroy(Object value){if(value!=null)value.destroyed=true;}}
public class GameObject:Object {public readonly Transform transform=new Transform();}
public class Transform {public Vector3 position;}
public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
public struct Vector2 {public Vector2(float x,float y){}}
public struct Rect {public Rect(float x,float y,float width,float height){}}
public enum TextureFormat {RGBA32}
public enum FilterMode {Bilinear}
public enum TextureWrapMode {Clamp}
public struct Color {public Color(float r,float g,float b,float a){}public static Color clear=>new Color();}
public class Texture2D:Object {public string name;public FilterMode filterMode;public TextureWrapMode wrapMode;public Texture2D(int w,int h,TextureFormat format,bool mipmaps){}public void SetPixels(Color[] pixels){}public void Apply(bool a,bool b){}}
public class Sprite:Object {public string name;public static Sprite Create(Texture2D texture,Rect rect,Vector2 pivot,int size)=>new Sprite();}
}
public class Minimap:UnityEngine.Object {
    public static Minimap instance;
    public enum PinType {Player}
    public sealed class PinData {public UnityEngine.Vector3 m_pos;public UnityEngine.Sprite m_icon;public string name;public bool save;}
    public readonly List<PinData> Pins=new List<PinData>();
    public PinData AddPin(UnityEngine.Vector3 position,PinType type,string name,bool save,bool isChecked){var pin=new PinData{m_pos=position,name=name,save=save};Pins.Add(pin);return pin;}
    public void RemovePin(PinData pin){Pins.Remove(pin);}
}
