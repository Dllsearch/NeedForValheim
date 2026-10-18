using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine {
 public enum FontStyle {BoldAndItalic}
 public enum ScaleMode {ScaleToFit,StretchToFill}
 public enum EventType {Repaint,ScrollWheel}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1);public static Color black=>new Color(0,0,0);}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 one=>new Vector3(1,1,1);public static Vector3 operator *(Vector3 v,float s)=>new Vector3(v.x*s,v.y*s,v.z*s);}
 public struct Quaternion {public static Quaternion identity=>new Quaternion();}
 public struct Matrix4x4 {public static Matrix4x4 identity=>new Matrix4x4();public static Matrix4x4 TRS(Vector3 v,Quaternion q,Vector3 s)=>identity;}
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}}
 public class Texture2D {public static Texture2D whiteTexture=new Texture2D();}
 public class GUIStyleState {public Color textColor;}
 public class GUIStyle {public GUIStyle(){}public GUIStyle(GUIStyle other){}public int fontSize;public FontStyle fontStyle;public GUIStyleState normal=new GUIStyleState();}
 public class GUISkin {}
 public static class GUI {
  public static Matrix4x4 matrix;public static GUISkin skin=new GUISkin();public static bool enabled=true,changed;public static int depth,Groups;
  public static Color color=Color.white,contentColor=Color.white,backgroundColor=Color.white;
  public static void DrawTexture(Rect rect,Texture2D texture,ScaleMode mode=ScaleMode.StretchToFill,bool alpha=true){}
  public static void BeginGroup(Rect rect){Groups++;}public static void EndGroup(){Groups--;if(Groups<0)throw new Exception("Unbalanced GUI group");}
 }
 public static class Screen {public static int width=1600,height=900;}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static int RoundToInt(float v)=>(int)Math.Round(v);}
 public class Event {public static Event current=new Event();public EventType type=EventType.Repaint;public bool isMouse;public void Use(){}}
}
namespace NeedForValheim {
 public class TestProfile {public string id="350z",displayName="350Z";public TestRig rig=new TestRig();public TestSound sound=new TestSound();}
 public class TestRig {public float mass=1450,rimNominal=18;}
 public class TestSound {public float[] gearRatios=new float[6];}
 public class TestSettings {public long Version;}
 public class VehicleRuntime {public string Name;public bool Disposed;public VehicleRuntime networkPreviewOf;public TestSettings networkSettings;public bool networkSetupInFlight;public long networkSetupAcknowledgedVersion;public int networkSetupRevision;}
 public class TestEntry {public string Value="U2";}
 public class TestConfig {public void Save(){}}
 public class TestAssets {public Dictionary<string,UnityEngine.Texture2D> MwFrontend=new Dictionary<string,UnityEngine.Texture2D>(),Frontend=new Dictionary<string,UnityEngine.Texture2D>();}
 public partial class Plugin {
  public VehicleRuntime current,garagePreview;public bool GaragePreviewReady;public Action<Plugin> Page;
  public int menuTab;public string workshopMessage="",garagePreviewStatus="";
  public bool NetworkSessionActive,IsMwFrontend,skinWasMw,menuMouseLookHeld,padActive;
  public TestProfile FrontendProfile=new TestProfile();
  public List<VehicleRuntime> fleet=new List<VehicleRuntime>();
  GUIStyle titleStyle=new GUIStyle(),mutedStyle=new GUIStyle(),labelStyle=new GUIStyle(),choiceSelectedStyle=new GUIStyle();
  GUISkin menuSkin=new GUISkin();TestEntry menuFrontend=new TestEntry();TestConfig Config=new TestConfig();
  TestAssets menuAssets;Texture2D garageFrame;
  int uiCount,uiRegion,uiLastCount,uiFocus,uiHorizontal,uiVertical,showcasePage,garageEffectMode;
  bool uiBuilding,uiPointerInRegion,uiActivate;
  readonly List<int> uiBounds=new List<int>();
  Color MenuAccent=>Color.white;
  string Tr(string value)=>value;string NetworkVehicleEditBlock(VehicleRuntime vehicle)=>null;
  void EnsureMenuStyles(){}void BuildMenuSkin(){}void MenuNavigation(bool available){}
  static int MenuRoot(int tab)=>tab;static string MenuTitle(int tab)=>"PAGE "+tab;
  static bool PreviewTab(int tab)=>tab==13||tab==14||tab==15||tab==16;
  bool MenuButton(Rect r,string text,GUIStyle style=null)=>false;
  void UiLabel(Rect r,string text,GUIStyle style=null){}int UiToolbar(Rect r,int value,string[] names)=>value;
  void CloseVehicleMenu(){}
  void DrawShowcaseMenu()=>Page?.Invoke(this);void DrawBuildMenu()=>Page?.Invoke(this);void DrawStylingMenu()=>Page?.Invoke(this);
  void DrawPerformanceMenu()=>Page?.Invoke(this);void DrawExhaustMenu()=>Page?.Invoke(this);void DrawInterfaceSettings()=>Page?.Invoke(this);
  void DrawGarageMenu()=>Page?.Invoke(this);void DrawVehiclePages()=>Page?.Invoke(this);void DrawRadioMenu()=>Page?.Invoke(this);
  void DrawLightsMenu()=>Page?.Invoke(this);void DrawNeonMenu()=>Page?.Invoke(this);void DrawNitroMenu()=>Page?.Invoke(this);
  void DrawWheelMenu()=>Page?.Invoke(this);void DrawBodyMenu()=>Page?.Invoke(this);void DrawTrunkMenu()=>Page?.Invoke(this);
  void DrawDrivingDisplayMenu()=>Page?.Invoke(this);void DrawTurretMenu()=>Page?.Invoke(this);
  public void Frame(int tab,Action<Plugin> action){menuTab=tab;Page=action;DrawGarageFrontend();}
 }
}
