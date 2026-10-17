using System.Linq;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    KeyHints vehicleHintOwner;
    GameObject[] originalHintGroups;
    bool[] originalHintStates;
    // Gauge placement never depends on the controls help page.
    float VehicleHintBottomInset=>24;
    void RestoreVehicleHints() {
        if(originalHintGroups!=null)for(int i=0;i<originalHintGroups.Length;i++)
            if(originalHintGroups[i]!=null)originalHintGroups[i].SetActive(originalHintStates[i]);
        originalHintGroups=null;originalHintStates=null;vehicleHintOwner=null;
    }
    void DisposeVehicleHints()=>RestoreVehicleHints();
    void UpdateVehicleHints() {
        var owner=KeyHints.instance;
        if(!driving||menuOpen||audioBlocked||owner==null||Hud.instance==null||Hud.IsUserHidden()){RestoreVehicleHints();return;}
        if(vehicleHintOwner!=owner) {
            RestoreVehicleHints();vehicleHintOwner=owner;
            originalHintGroups=new[]{owner.m_combatHints,owner.m_buildHints,owner.m_fishingHints,owner.m_inventoryHints,owner.m_inventoryWithContainerHints,owner.m_barberHints,owner.m_radialHints};
            originalHintStates=originalHintGroups.Select(group=>group!=null&&group.activeSelf).ToArray();
        }
        foreach(var group in originalHintGroups)if(group!=null)group.SetActive(false);
    }
}
}
