using System;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    GUIStyle frontendHeading,frontendSmall;
    static Rect GaragePreviewRect=>new Rect(0,180,950,630);
    string GarageStatusLine() {
        if(workshopMessage.Length>0)return Tr(workshopMessage);
        var vehicle=GaragePreviewReady?garagePreview.networkPreviewOf??current:current;
        if(NetworkSessionActive&&PreviewTab(menuTab)&&vehicle?.networkSettings!=null) {
            string blocked=NetworkVehicleEditBlock(vehicle);
            if(blocked!=null)return Tr(blocked);
            return vehicle.networkSetupInFlight||vehicle.networkSettings.Version!=vehicle.networkSetupAcknowledgedVersion?
                Tr("Saving changes to server…"):Tr("Server-confirmed setup")+" · "+Tr("Revision")+" "+vehicle.networkSetupRevision;
        }
        return "GARAGE  "+fleet.Count+" / 4    •    "+(IsMwFrontend?"MOST WANTED":"UNDERGROUND 2")+"    •    THREE SAVED BUILDS PER CAR";
    }
    void FrontendArt(string name,Rect rect,Color? tint=null) {
        if(menuAssets==null)return;
        Texture2D texture=null;if(IsMwFrontend||name.StartsWith("car_mw-",StringComparison.Ordinal))menuAssets.MwFrontend.TryGetValue(name,out texture);
        if(texture==null&&!menuAssets.Frontend.TryGetValue(name,out texture))return;
        var color=GUI.color;if(tint.HasValue)GUI.color=tint.Value;GUI.DrawTexture(rect,texture,ScaleMode.ScaleToFit,true);GUI.color=color;
    }
    void FrontendFill(Rect rect,Color colour) {var old=GUI.color;GUI.color=colour;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
    void DrawGarageFrontend() {
        EnsureMenuStyles();
        if(skinWasMw!=IsMwFrontend)BuildMenuSkin();
        if(frontendHeading==null){frontendHeading=new GUIStyle(titleStyle){fontSize=30,fontStyle=FontStyle.BoldAndItalic};frontendSmall=new GUIStyle(mutedStyle){fontSize=15};frontendSmall.normal.textColor=new Color(.9f,.92f,.94f);}
        uiCount=0;uiRegion=0;uiBuilding=false;uiPointerInRegion=true;
        if(Event.current.type==EventType.Repaint)uiBounds.Clear();
        if(menuMouseLookHeld&&(Event.current.isMouse||Event.current.type==EventType.ScrollWheel))Event.current.Use();
        var oldMatrix=GUI.matrix;var oldSkin=GUI.skin;bool oldEnabled=GUI.enabled,oldChanged=GUI.changed;int oldDepth=GUI.depth;
        var oldColor=GUI.color;var oldContent=GUI.contentColor;var oldBackground=GUI.backgroundColor;
        try {
            GUI.depth=-100;GUI.skin=menuSkin;GUI.changed=false;GUI.enabled=true;GUI.color=GUI.contentColor=GUI.backgroundColor=Color.white;
            GUI.matrix=Matrix4x4.identity;FrontendFill(new Rect(0,0,Screen.width,Screen.height),new Color(.012f,.018f,.022f,1));
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)*.5f,(Screen.height-900*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
            if(garageFrame!=null)GUI.DrawTexture(GaragePreviewRect,garageFrame,ScaleMode.StretchToFill,false);
            FrontendFill(new Rect(0,0,1600,110),IsMwFrontend?new Color(.075f,.07f,.045f,.95f):new Color(.008f,.014f,.022f,.95f));
            FrontendFill(new Rect(0,110,1600,62),new Color(.008f,.014f,.022f,.9f));
            FrontendFill(new Rect(964,180,612,630),IsMwFrontend?new Color(.045f,.04f,.025f,.96f):new Color(.012f,.022f,.028f,.96f));
            FrontendFill(new Rect(964,180,612,3),MenuAccent);
            FrontendFill(new Rect(0,822,1600,78),new Color(.008f,.014f,.022f,.9f));
            if(IsMwFrontend){
                FrontendArt("grit",new Rect(0,180,950,630),new Color(.7f,.68f,.56f,.08f));
                FrontendArt("banner",new Rect(964,183,612,100),new Color(.6f,.58f,.45f,.2f));
                FrontendArt("vignette",new Rect(964,655,612,155),new Color(.6f,.58f,.45f,.18f));
                UiLabel(new Rect(30,9,320,20),"NEED FOR SPEED",frontendSmall);FrontendArt("wordmark",new Rect(25,30,330,68));
                string header="nav_"+MenuRoot(menuTab);
                if(menuAssets!=null&&menuAssets.MwFrontend.ContainsKey(header))FrontendArt(header,new Rect(390,22,610,40));
                else UiLabel(new Rect(390,22,610,40),MenuTitle(MenuRoot(menuTab)),frontendHeading);
            }else{
                FrontendArt("fx",new Rect(964,183,612,100),new Color(.2f,.85f,1,.2f));
                FrontendArt("logo",new Rect(25,16,330,82));
                UiLabel(new Rect(390,22,610,40),MenuTitle(MenuRoot(menuTab)),frontendHeading);
            }
            UiLabel(new Rect(394,70,720,26),"NFVU2  /  "+FrontendProfile.displayName,frontendSmall);
            if(MenuButton(new Rect(1423,34,148,43),"CLOSE  ×")){CloseVehicleMenu();return;}
            if(MenuButton(new Rect(1178,36,100,40),"U2",!IsMwFrontend?choiceSelectedStyle:null)){menuFrontend.Value="U2";Config.Save();}
            if(MenuButton(new Rect(1288,36,100,40),"MW",IsMwFrontend?choiceSelectedStyle:null)){menuFrontend.Value="MW";Config.Save();}
            MenuNavigation(true);
            UiLabel(new Rect(30,201,880,38),FrontendProfile.displayName.ToUpperInvariant(),frontendHeading);
            FrontendArt("car_"+FrontendProfile.id,new Rect(35,250,285,66));
            if(garagePreviewStatus.Length>0)UiLabel(new Rect(140,420,670,100),garagePreviewStatus,frontendHeading);
            FrontendFill(new Rect(20,735,910,65),new Color(.01f,.02f,.025f,.78f));
            UiLabel(new Rect(36,742,870,25),Mathf.RoundToInt(FrontendProfile.rig.mass)+" KG   /   "+FrontendProfile.sound.gearRatios.Length+" SPEED   /   "+FrontendProfile.rig.rimNominal+" INCH",frontendSmall);
            UiLabel(new Rect(36,771,870,24),"RIGHT MOUSE / RIGHT STICK: ROTATE    •    WHEEL: ZOOM",frontendSmall);
            var actual=current;
            bool preview=PreviewTab(menuTab);
            bool borrowedPreview=preview&&GaragePreviewReady;
            if(borrowedPreview)current=garagePreview;
            uiBuilding=true;
            GUI.BeginGroup(new Rect(990,200,560,594));
            GUI.BeginGroup(new Rect(0,-99,560,693));
            try {
                UiLabel(new Rect(25,99,510,35),MenuTitle(MenuRoot(menuTab)),frontendHeading);
                if(preview&&!GaragePreviewReady)UiLabel(new Rect(25,175,510,120),garagePreviewStatus.Length==0?"Loading car preview…":garagePreviewStatus,labelStyle);
                else switch(menuTab){case 16:DrawShowcaseMenu();break;case 15:DrawBuildMenu();break;case 14:DrawStylingMenu();break;case 13:DrawPerformanceMenu();break;case 12:DrawExhaustMenu();break;case 11:DrawInterfaceSettings();break;case 10:DrawGarageMenu();break;case 0:DrawVehiclePages();break;case 1:DrawRadioMenu();break;case 2:DrawLightsMenu();break;case 3:DrawNeonMenu();break;case 4:DrawNitroMenu();break;case 5:DrawWheelMenu();break;case 6:DrawBodyMenu();break;case 7:DrawTrunkMenu();break;case 8:DrawDrivingDisplayMenu();break;case 9:DrawTurretMenu();break;}
            }finally{GUI.EndGroup();GUI.EndGroup();if(borrowedPreview)current=actual;uiBuilding=false;}
            if(menuTab==16&&(showcasePage==2||showcasePage==3)) {
                uiBuilding=true;
                UiLabel(new Rect(1015,706,510,27),"LIVE PREVIEW · no nitro consumption",frontendSmall);
                garageEffectMode=UiToolbar(new Rect(1015,745,510,34),garageEffectMode,new[]{"Off","Flames","Purge","Backfire"});
                uiBuilding=false;
            }
            uiLastCount=uiCount;uiFocus=Math.Min(uiFocus,Math.Max(0,uiCount-1));
            UiLabel(new Rect(32,835,1420,31),padActive?"B: BACK    •    LB/RB: CATEGORIES / CONTENT    •    A: SELECT    •    RIGHT STICK: ROTATE":"ESC: BACK    •    ↑↓←→: NAVIGATE    •    TAB: CATEGORIES / CONTENT    •    ENTER: SELECT    •    MOUSE: SELECT / ROTATE",frontendSmall);
            UiLabel(new Rect(32,870,1536,25),GarageStatusLine(),frontendSmall);
            if(Event.current.type==EventType.Repaint){uiActivate=false;uiHorizontal=uiVertical=0;}
        }finally{GUI.depth=oldDepth;GUI.matrix=oldMatrix;GUI.skin=oldSkin;GUI.enabled=oldEnabled;GUI.color=oldColor;GUI.contentColor=oldContent;GUI.backgroundColor=oldBackground;GUI.changed|=oldChanged;uiBuilding=false;}
    }
}
}
