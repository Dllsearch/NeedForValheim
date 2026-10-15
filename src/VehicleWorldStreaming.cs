using UnityEngine;
namespace NeedForValheim {
public static class VehicleWorldStreaming {
    // Player.LateUpdate normally publishes this position. The seated Player is
    // disabled, so our vehicle must keep ZoneSystem and ZNetScene centred on it.
    public static void Follow(MonoBehaviour seatedPlayer) {
        if(seatedPlayer!=null&&ZNet.instance!=null)ZNet.instance.SetReferencePosition(seatedPlayer.transform.position);
    }
}
}
