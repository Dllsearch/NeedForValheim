using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    GUIStyle hintStyle;
    float controlHintHeight;
    void DrawControlHints() {
        if(audioBlocked||menuOpen||Hud.IsUserHidden())return;
        if(hintStyle==null)hintStyle=new GUIStyle(GUI.skin.box){wordWrap=true,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(12,12,6,6)};
        hintStyle.fontSize=Mathf.RoundToInt(19*hintScale.Value);
        string text;
        if(padActive) {
            string pedals=ButtonName("RT/LT","R2/L2"),exit=ButtonName("B","Circle"),nitro=ButtonName("X","Square");
            if(driving)text=DriveModeName+" | "+pedals+": throttle/brake | LS: steer \u00b7 RS: camera\n"+(Flying?ButtonName("RB/LB","R1/L1")+": up/down · "+exit+": land & get out":ButtonName("A","Cross")+": Handbrake · "+ButtonName("LB","L1")+": turret \u00b7 "+exit+": exit")+" · "+nitro+": nitro\nD-pad \u2190/\u2192: songs \u00b7 \u2191/\u2193: drive mode \u00b7 L3: menu";
            else text="L3: garage / mod menu"+(CanAccessCurrent(LocalPlayer())?" · "+exit+": enter":"")+"\nSpawn a vehicle: garage \u2192 spawn vehicle";
        }else text=driving?DriveModeName+" | "+(Flying?"W/S: forward/back \u00b7 A/D: turn \u00b7 Shift/Space: up/down":"WASD: drive \u00b7 Space: handbrake/brake \u00b7 RMB: turret")+"\nE: "+(Flying?"land & get out":"exit")+" · 1–4: drive mode · ←/→: songs · LMB/Ctrl: nitro · TAB: garage":"Inventory → CARS / GARAGE · Look at car + E: drive · Shift+E: customize";
        text=Tr(text);
        float width=Mathf.Min(Screen.width-30,1050);
        if(driving&&hudEnabled.Value)width=Mathf.Min(width,Mathf.Max(300,Screen.width-350));
        float height=hintStyle.CalcHeight(new GUIContent(text),width);controlHintHeight=height;
        if(!driving){width=Mathf.Min(520,Screen.width-30);height=hintStyle.CalcHeight(new GUIContent(text),width);}
        if(!driving||!NativeVehicleHintsVisible)GUI.Box(new Rect(15,driving?Screen.height-height-110:130,width,height),text,hintStyle);
        if(!driving&&!string.IsNullOrEmpty(status)&&!status.StartsWith("F6:"))UiBox(new Rect(15,130+height+6,width,36),status);
        if(driving&&Time.unscaledTime<songToastUntil&&!string.IsNullOrEmpty(songToast)) {
            var old=GUI.color;GUI.color=new Color(1,1,1,Mathf.Clamp01(songToastUntil-Time.unscaledTime));
            GUI.Box(new Rect(Mathf.Max(15,(Screen.width-620)/2),28,Mathf.Min(620,Screen.width-30),58),"♫ "+songToast,hintStyle);GUI.color=old;
        }
    }
}
}
