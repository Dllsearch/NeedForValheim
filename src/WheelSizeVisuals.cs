using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeedForValheim {
// Meshes are owned by Plugin.vehicleMeshes; retain immutable source coordinates
// so repeated slider changes and resets never accumulate geometry errors.
public sealed class WheelSizeVisuals {
    sealed class Entry {
        public Mesh Mesh;
        public float Bead,Outside,Nominal;
        public Vector3[] Original,Normals,Vertices,UpdatedNormals;
    }
    readonly List<Entry> entries=new List<Entry>();
    readonly float[] beads=new float[2],outsides=new float[2];
    readonly float nominal,min,max;
    float applied;
    public WheelSizeVisuals(ModelData model,float nominal=20,float min=16,float max=22) {
        this.nominal=nominal;this.min=min;this.max=max;applied=nominal;
        for(int axle=0;axle<2;axle++) {
            foreach(var tyre in model.parts.Where(p=>p.group=="wheel"&&ModelFile.ForAxle(p,axle*2)&&(model.materials[p.material].wheelRubber||model.materials[p.material].name.StartsWith("78743ca1_",StringComparison.Ordinal)))) {
                var radii=tyre.indices.Select(i=>tyre.vertices[i]).Select(v=>Mathf.Sqrt(v.y*v.y+v.z*v.z)).ToArray();
                float inner=radii.Min(),outer=radii.Max();
                if(inner<=0||outer<=inner*1.1f)continue;
                beads[axle]=inner;outsides[axle]=outer;break;
            }
            if(outsides[axle]==0) {
                var rubber=model.parts.Where(p=>p.group=="wheel"&&ModelFile.ForAxle(p,axle*2)&&model.materials[p.material].wheelRubber).SelectMany(p=>p.vertices).ToArray();
                if(rubber.Length==0)throw new InvalidOperationException("Wheel sizing requires a supported tyre surface.");
                outsides[axle]=rubber.Max(v=>Mathf.Sqrt(v.y*v.y+v.z*v.z));beads[axle]=Math.Min(nominal*.0254f/2,outsides[axle]*.8f);
            }
        }
    }
    public void Track(Mesh mesh,ModelPart part=null) {
        var vertices=mesh.vertices;
        int axle=part?.axle=="rear"?1:0;
        entries.Add(new Entry{Mesh=mesh,Bead=part!=null&&part.wheelBead>0?part.wheelBead:beads[axle],Outside=part!=null&&part.wheelOutside>0?part.wheelOutside:outsides[axle],Nominal=part!=null&&part.rimInches>0?part.rimInches:nominal,Original=vertices,Normals=mesh.normals,
            Vertices=new Vector3[vertices.Length],UpdatedNormals=new Vector3[vertices.Length]});
        applied=float.NaN; // A newly selected rim must receive the saved size too.
    }
    public void Apply(float inches) {
        inches=WheelFitMath.Sanitize(inches,min,max,nominal);
        if(inches==applied)return;
        foreach(var entry in entries) {
            float scale=inches/entry.Nominal;
            for(int i=0;i<entry.Original.Length;i++) {
                var v=entry.Original[i];var n=entry.Normals[i];
                float radius=Mathf.Sqrt(v.y*v.y+v.z*v.z);
                if(radius<.000001f||inches==entry.Nominal){entry.Vertices[i]=v;entry.UpdatedNormals[i]=n;continue;}
                WheelFitMath.RadialShape(radius,entry.Bead,entry.Outside,scale,out var mapped,out var slope);
                float ratio=mapped/radius,uy=v.y/radius,uz=v.z/radius;
                entry.Vertices[i]=new Vector3(v.x,v.y*ratio,v.z*ratio);
                // Inverse-transpose of the radial deformation preserves the
                // original split normals instead of smoothing the rim edges.
                float radial=n.y*uy+n.z*uz;
                entry.UpdatedNormals[i]=new Vector3(n.x,
                    radial*uy/slope+(n.y-radial*uy)/ratio,
                    radial*uz/slope+(n.z-radial*uz)/ratio).normalized;
            }
            entry.Mesh.vertices=entry.Vertices;entry.Mesh.normals=entry.UpdatedNormals;
            entry.Mesh.RecalculateTangents();entry.Mesh.RecalculateBounds();
        }
        applied=inches;
    }
}
}
