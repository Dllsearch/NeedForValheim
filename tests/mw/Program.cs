using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NeedForValheim;
using UnityEngine;
class Program {
 static int checks;static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
 static void Main(string[] args) {try{Run(args);}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(string[] args) {
  var root=args[0];var manifest=JObject.Parse(File.ReadAllText(Path.Combine(root,"manifest.json")));
  var catalog=CarCatalog.Load(root).ForSource(CarCatalog.MostWanted);
  Check(catalog.Length==manifest["roster"].Count(),"Every source Racing registration exported");Check(!manifest["failures"].Any(),"No incomplete exports");
  var artwork=DecalArtworkCatalog.Load(Path.Combine(root,"decals","mw-logos.json"));
  foreach(var art in artwork.entries){var bytes=VinylCatalog.Unpack(art.data,art.width*art.height*4);Check(Enumerable.Range(0,art.width*art.height).Any(i=>bytes[i*4+3]>0),"Original decal artwork coverage: "+art.name);}
  foreach(var car in catalog) {
   var data=ModelFile.Load(Path.Combine(root,car.assets.modelFile));var library=ModelFile.Load(Path.Combine(root,"styling",car.id+".json"));
   if(!string.IsNullOrEmpty(car.assets.decalFile)) {
    var decals=ModelFile.Load(Path.Combine(root,car.assets.decalFile));
    Check(decals.decalSlots.Select(s=>s.key).SequenceEqual(car.assets.decalSlots),car.id+" decal metadata agrees with the garage slots");
    foreach(var slot in decals.decalSlots){Check(artwork.entries.Any(a=>a.kind==slot.kind),car.id+" original decal artwork fits projection type");Check(decals.parts.Any(p=>p.option==slot.key&&(p.sourceKit<0||p.sourceKit==0)),car.id+" stock decal slot is usable");}
   }else Check(car.assets.decalSlots.Length==0,car.id+" no fabricated service-car decal slots");
   var original=JObject.Parse(File.ReadAllText(Path.Combine(root,car.id,"provenance.json")));
   Check(!original["unresolvedTextures"].Any(),car.id+" has all required textures");
   Check(car.capabilities.nitro&&data.nitroFlameTexture>=0&&data.nitroPurgeTexture>=0&&data.nitroAtlasColumns==1&&data.nitroAtlasRows==1,car.id+" MW original NOS sprites and complete tuning available");
   Check(data.wheelVisualLift==0,car.id+" visual and physical hub coincide");
   for(int axle=0;axle<2;axle++) {
    var wheel=data.parts.Where(p=>p.group=="wheel"&&ModelFile.ForAxle(p,axle*2)&&data.materials[p.material].wheelRubber).SelectMany(p=>p.vertices).ToArray();
    float radius=car.rig.WheelRadius(axle*2);float width=(float)original["tyreWidths"][axle];
    Check(wheel.Length>0,car.id+" axle wheel present");
    Check(Math.Abs(wheel.Max(v=>Math.Sqrt(v.y*v.y+v.z*v.z))-radius)<.002,car.id+" exact VLT rolling radius at axle "+axle);
    Check(Math.Abs(wheel.Max(v=>v.x)-wheel.Min(v=>v.x)-width)<.003,car.id+" exact VLT tyre width at axle "+axle);
    Check(Math.Abs(car.rig.WheelHeight(axle*2)-radius)<.001,car.id+" tyre contacts ground at axle "+axle);
    foreach(var option in library.styleOptions.Where(o=>o.category=="Rims")) {
     var tyre=library.parts.Where(p=>p.option=="Rims:"+option.id&&p.group=="wheel"&&ModelFile.ForAxle(p,axle*2)&&library.materials[p.material].wheelRubber).SelectMany(p=>p.vertices).ToArray();
     Check(tyre.Length>0&&Math.Abs(tyre.Max(v=>Math.Sqrt(v.y*v.y+v.z*v.z))-radius)<.003,car.id+" accessory tyre matches VLT radius");
    }
   }
   foreach(var part in data.parts.Concat(library.parts).Where(p=>p.group=="wheel")) {
    var sizing=new WheelSizeVisuals(data,car.rig.rimNominal,car.rig.rimMin,car.rig.rimMax);
    var mesh=new Mesh{vertices=part.vertices.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray(),normals=part.normals.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray()};
    var originalVertices=mesh.vertices.ToArray();sizing.Track(mesh,part);
    foreach(var size in new[]{car.rig.rimMin,car.rig.rimMax,part.rimInches>0?part.rimInches:car.rig.rimNominal}) {
     sizing.Apply(size);Check(mesh.vertices.Select((v,i)=>!float.IsNaN(v.y)&&!float.IsInfinity(v.z)&&v.x==originalVertices[i].x).All(b=>b),car.id+" stock/accessory rim deformation stays finite and preserves width");
    }
    Check(mesh.vertices.Select((v,i)=>v.x==originalVertices[i].x&&v.y==originalVertices[i].y&&v.z==originalVertices[i].z).All(b=>b),car.id+" exact wheel reset");
   }
   Check(library.styleReplacements.All(r=>data.parts.Any(p=>p.name==r.Substring(r.IndexOf('|')+1))),car.id+" original replacement targets exist");
   Check(library.styleOptions.Count(o=>o.category=="Rims")==164,car.id+" all 41 wheel designs in all four native sizes exported");
   Check(library.styleOptions.Count(o=>o.category=="Brakes")==4,car.id+" all 4 brake models exported");
   Check(library.parts.Where(p=>p.option.StartsWith("Brakes:")).Any(p=>p.rotatesWithWheel)&&library.parts.Where(p=>p.option.StartsWith("Brakes:")).Any(p=>!p.rotatesWithWheel),car.id+" brake rotors and calipers are separate");
   foreach(var folder in car.assets.engineStageFolders)foreach(var rpm in car.sound.rpmBands)foreach(var prefix in new[]{"engine_","coast_"})Check(File.Exists(Path.Combine(root,folder,prefix+rpm+".wav")),car.id+" complete original stage banks");
   var vinyl=VinylCatalog.Load(Path.Combine(root,"menu","vinyl-"+data.preset+".json"),data.preset);
   Check(vinyl.sourceGame=="mw",car.id+" isolated MW vinyl catalog");
   foreach(var entry in vinyl.entries){var bytes=VinylCatalog.Unpack(entry.data,512*512*4);Check(Enumerable.Range(0,512*512).Any(i=>bytes[i*4+3]>0),car.id+" nonempty original vinyl coverage");}
   Console.WriteLine(car.id+": factory, styling, tyres, all audio stages and "+vinyl.entries.Length+" vinyls verified");
  }
  Check(catalog.Single(c=>c.id=="mw-cemtr").rig.extraRearAxleOffset>.9,"Cement truck retains its original tandem axle");
  Console.WriteLine(checks+" MW full-fleet runtime-loader checks passed. Unity runtime still requires game acceptance.");
 }
}
