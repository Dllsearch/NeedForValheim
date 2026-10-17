using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    int wheelWorkshopTab,showcasePage,vehiclePage,garageEffectMode=1;
    static string RimDesign(ModelStyleOption option)=>WheelFitMath.RimDesign(option.title,option.id==0);
    static string RimLabel(ModelStyleOption option)=>Regex.Replace(RimDesign(option),@"\s+STYLE\s*"," ",RegexOptions.IgnoreCase);
    void DrawWheelWorkshop() {
        var style=current.styling;
        if(style==null||!style.HasCatalog){UiLabel(new Rect(25,190,510,70),"Original wheel catalog is unavailable.",labelStyle);return;}
        wheelWorkshopTab=UiToolbar(new Rect(25,181,510,30),wheelWorkshopTab,new[]{"Design","Diameter","Colour","Fitment"});
        var entry=VehicleConfig(styleSettings["Rims"]);var options=style.Options("Rims");
        var selected=options.FirstOrDefault(o=>o.id==entry.Value)??options[0];
        GUI.enabled=WorkshopParked;
        if(wheelWorkshopTab==0) {
            var designs=options.GroupBy(RimDesign).Select(g=>g.First()).ToArray();
            stylePage=Mathf.Clamp(stylePage,0,(designs.Length-1)/9);int offset=stylePage*9;
            var page=designs.Skip(offset).Take(9).ToArray();int index=Array.FindIndex(page,o=>RimDesign(o)==RimDesign(selected));
            int choice=UiGrid(new Rect(25,234,510,210),index,page.Select(RimLabel).ToArray(),3);
            if(choice>=0&&choice!=index) {
                var design=RimDesign(page[choice]);
                entry.Value=options.Where(o=>RimDesign(o)==design).OrderBy(o=>Mathf.Abs(o.rimInches-RimSetting.Value)).First().id;
            }
            GUI.enabled=stylePage>0;if(MenuButton(new Rect(25,460,120,30),"‹ Previous"))stylePage--;
            GUI.enabled=offset+9<designs.Length;if(MenuButton(new Rect(410,460,120,30),"Next ›"))stylePage++;
            GUI.enabled=true;UiLabel(new Rect(160,462,230,27),"DESIGNS   "+(stylePage+1)+" / "+((designs.Length+8)/9),labelStyle);
        } else if(wheelWorkshopTab==1) {
            UiLabel(new Rect(25,237,510,30),"Rim diameter   "+RimSetting.Value.ToString("0.#")+" inches",labelStyle);
            if(current.Profile.sourceGame==CarCatalog.MostWanted) {
                int before=Mathf.Clamp(Mathf.RoundToInt(RimSetting.Value)-17,0,3);
                int size=17+UiToolbar(new Rect(25,281,510,38),before,new[]{"17 in","18 in","19 in","20 in"});
                if(size!=Mathf.RoundToInt(RimSetting.Value)) {
                    RimSetting.Value=size;
                    if(selected.id!=0)entry.Value=options.Where(o=>RimDesign(o)==RimDesign(selected)).OrderBy(o=>Mathf.Abs(o.rimInches-size)).First().id;
                }
            }else RimSetting.Value=UiSlider(new Rect(25,291,510,22),RimSetting.Value,current.Profile.rig.rimMin,current.Profile.rig.rimMax,.5f);
            UiLabel(new Rect(25,341,510,85),"Keep the selected design while changing diameter. Tyre outside diameter stays fixed.",mutedStyle);
        } else if(wheelWorkshopTab==2)rimColor.Value=UiGrid(new Rect(25,235,510,235),rimColor.Value,PaintNames,3);
        else {
            WheelSlider(frontCamber,"Front camber",230,-15,10,.5f,"°");
            WheelSlider(rearCamber,"Rear camber",278,-15,10,.5f,"°");
            WheelSlider(frontWheelOffset,"Front wheel offset",326,-10,20,.5f," cm / wheel");
            WheelSlider(rearWheelOffset,"Rear wheel offset",374,-10,20,.5f," cm / wheel");
            WheelSlider(lowering,"Lowering",422,0,12,.5f," cm");
            if(MenuButton(new Rect(25,482,510,30),"Reset fitment")){lowering.Value=0;frontCamber.Value=rearCamber.Value=0;frontWheelOffset.Value=rearWheelOffset.Value=0;}
        }
        GUI.enabled=true;
        if(wheelWorkshopTab!=3)UiLabel(new Rect(25,512,510,30),WorkshopParked?"Design, diameter, colour and fitment are saved together.":"Park before changing wheels.",mutedStyle);
    }
    void DrawShowcaseMenu() {
        var pages=showroom!=null?new[]{2,3,4,12,6,7}:new[]{2,3,4,12};
        showcasePage=Mathf.Clamp(showcasePage,0,pages.Length-1);
        showcasePage=UiToolbar(new Rect(25,142,510,30),showcasePage,pages.Select(p=>MenuNames[p]).ToArray());
        GUI.BeginGroup(new Rect(0,43,560,517));
        try {switch(pages[showcasePage]){case 2:DrawLightsMenu();break;case 3:DrawNeonMenu();break;case 4:DrawNitroMenu();break;case 12:DrawExhaustMenu();break;case 6:DrawBodyMenu();break;case 7:DrawTrunkMenu();break;}}
        finally {GUI.EndGroup();}
    }
    void DrawVehiclePages() {
        string[] pages=current.Profile.capabilities.turret?new[]{"Drive","Display","Turret"}:new[]{"Drive","Display"};
        vehiclePage=UiToolbar(new Rect(25,142,510,30),Mathf.Clamp(vehiclePage,0,pages.Length-1),pages);
        GUI.BeginGroup(new Rect(0,43,560,517));
        try {if(vehiclePage==1)DrawDrivingDisplayMenu();else if(vehiclePage==2)DrawTurretMenu();else DrawCarMenu();}
        finally {GUI.EndGroup();}
    }
}
}
