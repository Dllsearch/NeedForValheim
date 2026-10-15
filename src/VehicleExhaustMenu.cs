using System;
using BepInEx.Configuration;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    ConfigEntry<int> baseConfig_exhaustSound;
    ConfigEntry<int> baseConfig_exhaustStyle;
    ConfigEntry<string> baseConfig_exhaustFlameColor;
    ConfigEntry<int> exhaustSound=>VehicleConfig(baseConfig_exhaustSound);
    ConfigEntry<int> exhaustStyle=>VehicleConfig(baseConfig_exhaustStyle);
    ConfigEntry<string> exhaustFlameColor=>VehicleConfig(baseConfig_exhaustFlameColor);
    static readonly string[] ExhaustNames={"Original","Deep","Sport"};
    void InitializeExhaustMenu() {
        baseConfig_exhaustStyle=BindVehicle("Exhaust","Style",8,new ConfigDescription("Original NFS exhaust tip STYLE01..10. Requires the updated local model catalog; STYLE08 is the existing default.",new AcceptableValueRange<int>(1,10)));
        baseConfig_exhaustSound=BindVehicle("Exhaust","SoundProfile",0,new ConfigDescription("NFVU2 treatments of the installed original engine recordings: 0 original, 1 deeper, 2 sport. Not recovered NFS tuning banks.",new AcceptableValueRange<int>(0,2)));
        baseConfig_exhaustFlameColor=BindVehicle("Exhaust","FlameColor","Original",new ConfigDescription("Exhaust nitro and wheel-thruster colour. Original preserves green exhaust and blue wheel thrusters.",new AcceptableValueList<string>(ExhaustColors)));
    }
    static readonly string[] ExhaustColors={"Original","Green","Cyan","Blue","Purple","Red","Orange","White"};
    static readonly string[] ExhaustColorLabels={"Original","Green","Cyan","Blue","Purple","Red","Orange","White"};
    static readonly Color[] ExhaustFlameColors={Color.white,new Color(.12f,1,.025f),new Color(0,1,1),new Color(.12f,.35f,1),new Color(.65f,.12f,1),new Color(1,.04f,.025f),new Color(1,.4f,.02f),Color.white};
    void TickExhaustCustomization() {
        current.exhaustVisuals?.Apply(exhaustStyle.Value);
        nitroEffects?.SetExhaustStyle(exhaustStyle.Value);
        vehicleAudio?.SetExhaustProfile(exhaustSound.Value);
        int color=Math.Max(0,Array.IndexOf(ExhaustColors,exhaustFlameColor.Value));
        nitroEffects?.SetFlameColor(color==0?new Color(.12f,1,.025f):ExhaustFlameColors[color]);
        current.flightEffects?.SetFlameColor(color==0?new Color(.12f,.65f,1):ExhaustFlameColors[color]);
    }
    void DrawExhaustMenu() {
        UiLabel(new Rect(25,140,510,25),"Engine / exhaust sound",labelStyle);
        exhaustSound.Value=UiToolbar(new Rect(25,173,510,32),ExhaustSoundProfile.Clamp(exhaustSound.Value),ExhaustNames);
        UiLabel(new Rect(25,212,510,46),"Original recordings with NFVU2 sound profiles.\nAudible immediately with the engine running.",mutedStyle);
        UiLabel(new Rect(25,270,510,25),"Nitro flame colour",labelStyle);
        int selected=Math.Max(0,Array.IndexOf(ExhaustColors,exhaustFlameColor.Value));
        exhaustFlameColor.Value=ExhaustColors[UiGrid(new Rect(25,303,510,58),selected,ExhaustColorLabels,4)];
        UiLabel(new Rect(25,370,510,26),current.Profile.capabilities.exhaust?"Colour applies to exhaust flames and wheel thrusters.":"Colour applies to wheel thrusters.",mutedStyle);
        UiLabel(new Rect(25,410,510,25),"Original exhaust tips",labelStyle);
        if(current.exhaustVisuals?.Available==true) {
            exhaustStyle.Value=1+UiGrid(new Rect(25,447,510,60),exhaustStyle.Value-1,new[]{"01","02","03","04","05","06","07","08","09","10"},5);
        }else UiLabel(new Rect(25,447,510,60),"Exhaust tip options are missing from the local car model.",mutedStyle);
    }
}
}
