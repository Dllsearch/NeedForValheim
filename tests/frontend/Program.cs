using System;
using NeedForValheim;
using UnityEngine;
class Program {
 static int checks;
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
 static void Main() {
  foreach(bool mw in new[]{false,true}) {
   var old=new VehicleRuntime{Name="old"};var selected=new VehicleRuntime{Name="selected"};var preview=new VehicleRuntime{Name="preview"};
   var menu=new Plugin{current=old,garagePreview=preview,GaragePreviewReady=true,IsMwFrontend=mw};
   menu.Frame(10,p=>p.current=selected);
   Check(menu.current==selected,"car selection survives the "+(mw?"MW":"U2")+" frontend frame");
   menu.Frame(10,p=>{p.current.Disposed=true;p.current=old;});
   Check(menu.current==old&&!menu.current.Disposed,"removing the selected car preserves its chosen replacement");
   bool sawPreview=false;menu.Frame(14,p=>sawPreview=p.current==preview);
   Check(sawPreview&&menu.current==old,"styling borrows the preview and restores the world vehicle afterwards");
   menu.GaragePreviewReady=false;bool drew=false;menu.Frame(14,p=>drew=true);
   Check(!drew&&menu.current==old,"loading a preview cannot replace the current world vehicle");
   menu.GaragePreviewReady=true;
   bool threw=false;try{menu.Frame(13,p=>throw new InvalidOperationException("fixture"));}catch(InvalidOperationException){threw=true;}
   Check(threw&&menu.current==old&&GUI.Groups==0,"preview-page failure restores selection and balances GUI groups");
   var replacement=new VehicleRuntime{Name="replacement"};
   try{menu.Frame(10,p=>{p.current=replacement;throw new InvalidOperationException("fixture");});}catch(InvalidOperationException){}
   Check(menu.current==replacement&&!menu.current.Disposed&&GUI.Groups==0,"non-preview failure does not undo an intentional world selection");
  }
  Console.WriteLine(checks+" frontend context checks passed using production frame code with drawing/page doubles; no native Unity layout executed.");
 }
}
