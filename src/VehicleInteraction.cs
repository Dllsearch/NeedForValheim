using System.Linq;
using UnityEngine;
namespace NeedForValheim {
// Use Valheim's normal interaction raycast instead of requiring function keys.
public sealed class VehicleInteraction:MonoBehaviour,Hoverable,Interactable {
    internal Plugin Owner;
    internal VehicleRuntime Vehicle;
    public string GetHoverName()=>Vehicle?.Profile.displayName??"Car";
    public float GetHoverOffset()=>.5f;
    public string GetHoverText()=>Owner!=null&&Owner.VehicleSeated?"":Localization.instance.Localize(GetHoverName()+"\n[<color=yellow><b>$KEY_Use</b></color>] Drive\n[<color=yellow><b>Shift + $KEY_Use</b></color>] Garage");
    public bool Interact(Humanoid user,bool hold,bool alt)=>!hold&&Owner!=null&&Owner.InteractWithVehicle(user,Vehicle,alt);
    public bool UseItem(Humanoid user,ItemDrop.ItemData item)=>false;
}
public partial class Plugin {
    internal bool VehicleSeated=>fleet.Any(vehicle=>vehicle.driving);
    int vehicleInteractFrame=-1;
    internal bool InteractWithVehicle(Humanoid user,VehicleRuntime vehicle,bool garage) {
        if(user!=LocalPlayer()||vehicle==null||!fleet.Contains(vehicle)||driving||Multiplayer()||SpawnBusy)return false;
        current=vehicle;
        if(!CanAccessCurrent(user))return false;
        vehicleInteractFrame=Time.frameCount;
        if(garage){menuTab=0;SetVehicleMenu(true);}else Enter(user);
        return true;
    }
}
}
