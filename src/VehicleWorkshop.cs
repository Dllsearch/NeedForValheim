using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Newtonsoft.Json;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    ConfigEntry<int> baseConfig_engineStage,baseConfig_turboStage,baseConfig_gearStage,baseConfig_tyreStage,baseConfig_brakeStage,baseConfig_nosStage;
    ConfigEntry<int> engineStage=>VehicleConfig(baseConfig_engineStage);
    ConfigEntry<int> turboStage=>VehicleConfig(baseConfig_turboStage);
    ConfigEntry<int> gearStage=>VehicleConfig(baseConfig_gearStage);
    ConfigEntry<int> tyreStage=>VehicleConfig(baseConfig_tyreStage);
    ConfigEntry<int> brakeStage=>VehicleConfig(baseConfig_brakeStage);
    ConfigEntry<int> nosStage=>VehicleConfig(baseConfig_nosStage);
    ConfigEntry<int> baseConfig_paint,baseConfig_finish,baseConfig_rimColor,baseConfig_vinyl,baseConfig_vinylColor,baseConfig_glassColour,baseConfig_glassShade;
    ConfigEntry<bool> baseConfig_paintTrim;
    ConfigEntry<bool> paintTrim=>VehicleConfig(baseConfig_paintTrim);
    ConfigEntry<int> paintChoice=>VehicleConfig(baseConfig_paint);
    ConfigEntry<int> paintFinish=>VehicleConfig(baseConfig_finish);
    ConfigEntry<int> rimColor=>VehicleConfig(baseConfig_rimColor);
    ConfigEntry<int> vinylChoice=>VehicleConfig(baseConfig_vinyl);
    ConfigEntry<int> vinylColor=>VehicleConfig(baseConfig_vinylColor);
    ConfigEntry<int> glassColour=>VehicleConfig(baseConfig_glassColour);
    ConfigEntry<int> glassShade=>VehicleConfig(baseConfig_glassShade);
    static readonly string[] StyleCategories={"Bodykit","Front","Rear","Skirts","Hood","Spoiler","Roof","Brakes","Rims"};
    static readonly string[] StyleLabels={"Bodykit","Front bumper","Rear bumper","Side skirts","Hood","Spoiler","Roof scoops","Brakes","Rims"};
    readonly Dictionary<string,ConfigEntry<int>> styleSettings=new Dictionary<string,ConfigEntry<int>>();
    readonly Dictionary<string,ConfigEntry<string>[]> garageBuilds=new Dictionary<string,ConfigEntry<string>[]>();
    static readonly string[] PaintNames={"Original","Black","White","Silver","Graphite","Red","Orange","Yellow","Lime","Green","Cyan","Blue","Purple","Pink","Bronze"};
    internal static readonly Color[] PaintColors={Color.white,new Color(.035f,.035f,.04f),new Color(.92f,.92f,.9f),new Color(.65f,.68f,.72f),new Color(.18f,.2f,.23f),new Color(.8f,.025f,.04f),new Color(1,.25f,.02f),new Color(1,.78f,.04f),new Color(.6f,1,.025f),new Color(.02f,.6f,.16f),new Color(.02f,.8f,.85f),new Color(.025f,.12f,.8f),new Color(.45f,.04f,.7f),new Color(.95f,.08f,.4f),new Color(.5f,.3f,.11f)};
    internal static int PaintPaletteSize=>PaintColors.Length;
    int workshopTab,styleCategory,stylePage,buildSlot;
    string buildName="My build",workshopMessage="";
    ConfigEntry<int> WorkshopInt(string section,string key,int value,int max)=>BindVehicle(section,key,value,new ConfigDescription("Saved independently for each vehicle. Zero preserves the original setup.",new AcceptableValueRange<int>(0,max)));
    void InitializeWorkshop() {
        InitializeDecals();
        baseConfig_glassColour=WorkshopInt("Paint","GlassColor",0,PaintPaletteSize-1);baseConfig_glassShade=WorkshopInt("Paint","GlassShade",0,4);
        baseConfig_engineStage=WorkshopInt("Performance","Engine",0,3);baseConfig_turboStage=WorkshopInt("Performance","Turbo",0,3);
        baseConfig_gearStage=WorkshopInt("Performance","Transmission",0,3);baseConfig_tyreStage=WorkshopInt("Performance","Tyres",0,3);
        baseConfig_brakeStage=WorkshopInt("Performance","Brakes",0,3);baseConfig_nosStage=WorkshopInt("Performance","Nitrous",0,3);
        baseConfig_paint=WorkshopInt("Styling","Paint",0,PaintNames.Length-1);baseConfig_finish=WorkshopInt("Styling","Finish",0,3);
        baseConfig_paintTrim=BindVehicle("Styling","PaintChromeTrim",true,"With a custom body colour, also paint exterior chrome trim. Factory preserves original chrome. Independent from rim colour.");
        baseConfig_rimColor=WorkshopInt("Styling","RimColor",0,PaintNames.Length-1);baseConfig_vinyl=WorkshopInt("Styling","Vinyl",0,int.MaxValue);baseConfig_vinylColor=WorkshopInt("Styling","VinylColor",2,PaintNames.Length-1);
        InitializeVinylLayers();
        foreach(var category in StyleCategories)styleSettings[category]=WorkshopInt("Parts",category,0,category=="Rims"?256:128);
        foreach(var profile in vehicleCatalog.Profiles) {
            var slots=new ConfigEntry<string>[3];
            for(int i=0;i<3;i++)slots[i]=Config.Bind("GarageBuilds",profile.id+".Slot"+(i+1),"","Named garage setup, stored across game/world restarts. Cars themselves remain temporary.");
            garageBuilds[profile.id]=slots;
        }
    }
    bool WorkshopParked=>!Flying&&!hordeWave.Active&&body!=null&&body.linearVelocity.magnitude<.5f&&(!driving||DriverInputReady)&&fleet.All(v=>v.Profile.id!=current.Profile.id||v.body==null||v.body.linearVelocity.magnitude<.5f&&!(v.flight!=null&&v.flight.State.Active));
    void PreparePerformance() {
        ClampPaidPerformance();
        vehicleAudio?.SetEngineStage(PerformanceTuning.Stage(engineStage.Value));
        // Remove last frame's tyre multiplier before the proven mode/burnout code.
        foreach(var wheel in wheels) {
            var f=wheel.forwardFriction;f.stiffness/=current.appliedGrip;wheel.forwardFriction=f;
            var s=wheel.sidewaysFriction;s.stiffness/=current.appliedGrip;wheel.sidewaysFriction=s;
        }
        current.appliedGrip=1;
        if(ActiveNitro!=null){ActiveNitro.CapacityMultiplier=PerformanceTuning.Capacity(nosStage.Value);ActiveNitro.SpeedMultiplier=PerformanceTuning.Speed(engineStage.Value,turboStage.Value,gearStage.Value);}
        if(vehicleNitro!=null)vehicleNitro.PowerMultiplier=PerformanceTuning.Boost(nosStage.Value);
    }
    void ApplyPerformanceGrip() {
        float grip=PerformanceTuning.Grip(tyreStage.Value);
        foreach(var wheel in wheels) {
            var f=wheel.forwardFriction;f.stiffness*=grip;wheel.forwardFriction=f;
            var s=wheel.sidewaysFriction;s.stiffness*=grip;wheel.sidewaysFriction=s;
        }
        current.appliedGrip=grip;
    }
    void TickWorkshop() {
        if(current.styling==null)return;
        foreach(var category in StyleCategories) {
            var setting=VehicleConfig(styleSettings[category]);
            if(current.styling.HasCatalog&&!current.styling.Options(category).Any(o=>o.id==setting.Value))setting.Value=0;
            current.styleSelection[category]=setting.Value;
        }
        current.styling.Tick(current.styleSelection,paintChoice.Value,paintFinish.Value,rimColor.Value,vinylChoice.Value,vinylColor.Value,paintTrim.Value,CurrentVinylLayers());
        current.styling.SetWindowTint(glassColour.Value,glassShade.Value);
        TickDecals();
    }
    void PerformanceRow(ConfigEntry<int> entry,string title,float y,bool enabled=true) {
        UiLabel(new Rect(25,y,166,30),title,labelStyle);
        GUI.enabled=WorkshopParked&&enabled;
        int next=UiToolbar(new Rect(195,y,335,30),PerformanceTuning.Stage(entry.Value),new[]{"Stock","Stage 1","Stage 2","Stage 3"});
        if(next!=entry.Value)SetPaidPerformance(entry,next);GUI.enabled=true;
    }
    void DrawPerformanceMenu() {
        PerformanceRow(engineStage,"Engine",143);PerformanceRow(turboStage,"Turbo / intake",183);
        PerformanceRow(gearStage,"Transmission",223);PerformanceRow(tyreStage,"Tyres",263);
        PerformanceRow(brakeStage,"Brakes",303);PerformanceRow(nosStage,"Nitrous",343,current.Profile.capabilities.nitro);
        float speed=PerformanceTuning.Speed(engineStage.Value,turboStage.Value,gearStage.Value);
        UiLabel(new Rect(25,389,510,59),"Torque ×"+PerformanceTuning.Torque(engineStage.Value,turboStage.Value,gearStage.Value).ToString("0.00")+"   Grip ×"+PerformanceTuning.Grip(tyreStage.Value).ToString("0.00")+"   Brakes ×"+PerformanceTuning.Brakes(brakeStage.Value).ToString("0.00")+"\nSpeed limiter: "+Mathf.RoundToInt((EffectiveTerrain?20:34)*speed*3.6f)+" km/h   NOS: "+(6*PerformanceTuning.Capacity(nosStage.Value)).ToString("0.0")+" s",labelStyle);
        GUI.enabled=WorkshopParked;
        if(MenuButton(new Rect(25,454,245,32),"Stock performance"))SetPerformancePackage(0);
        if(MenuButton(new Rect(285,454,245,32),"Stage 3 package"))SetPerformancePackage(3);
        GUI.enabled=true;
        UiLabel(new Rect(25,491,510,40),WorkshopParked?(string.IsNullOrEmpty(workshopMessage)?GarageEconomySummary:workshopMessage):"Park and finish the horde run before changing parts.",mutedStyle);
    }
    void SetPerformancePackage(int stage){var wanted=Enumerable.Repeat(stage,6).ToArray();if(!current.Profile.capabilities.nitro)wanted[5]=0;PurchasePerformance(wanted,()=>{var entries=PerformanceEntries();for(int i=0;i<6;i++)entries[i].Value=wanted[i];});}
    void DrawStylingMenu() {
        bool mw=current.Profile.sourceGame==CarCatalog.MostWanted;workshopTab=Math.Min(workshopTab,mw?5:3);
        workshopTab=UiToolbar(new Rect(25,140,510,30),workshopTab,mw?new[]{"Paint","Parts","Wheels","Vinyl","Glass","Decals"}:new[]{"Paint","Body parts","Wheels","Vinyl"});
        if(workshopTab==5){DrawDecalWorkshop();return;}
        if(workshopTab==1||workshopTab==2){DrawPartsBrowser(workshopTab==2);return;}
        GUI.enabled=WorkshopParked;
        if(workshopTab==0) {
            UiLabel(new Rect(25,185,510,28),"Body paint",labelStyle);
            paintChoice.Value=UiGrid(new Rect(25,220,510,152),paintChoice.Value,PaintNames,3);
            UiLabel(new Rect(25,395,510,25),"Finish",labelStyle);
            paintFinish.Value=UiToolbar(new Rect(25,428,510,32),paintFinish.Value,new[]{"Original","Gloss","Metallic","Matte"});
            paintTrim.Value=UiToggle(new Rect(25,465,510,25),paintTrim.Value," Paint chrome trim with the body");
        }else if(workshopTab==4) {
            UiLabel(new Rect(25,185,510,28),"Window tint",labelStyle);
            glassColour.Value=UiGrid(new Rect(25,220,510,152),glassColour.Value,PaintNames,3);
            UiLabel(new Rect(25,395,510,25),"Opacity",labelStyle);
            glassShade.Value=UiToolbar(new Rect(25,428,510,32),glassShade.Value,new[]{"Original","Light","Medium","Dark","Limo"});
        }else {
            DrawVinylWorkshop();
        }
        GUI.enabled=true;
        UiLabel(new Rect(25,495,510,25),WorkshopParked?"Live preview · saved for this vehicle · Builds stores full setups.":"Park before changing the setup.",mutedStyle);
    }
    void DrawPartsBrowser(bool rims) {
        var style=current.styling;
        if(style==null||!style.HasCatalog){UiLabel(new Rect(25,194,510,68),"Original parts catalog is unavailable. Build the local styling assets first.",labelStyle);return;}
        if(!rims) {
            int next=UiGrid(new Rect(25,186,510,99),styleCategory,StyleLabels.Take(StyleCategories.Length-1).ToArray(),3);
            if(next!=styleCategory){styleCategory=next;stylePage=0;}
        }else {
            GUI.enabled=WorkshopParked;rimColor.Value=UiGrid(new Rect(25,183,510,95),rimColor.Value,PaintNames,5);GUI.enabled=true;
        }
        string category=rims?"Rims":StyleCategories[styleCategory];
        var options=style.Options(category);var entry=VehicleConfig(styleSettings[category]);
        stylePage=Mathf.Clamp(stylePage,0,(options.Length-1)/9);
        bool supported=style.Supports(category,VehicleConfig(styleSettings["Bodykit"]).Value);
        GUI.enabled=WorkshopParked&&supported;
        int offset=stylePage*9;var page=options.Skip(offset).Take(9).ToArray();
        int selected=Array.FindIndex(page,o=>o.id==entry.Value);
        int choice=UiGrid(new Rect(25,296,510,135),selected,page.Select(o=>o.title).ToArray(),3);
        if(choice!=selected&&choice>=0){entry.Value=page[choice].id;if(category=="Rims"&&page[choice].rimInches>0)RimSetting.Value=page[choice].rimInches;}
        GUI.enabled=true;
        GUI.enabled=stylePage>0;if(MenuButton(new Rect(25,448,120,30),"‹ Previous"))stylePage--;GUI.enabled=true;
        UiLabel(new Rect(159,451,240,27),category+"   "+(stylePage+1)+" / "+((options.Length+8)/9),labelStyle);
        GUI.enabled=offset+9<options.Length;if(MenuButton(new Rect(410,448,120,30),"Next ›"))stylePage++;GUI.enabled=true;
        UiLabel(new Rect(25,487,510,32),!supported?"Widebody uses integrated bumpers and skirts. Choose the standard bodykit first.":"Original "+(current.Profile.sourceGame==CarCatalog.MostWanted?"MW":"U2")+" geometry · factory restores the installed model.",mutedStyle);
    }
    IEnumerable<ConfigEntryBase> CurrentBuildSettings() {
        foreach(var pair in vehicleSettings.Values)if(pair.TryGetValue(current.Profile.ConfigPrefix,out var entry))yield return entry;
        yield return RimSetting;
        foreach(var entry in CurrentDecalSettings())yield return entry;
    }
    sealed class GarageBuild {
        public int version=1;public string vehicle,name;public Dictionary<string,string> settings;
    }
    void DrawBuildMenu() {
        var slots=garageBuilds[current.Profile.id];
        buildSlot=UiToolbar(new Rect(25,145,510,35),buildSlot,new[]{"Build 1","Build 2","Build 3"});
        GarageBuild saved=null;
        try {if(slots[buildSlot].Value.Length>0)saved=JsonConvert.DeserializeObject<GarageBuild>(slots[buildSlot].Value);}catch{}
        UiLabel(new Rect(25,202,510,48),VehicleName+" / "+(saved?.name??"Empty slot"),titleStyle);
        UiLabel(new Rect(25,263,510,26),"Build name",labelStyle);
        buildName=GUI.TextField(new Rect(25,299,510,32),buildName,32);
        GUI.enabled=WorkshopParked;
        if(MenuButton(new Rect(25,353,245,36),"Save current setup")) {
            var build=new GarageBuild{vehicle=current.Profile.id,name=string.IsNullOrWhiteSpace(buildName)?"Build "+(buildSlot+1):buildName.Trim(),settings=CurrentBuildSettings().ToDictionary(e=>e.Definition.Section+"/"+e.Definition.Key,e=>e.GetSerializedValue())};
            slots[buildSlot].Value=JsonConvert.SerializeObject(build);Config.Save();workshopMessage="Build saved.";
        }
        GUI.enabled=WorkshopParked&&saved!=null;
        if(MenuButton(new Rect(285,353,245,36),"Load saved setup"))LoadGarageBuild(saved);
        GUI.enabled=WorkshopParked;
        if(MenuButton(new Rect(25,406,510,34),"Restore original vehicle setup")) {
            if(!CanSetTrunkLayout((int)trunkLayout.DefaultValue)){workshopMessage="Unload cargo before restoring the trunk layout.";GUI.enabled=true;return;}
            foreach(var entry in CurrentBuildSettings())entry.BoxedValue=entry.DefaultValue;
            ApplyDriveMode();ApplyShowroomSettings();TickWorkshop();Config.Save();workshopMessage="Original setup restored. Saved builds retained.";
        }
        GUI.enabled=true;
        UiLabel(new Rect(25,452,510,30),workshopMessage,labelStyle);
        UiLabel(new Rect(25,487,510,32),"Three builds per car · tuning, styling, wheels, lights, audio and trunk. Saved across restarts.",mutedStyle);
    }
    void LoadGarageBuild(GarageBuild build) {
        var entries=CurrentBuildSettings().ToDictionary(e=>e.Definition.Section+"/"+e.Definition.Key);
        var before=entries.ToDictionary(e=>e.Key,e=>e.Value.GetSerializedValue());
        bool autoSave=Config.SaveOnConfigSet;Config.SaveOnConfigSet=false;
        try {
            if(build.version!=1||build.vehicle!=current.Profile.id||build.settings==null||build.settings.Count>512)throw new InvalidOperationException("Invalid build for this vehicle");
            // Do not shrink an occupied cargo container through a saved build.
            string cargo=trunkLayout.Definition.Section+"/"+trunkLayout.Definition.Key;
            if(build.settings.TryGetValue(cargo,out var layout)&&int.TryParse(layout,out var next)&&!CanSetTrunkLayout(next))throw new InvalidOperationException("Unload cargo before changing the trunk layout");
            RestoreMissingVinylSettings(build.settings);
            RestoreMissingDecalSettings(build.settings);
            foreach(var setting in build.settings)if(entries.TryGetValue(setting.Key,out var entry))entry.SetSerializedValue(setting.Value);
            ApplyDriveMode();ApplyShowroomSettings();TickWorkshop();
            if(!PurchasePerformance(PerformanceEntries().Select(e=>PerformanceTuning.Stage(e.Value)).ToArray()))throw new InvalidOperationException(workshopMessage);
            buildName=build.name;workshopMessage="Build loaded.";
        }catch(Exception error) {
            foreach(var entry in entries)entry.Value.SetSerializedValue(before[entry.Key]);
            ApplyDriveMode();ApplyShowroomSettings();workshopMessage=error.Message;
        }
        finally {Config.SaveOnConfigSet=autoSave;}
    }
}
}
