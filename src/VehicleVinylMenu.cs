using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    readonly ConfigEntry<int>[] vinylDesigns=new ConfigEntry<int>[4],vinylPrimaries=new ConfigEntry<int>[4],vinylSecondaries=new ConfigEntry<int>[4],vinylTertiaries=new ConfigEntry<int>[4];
    readonly ConfigEntry<float>[] vinylOpacities=new ConfigEntry<float>[4];
    readonly ConfigEntry<bool>[] vinylOriginalColors=new ConfigEntry<bool>[4];
    int vinylLayer,vinylEditor,vinylCategory,vinylPage,vinylColourChannel;
    string vinylBrowserModel="";
    GUIStyle vinylTileStyle,vinylSelectedTileStyle;
    void InitializeVinylLayers(){
        for(int i=0;i<4;i++){
            string slot="VinylLayer"+(i+1);
            vinylDesigns[i]=i==0?baseConfig_vinyl:WorkshopInt("Styling",slot,0,int.MaxValue);
            vinylPrimaries[i]=i==0?baseConfig_vinylColor:WorkshopInt("Styling",slot+"Color",2,PaintNames.Length-1);
            vinylSecondaries[i]=WorkshopInt("Styling",slot+"Color2",5,PaintNames.Length-1);
            vinylTertiaries[i]=WorkshopInt("Styling",slot+"Color3",1,PaintNames.Length-1);
            vinylOpacities[i]=BindVehicle("Styling",slot+"Opacity",1f,new ConfigDescription("Vinyl layer opacity; later layers cover earlier ones.",new AcceptableValueRange<float>(0,1)));
            vinylOriginalColors[i]=BindVehicle("Styling",slot+"OriginalColors",false,"Keep original artwork colours instead of the three selected mask colours. Artist/flag artwork always retains its original palette.");
        }
    }
    VinylLayerState[] CurrentVinylLayers()=>Enumerable.Range(0,4).Select(i=>new VinylLayerState{design=VehicleConfig(vinylDesigns[i]).Value,primary=VehicleConfig(vinylPrimaries[i]).Value,secondary=VehicleConfig(vinylSecondaries[i]).Value,tertiary=VehicleConfig(vinylTertiaries[i]).Value,opacity=VehicleConfig(vinylOpacities[i]).Value,originalColors=VehicleConfig(vinylOriginalColors[i]).Value}).ToArray();
    void RestoreMissingVinylSettings(System.Collections.Generic.Dictionary<string,string> saved){
        void Reset<T>(ConfigEntry<T> basis){var entry=VehicleConfig(basis);if(!saved.ContainsKey(entry.Definition.Section+"/"+entry.Definition.Key))entry.BoxedValue=entry.DefaultValue;}
        for(int i=0;i<4;i++){Reset(vinylDesigns[i]);Reset(vinylPrimaries[i]);Reset(vinylSecondaries[i]);Reset(vinylTertiaries[i]);Reset(vinylOpacities[i]);Reset(vinylOriginalColors[i]);}
    }
    void DrawVinylWorkshop(){
        var engine=current.styling?.Vinyls;if(engine==null){UiLabel(new Rect(25,200,510,80),"Vinyl preview unavailable",labelStyle);return;}
        if(vinylBrowserModel!=current.Profile.id){vinylBrowserModel=current.Profile.id;vinylPage=vinylCategory=0;}
        int layer=UiToolbar(new Rect(25,182,510,30),vinylLayer,new[]{"Layer 1","Layer 2","Layer 3","Layer 4"});
        if(layer!=vinylLayer){vinylLayer=layer;vinylPage=vinylCategory=0;}
        vinylEditor=UiToolbar(new Rect(25,218,510,30),vinylEditor,new[]{"Design catalog","Colours / opacity"});
        var selection=VehicleConfig(vinylDesigns[vinylLayer]);
        var chosen=engine.Entries.FirstOrDefault(e=>e.id==selection.Value);
        if(vinylEditor==1){
            bool fixedColors=chosen?.fixedColors??false;
            GUI.enabled=WorkshopParked&&!fixedColors;
            vinylColourChannel=UiToolbar(new Rect(25,267,510,30),vinylColourChannel,new[]{"Colour 1","Colour 2","Colour 3"});
            var colour=VehicleConfig((vinylColourChannel==0?vinylPrimaries:vinylColourChannel==1?vinylSecondaries:vinylTertiaries)[vinylLayer]);
            int palette=UiGrid(new Rect(25,307,510,99),Math.Max(0,(colour.Value==0?2:colour.Value)-1),PaintNames.Skip(1).ToArray(),5);colour.Value=palette+1;
            var original=VehicleConfig(vinylOriginalColors[vinylLayer]);original.Value=UiToggle(new Rect(25,412,510,26),original.Value," Use original artwork colours");
            GUI.enabled=WorkshopParked;
            var opacity=VehicleConfig(vinylOpacities[vinylLayer]);UiLabel(new Rect(25,445,250,25),"Opacity  "+Mathf.RoundToInt(opacity.Value*100)+" %",labelStyle);opacity.Value=UiSlider(new Rect(285,450,245,20),opacity.Value,0,1,.05f);
            if(fixedColors)UiLabel(new Rect(25,475,510,20),"Artist and flag artwork keeps its original palette.",mutedStyle);
            return;
        }
        var categories=new[]{"All"}.Concat(engine.Entries.Select(e=>e.category).Distinct().OrderBy(s=>s)).ToArray();
        vinylCategory=Mathf.Clamp(vinylCategory,0,categories.Length-1);
        // Category carousel leaves enough room for visible design thumbnails.
        if(MenuButton(new Rect(25,258,52,28),"‹")){vinylCategory=ControlRules.MenuMove(vinylCategory,-1,categories.Length);vinylPage=0;}
        UiLabel(new Rect(90,260,360,24),categories[vinylCategory]+"  /  "+(vinylCategory+1)+" of "+categories.Length,labelStyle);
        if(MenuButton(new Rect(478,258,52,28),"›")){vinylCategory=ControlRules.MenuMove(vinylCategory,1,categories.Length);vinylPage=0;}
        var classics=new[]{new VinylEntry{id=0,title=vinylLayer==0?"Factory":"None"},new VinylEntry{id=1,title="Clean body"},new VinylEntry{id=2,title="Racing stripes"},new VinylEntry{id=3,title="Checkers"}};
        var options=(vinylCategory==0?classics.Where(e=>vinylLayer==0||e.id!=1).Concat(engine.Entries):engine.Entries.Where(e=>e.category==categories[vinylCategory])).ToArray();
        int pages=Math.Max(1,(options.Length+5)/6);vinylPage=Mathf.Clamp(vinylPage,0,pages-1);
        if(vinylTileStyle==null){vinylTileStyle=new GUIStyle(menuSkin.button){fontSize=11,alignment=TextAnchor.LowerCenter,padding=new RectOffset(3,3,1,4)};vinylSelectedTileStyle=new GUIStyle(choiceSelectedStyle){fontSize=11,alignment=TextAnchor.LowerCenter,padding=new RectOffset(3,3,1,4)};}
        var page=options.Skip(vinylPage*6).Take(6).ToArray();
        for(int i=0;i<page.Length;i++){
            var e=page[i];var r=new Rect(25+i%3*172,295+i/3*74,166,68);
            if(MenuButton(r,e.title,e.id==selection.Value?vinylSelectedTileStyle:vinylTileStyle,false))selection.Value=e.id;
            if(e.id>=100){var thumb=engine.Thumbnail(e);GUI.DrawTexture(new Rect(r.x+53,r.y+2,60,45),thumb,ScaleMode.ScaleToFit,true);}
        }
        GUI.enabled=WorkshopParked&&vinylPage>0;if(MenuButton(new Rect(25,449,120,29),"‹ Previous"))vinylPage--;
        GUI.enabled=WorkshopParked&&vinylPage<pages-1;if(MenuButton(new Rect(410,449,120,29),"Next ›"))vinylPage++;
        GUI.enabled=WorkshopParked;UiLabel(new Rect(165,453,225,22),(vinylPage+1)+" / "+pages+"   ·   "+options.Length+" designs",mutedStyle);
        UiLabel(new Rect(25,480,510,14),engine.Loading?engine.Status:"Original car UVs · four layers · artist artwork retained",mutedStyle);
    }
}
}
