using System;
using System.IO;
using Newtonsoft.Json;
namespace NeedForValheim {
public sealed class HudDesign { public string name,kind; public float needlePivot=1; public ModelTexture fill,lines,needle,turboFill,turboLines; }
public sealed class HudData { public int schemaVersion; public HudDesign[] designs; public ModelTexture nitro,nitroBacking,mwNitro; }
public static class HudFile {
    public static HudData Load(string path) {
        var file=new FileInfo(path);
        if(!file.Exists||file.Length==0||file.Length>16*1024*1024)throw new InvalidDataException("Missing or oversized HUD data");
        HudData data;
        using(var stream=File.OpenText(path))using(var reader=new JsonTextReader(stream))
            data=JsonSerializer.Create(new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=12,CheckAdditionalContent=true}).Deserialize<HudData>(reader);
        if(data==null||data.schemaVersion!=1||data.designs==null||(data.designs.Length!=11&&data.designs.Length!=22))throw new InvalidDataException("Invalid HUD catalog");
        foreach(var d in data.designs) {
            if(d==null||string.IsNullOrWhiteSpace(d.name)||d.name.Length>40)throw new InvalidDataException("Invalid HUD design");
            Texture(d.fill);Texture(d.lines);Texture(d.needle);
            // Older local imports remain usable with the procedural pressure dial.
            if(d.needlePivot<.5f||d.needlePivot>1||float.IsNaN(d.needlePivot))throw new InvalidDataException("Invalid HUD needle pivot");
            if(d.kind=="mw-tach")Texture(d.turboLines);
            else if(d.turboFill!=null||d.turboLines!=null){Texture(d.turboFill);Texture(d.turboLines);}
            if(!string.IsNullOrEmpty(d.kind)) {
                if(d.kind!="mw-tach")throw new InvalidDataException("Invalid HUD layout");
            }
        }
        Texture(data.nitro);Texture(data.nitroBacking);if(data.mwNitro!=null)Texture(data.mwNitro);return data;
    }
    static void Texture(ModelTexture t) {
        if(t==null||t.width<1||t.height<1||t.width>256||t.height>256||t.rgba==null||t.rgba.Length!=(long)t.width*t.height*4)
            throw new InvalidDataException("Invalid HUD texture");
    }
}
public static class HudMath {
    // Shared layout for all U2 designs, calibrated against the visible 08 face.
    // Measured inner edge of the source artwork (tools/fit_u2_nos.py).
    // Preserve its circle and align it with the tachometer before HUD scaling.
    public const float U2NosCenterX=95.01085f,U2NosCenterY=113.09155f,U2NosScale=140f/70.26089f;
    public const float U2NosX=140-U2NosCenterX*U2NosScale,U2NosY=170-U2NosCenterY*U2NosScale,U2NosWidth=256*U2NosScale,U2NosHeight=128*U2NosScale;
    public static float Unit(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:DrivingMath.Clamp(value,0,1);
    public static float NeedleAngle(float rpm)=>180+240*Unit(rpm/7500);
    public static float MwNeedleAngle(float rpm)=>-115+230*Unit(rpm/8000);
    // Original TURBOLINES art: -30 at -165 degrees, zero at -75, +30 at +45.
    // Negative (vacuum) and positive (boost) halves have different sweeps.
    public static float TurboNeedleAngle(float pressure) {
        if(float.IsNaN(pressure)||float.IsInfinity(pressure))pressure=0;
        pressure=DrivingMath.Clamp(pressure,-30,30);
        return -75+pressure*(pressure<0?3:4);
    }
    public static float Speed(float metresPerSecond,bool mph)=>float.IsNaN(metresPerSecond)||float.IsInfinity(metresPerSecond)?0:Math.Abs(metresPerSecond)*(mph?2.2369363f:3.6f);
    // Top-down source pixels, counterclockwise-looking left endpoint to upper-right.
    public static float MwNitroPixelProgress(int x,int y,int width,int height)=>1-Unit(((float)Math.Atan2((y+.5f)/height-.5f,(x+.5f)/width-.5f)*(180f/(float)Math.PI)+180)/180);
    public static float NitroPixelProgress(int x,int y) {
        // The short lower-left tail belongs to the empty end of the arc;
        // atan2 wraps it to +180 degrees unless handled explicitly.
        if(x<U2NosCenterX&&y>=U2NosCenterY)return 0;
        return Unit(((float)Math.Atan2(y-U2NosCenterY,x-U2NosCenterX)*(180f/(float)Math.PI)+180)/150);
    }
}
}
