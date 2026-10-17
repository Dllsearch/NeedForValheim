using System;
using System.IO;
using System.Linq;
using NeedForValheim;
using UnityEngine;
class Program {
    static int checks;
    static void Check(bool valid,string name){if(!valid)throw new Exception(name);checks++;}
    static void Main(string[] args) {
        Check(WheelFitMath.RimDesign("5ZIGEN STYLE01 17 in")==WheelFitMath.RimDesign("5ZIGEN STYLE01 20 in"),"native diameter variants share one rim design");
        Check(WheelFitMath.RimDesign("BBS STYLE01 18 in")!=WheelFitMath.RimDesign("BBS STYLE02 18 in"),"different rim designs stay distinct");
        Check(WheelFitMath.RimDesign("Factory",true)=="Factory"&&WheelFitMath.RimDesign("OZ 20",false)=="OZ 20","factory and number-only model names remain intact");
        var catalog=CarCatalog.Load(args[0]);
        foreach(var car in catalog.Profiles) {
            var model=ModelFile.Load(Path.Combine(args[0],car.assets.modelFile));
            var sizing=new WheelSizeVisuals(model,car.rig.rimNominal,car.rig.rimMin,car.rig.rimMax);
            var part=model.parts.First(p=>p.group=="wheel");
            var mesh=new Mesh{vertices=part.vertices.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray(),normals=part.normals.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray()};
            var original=mesh.vertices.ToArray();sizing.Track(mesh);
            foreach(float size in new[]{car.rig.rimMin,car.rig.rimMax,car.rig.rimNominal}) {
                sizing.Apply(size);
                Check(mesh.vertices.All(v=>!float.IsNaN(v.y)&&!float.IsInfinity(v.z)),car.id+" rim slider stays finite");
                Check(mesh.vertices.Select((v,i)=>v.x==original[i].x).All(b=>b),car.id+" rim slider preserves tyre width");
            }
            Check(mesh.vertices.Select((v,i)=>v.x==original[i].x&&v.y==original[i].y&&v.z==original[i].z).All(b=>b),car.id+" factory reset restores the exact original mesh");
            if(car.sourceGame==CarCatalog.MostWanted) {
                Check(model.parts.Any(p=>p.group=="wheel"&&model.materials[p.material].wheelRubber),car.id+" identifies MW rubber without a U2 material hash");
                Check(model.wheelVisualLift==0,car.id+" preview and collider share the tyre centre");
                for(int axle=0;axle<2;axle++) {
                    var brakes=model.parts.Where(p=>p.group=="brake"&&ModelFile.ForAxle(p,axle*2)).ToArray();
                    Check((brakes.Length>0||new[]{"mw-cemtr","mw-garb","mw-pizza","mw-taxi","mw-minivan","mw-pickupa"}.Contains(car.id))&&brakes.All(p=>p.axle==(axle==0?"front":"rear")),car.id+" renders only the correct axle's brakes");
                }
            }
        }
        Check(ModelFile.ForAxle(new ModelPart(),0)&&ModelFile.ForAxle(new ModelPart(),3),"legacy untagged parts remain on both axles");
        Check(!ModelFile.ForAxle(new ModelPart{axle="rear"},0)&&!ModelFile.ForAxle(new ModelPart{axle="front"},3),"axle-specific parts do not leak to the other axle");
        Console.WriteLine(checks+" actual wheel-sizing and axle checks passed; Unity rendering/physics require game validation.");
    }
}
