using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    bool worldFeaturePage;
    bool DrawVehicleWorldFeaturePage() {
        if(!worldFeaturePage)return false;
        if(MenuButton(new Rect(25,140,510,30),"‹ Vehicle lights")){worldFeaturePage=false;return true;}
        UiLabel(new Rect(25,182,510,28),"Map and car surface effects",labelStyle);
        DrawVehicleWorldFeatureOptions(25,222,510);
        UiLabel(new Rect(25,378,510,92),"Tyre dust follows the ground surface. Dirt accumulates while driving; rain and water wash it off. Paint and wet reflections affect the car only.",mutedStyle);
        return true;
    }
}
}
