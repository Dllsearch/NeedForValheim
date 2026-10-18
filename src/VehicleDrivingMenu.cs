using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    ConfigEntry<bool> dynamicCamera,hudEnabled,hudMph;
    ConfigEntry<int> hudDesign;
    ConfigEntry<float> cameraFollow,hudScale;
    ConfigEntry<string> hudColor;
    int gaugeSettingsPage;
    static readonly string[] GaugeNames={"NFSU2 01","NFSU2 02","NFSU2 03","NFSU2 04","NFSU2 05","NFSU2 06","NFSU2 07","NFSU2 08","NFSU2 09","NFSU2 10","NFSU2 11","MW 01","MW 02","MW 03","MW 04","MW 05","MW 06","MW 07","MW 08","MW 09","MW 10","MW 11"};
    void InitializeDrivingMenu() {
        driftMode=BindVehicle("Driving","DriftMode",false,"Optional drift setup: rear drive, lower rear lateral grip and rear handbrake on Space.");
        dynamicCamera=Config.Bind("Camera","DynamicFollow",true,"Let camera heading trail corners and partly follow travel direction; mouse look holds before recentering while moving.");
        cameraFollow=Config.Bind("Camera","FollowSpeed",2.5f,new ConfigDescription("Heading follow response; lower values create more corner lag.",new AcceptableValueRange<float>(1,6)));
        hudEnabled=Config.Bind("HUD","Enabled",true,"Show the vehicle gauges while driving.");
        hudDesign=Config.Bind("HUD","Design",0,new ConfigDescription("Gauge design: 0-10 Underground 2, 11-21 Most Wanted.",new AcceptableValueRange<int>(0,21)));
        hudScale=Config.Bind("HUD","Scale",1f,new ConfigDescription("Gauge size.",new AcceptableValueRange<float>(.65f,1.4f)));
        hudMph=Config.Bind("HUD","MilesPerHour",false,"Show MPH instead of KM/H.");
        hudColor=Config.Bind("HUD","Color","Cyan",new ConfigDescription("Gauge accent colour.",new AcceptableValueList<string>(ColorNames)));
        if(driftMode.Value)terrainMode.Value=false;
    }
    void ApplyDriveMode(){if(Flying)return;var caps=current.Profile.capabilities;if(EffectiveDrift&&caps.drift)handling?.SetDriftMode();else handling?.SetTerrainMode(EffectiveTerrain);if(caps.suvTuning&&body!=null)SuvTuning.Configure(body,wheels);else if(body!=null&&current.Profile.sourceGame==CarCatalog.MostWanted)MwChassisTuning.Configure(body,wheels);current.appliedGrip=1;}
    string DriveModeName=>Flying?(current.flight.State.Landing?"Flight / landing":current.flight.State.Phase==FlightPhase.Folding?"Flight / take-off":"Flight / hovering"):EffectiveDrift&&current.Profile.capabilities.drift?"Drift / rear-wheel drive":EffectiveTerrain?"Off-road / all-wheel assist":"Road / rear-wheel drive";
    void DrawDrivingDisplayMenu() {
        gaugeSettingsPage=UiToolbar(new Rect(25,142,510,32),gaugeSettingsPage,new[]{"Gauge design","Appearance"});
        int selected=Math.Max(0,Array.IndexOf(ColorNames,hudColor.Value));
        if(gaugeSettingsPage==0) {
            int family=UiToolbar(new Rect(25,190,510,32),hudDesign.Value/11,new[]{"Underground 2","Most Wanted"});
            int design=hudDesign.Value%11;
            for(int i=0;i<11;i++) {
                var cell=new Rect(25+i%3*171,239+i/3*65,167,61);
                if(UiButton(cell,"",i==design?choiceSelectedStyle:null))design=i;
                dashboard?.DrawDesignPreview(new Rect(cell.x+5,cell.y+5,51,51),family*11+i,NeonColors[selected]);
                var contentColor=GUI.contentColor;GUI.contentColor=i==design?Color.black:Color.white;
                UiLabel(new Rect(cell.x+66,cell.y+18,94,27),(family==0?"U2 ":"MW ")+(i+1).ToString("00"),labelStyle);
                GUI.contentColor=contentColor;
            }
            hudDesign.Value=family*11+design;
        }else {
            hudEnabled.Value=UiToggle(new Rect(25,195,250,30),hudEnabled.Value," Show gauges");
            hudMph.Value=UiToggle(new Rect(310,195,210,30),hudMph.Value," MPH instead of KM/H");
            UiLabel(new Rect(25,250,510,26),"Size  "+Mathf.RoundToInt(hudScale.Value*100)+" %",labelStyle);
            hudScale.Value=UiSlider(new Rect(25,289,510,20),hudScale.Value,.65f,1.4f);
            hudColor.Value=ColorNames[UiGrid(new Rect(25,341,510,96),selected,ColorNames,4)];
            if(MenuButton(new Rect(25,460,510,36),"Driving camera settings")){ChangeMenuTab(11);interfaceSettingsPage=1;}
        }
    }
    void DrawDashboard() {
        if(!driving||body==null||!hudEnabled.Value||audioBlocked||menuOpen||Hud.IsUserHidden())return;
        float speed=body.linearVelocity.magnitude,rpm=vehicleAudio?.Rpm??950;
        int gear=vehicleAudio?.Gear??(throttle<0?-1:1);
        float charge=ActiveNitro?.Charge??0;
        bool nos=ActiveNitro!=null&&nitroEnabled.Value;
        if(dashboard==null){UiBox(new Rect(Screen.width-280,Screen.height-VehicleHintBottomInset-75,265,65),Mathf.RoundToInt(HudMath.Speed(speed,hudMph.Value))+(hudMph.Value?" MPH":" KM/H")+" | "+(gear<0?"R":"G "+gear)+"\nNOS "+(nos?Mathf.RoundToInt(charge*100)+" %":"OFF"));return;}
        dashboard.Draw(hudDesign.Value,hudScale.Value,NeonColors[Math.Max(0,Array.IndexOf(ColorNames,hudColor.Value))],hudMph.Value,speed,rpm,gear,charge,nos,ActiveNitro!=null&&ActiveNitro.Boosting,VehicleHintBottomInset,vehicleAudio?.TurboPressure??0,current.Profile.capabilities.turbo,current.Profile.sound.maxRpm);
    }
}
}
