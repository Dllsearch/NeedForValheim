using HarmonyLib;

namespace NeedForValheim {
// GamepadRumble reads this value even when our orbit/menu camera has disabled
// GameCamera.UpdateCamera. Discard shakes there: otherwise they never decay.
[HarmonyPatch(typeof(GameCamera), "GetAttenuatedShakeIntensity")]
static class NativeVehicleCameraShake {
    internal static void Prefix(GameCamera __instance,ref float ___m_shakeIntensity) {
        if(Plugin.ControlsOwner!=null&&Plugin.ControlsOwner.CaptureCameraShake(__instance))
            ___m_shakeIntensity=0;
    }
}
}
