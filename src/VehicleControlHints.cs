using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    GUIStyle hintStyle;
    void DrawControlHints() {
        if(!driving||audioBlocked||menuOpen||Hud.IsUserHidden())return;
        if(Time.unscaledTime>=songToastUntil||string.IsNullOrEmpty(songToast))return;
        if(hintStyle==null)hintStyle=new GUIStyle(GUI.skin.box){wordWrap=true,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(12,12,6,6)};
        hintStyle.fontSize=Mathf.RoundToInt(19*hintScale.Value);
        var old=GUI.color;GUI.color=new Color(1,1,1,Mathf.Clamp01(songToastUntil-Time.unscaledTime));
        GUI.Box(new Rect(Mathf.Max(15,(Screen.width-620)/2),28,Mathf.Min(620,Screen.width-30),58),"♫ "+songToast,hintStyle);GUI.color=old;
    }
    void DrawDrivingControlsHelp() {
        UiLabel(new Rect(25,188,510,48),"Inventory → CARS / GARAGE · E: enter · Shift+E: customize",mutedStyle);
        var rows=VehicleHintContent.Build(false,false,Flying,false,current.Profile.capabilities.nitro,"Tab",Tr);
        for(int i=0;i<rows.Length;i++)UiLabel(new Rect(25,244+i*22,510,24),rows[i],labelStyle);
        UiLabel(new Rect(25,520,510,30),padActive?ButtonName("RT/LT: pedals · LS: steer · B: exit · L3: menu","R2/L2: pedals · LS: steer · Circle: exit · L3: menu"):"Controls are shown here so the road stays clear.",mutedStyle);
    }
}
}
