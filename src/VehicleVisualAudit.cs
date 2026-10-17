using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using Newtonsoft.Json;
using UnityEngine;
namespace NeedForValheim {
// Opt-in production UI/PhysX checks at the title screen, with separate save data.
// Normal gameplay never enables this route. Screenshots require visual review.
public partial class Plugin {
    sealed class VisualAuditReport {
        public string status="waiting_for_assets",assembly,unity;
        public bool complete;
        public bool physicsSkipped;
        public List<string> frames=new List<string>(),errors=new List<string>();
        public List<VehicleSuspensionAudit.Result> suspension=new List<VehicleSuspensionAudit.Result>();
    }
    bool visualAuditActive,visualAuditHud;
    VehicleDashboard visualAuditDashboard;
    int visualAuditDesign;
    void DrawVisualAudit() {
        if(visualAuditHud) {
            GUI.matrix=Matrix4x4.identity;GUI.color=Color.white;
            FrontendFill(new Rect(0,0,Screen.width,Screen.height),new Color(.035f,.045f,.055f));
            visualAuditDashboard?.Draw(visualAuditDesign,1,Color.cyan,false,23.89f,4500,3,.65f,true,false,210,18,true,7000);
        } else if(menuOpen)DrawGarageFrontend();
    }
    static void CaptureAuditFrame(string path) {
        var previous=RenderTexture.active;Texture2D frame=null;
        try {
            RenderTexture.active=null;frame=new Texture2D(Screen.width,Screen.height,TextureFormat.RGBA32,false);
            frame.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);frame.Apply();File.WriteAllBytes(path,frame.EncodeToPNG());
        } finally {RenderTexture.active=previous;if(frame!=null)Destroy(frame);}
    }
    IEnumerator RunVisualAudit(string reportPath) {
        reportPath=Path.GetFullPath(reportPath);string output=Path.GetDirectoryName(reportPath);Directory.CreateDirectory(output);
        var report=new VisualAuditReport{assembly=Info.Location,unity=Application.unityVersion};
        Action Save=()=>File.WriteAllText(reportPath,JsonConvert.SerializeObject(report,Formatting.Indented));Save();
        float deadline=Time.realtimeSinceStartup+180;
        while(!VehicleMaterials.TemplateReady) {
            if(Time.realtimeSinceStartup>deadline){report.status="3D templates unavailable";Save();yield break;}
            yield return new WaitForSecondsRealtime(1);
        }
        var saved=Config.ToDictionary(p=>p.Value,p=>p.Value.BoxedValue);bool saveOnSet=Config.SaveOnConfigSet;Config.SaveOnConfigSet=false;
        int originalWidth=Screen.width,originalHeight=Screen.height;bool originalFullScreen=Screen.fullScreen;
        visualAuditActive=true;report.status="testing";Save();
        try {
            EnsureMenuAssets();uiLanguage.Value="EN";
            report.physicsSkipped=Environment.GetEnvironmentVariable("NFVU2_VISUAL_AUDIT_UI_ONLY")=="1";
            foreach(var profile in vehicleCatalog.Profiles.Where(p=>!report.physicsSkipped&&p.sourceGame!=CarCatalog.MostWanted&&p.id!="350z"&&p.id!="h2")) {
                using(var work=StagedWork.Optional(VehicleSuspensionAudit.Check(profile,result=>report.suspension.Add(result)),error=>report.errors.Add(profile.id+": "+error)).GetEnumerator())
                    while(work.MoveNext())yield return null;
                Save();
            }
            foreach(var theme in new[]{"U2","MW"})foreach(var id in new[]{"imprezawrx","is300","lancerevo8","mustanggt","supra","mw-bmwm3"}) {
                menuFrontend.Value=theme;menuTab=14;menuOpen=true;visualAuditHud=false;
                EnsureGarageStage();SelectFrontendProfile(vehicleCatalog.Get(id),true);
                foreach(var entries in vehicleSettings.Values)if(entries.TryGetValue(FrontendProfile.ConfigPrefix,out var entry))entry.BoxedValue=entry.DefaultValue;
                float until=Time.realtimeSinceStartup+90;
                while(!GaragePreviewReady&&Time.realtimeSinceStartup<until)yield return null;
                if(!GaragePreviewReady){report.errors.Add(theme+" "+id+": "+garagePreviewStatus);Save();continue;}
                garageYaw=90;garagePitch=9;garageZoom=7;
                yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
                string name=theme+"-"+id+".png";CaptureAuditFrame(Path.Combine(output,name));report.frames.Add(name);Save();
                if(id=="mw-bmwm3"&&theme=="MW") {
                    workshopTab=1;styleCategory=7;garageYaw=100;garagePitch=12;garageZoom=4;
                    for(int brake=0;brake<=4;brake++) {
                        var actual=current;current=garagePreview;try{VehicleConfig(styleSettings["Brakes"]).Value=brake;}finally{current=actual;}
                        yield return new WaitForSecondsRealtime(1);yield return new WaitForEndOfFrame();
                        name="MW-brakes-"+brake+".png";CaptureAuditFrame(Path.Combine(output,name));report.frames.Add(name);Save();
                    }
                }
            }
            menuOpen=false;EndGarageFrontend();visualAuditHud=true;
            var data=HudFile.Load(Path.Combine(Path.GetDirectoryName(Info.Location),"hud","hud.json"));visualAuditDashboard=new VehicleDashboard();
            foreach(var step in visualAuditDashboard.LoadSteps(data))yield return null;
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(960,540)}) {
                Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.5f);
                for(int design=0;design<data.designs.Length;design++) {
                    visualAuditDesign=design;yield return new WaitForEndOfFrame();
                    string name="hud-"+design+"-"+Screen.width+"x"+Screen.height+".png";
                    CaptureAuditFrame(Path.Combine(output,name));report.frames.Add(name);Save();
                }
            }
            report.complete=true;report.status=report.errors.Count==0?"passed_pending_image_review":"failed";Save();
        } finally {
            visualAuditDashboard?.Dispose();visualAuditDashboard=null;visualAuditActive=visualAuditHud=menuOpen=false;DisposeGarageFrontend();
            foreach(var entry in saved)entry.Key.BoxedValue=entry.Value;Config.SaveOnConfigSet=saveOnSet;Config.Save();
            Screen.SetResolution(originalWidth,originalHeight,originalFullScreen);
            Logger.LogInfo("VISUAL_AUDIT_COMPLETE "+report.status);
            if(Environment.GetEnvironmentVariable("NFVU2_VISUAL_AUDIT_QUIT")=="1")Application.Quit();
        }
    }
}
}
