namespace NeedForValheim {
public static class VehicleExitRules {
    // Leaving this small capsule or jumping/falling away ends the exit landing
    // grace immediately, even if its short timer has not elapsed yet.
    public static bool Active(float now,float until,float distanceSquared,float verticalDelta)=>
        now<until&&distanceSquared<4&&verticalDelta>-.8f&&verticalDelta<.8f;
}
}
