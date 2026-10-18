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
    int selectedAudioSlot,bodyDetailPage,trunkDetailPage;
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
        bool supported=showroom.SupportsMotion(part);
        GUI.enabled=supported&&(stationary||showroom.State.Open[part])&&!(driving&&cabinDriver!=null&&!DriverInputReady&&part==1);
        if(MenuButton(rect,label+(supported?(showroom.State.Open[part]?" close":" open"):" unavailable")))showroom.State.Toggle(part,stationary);
        GUI.enabled=true;
    }
    void DrawBodyMenu() {
        if(!ShowroomReady())return;
        bodyDetailPage=UiToolbar(new Rect(25,142,510,34),bodyDetailPage,new[]{"Panels","Hinges","Engine","Glass"});
        if(bodyDetailPage==0) {
            // The existing car conversion maps the NFS RIGHT mesh to Unity -X.
            MotionButton(new Rect(25,201,245,40),1,"Left door");
            MotionButton(new Rect(285,201,245,40),0,"Right door");
            MotionButton(new Rect(25,255,245,40),2,"Hood");
            MotionButton(new Rect(285,255,245,40),3,"Trunk");
            if(MenuButton(new Rect(25,317,510,36),"Close all panels"))showroom.State.CloseAll();
            UiLabel(new Rect(25,379,510,66),"Open panels while parked. Driving closes them automatically.\nOnly panels with imported geometry and animation are available.",mutedStyle);
        }else if(bodyDetailPage==1) {
            UiLabel(new Rect(25,201,510,26),"Door hinges",labelStyle);
            string[] names={"Normal","Suicide","Scissor doors"};
            for(int i=0;i<names.Length;i++) {
                GUI.enabled=WorkshopParked&&showroom.SupportsDoorStyle(i)&&!(driving&&cabinDriver!=null&&!DriverInputReady);
                if(MenuButton(new Rect(25,241+i*46,510,36),names[i],doorStyle.Value==i?choiceSelectedStyle:null))doorStyle.Value=i;
            }
            GUI.enabled=true;
            MotionButton(new Rect(25,399,245,36),1,"Left door");
            MotionButton(new Rect(285,399,245,36),0,"Right door");
            UiLabel(new Rect(25,455,510,42),"Changing hinges closes both doors before switching animations.",mutedStyle);
        }else if(bodyDetailPage==2) {
            UiLabel(new Rect(25,201,510,26),"Engine appearance",labelStyle);
            GUI.enabled=WorkshopParked;
            engineStyle.Value=UiGrid(new Rect(25,243,510,100),engineStyle.Value,new[]{"Stock","Style 1","Style 2"},1);
            GUI.enabled=true;
            MotionButton(new Rect(25,369,510,38),2,"Hood");
            UiLabel(new Rect(25,437,510,42),"Visual engine model. Performance upgrades are separate.",mutedStyle);
        }else {
            bool tintAvailable=current.interior!=null&&current.interior.SupportsTint;
            UiLabel(new Rect(25,201,510,28),"Window tint   "+Mathf.RoundToInt(CabinAppearance.Tint(windowTint.Value)*100)+" %",labelStyle);
            GUI.enabled=WorkshopParked&&tintAvailable;
            float tint=Mathf.Round(UiSlider(new Rect(25,259,510,22),CabinAppearance.Tint(windowTint.Value),0,1)*100)/100;
            if(tintAvailable&&tint!=windowTint.Value)windowTint.Value=tint;
            GUI.enabled=true;
            UiLabel(new Rect(25,327,510,80),tintAvailable?"0% clear \u00b7 100% dark. Driver window open while seated.\nDoors can also be opened from inside at rest.":"Window tint requires an interior and a transparent glass shader.",mutedStyle);
        }
    }
    void DrawTrunkMenu() {
        if(!ShowroomReady())return;
        trunkDetailPage=UiToolbar(new Rect(25,142,510,34),trunkDetailPage,new[]{"Layout","Equipment","Lighting"});
        int layoutIndex=Mathf.Clamp(trunkLayout.Value,0,showroom.Layouts.Length-1);
        int slots=showroom.Layouts[layoutIndex].sockets.Length;
        GUI.enabled=WorkshopParked;
        if(trunkDetailPage==0) {
            int layout=UiGrid(new Rect(25,201,510,100),layoutIndex,showroom.Layouts.Select(l=>l.title).ToArray(),2);
            if(layout!=layoutIndex)SetTrunkLayout(layout);
            UiLabel(new Rect(25,323,510,54),Tr("Storage")+": "+VehicleStorageRules.Capacity(current.Profile,trunkLayout.Value)+" "+Tr("Slots")+"\n"+Tr("More audio equipment, less cargo space."),mutedStyle);
            if(MenuButton(new Rect(25,399,510,36),"Default system")&&SetTrunkLayout(2)) {
                foreach(var slot in audioSlots)VehicleConfig(slot).Value=1;
                trunkNeonEnabled.Value=true;trunkNeonPulsing.Value=false;trunkNeonColor.Value="Green";
            }
        }else if(trunkDetailPage==1) {
            if(slots>0) {
                selectedAudioSlot=Mathf.Clamp(selectedAudioSlot,0,slots-1);
                selectedAudioSlot=UiToolbar(new Rect(25,201,510,32),selectedAudioSlot,Enumerable.Range(1,slots).Select(i=>"Slot "+i).ToArray());
                var choices=showroom.Choices(layoutIndex,selectedAudioSlot);
                VehicleConfig(audioSlots[selectedAudioSlot]).Value=UiGrid(new Rect(25,253,510,156),VehicleConfig(audioSlots[selectedAudioSlot]).Value,choices.Select(c=>c.title).ToArray(),4,songButtonStyle);
            }else UiLabel(new Rect(25,211,510,100),"Stock trunk. Select a layout to add subwoofers, amplifiers and displays.",labelStyle);
        }else {
            trunkNeonEnabled.Value=UiToggle(new Rect(25,201,245,32),trunkNeonEnabled.Value,"Trunk neon");
            trunkNeonPulsing.Value=UiToggle(new Rect(285,201,245,32),trunkNeonPulsing.Value,"Pulse");
            int color=Math.Max(0,Array.IndexOf(ColorNames,trunkNeonColor.Value));
            int next=UiGrid(new Rect(25,257,510,136),color,ColorNames,4);
            if(next!=color)trunkNeonColor.Value=ColorNames[next];
        }
        GUI.enabled=true;
        MotionButton(new Rect(25,455,510,38),3,"Trunk");
    }
}
}
