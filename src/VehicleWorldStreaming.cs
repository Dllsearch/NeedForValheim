using UnityEngine;
namespace NeedForValheim {
public static class VehicleWorldStreaming {
    public static bool KeepObserver(float distance,bool loaded,bool anchorLoaded) =>
        !float.IsNaN(distance)&&distance>=0&&distance<=(loaded?180:160)&&(loaded||anchorLoaded);

    // Player.LateUpdate normally publishes this position. The seated Player is
    // disabled, so our vehicle must keep ZoneSystem and ZNetScene centred on it.
    public static void Follow(MonoBehaviour seatedPlayer) {
        if(seatedPlayer!=null&&ZNet.instance!=null)ZNet.instance.SetReferencePosition(seatedPlayer.transform.position);
    }
}
}
