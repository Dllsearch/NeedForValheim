using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace NeedForValheim {
public partial class Plugin {
    ConfigEntry<int> baseConfig_trunkLayout,baseConfig_engineStyle,baseConfig_doorStyle;
    ConfigEntry<int> trunkLayout=>VehicleConfig(baseConfig_trunkLayout);
    ConfigEntry<int> engineStyle=>VehicleConfig(baseConfig_engineStyle);
    ConfigEntry<int> doorStyle=>VehicleConfig(baseConfig_doorStyle);
    readonly ConfigEntry<int>[] audioSlots=new ConfigEntry<int>[6];
    readonly int[] audioOptions=new int[6];
    ConfigEntry<bool> baseConfig_trunkNeonEnabled,baseConfig_trunkNeonPulsing;
    ConfigEntry<bool> trunkNeonEnabled=>VehicleConfig(baseConfig_trunkNeonEnabled);
    ConfigEntry<bool> trunkNeonPulsing=>VehicleConfig(baseConfig_trunkNeonPulsing);
    ConfigEntry<string> baseConfig_trunkNeonColor;
    ConfigEntry<string> trunkNeonColor=>VehicleConfig(baseConfig_trunkNeonColor);
    ConfigEntry<float> baseConfig_windowTint;
    ConfigEntry<float> windowTint=>VehicleConfig(baseConfig_windowTint);
    int selectedAudioSlot;
    void InitializeShowroomMenu() {
        baseConfig_windowTint=BindVehicle("Cabin","WindowTint",.16f,new ConfigDescription("Vehicle window tint: 0 clear, 1 dark. The driver's window stays open while occupied.",new AcceptableValueRange<float>(0,1)));
        baseConfig_trunkLayout=BindVehicle("Showroom","TrunkLayout",2,new ConfigDescription("Trunk audio enclosure: 0 stock, 1 two slots, 2 four slots, 3 six slots.",new AcceptableValueRange<int>(0,3)));
        baseConfig_engineStyle=BindVehicle("Showroom","EngineStyle",0,new ConfigDescription("Visual engine model: 0 stock, 1 style 1, 2 style 2. Does not change performance.",new AcceptableValueRange<int>(0,2)));
        baseConfig_doorStyle=BindVehicle("Showroom","DoorStyle",0,new ConfigDescription("Door animation: 0 regular, 1 suicide, 2 scissor. Changes apply after closing.",new AcceptableValueRange<int>(0,2)));
        for(int i=0;i<6;i++)audioSlots[i]=BindVehicle("Showroom","AudioSlot"+(i+1),1,new ConfigDescription("Per-slot part: 0 cover, 1-4 subwoofer, 5-8 amplifier, 9-12 LCD. Size follows the enclosure.",new AcceptableValueRange<int>(0,12)));
        baseConfig_trunkNeonEnabled=BindVehicle("Showroom","NeonEnabled",true,"Illuminate the trunk system, independent of underbody neon.");
        baseConfig_trunkNeonPulsing=BindVehicle("Showroom","NeonPulsing",false,"Pulse the trunk neon; this is a visual pulse, not beat detection.");
        baseConfig_trunkNeonColor=BindVehicle("Showroom","NeonColor","Green",new ConfigDescription("Trunk neon colour.",new AcceptableValueList<string>(ColorNames)));
    }
    void ApplyShowroomSettings() {
        if(showroom==null)return;
        for(int i=0;i<6;i++)audioOptions[i]=VehicleConfig(audioSlots[i]).Value;
        showroom.Configure(trunkLayout.Value,engineStyle.Value,audioOptions);
    }
    bool ShowroomReady() {
        if(showroom!=null)return true;
        UiLabel(new Rect(25,150,510,80),"Model data has no trunk layouts or animations yet.",labelStyle);return false;
    }
    void MotionButton(Rect rect,int part,string label) {
        bool stationary=!Flying&&body!=null&&body.linearVelocity.magnitude<=.5f;
        GUI.enabled=(stationary||showroom.State.Open[part])&&!(driving&&cabinDriver!=null&&!DriverInputReady&&part==1);
        if(MenuButton(rect,label+(showroom.State.Open[part]?" close":" open")))showroom.State.Toggle(part,stationary);
        GUI.enabled=true;
    }
    void DrawBodyMenu() {
        if(!ShowroomReady())return;
        UiLabel(new Rect(25,140,510,26),"Doors and lids",labelStyle);
        // The existing car conversion maps the NFS RIGHT mesh to Unity -X.
        MotionButton(new Rect(25,179,245,36),1,"Left door");
        MotionButton(new Rect(285,179,245,36),0,"Right door");
        MotionButton(new Rect(25,225,245,36),2,"Hood");
        MotionButton(new Rect(285,225,245,36),3,"Trunk");
        UiLabel(new Rect(25,273,510,24),"Door hinges",labelStyle);
        GUI.enabled=!(driving&&cabinDriver!=null&&!DriverInputReady);
        doorStyle.Value=UiToolbar(new Rect(25,302,505,32),doorStyle.Value,new[]{"Normal","Suicide","Scissor doors"});
        GUI.enabled=true;
        UiLabel(new Rect(25,345,510,24),"Engine appearance",labelStyle);
        engineStyle.Value=UiToolbar(new Rect(25,374,505,32),engineStyle.Value,new[]{"Stock","Style 1","Style 2"});
        bool tintAvailable=current.interior!=null&&current.interior.SupportsTint;
        UiLabel(new Rect(25,418,510,24),"Window tint   "+Mathf.RoundToInt(CabinAppearance.Tint(windowTint.Value)*100)+" %",labelStyle);
        GUI.enabled=tintAvailable;
        float tint=Mathf.Round(UiSlider(new Rect(25,450,505,18),CabinAppearance.Tint(windowTint.Value),0,1)*100)/100;
        if(tintAvailable&&tint!=windowTint.Value)windowTint.Value=tint;
        GUI.enabled=true;
        UiLabel(new Rect(25,480,510,44),tintAvailable?"0% clear \u00b7 100% dark. Driver window open while seated.\nDoors can also be opened from inside at rest.":"Window tint requires an interior and a transparent glass shader.",mutedStyle);
    }
    void DrawTrunkMenu() {
        if(!ShowroomReady())return;
        int layout=UiToolbar(new Rect(25,143,505,32),trunkLayout.Value,showroom.Layouts.Select(l=>l.title).ToArray());
        if(layout!=trunkLayout.Value)SetTrunkLayout(layout);
        int slots=showroom.Layouts[trunkLayout.Value].sockets.Length;
        if(slots>0) {
            selectedAudioSlot=Mathf.Clamp(selectedAudioSlot,0,slots-1);
            selectedAudioSlot=UiToolbar(new Rect(25,188,505,30),selectedAudioSlot,Enumerable.Range(1,slots).Select(i=>"Slot "+i).ToArray());
            var choices=showroom.Choices(trunkLayout.Value,selectedAudioSlot);
            VehicleConfig(audioSlots[selectedAudioSlot]).Value=UiGrid(new Rect(25,225,505,128),VehicleConfig(audioSlots[selectedAudioSlot]).Value,choices.Select(c=>c.title).ToArray(),4,songButtonStyle);
        } else UiLabel(new Rect(25,205,505,80),"Stock trunk. Select a layout to add subwoofers, amplifiers and displays.",labelStyle);
        trunkNeonEnabled.Value=UiToggle(new Rect(25,363,250,28),trunkNeonEnabled.Value," trunk neon");
        trunkNeonPulsing.Value=UiToggle(new Rect(285,363,245,28),trunkNeonPulsing.Value," Pulsierend");
        int color=Math.Max(0,Array.IndexOf(ColorNames,trunkNeonColor.Value));
        int next=UiGrid(new Rect(25,402,505,50),color,ColorNames,4);
        if(next!=color)trunkNeonColor.Value=ColorNames[next];
        MotionButton(new Rect(25,478,245,34),3,"Trunk");
        if(MenuButton(new Rect(285,478,245,34),"Default system")) {
            if(!SetTrunkLayout(2))return;
            foreach(var slot in audioSlots)VehicleConfig(slot).Value=1;
            trunkNeonEnabled.Value=true;trunkNeonPulsing.Value=false;trunkNeonColor.Value="Green";
        }
        UiLabel(new Rect(25,456,505,21),Tr("Storage")+": "+VehicleStorageRules.Capacity(current.Profile,trunkLayout.Value)+" "+Tr("Slots")+" · "+Tr("More audio equipment, less cargo space."),mutedStyle);
    }
}
}
