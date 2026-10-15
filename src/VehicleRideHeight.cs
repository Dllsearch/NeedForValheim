using System;
using UnityEngine;
namespace NeedForValheim {
// Raise the suspension anchors relative to the hull: gravity settles the body
// lower while wheels, suspension travel, springs and visual attachments agree.
public sealed class VehicleRideHeight {
    readonly Rigidbody body;
    readonly WheelCollider[] wheels;
    readonly float[] originalY;
    readonly bool alignWheelCentres;
    public VehicleRideHeight(Rigidbody body,WheelCollider[] wheels,bool alignWheelCentres=false) {
        this.body=body;this.wheels=wheels;this.alignWheelCentres=alignWheelCentres;originalY=new float[wheels.Length];
        for(int i=0;i<wheels.Length;i++)originalY[i]=wheels[i].transform.localPosition.y;
    }
    public void Apply(float centimetres) {
        float next=float.IsNaN(centimetres)||float.IsInfinity(centimetres)?0:Math.Max(0,Math.Min(12,centimetres))*.01f;
        bool changed=false;
        for(int i=0;i<wheels.Length;i++) {
            // Imported MW rig coordinates describe the hub at rest. A collider's
            // transform is the TOP of its travel, not the hub. Unity's sprung-mass
            // preload already supports gravity at targetPosition: do not subtract
            // mass*g/spring here. Re-evaluate after road/offroad/flight changes.
            float rest=alignWheelCentres?wheels[i].suspensionDistance*(1-wheels[i].suspensionSpring.targetPosition):0;
            var pos=wheels[i].transform.localPosition;float y=originalY[i]+rest+next;
            if(Math.Abs(pos.y-y)<.000001f)continue;
            pos.y=y;wheels[i].transform.localPosition=pos;changed=true;
        }
        if(changed)body.WakeUp();
    }
}
}
