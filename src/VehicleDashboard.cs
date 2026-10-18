using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeedForValheim {
public sealed class VehicleDashboard : IDisposable {
    readonly List<Texture2D> textures=new List<Texture2D>();
    Texture2D[] faces,lines,needles;
    string[] kinds;
    float[] needlePivots;
    readonly Texture2D[] nitroLevels=new Texture2D[49],mwNitroLevels=new Texture2D[49];
    Texture2D nitroBacking,mwNitroBacking;
    Texture2D[] turboFaces,turboLines;
    Texture2D turboFallback;
    GUIStyle speedStyle,smallStyle,gearStyle,dialStyle;
    public void DrawDesignPreview(Rect rect,int design,Color accent) {
        if(faces==null||design<0||design>=faces.Length||faces[design]==null)return;
        var color=GUI.color;
        try {
            bool mw=kinds[design]=="mw-tach";
            GUI.color=mw?accent:Color.white;GUI.DrawTexture(rect,faces[design],ScaleMode.ScaleToFit,true);
            GUI.color=accent;
            var overlay=mw?rect:new Rect(rect.x+rect.width*12/280,rect.y+rect.height*12/280,rect.width*256/280,rect.height*256/280);
            if(lines[design]!=null)GUI.DrawTexture(overlay,lines[design],ScaleMode.ScaleToFit,true);
        }finally{GUI.color=color;}
    }
    Texture2D Texture(ModelTexture data,byte[] pixels=null) {
        var result=new Texture2D(data.width,data.height,TextureFormat.RGBA32,false){name="Vehicle_HUD_"+data.name,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        textures.Add(result);pixels=pixels??data.rgba;var flipped=new byte[pixels.Length];int row=data.width*4;
        for(int y=0;y<data.height;y++)Array.Copy(pixels,y*row,flipped,(data.height-1-y)*row,row);
        result.SetPixelData<byte>(flipped,0);result.Apply(false,true);return result;
    }
    public IEnumerable<int> LoadSteps(HudData data) {
        int count=data.designs.Length;faces=new Texture2D[count];lines=new Texture2D[count];needles=new Texture2D[count];
        turboFaces=new Texture2D[count];turboLines=new Texture2D[count];kinds=new string[count];needlePivots=new float[count];
        turboFallback=new Texture2D(128,128,TextureFormat.RGBA32,false){name="NFVU2_TurboDial",filterMode=FilterMode.Bilinear};textures.Add(turboFallback);
        var dialPixels=new Color[128*128];
        for(int y=0;y<128;y++)for(int x=0;x<128;x++) {
            float radius=Mathf.Sqrt((x-63.5f)*(x-63.5f)+(y-63.5f)*(y-63.5f));
            dialPixels[y*128+x]=radius>63?Color.clear:radius>60?new Color(.3f,.35f,.4f,.95f):new Color(.025f,.035f,.045f,.94f);
        }
        turboFallback.SetPixels(dialPixels);turboFallback.Apply(false,true);
        for(int i=0;i<count;i++) {
            var d=data.designs[i];faces[i]=Texture(d.fill);yield return 0;
            lines[i]=Texture(d.lines);yield return 0;needles[i]=Texture(d.needle);yield return 0;
            kinds[i]=d.kind;needlePivots[i]=d.needlePivot;
            if(d.turboFill!=null&&d.turboLines!=null) {
                turboFaces[i]=Texture(d.turboFill);yield return 0;
                turboLines[i]=Texture(d.turboLines);yield return 0;
            }
            else if(d.kind=="mw-tach"){turboLines[i]=Texture(d.turboLines);yield return 0;}
        }
        if(data.mwNitro!=null) {
            mwNitroBacking=Texture(data.mwNitro);
            for(int level=0;level<49;level++) {
                var pixels=(byte[])data.mwNitro.rgba.Clone();
                for(int y=0;y<data.mwNitro.height;y++)for(int x=0;x<data.mwNitro.width;x++)
                    if(level==0||HudMath.MwNitroPixelProgress(x,y,data.mwNitro.width,data.mwNitro.height)>level/48f)pixels[(y*data.mwNitro.width+x)*4+3]=0;
                mwNitroLevels[level]=Texture(data.mwNitro,pixels);yield return 0;
            }
        }
        nitroBacking=Texture(data.nitroBacking);
        for(int level=0;level<nitroLevels.Length;level++) {
            var pixels=(byte[])data.nitro.rgba.Clone();
            for(int y=0;y<data.nitro.height;y++)for(int x=0;x<data.nitro.width;x++)
                if(level==0||HudMath.NitroPixelProgress(x,y)>level/48f)pixels[(y*data.nitro.width+x)*4+3]=0;
            nitroLevels[level]=Texture(data.nitro,pixels);
            yield return 0;
        }
    }
    public void Draw(int design,float scale,Color accent,bool mph,float speed,float rpm,int gear,float charge,bool nitroEnabled,bool boosting,float bottomInset=90,float turboPressure=0,bool turboAvailable=true,float maxRpm=7500) {
        if(speedStyle==null) {
            speedStyle=new GUIStyle(GUI.skin.label){fontSize=42,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleRight};
            smallStyle=new GUIStyle(GUI.skin.label){fontSize=14,alignment=TextAnchor.MiddleRight};
            gearStyle=new GUIStyle(smallStyle){fontSize=22,fontStyle=FontStyle.Bold};
            dialStyle=new GUIStyle(smallStyle){fontSize=11,alignment=TextAnchor.MiddleCenter};dialStyle.normal.textColor=Color.white;
        }
        scale=Mathf.Clamp(scale,.65f,1.4f);design=Mathf.Clamp(design,0,faces.Length-1);
        if(kinds[design]=="mw-tach") {DrawMwTach(design,scale,accent,mph,speed,rpm,gear,charge,nitroEnabled,boosting,bottomInset,turboPressure,turboAvailable);return;}
        scale=Mathf.Min(scale,Mathf.Min(Screen.width/620f,Mathf.Max(.1f,(Screen.height-bottomInset-60)/365f)));
        dialStyle.fontSize=11;dialStyle.normal.textColor=Color.white;
        var matrix=GUI.matrix;var color=GUI.color;
        try {
            GUI.matrix=Matrix4x4.TRS(new Vector3(Screen.width-325*scale-18,Screen.height-315*scale-bottomInset),Quaternion.identity,Vector3.one*scale);
            GUI.color=Color.white;GUI.DrawTexture(new Rect(0,30,280,280),faces[design]);
            GUI.color=accent;GUI.DrawTexture(new Rect(12,42,256,256),lines[design]);
            // One shared NOS arc; reference face is U2 08.
            var nosRect=new Rect(HudMath.U2NosX,HudMath.U2NosY,HudMath.U2NosWidth,HudMath.U2NosHeight);
            GUI.color=new Color(.65f,.65f,.65f,1);GUI.DrawTexture(nosRect,nitroBacking);
            if(nitroEnabled) {
                GUI.color=boosting?new Color(.3f,1,.25f):accent;
                GUI.DrawTexture(nosRect,nitroLevels[Mathf.RoundToInt(HudMath.Unit(charge)*48)]);
            }
            NativeNeedle(design,new Vector2(140,170),HudMath.NeedleAngle(rpm),116);
            if(turboAvailable)DrawTurboDial(design,turboPressure,accent);
            GUI.color=Color.white;
            Text(new Rect(204,188,76,47),Mathf.RoundToInt(HudMath.Speed(speed,mph)).ToString(),34,Color.white);
            Text(new Rect(204,233,76,20),mph?"MPH":"KM/H",11,accent);
            Text(new Rect(214,157,54,28),gear<0?"R":Mathf.Max(1,gear).ToString(),22,Color.white);
        } finally {GUI.matrix=matrix;GUI.color=color;}
    }
    void NativeNeedle(int design,Vector2 centre,float angle,float length) {
        var saved=GUI.matrix;var art=needles[design];float height=length/needlePivots[design],width=height*art.width/art.height;
        GUI.matrix=saved*Matrix4x4.TRS(new Vector3(centre.x,centre.y,0),Quaternion.Euler(0,0,angle),Vector3.one);
        GUI.color=Color.white;GUI.DrawTexture(new Rect(-width*.5f,-length,width,height),art);GUI.matrix=saved;
    }
    void Text(Rect rect,string text,int size,Color colour) {
        dialStyle.fontSize=size;dialStyle.fontStyle=FontStyle.Bold;dialStyle.normal.textColor=colour;
        GUI.color=Color.white;GUI.Label(rect,text,dialStyle);
    }
    void DrawMwTach(int design,float scale,Color accent,bool mph,float speed,float rpm,int gear,float charge,bool enabled,bool boosting,float bottomInset,float pressure,bool turboAvailable) {
        var saved=GUI.matrix;var old=GUI.color;
        scale=Mathf.Min(scale,Mathf.Min((Screen.width-36)/340f,Mathf.Max(.1f,(Screen.height-bottomInset-30)/390f)));
        try {
            GUI.matrix=Matrix4x4.TRS(new Vector3(Screen.width-306*scale-18,Screen.height-390*scale-bottomInset),Quaternion.identity,Vector3.one*scale);
            var face=new Rect(0,0,288,288);
            GUI.color=accent;GUI.DrawTexture(face,faces[design]);GUI.DrawTexture(face,lines[design]);
            DrawMwNitro(charge,enabled,boosting,accent);
            NativeNeedle(design,new Vector2(144,148.5f),HudMath.MwNeedleAngle(rpm),124);
            var ink=accent.grayscale<.45f?Color.white:Color.black;
            Text(new Rect(119,81,50,34),gear<0?"R":Mathf.Max(1,gear).ToString(),23,ink);
            Text(new Rect(90,171,108,45),Mathf.RoundToInt(HudMath.Speed(speed,mph)).ToString(),32,ink);
            Text(new Rect(110,228,68,20),mph?"MPH":"KM/H",12,Color.white);
            Text(new Rect(102,264,84,18),"RPM ×1000",10,Color.white);
            Text(new Rect(-9,278,52,18),"N2O",10,accent);
            if(turboAvailable) {
                GUI.color=Color.white;GUI.DrawTexture(new Rect(0,312,288,72),turboLines[design]);
                float a=(-40+80*HudMath.Unit((pressure+30)/60))*Mathf.Deg2Rad;
                var point=new Vector2(144,172)+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*201;
                Pointer(point,180-a*Mathf.Rad2Deg,12,Color.white);
                Text(new Rect(98,327,92,20),Mathf.RoundToInt(pressure)+" PSI",12,Color.white);
            }
        } finally {GUI.matrix=saved;GUI.color=old;}
    }
    void DrawMwNitro(float charge,bool enabled,bool boosting,Color accent) {
        if(mwNitroBacking==null)return;
        // HUD_SingleRace FNG 931705 / 930097, relative to its 140px tach.
        // Quaternion plus negative width projects to a 140-degree 2D rotation.
        var saved=GUI.matrix;
        try {
            GUI.matrix=saved*Matrix4x4.TRS(new Vector3(145.03f,149.52f,0),Quaternion.Euler(0,0,140),Vector3.one);
            var rect=new Rect(-156.343f,-154.286f,312.686f,308.572f);
            GUI.color=new Color(1,1,1,60/255f);GUI.DrawTexture(rect,mwNitroBacking);
            if(enabled) {
                GUI.matrix=saved*Matrix4x4.TRS(new Vector3(145.34f,150.08f,0),Quaternion.Euler(0,0,140),Vector3.one);
                GUI.color=boosting?Color.white:accent;
                GUI.DrawTexture(rect,mwNitroLevels[Mathf.RoundToInt(HudMath.Unit(charge)*48)]);
            }
        } finally {GUI.matrix=saved;}
    }
    static void Pointer(Vector2 centre,float angle,float length,Color colour) {
        var saved=GUI.matrix;
        GUI.matrix=saved*Matrix4x4.TRS(new Vector3(centre.x,centre.y,0),Quaternion.Euler(0,0,angle),Vector3.one);
        GUI.color=Color.black;GUI.DrawTexture(new Rect(-3,-length-1,6,length+7),Texture2D.whiteTexture);
        GUI.color=colour;GUI.DrawTexture(new Rect(-1.5f,-length,3,length+4),Texture2D.whiteTexture);
        GUI.matrix=saved;
    }
    void DrawTurboDial(int design,float pressure,Color accent) {
        var centre=new Vector2(-84,222);
        var rect=new Rect(centre.x-68,centre.y-68,136,136);
        if(turboFaces[design]!=null&&turboLines[design]!=null) {
            GUI.color=Color.white;GUI.DrawTexture(rect,turboFaces[design]);
            GUI.color=accent;GUI.DrawTexture(rect,turboLines[design]);
        } else {
            GUI.color=Color.white;GUI.DrawTexture(rect,turboFallback);
            var saved=GUI.matrix;
            for(int i=0;i<=7;i++) {
                float angle=-165+30*i;
                GUI.matrix=saved*Matrix4x4.TRS(new Vector3(centre.x,centre.y,0),Quaternion.Euler(0,0,angle),Vector3.one);
                GUI.color=accent;GUI.DrawTexture(new Rect(-1,-61,2,7),Texture2D.whiteTexture);
                GUI.matrix=saved;
            }
            foreach(int value in new[]{-30,0,30}) {
                float angle=HudMath.TurboNeedleAngle(value)*Mathf.Deg2Rad;
                GUI.color=Color.white;
                GUI.Label(new Rect(centre.x+Mathf.Sin(angle)*43-16,centre.y-Mathf.Cos(angle)*43-9,32,18),value.ToString(),dialStyle);
            }
        }
        GUI.color=Color.white;GUI.Label(new Rect(centre.x-35,centre.y+69,70,18),"TURBO",dialStyle);
        Pointer(centre,HudMath.TurboNeedleAngle(pressure),51,Color.white);
        GUI.color=accent;GUI.DrawTexture(new Rect(centre.x-3,centre.y-3,6,6),Texture2D.whiteTexture);
    }
    public void Dispose(){foreach(var t in textures)if(t!=null)UnityEngine.Object.Destroy(t);textures.Clear();}
}
}
