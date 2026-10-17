using System;
using System.Reflection;
class InspectServer {
 static void Main(string[] args) {
  var assembly=Assembly.LoadFile(System.IO.Path.GetFullPath(args[0]));
  if(assembly.GetManifestResourceNames().Length!=0)throw new Exception("Server DLL embeds assets");
  foreach(var reference in assembly.GetReferencedAssemblies()) {
   var name=reference.Name;
   if(name.Contains("AudioModule")||name.Contains("ImageConversionModule")||name.Contains("InputSystem")||name.Contains("IMGUIModule")||name.Contains("ParticleSystemModule"))throw new Exception("Client assembly dependency: "+name);
  }
  Console.WriteLine("Server DLL: zero embedded resources; no audio, image, input, GUI or particle assembly dependencies.");
 }
}
