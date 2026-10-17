using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HarmonyLib;
namespace NeedForValheim {
public partial class Plugin {
    internal bool CaptureInventoryOpening=>ready&&(driving||menuOpen)&&!Multiplayer();
    InventoryGui garageInventoryOwner;
    GameObject inventoryGarageObject;
    Button inventoryGarageButton;
    bool inventoryGarageRequested;
    void TickInventoryGarage() {
        var inventory=InventoryGui.instance;
        if(inventory==null||inventory.m_player==null)return;
        if(garageInventoryOwner!=inventory||inventoryGarageObject==null) {
            if(inventoryGarageObject!=null)Destroy(inventoryGarageObject);
            garageInventoryOwner=inventory;
            inventoryGarageObject=new GameObject("NFVU2 Garage",typeof(RectTransform),typeof(Image),typeof(Button));
            // inventoryRoot fills the screen: putting a button above it put it offscreen.
            var rect=inventoryGarageObject.GetComponent<RectTransform>();rect.SetParent(inventory.m_inventoryRoot,false);
            rect.anchorMin=rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(1,0);
            rect.sizeDelta=new Vector2(210,38);rect.anchoredPosition=new Vector2(-8,8);
            rect.SetAsLastSibling();
            var image=inventoryGarageObject.GetComponent<Image>();image.color=new Color(.12f,.16f,.04f,.98f);
            inventoryGarageButton=inventoryGarageObject.GetComponent<Button>();inventoryGarageButton.targetGraphic=image;
            var colors=inventoryGarageButton.colors;colors.highlightedColor=new Color(.8f,1,.2f);colors.pressedColor=new Color(.55f,.7f,.1f);inventoryGarageButton.colors=colors;
            var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(rect,false);
            var labelRect=label.GetComponent<RectTransform>();labelRect.anchorMin=Vector2.zero;labelRect.anchorMax=Vector2.one;labelRect.offsetMin=labelRect.offsetMax=Vector2.zero;
            var text=label.GetComponent<TextMeshProUGUI>();if(inventory.m_weight!=null)text.font=inventory.m_weight.font;text.fontSize=19;text.fontStyle=FontStyles.Bold;text.alignment=TextAlignmentOptions.Center;text.color=Color.white;text.raycastTarget=false;text.text="CARS  /  GARAGE";
            inventoryGarageButton.onClick.AddListener(()=>{
                if(!CanUseVehicleMenu(LocalPlayer())) {
                    ShowVehicleWarning(ExperimentalNetwork?"Waiting for a compatible NFVU2 server connection. Check that the server mod is loaded.":"This client build supports single-player only. Activate the network client for multiplayer.");
                    return;
                }
                inventoryGarageRequested=true;inventory.Hide();Logger.LogInfo("Inventory garage requested");
            });
        }
        // Place a topmost sibling in inventory coordinates, outside grid masks.
        inventoryGarageObject.transform.position=inventory.m_player.TransformPoint(new Vector3(inventory.m_player.rect.xMax-8,inventory.m_player.rect.yMax+8,0));
        inventoryGarageObject.transform.SetAsLastSibling();
        inventoryGarageButton.interactable=LocalPlayer()!=null&&!SpawnBusy;
    }
    bool OpenRequestedInventoryGarage() {
        if(!inventoryGarageRequested)return false;
        if(!CanUseVehicleMenu(LocalPlayer())){inventoryGarageRequested=false;return false;}
        InventoryGui.instance?.Hide();
        if(InventoryGui.IsVisible())return false;
        inventoryGarageRequested=false;audioBlocked=false;garageView=0;menuTab=10;SetVehicleMenu(true);
        Logger.LogInfo("Garage opened from inventory");return true;
    }
    void DisposeInventoryGarage(){if(inventoryGarageObject!=null)Destroy(inventoryGarageObject);inventoryGarageObject=null;inventoryGarageButton=null;garageInventoryOwner=null;}
}
[HarmonyPatch(typeof(InventoryGui),"Show")]
static class NativeInventoryDuringGarage {
    static bool Prefix()=>Plugin.ControlsOwner==null||!Plugin.ControlsOwner.CaptureInventoryOpening;
}
}
