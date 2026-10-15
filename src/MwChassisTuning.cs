using System;
using UnityEngine;
namespace NeedForValheim {
// Keep the established controller's suspension loading for imported MW masses.
// A 5-tonne service vehicle otherwise exceeds road suspension's maximum support.
public static class MwChassisTuning {
    public static void Configure(Rigidbody body,WheelCollider[] wheels) {
        if(float.IsNaN(body.mass)||float.IsInfinity(body.mass)||body.mass<=0)throw new ArgumentException("Invalid imported chassis mass");
        float scale=Math.Max(.5f,Math.Min(4f,body.mass/1450f));
        foreach(var wheel in wheels) {
            bool terrain=wheel.suspensionDistance>.3f;
            var spring=wheel.suspensionSpring;spring.spring=(terrain?42000:48000)*scale;spring.damper=(terrain?5600:6000)*scale;wheel.suspensionSpring=spring;
            wheel.mass=24*scale;
        }
    }
}
}
