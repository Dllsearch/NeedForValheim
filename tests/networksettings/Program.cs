using System;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using NeedForValheim;
class Program {
 static int checks;static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
 static void Main() {
  string path=Path.Combine(Path.GetTempPath(),"nfvu2-source-"+Guid.NewGuid().ToString("N")+".cfg");
  var original=new ConfigFile(path,false){SaveOnConfigSet=false};
  var source=original.Bind("miata.Styling","Paint",0,new ConfigDescription("Paint",new AcceptableValueRange<int>(0,15)));source.Value=5;
  var before=Directory.GetFiles(Path.GetTempPath(),"nfvu2-memory-*.cfg");
  var first=new NetworkVehicleSettings("{\"miata.Styling/Paint\":\"2\",\"miata.Parts/Bodykit\":\"4\"}");
  var second=new NetworkVehicleSettings("{\"miata.Styling/Paint\":\"7\"}");
  Check(first.Get(source).Value==2&&second.Get(source).Value==7&&source.Value==5,"same-model instances and local setup remain independent");
  first.Get(source).Value=9;Check(second.Get(source).Value==7&&source.Value==5,"editing one instance cannot recolor another");
  Check(ReferenceEquals(first.Get(source),first.Get(source)),"bindings retain their per-instance entry identity");
  var empty=new NetworkVehicleSettings("{}");Check(empty.Get(source).Value==0,"missing network values use factory defaults instead of local customization");
  var limited=new NetworkVehicleSettings("{\"miata.Styling/Paint\":\"999\"}");Check(limited.Get(source).Value==15,"network values retain source acceptable range");
  first.Replace("{\"miata.Styling/Paint\":\"3\"}");Check(first.Get(source).Value==3&&second.Get(source).Value==7,"replacement affects only its target instance");
  first.Replace("{}");Check(first.Get(source).Value==0,"removed snapshot keys reset previously bound entries");
  bool invalid=false;try{first.Replace("null");}catch(InvalidDataException){invalid=true;}
  Check(invalid&&first.Get(source).Value==0,"invalid snapshot leaves previous state intact");
  bool oversized=false;try{new NetworkVehicleSettings(new string(' ',131073));}catch(InvalidDataException){oversized=true;}
  Check(oversized,"oversized network snapshot rejected before binding");
  Check(second.Snapshot().Contains("miata.Styling/Paint")&&second.Snapshot().Contains("7"),"snapshot retains canonical profile definition names");
  Check(!File.Exists(path)&&Directory.GetFiles(Path.GetTempPath(),"nfvu2-memory-*.cfg").SequenceEqual(before),"network and local test bindings write no configuration files");
  bool editing=false,permission=true;var guarded=new NetworkVehicleSettings("{\"miata.Styling/Paint\":\"2\"}",()=>editing,()=>permission);
  var guardedPaint=guarded.Get(source);guardedPaint.Value=4;
  Check(guarded.Version==0&&guarded.Snapshot().Contains("2"),"automatic visual fallback cannot overwrite original server values");
  editing=true;permission=false;guardedPaint.Value=6;Check(guardedPaint.Value==4&&guarded.Version==0,"read-only UI mutation is reverted before it can alter an instance");
  permission=true;guardedPaint.Value=8;Check(guarded.Version==1&&guarded.Snapshot().Contains("8"),"authorized UI edits become a publishable draft");
  guarded.Replace("{\"miata.Styling/Paint\":\"3\"}");Check(guarded.Version==1&&guardedPaint.Value==3,"server refresh does not become another local edit");
  permission=false;guarded.SetRuntime(guardedPaint,6);
  Check(guardedPaint.Value==6&&guarded.Version==1&&guarded.Snapshot().Contains("3"),"runtime controls bypass workshop permissions without dirtying the saved build");
  guarded.Replace("{\"miata.Styling/Paint\":\"5\"}");
  Check(guardedPaint.Value==6&&guarded.Version==1&&guarded.Snapshot().Contains("5"),"server refresh preserves active runtime controls while updating saved settings");
  permission=true;guardedPaint.Value=9;guarded.SetRuntime(guardedPaint,7);
  Check(guarded.Version==2&&guarded.Snapshot().Contains("9")&&!guarded.Snapshot().Contains("7"),"runtime controls cannot leak into a pending workshop draft");
  guarded.Replace("{}");Check(guardedPaint.Value==7&&!guarded.Snapshot().Contains("7"),"runtime controls survive a default-server refresh without becoming persisted");
  var transient=new NetworkVehicleSettings("{}");var transientPaint=transient.Get(source);transient.SetRuntime(transientPaint,7);
  Check(transientPaint.Value==7&&transient.Version==0&&!transient.Snapshot().Contains("7"),"runtime controls preserve default snapshot values when a key was absent");
  Console.WriteLine(checks+" settings isolation checks passed using the real BepInEx configuration assembly; no Unity scene executed.");
 }
}
