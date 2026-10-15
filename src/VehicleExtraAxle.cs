using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    // MW traffic models can draw a tandem axle. It follows the simulated rear axle;
    // NFVU2 handling retains its proven four contact points.
    IEnumerable<int> BuildExtraRearAxle(ModelData model,Material paint,Material dark) {
        if(current.Profile.rig.extraRearAxleOffset<=0)yield break;
        current.extraRearWheels=new Transform[2];current.extraRearBrakes=new Transform[2];
        for(int side=0;side<2;side++) {
            var root=new GameObject("MW tandem wheel "+side);root.transform.SetParent(car.transform,false);current.extraRearWheels[side]=root.transform;
            var brake=new GameObject("MW tandem caliper "+side);brake.transform.SetParent(car.transform,false);current.extraRearBrakes[side]=brake.transform;
            foreach(var part in model.parts.Where(p=>ModelFile.ForAxle(p,2)&&(p.group=="wheel"||p.group=="brake"))) {
                AddVisual(part,part.group=="brake"&&!part.rotatesWithWheel?brake.transform:root.transform,model,paint,dark);yield return 0;
            }
        }
    }
    Transform[] StylingWheels=>visuals.Concat(current.extraRearWheels??Array.Empty<Transform>()).ToArray();
    Transform[] StylingBrakes=>brakeVisuals.Concat(current.extraRearBrakes??Array.Empty<Transform>()).ToArray();
}
}
