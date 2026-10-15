using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    readonly List<Texture2D> menuTextures=new List<Texture2D>();
    GUISkin menuSkin;
    GUIStyle navStyle,navSelectedStyle,choiceSelectedStyle;
    VehicleMenuAssets menuAssets;
    ConfigEntry<float> menuVolume;
    ConfigEntry<string> menuFrontend;
    bool skinWasMw;
    bool IsMwFrontend=>menuFrontend!=null&&menuFrontend.Value=="MW";
    ConfigEntry<float> menuMouseSensitivity,menuControllerSensitivity;
    ConfigEntry<bool> menuMouseInvert,menuControllerInvert;
    int hoveredTab=-1;
    static readonly string[] MenuNames={"Vehicle","Radio","Lights","Neon","Nitro","Wheels","Doors","Trunk","Display","Turret","Garage","Settings","Exhaust","Performance","Styling","Builds"};
    Color MenuAccent=>IsMwFrontend?new Color(.84f,.84f,.66f):new Color(.2f,.85f,1);
    Color MenuSelection=>IsMwFrontend?new Color(.82f,.78f,.3f):new Color(.83f,.96f,.22f);
    static readonly int[] MenuOrder={10,13,14,15,0,5,6,7,2,3,4,12,1,8,9,11};
    static readonly int[] MenuRoots={10,13,14,15,0,1,11};
    static int MenuRoot(int tab)=>tab==5||tab==6||tab==7||tab==2||tab==3||tab==4||tab==12?14:tab==8||tab==9?0:tab;
    int[] VisibleMenuOrder(){int root=MenuRoot(menuTab);return MenuRoots.SelectMany(r=>r==root?new[]{r}.Concat(MenuOrder.Where(t=>t!=root&&MenuRoot(t)==root)):new[]{r}).ToArray();}
    int NextMenuTab(int direction) {
        var order=VisibleMenuOrder();int index=Math.Max(0,Array.IndexOf(order,menuTab));
        return order[ControlRules.MenuMove(index,direction,order.Length)];
    }
    Texture2D MenuTexture(Color color) {
        var texture=new Texture2D(1,1);texture.SetPixel(0,0,color);texture.Apply();menuTextures.Add(texture);return texture;
    }
    Texture2D MenuShape(Color colour) {
        var texture=new Texture2D(64,32){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        var pixels=new Color[64*32];
        for(int y=0;y<32;y++)for(int x=0;x<64;x++) {
            bool inside=x>=Mathf.Max(0,10-y)&&x<64-Mathf.Max(0,y-22);
            pixels[y*64+x]=inside?colour:Color.clear;
        }
        texture.SetPixels(pixels);texture.Apply(false,true);menuTextures.Add(texture);return texture;
    }
    void InitializeMenuTheme() {
        menuFrontend=Config.Bind("Menu","Frontend","U2",new ConfigDescription("Original local frontend artwork and theme: U2 or MW.",new AcceptableValueList<string>("U2","MW")));
        InitializeRadioTrax();
        menuVolume=Config.Bind("Menu","SoundVolume",.5f,new ConfigDescription("Original NFS frontend navigation sound volume. Zero mutes menu sounds.",new AcceptableValueRange<float>(0,1)));
        menuMouseSensitivity=Config.Bind("Menu","MouseSensitivity",1f,new ConfigDescription("Showroom mouse orbit speed, independent of driving camera.",new AcceptableValueRange<float>(.05f,4)));
        menuMouseInvert=Config.Bind("Menu","InvertMouseY",false,"Invert showroom mouse orbit vertically.");
        menuControllerSensitivity=Config.Bind("Menu","ControllerSensitivity",180f,new ConfigDescription("Showroom controller orbit degrees per second.",new AcceptableValueRange<float>(30,400)));
        menuControllerInvert=Config.Bind("Menu","InvertControllerY",false,"Invert showroom right stick vertically.");
    }
    void EnsureMenuAssets() {
        if(menuAssets==null)menuAssets=VehicleMenuAssets.Load(Path.Combine(Path.GetDirectoryName(Info.Location),"menu"),message=>Logger.LogInfo(message));
    }
    void DrawFrontendDesignSettings(){
        UiLabel(new Rect(25,192,510,36),"MENU DESIGN",labelStyle);
        int design=UiToolbar(new Rect(25,241,510,38),IsMwFrontend?1:0,new[]{"UNDERGROUND 2","MOST WANTED 2005"});
        string choice=design==1?"MW":"U2";if(choice!=menuFrontend.Value){menuFrontend.Value=choice;Config.Save();}
        UiLabel(new Rect(25,302,510,95),"Original local artwork, bitmap titles and menu sounds. Garage controls and your cars are shared between both designs.",mutedStyle);
        if(menuAssets==null||menuAssets.MwFrontend.Count==0)UiLabel(new Rect(25,409,510,60),"MW artwork is unavailable. Import it locally to enable the native MW graphics.",mutedStyle);
    }
    void MenuSound(int sound){if(!audioBlocked)menuAssets?.Play(sound,menuVolume.Value,IsMwFrontend);}
    bool MenuButton(Rect rect,string text,GUIStyle style=null,bool translate=true) {
        bool pressed=UiButton(rect,text,style,translate);
        if(pressed)MenuSound(1);return pressed;
    }
    void BuildMenuSkin() {
        if(menuSkin!=null)Destroy(menuSkin);
        foreach(var texture in menuTextures)Destroy(texture);menuTextures.Clear();
        skinWasMw=IsMwFrontend;
        menuSkin=Instantiate(GUI.skin);
        var normal=MenuShape(IsMwFrontend?new Color(.09f,.085f,.06f,.84f):new Color(.055f,.085f,.1f,.9f));
        var hover=MenuShape(new Color(.18f,.23f,.12f));
        var selected=MenuShape(MenuSelection);
        var button=menuSkin.button;button.fontSize=14;button.fontStyle=FontStyle.Bold;
        button.border=new RectOffset(10,10,3,3);button.margin=new RectOffset(2,2,2,2);
        button.normal.background=normal;button.normal.textColor=Color.white;
        button.hover.background=hover;button.hover.textColor=MenuAccent;
        button.active.background=selected;button.active.textColor=Color.black;
        button.onNormal.background=button.onHover.background=button.onActive.background=selected;
        button.onNormal.textColor=button.onHover.textColor=button.onActive.textColor=Color.black;
        button.focused.background=hover;button.focused.textColor=MenuAccent;
        button.onFocused.background=selected;button.onFocused.textColor=Color.black;
        menuSkin.toggle.fontSize=15;menuSkin.toggle.normal.textColor=menuSkin.toggle.onNormal.textColor=Color.white;
        menuSkin.horizontalSlider.normal.background=normal;menuSkin.horizontalSlider.fixedHeight=6;
        menuSkin.horizontalSlider.border=new RectOffset();
        foreach(var state in new[]{menuSkin.horizontalSliderThumb.normal,menuSkin.horizontalSliderThumb.hover,menuSkin.horizontalSliderThumb.active})state.background=selected;
        menuSkin.horizontalSliderThumb.fixedWidth=14;menuSkin.horizontalSliderThumb.fixedHeight=16;
        menuSkin.horizontalSliderThumb.border=new RectOffset();
        choiceSelectedStyle=new GUIStyle(button);choiceSelectedStyle.normal.background=selected;choiceSelectedStyle.normal.textColor=Color.black;choiceSelectedStyle.hover.background=selected;choiceSelectedStyle.hover.textColor=Color.black;
        navStyle=new GUIStyle(button){alignment=TextAnchor.MiddleLeft,fontSize=23,fontStyle=FontStyle.BoldAndItalic,padding=new RectOffset(49,8,0,0)};
        navStyle.normal.background=MenuShape(IsMwFrontend?new Color(.13f,.12f,.08f,.48f):new Color(.015f,.03f,.04f,.3f));
        navSelectedStyle=new GUIStyle(navStyle);navSelectedStyle.normal.background=selected;navSelectedStyle.normal.textColor=Color.black;
        navSelectedStyle.hover.background=selected;navSelectedStyle.hover.textColor=Color.black;
        songButtonStyle=new GUIStyle(button){fontSize=13,wordWrap=true,alignment=TextAnchor.MiddleLeft};
        titleStyle.normal.textColor=MenuAccent;frontendHeading=null;frontendSmall=null;
    }
    void MenuNavigation(bool accessible) {
        var e=Event.current;
        int hover=-1,row=0;
        foreach(int i in VisibleMenuOrder()) {
            if(!ControlRules.MenuTabVisible(i,accessible))continue;
            bool child=Array.IndexOf(MenuRoots,i)<0;
            float y=160+row++*43;
            var rect=new Rect(child?34:16,y,child?238:256,38);
            if(rect.Contains(e.mousePosition))hover=i;
            string title=i==10?"CAR SELECT":i==14?"CUSTOMIZE":i==15?"SAVED BUILDS":i==11?"OPTIONS":Tr(MenuNames[i]).ToUpperInvariant();
            bool nativeLabel=IsMwFrontend&&menuAssets!=null&&menuAssets.MwFrontend.ContainsKey("nav_"+i);
            if(UiButton(rect,nativeLabel?"":title,i==menuTab?navSelectedStyle:navStyle,false)){ChangeMenuTab(i);FocusMenuPane(false);MenuSound(1);}
            if(nativeLabel)FrontendArt("nav_"+i,new Rect(rect.x+45,y+3,rect.width-54,32),i==menuTab?Color.black:Color.white);
            Texture2D icon=null;if(IsMwFrontend&&menuAssets!=null)menuAssets.MwFrontend.TryGetValue("icon_"+i,out icon);
            if(icon==null&&i<11)icon=menuAssets?.Icons[i];
            if(icon!=null)GUI.DrawTexture(new Rect(24,y+1,33,30),icon,ScaleMode.ScaleToFit,true);
            else FrontendArt("arrow",new Rect(25,y+2,28,28),i==menuTab?Color.black:MenuAccent);
            if(i==9)DrawRustMenuBadge(rect);
            if((padActive||keyboardMenuActive)&&uiNavigation.Sidebar&&i==menuTab&&e.type==EventType.Repaint)DrawMenuFocus(rect);
        }
        if(!accessible)UiLabel(new Rect(20,184,150,112),"Spawn a vehicle and approach within 5 m to unlock tuning.",mutedStyle);
        GUI.enabled=true;
        if(e.type==EventType.Repaint&&hover!=hoveredTab){if(hover>=0)MenuSound(0);hoveredTab=hover;}
    }
    void DisposeMenuTheme() {
        menuAssets?.Dispose();menuAssets=null;
        if(menuSkin!=null)Destroy(menuSkin);menuSkin=null;
        foreach(var texture in menuTextures)Destroy(texture);menuTextures.Clear();
    }
}
}
