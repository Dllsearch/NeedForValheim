using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace NeedForValheim {
public partial class Plugin {
    ConfigEntry<KeyboardShortcut> menuKey;
    static readonly string[] ColorNames={"Green","Blue","Cyan","Purple","Red","Yellow","White"};
    static readonly Color[] NeonColors={new Color(.2f,1,.08f),new Color(.12f,.3f,1),Color.cyan,new Color(.7f,.08f,1),Color.red,new Color(1,.8f,.05f),Color.white};
    bool menuOpen;
    int menuTab;
    CursorLockMode menuOldLock;
    bool menuOldVisible;
    MonoBehaviour menuPlayer, menuCamera;
    bool menuPlayerEnabled, menuCameraEnabled;
    Texture2D menuBackground;
    GUIStyle panelStyle, titleStyle, labelStyle, mutedStyle, songButtonStyle;
    void InitializeMenu() {
        InitializeMenuPlacement();
        InitializeMenuTheme();
        InitializeWheelTuning();
        InitializeShowroomMenu();
        InitializeWorkshop();
        InitializeDrivingMenu();
        InitializeTurretMenu();
        keepRunning=BindVehicle("Vehicle","KeepRunningAfterExit",true,"After driving, keep the engine idling, enabled neon and muffled spatial radio running outside. Can be toggled beside the car.");
        headlightPreset=BindVehicle("Lights","HeadlightPreset","Stock",new ConfigDescription("Front headlight colour preset. Names follow NFSU2; RGB values are adapted for Unity.",new AcceptableValueList<string>(VehicleLighting.HeadlightPresets)));
        headlightStrength=BindVehicle("Lights","HeadlightStrength",1f,new ConfigDescription("Front headlight strength multiplier: 0 to 3. Rear, reverse and plate lights are independent.",new AcceptableValueRange<float>(0,3)));
        headlightShowOff=BindVehicle("Lights","ShowOffAfterExit",false,"After exiting: two left flashes, two right flashes, pause, repeat. Left/right from the driver's seat. Stops on entry; independent of parked engine/radio.");
        menuKey=Config.Bind("Controls","VehicleMenu",new KeyboardShortcut(KeyCode.None),"Optional global menu shortcut. Function keys are disabled. Use Inventory > CARS / GARAGE, or Tab while seated.");
        if(FunctionKey(menuKey.Value.MainKey)||menuKey.Value.Modifiers.Any(FunctionKey))menuKey.Value=new KeyboardShortcut(KeyCode.None);
        nitroEnabled=BindVehicle("Nitro","Enabled",true,"Hold left mouse: forward throttle boosts; stationary purges. Six seconds of boost, refills while driving after release.");
        backfireEnabled=BindVehicle("Nitro","LiftOffBackfire",false,"Optional high-RPM lift-off effect. Uses original assets with NFVU2 trigger thresholds, not verified NFS tuning rules.");
        radioEnabled=BindVehicle("Radio","Enabled",true,"Play the local NFS radio while seated, and outside if KeepRunningAfterExit is enabled.");
        radioVolume=BindVehicle("Radio","Volume",.55f,new ConfigDescription("Radio volume, independent of engine volume.",new AcceptableValueRange<float>(0,1)));
        neonEnabled=BindVehicle("Neon","Enabled",false,"Enable underbody neon while seated, and outside if KeepRunningAfterExit is enabled.");
        neonPulsing=BindVehicle("Neon","Pulsing",false,"Pulse the underbody neon.");
        neonColor=BindVehicle("Neon","Color","Green",new ConfigDescription("Underbody neon colour.",new AcceptableValueList<string>(ColorNames)));
    }
    bool CanUseVehicleMenu(MonoBehaviour local)=>local!=null&&!Multiplayer();
    static bool FunctionKey(KeyCode key)=>key>=KeyCode.F1&&key<=KeyCode.F15;
    bool MenuShortcutDown=>menuKey.Value.MainKey!=KeyCode.None&&!FunctionKey(menuKey.Value.MainKey)&&!menuKey.Value.Modifiers.Any(FunctionKey)&&menuKey.Value.IsDown();
    internal bool FrontendOpen=>ready&&menuOpen;
    void BlockDrivingInput(bool blockCamera=true){controlsGate.Block();handbrakeHeld=turretHeld=false;throttle=steer=0;current.flightVertical=0;brake=1;honking=false;burnoutHeld=false;nitroHeld=false;nitroInput.Reset();ActiveNitro?.Stop();if(blockCamera)orbitCamera?.InputFrame(false,mouseSensitivity.Value,invertMouseY.Value);}
    void SetVehicleMenu(bool open) {
        if(!open)garageStationContext=null;
        if(open==menuOpen)return;
        if(open){
            EnsureMenuAssets();MenuSound(1);hoveredTab=-1;
            if(!CanAccessCurrent(LocalPlayer()))menuTab=10;
            menuOldLock=Cursor.lockState;menuOldVisible=Cursor.visible;menuOpen=true;
            if(!driving){
                menuPlayer=LocalPlayer();if(menuPlayer!=null){menuPlayerEnabled=menuPlayer.enabled;menuPlayer.enabled=false;}
                var type=playerType.Assembly.GetType("GameCamera");
                menuCamera=type?.GetProperty("instance",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null) as MonoBehaviour;
                if(menuCamera!=null){menuCameraEnabled=menuCamera.enabled;menuCamera.enabled=false;}
            }
            BeginMenuCamera();
            BeginGarageFrontend();
            keyboardMenuActive=false;uiFocus=0;uiNavigation.SelectPane(true);uiLastDirection=0;uiRepeat=0;uiWaitNeutral=true;controlsGate.Block();
            BlockDrivingInput();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        else {
            controlsGate.Block();uiActivate=false;uiHorizontal=uiVertical=0;
            StopMenuDrag();SaveMenuPlacement();
            MenuSound(2);
            menuOpen=false;EndGarageFrontend();EndMenuCamera();
            if(menuPlayer!=null)menuPlayer.enabled=menuPlayerEnabled;
            ReleaseCameraRumble(menuCamera);
            if(menuCamera!=null)menuCamera.enabled=menuCameraEnabled;
            menuPlayer=null;menuCamera=null;
            Cursor.lockState=menuOldLock;Cursor.visible=menuOldVisible;Config.Save();
        }
    }
    void CloseVehicleMenu(){if(menuOpen)SetVehicleMenu(false);}
    void ApplyNeonSettings() {
        if(vehicleNeon==null)return;
        vehicleNeon.Enabled=neonEnabled.Value;vehicleNeon.Pulsing=neonPulsing.Value;
        int index=Array.IndexOf(ColorNames,neonColor.Value);vehicleNeon.Color=NeonColors[Math.Max(0,index)];
    }
    void EnsureMenuStyles() {
        if(panelStyle!=null)return;
        menuBackground=new Texture2D(1,1);menuBackground.SetPixel(0,0,new Color(.035f,.045f,.055f,.98f));menuBackground.Apply();
        panelStyle=new GUIStyle(GUI.skin.box);panelStyle.normal.background=menuBackground;
        titleStyle=new GUIStyle(GUI.skin.label){fontSize=25,fontStyle=FontStyle.Bold};titleStyle.normal.textColor=new Color(.5f,1,.15f);
        labelStyle=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};labelStyle.normal.textColor=Color.white;
        mutedStyle=new GUIStyle(labelStyle){fontSize=13};mutedStyle.normal.textColor=new Color(.65f,.7f,.72f);
        songButtonStyle=new GUIStyle(GUI.skin.button){fontSize=13,wordWrap=true,alignment=TextAnchor.MiddleLeft};
        titleStyle.normal.textColor=MenuAccent;
        BuildMenuSkin();
    }
    void DisposeMenu(){DisposeMenuTheme();if(menuBackground!=null)Destroy(menuBackground);menuBackground=null;panelStyle=titleStyle=labelStyle=mutedStyle=songButtonStyle=null;}
    void OnGUI() {
        if(visualAuditActive){DrawVisualAudit();return;}
        if(!menuOpen||audioBlocked||!Application.isFocused)StopMenuDrag();
        if(!ready||LocalPlayer()==null)return;
        DrawControlHints();
        DrawSpawnProgress();
        DrawDashboard();
        DrawTurretAim();
        DrawHordeHud();
        if(driving&&!Flying&&!hudEnabled.Value&&ActiveNitro!=null&&nitroEnabled.Value)
            UiBox(new Rect(15,Screen.height-120,310,32),(Flying?"LMB + W: flight nitro":"LMB: Nitro / Purge")+" | Tank "+Mathf.RoundToInt(ActiveNitro.Charge*100)+" %");
        if(!menuOpen||audioBlocked)return;
        DrawGarageFrontend();
    }

    void DrawCarMenu() {
        if(car==null){UiLabel(new Rect(25,155,510,100),"Choose a car in Garage → Car lot first. Use My garage to select a parked car.",labelStyle);return;}
        UiLabel(new Rect(25,145,510,30),"Drive mode",labelStyle);
        var caps=current.Profile.capabilities;
        var modes=new List<int>{0,1};var modeNames=new List<string>{"Off-road","Road"};
        if(caps.drift){modes.Add(2);modeNames.Add("Drift");}
        if(caps.flight){modes.Add(3);modeNames.Add("Flight");}
        int active=HordeVehicle?0:Flying&&!current.flight.State.Landing?3:caps.drift&&driftMode.Value?2:terrainMode.Value?0:1;
        int selected=Math.Max(0,modes.IndexOf(active));
        GUI.enabled=!HordeVehicle;
        int mode=UiToolbar(new Rect(25,180,510,30),selected,modeNames.ToArray());
        if(mode!=selected)SelectControlMode(modes[Math.Min(Math.Max(mode,0),modes.Count-1)]);
        GUI.enabled=true;
        string description=Flying?(current.flight.State.Landing?"Lands automatically and unfolds the wheels.\nW/S and A/D: adjust the landing spot \u00b7 Flight: cancel":"W/S: forward/back \u00b7 A/D: turn \u00b7 Shift: up \u00b7 Space: down\nNo input keeps it hovering. Choose a ground mode to land."):
            (!driving?"Enter the vehicle before using flight mode.\n":"")+(mode==2&&caps.drift?"Drift: rear-wheel drive \u00b7 Space: handbrake":mode==0?"Off-road mode with all-wheel assistance.":"Road mode with rear-wheel drive.");
        if(padActive)description=Flying?(current.flight.State.Landing?"Automatically lands and unfolds the wheels.":ButtonName("RT/LT","R2/L2")+": forward/back \u00b7 LS: turn \u00b7 "+ButtonName("RB/LB","R1/L1")+": up/down")+"\nChoose a ground mode to land.":ButtonName("RT/LT","R2/L2")+": throttle/brake \u00b7 LS: steer \u00b7 "+ButtonName("A","Cross")+": Handbrake";
        UiLabel(new Rect(25,218,510,52),description,mutedStyle);
        GUI.enabled=!Flying&&(!driving||DriverInputReady);
        if(MenuButton(new Rect(25,280,245,38),driving?"Exit":"Enter")){
            var local=LocalPlayer();CloseVehicleMenu();if(driving)RequestExit();else if(CanAccessCurrent(local))Enter(local);return;
        }
        GUI.enabled=!SpawnBusy;
        if(MenuButton(new Rect(285,280,245,38),"UNSPAWN")){UnspawnCurrent();return;}
        GUI.enabled=true;
        keepRunning.Value=UiToggle(new Rect(25,340,510,32),keepRunning.Value," Keep running after exit");
        UiLabel(new Rect(25,378,510,60),"Idling engine, enabled neon and muffled music. Can also be switched beside the car.\nRemoving the car leaves its cargo in a chest.",mutedStyle);
        GUI.enabled=showroom!=null;
        if(MenuButton(new Rect(25,445,245,38),"Doors and hood"))ChangeMenuTab(6);
        if(MenuButton(new Rect(285,445,245,38),"Trunk layout"))ChangeMenuTab(7);
        GUI.enabled=true;
        DrawStorageButton(new Rect(25,488,245,30));
        UiLabel(new Rect(285,488,245,30),"Park and walk to the rear to load cargo.",mutedStyle);
    }
    void DrawNitroMenu() {
        bool groundNitro=current.Profile.capabilities.nitro;
        nitroEnabled.Value=UiToggle(new Rect(25,145,510,30),nitroEnabled.Value,groundNitro?" enable nitro":" enable nitro in flight");
        UiLabel(new Rect(25,186,510,66),padActive?ButtonName("X","Square")+": Nitro · "+ButtonName("RT","R2")+": throttle\n"+(groundNitro?"Nitro on the ground and in flight. Stationary purge.":"Flight nitro only.")
            :groundNitro
            ?"Nitro on the ground and in flight; stationary purge. Six-second tank; refills while moving."
            :"In flight W + LMB/Ctrl: more thrust. No nitro on the ground. Six-second tank; refills while moving.",labelStyle);
        if(groundNitro)backfireEnabled.Value=UiToggle(new Rect(25,270,510,30),backfireEnabled.Value," exhaust flames on throttle release");
        DrawNitroVisualSettings();
    }

    void DrawRadioMenu() {DrawRadioTraxMenu();}
    void DrawLightsMenu() {
        if(DrawVehicleWorldFeaturePage())return;
        GUI.enabled=vehicleLighting!=null;
        bool lights=UiToggle(new Rect(25,140,510,32),vehicleLighting!=null&&vehicleLighting.Enabled," headlights, taillights and plate light");
        if(vehicleLighting!=null)vehicleLighting.Enabled=lights;
        bool showOff=UiToggle(new Rect(25,176,510,30),headlightShowOff.Value," show-off on exit: 2\u00d7 left / 2\u00d7 right");
        if(showOff!=headlightShowOff.Value)headlightShowOff.Value=showOff;
        UiLabel(new Rect(25,207,510,32),"Alternating double flashes, then pause. Driver-relative sides. Stops on entry.",mutedStyle);
        float strength=headlightStrength.Value;
        if(float.IsNaN(strength)||float.IsInfinity(strength))strength=1;
        UiLabel(new Rect(25,242,510,26),"Headlight brightness   "+Mathf.RoundToInt(strength*100)+" %",labelStyle);
        float next=Mathf.Round(UiSlider(new Rect(25,271,510,20),strength,0,3)*20)/20;
        if(next!=headlightStrength.Value)headlightStrength.Value=next;
        UiLabel(new Rect(25,296,510,25),"Headlight colour",labelStyle);
        int selected=Math.Max(0,Array.IndexOf(VehicleLighting.HeadlightPresets,headlightPreset.Value));
        int choice=UiGrid(new Rect(25,324,510,112),selected,VehicleLighting.HeadlightPresets,4,songButtonStyle);
        if(choice!=selected)headlightPreset.Value=VehicleLighting.HeadlightPresets[choice];
        if(MenuButton(new Rect(25,446,510,30),"Reset headlights")) {headlightPreset.Value="Stock";headlightStrength.Value=1;headlightShowOff.Value=false;}
        GUI.enabled=true;
        if(MenuButton(new Rect(25,482,510,32),"Map / car surface effects"))worldFeaturePage=true;
    }
    void DrawNeonMenu() {
        GUI.enabled=vehicleNeon!=null;
        bool enabled=UiToggle(new Rect(25,145,510,32),neonEnabled.Value," Unterbodenbeleuchtung einschalten");
        bool pulse=UiToggle(new Rect(25,190,510,32),neonPulsing.Value," pulsing lights");
        UiLabel(new Rect(25,244,510,30),"Colour",labelStyle);
        int selected=Math.Max(0,Array.IndexOf(ColorNames,neonColor.Value));
        int choice=UiGrid(new Rect(25,285,510,100),selected,ColorNames,4);
        if(enabled!=neonEnabled.Value||pulse!=neonPulsing.Value||choice!=selected){neonEnabled.Value=enabled;neonPulsing.Value=pulse;neonColor.Value=ColorNames[choice];ApplyNeonSettings();}
        GUI.enabled=true;
        UiLabel(new Rect(25,420,510,70),"Colour and pulse mode are saved. Keep running after exit also keeps enabled neon on outside.",mutedStyle);
    }
}
}
