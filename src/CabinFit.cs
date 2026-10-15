using System;
using System.Collections.Generic;
using System.Linq;
namespace NeedForValheim {
// Imported cars have no common seat marker. Fit to the actual cabin glazing, not
// the whole vehicle height (the box on a delivery truck is not its driver's cab).
public sealed class CabinFit {
    public float seatX,seatY,seatZ,headCeiling,exitSide,frontEnd,rearEnd,driverScale=.85f;
    public string evidence;
    public static CabinFit Stock350Z()=>new CabinFit {
        seatX=-.354f,seatY=.35f,seatZ=-.25f,headCeiling=1.09f,
        exitSide=-1.3f,frontEnd=2.7f,rearEnd=-2.7f,evidence="fitted separate 350Z interior"
    };
    public static CabinFit FromModel(CarProfile profile,ModelData model) {
        var rig=profile.rig;
        var body=model.parts.Where(p=>p.group=="body").ToArray();
        var side=body.Where(p=>p.name.IndexOf("FRONT_RIGHT_WINDOW",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
        var glass=side.Length>0?side:body.Where(p=>{
            string n=p.name.ToUpperInvariant(),material=model.materials[p.material].name??"";
            return n.Contains("FRONT_WINDOW")||IsGlazing(model,model.materials[p.material]);
        }).ToArray();
        var points=glass.SelectMany(Referenced).Where(v=>v.y>rig.wheelHeight+.15f).ToArray();
        bool measured=points.Length>3;
        float roof=measured?points.Max(v=>v.y):rig.boxCenterY+rig.boxSizeY*.5f;
        float front=measured?points.Max(v=>v.z):Math.Min(rig.frontAxleZ,rig.boxCenterZ+rig.boxSizeZ*.25f);
        float outer=measured?points.Max(v=>Math.Abs(v.x)):rig.boxSizeX*.5f;
        float seatY=Math.Max(.2f,roof-.82f),ceiling=roof-.08f;
        var all=body.SelectMany(Referenced).ToArray();
        float left=all.Length>0?all.Min(v=>v.x):rig.boxCenterX-rig.boxSizeX*.5f;
        return new CabinFit {
            seatX=-Math.Max(.27f,Math.Min(.48f,outer*.48f)),
            seatY=seatY,seatZ=front-(side.Length>0?.85f:1.02f),
            headCeiling=ceiling,driverScale=Math.Min(.85f,.85f*Math.Max(.55f,(ceiling-seatY)/.74f)),exitSide=Math.Min(-1.05f,left-.4f),
            frontEnd=(all.Length>0?all.Max(v=>v.z):rig.boxCenterZ+rig.boxSizeZ*.5f)+.65f,
            rearEnd=(all.Length>0?all.Min(v=>v.z):rig.boxCenterZ-rig.boxSizeZ*.5f)-.65f,
            evidence=measured?(side.Length>0?"original front driver window":"original glazing"):"chassis bounds fallback"
        };
    }
    public static bool IsGlazing(ModelData model,ModelMaterial material) {
        string name=material.name??"";
        if(name.IndexOf("471a1dca",StringComparison.OrdinalIgnoreCase)>=0||name.IndexOf("1ff8c329",StringComparison.OrdinalIgnoreCase)>=0||
            name.StartsWith("mw_window_",StringComparison.OrdinalIgnoreCase))return true;
        // The older H2 exporter names materials by slice, and mapped these two
        // source window keys to this exact blue glass swatch instead of retaining hashes.
        var color=material.color;
        return (model.preset??"").StartsWith("NFVU2_H2_",StringComparison.Ordinal)&&material.texture==-1&&material.smoothness==.9f&&
            color!=null&&color.Length==4&&color[0]==.04f&&color[1]==.08f&&color[2]==.1f;
    }
    static IEnumerable<ModelVertex> Referenced(ModelPart part) {
        // U2 slices retain a shared vertex stream. Unreferenced vertices would
        // accidentally include the roof/body in the glass measurement.
        foreach(var index in part.indices.Distinct())yield return part.vertices[index];
    }
}
}
