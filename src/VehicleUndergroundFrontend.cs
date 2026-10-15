using System;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    GUIStyle frontendHeading,frontendSmall;
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
        if(frontendHeading==null){frontendHeading=new GUIStyle(titleStyle){fontSize=30,fontStyle=FontStyle.BoldAndItalic};frontendSmall=new GUIStyle(mutedStyle){fontSize=14};}
        uiCount=0;uiRegion=0;uiBuilding=false;uiPointerInRegion=true;
        if(Event.current.type==EventType.Repaint)uiBounds.Clear();
        if(menuMouseLookHeld&&(Event.current.isMouse||Event.current.type==EventType.ScrollWheel))Event.current.Use();
        var oldMatrix=GUI.matrix;var oldSkin=GUI.skin;bool oldEnabled=GUI.enabled,oldChanged=GUI.changed;int oldDepth=GUI.depth;
        try {
            GUI.depth=-100;GUI.skin=menuSkin;GUI.changed=false;
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)*.5f,(Screen.height-900*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
            if(garageFrame!=null)GUI.DrawTexture(new Rect(0,0,1200,900),garageFrame,ScaleMode.StretchToFill,false);
            FrontendFill(new Rect(0,0,1600,128),IsMwFrontend?new Color(.075f,.07f,.045f,.65f):new Color(.008f,.014f,.022f,.42f));
            FrontendFill(new Rect(0,128,290,686),new Color(.008f,.014f,.022f,.18f));
            FrontendFill(new Rect(1040,128,560,686),new Color(.012f,.022f,.028f,.64f));
            FrontendFill(new Rect(0,814,1600,86),new Color(.008f,.014f,.022f,.6f));
            if(IsMwFrontend){
                FrontendArt("grit",new Rect(-115,110,445,705),new Color(.7f,.68f,.56f,.26f));
                FrontendArt("banner",new Rect(1030,120,570,145),new Color(.6f,.58f,.45f,.36f));
                FrontendArt("vignette",new Rect(1000,664,620,155),new Color(.6f,.58f,.45f,.25f));
                if(menuTab==1)FrontendArt("trax",new Rect(1000,300,35,115));
                FrontendArt("graf",new Rect(27,684,236,115),new Color(1,1,1,.55f));
                UiLabel(new Rect(38,11,410,22),"NEED FOR SPEED",frontendSmall);FrontendArt("wordmark",new Rect(28,27,430,89));
                FrontendArt("nav_"+MenuRoot(menuTab),new Rect(510,32,620,48));
            }else{
                FrontendArt("fx",new Rect(1030,120,570,170),new Color(.2f,.85f,1,.38f));
                FrontendArt("logo",new Rect(30,18,430,96));
                UiLabel(new Rect(510,34,660,45),MenuRoot(menuTab)==10?"CAR SELECT":"CUSTOMIZE",frontendHeading);
            }
            UiLabel(new Rect(514,82,750,27),"NFVU2  /  "+FrontendProfile.displayName+"  /  "+(garagePreviewStatus.Length>0?garagePreviewStatus:"CUSTOMIZE YOUR RIDE"),frontendSmall);
            if(MenuButton(new Rect(1423,40,148,43),"BACK  ×")){CloseVehicleMenu();return;}
            if(MenuButton(new Rect(1178,40,100,40),"U2",!IsMwFrontend?choiceSelectedStyle:null)){menuFrontend.Value="U2";Config.Save();}
            if(MenuButton(new Rect(1288,40,100,40),"MW",IsMwFrontend?choiceSelectedStyle:null)){menuFrontend.Value="MW";Config.Save();}
            MenuNavigation(true);
            UiLabel(new Rect(321,153,650,38),FrontendProfile.displayName.ToUpperInvariant(),frontendHeading);
            FrontendArt("car_"+FrontendProfile.id,new Rect(332,197,320,74));
            if(garagePreviewStatus.Length>0)UiLabel(new Rect(420,391,570,100),garagePreviewStatus,frontendHeading);
            UiLabel(new Rect(335,731,650,33),Mathf.RoundToInt(FrontendProfile.rig.mass)+" KG   /   "+FrontendProfile.sound.gearRatios.Length+" SPEED   /   "+FrontendProfile.rig.rimNominal+" INCH",frontendSmall);
            UiLabel(new Rect(335,768,650,27),"RIGHT MOUSE / RIGHT STICK: ROTATE    •    WHEEL: ZOOM",frontendSmall);
            var actual=current;
            bool preview=PreviewTab(menuTab);
            if(preview&&GaragePreviewReady)current=garagePreview;
            uiBuilding=true;
            GUI.BeginGroup(new Rect(1040,115,560,560));
            try {
                UiLabel(new Rect(25,99,510,35),Tr(MenuNames[menuTab]).ToUpperInvariant(),frontendHeading);
                if(preview&&!GaragePreviewReady)UiLabel(new Rect(25,175,510,120),garagePreviewStatus.Length==0?"Loading car preview…":garagePreviewStatus,labelStyle);
                else switch(menuTab){case 15:DrawBuildMenu();break;case 14:DrawStylingMenu();break;case 13:DrawPerformanceMenu();break;case 12:DrawExhaustMenu();break;case 11:DrawInterfaceSettings();break;case 10:DrawGarageMenu();break;case 0:DrawCarMenu();break;case 1:DrawRadioMenu();break;case 2:DrawLightsMenu();break;case 3:DrawNeonMenu();break;case 4:DrawNitroMenu();break;case 5:DrawWheelMenu();break;case 6:DrawBodyMenu();break;case 7:DrawTrunkMenu();break;case 8:DrawDrivingDisplayMenu();break;case 9:DrawTurretMenu();break;}
            }finally{GUI.EndGroup();current=actual;uiBuilding=false;}
            uiLastCount=uiCount;uiFocus=Math.Min(uiFocus,Math.Max(0,uiCount-1));
            UiLabel(new Rect(32,835,1420,31),padActive?"B: BACK    •    LB/RB: CATEGORIES / CONTENT    •    A: SELECT    •    RIGHT STICK: ROTATE":"ESC: BACK    •    ↑↓←→: NAVIGATE    •    TAB: CATEGORIES / CONTENT    •    ENTER: SELECT    •    MOUSE: SELECT / ROTATE",frontendSmall);
            UiLabel(new Rect(32,870,1420,25),workshopMessage.Length>0?Tr(workshopMessage):"GARAGE  "+fleet.Count+" / 4    •    "+(IsMwFrontend?"MOST WANTED":"UNDERGROUND 2")+"    •    THREE SAVED BUILDS PER CAR",frontendSmall);
            if(Event.current.type==EventType.Repaint){uiActivate=false;uiHorizontal=uiVertical=0;}
        }finally{GUI.depth=oldDepth;GUI.matrix=oldMatrix;GUI.skin=oldSkin;GUI.enabled=oldEnabled;GUI.changed|=oldChanged;uiBuilding=false;}
    }
}
}
