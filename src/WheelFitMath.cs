using System;
using System.Text.RegularExpressions;

namespace NeedForValheim {
public static class WheelFitMath {
    public static string RimDesign(string title,bool factory=false)=>factory?"Factory":Regex.Replace(title??"",@"\s+(?:17|18|19|20)\s*in(?:ch(?:es)?)?$","",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant).Trim();
    public static float Sanitize(float value,float min,float max,float fallback) =>
        float.IsNaN(value)||float.IsInfinity(value)?fallback:Math.Max(min,Math.Min(max,value));
    public static float StylingTrack(float stock,float tyreWidth,float offset,float widthScale)=>stock+offset-tyreWidth*(widthScale-1)/2;
    // Resize the rim and blend the tyre sidewall to the unchanged tread.
    public static void RadialShape(float radius,float bead,float outside,float scale,out float mapped,out float slope) {
        if(radius<=bead){mapped=radius*scale;slope=scale;return;}
        if(radius>=outside){mapped=radius;slope=1;return;}
        slope=(outside-bead*scale)/(outside-bead);
        mapped=bead*scale+(radius-bead)*slope;
    }
}
}
