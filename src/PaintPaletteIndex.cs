using System;
namespace NeedForValheim {
// Keep every legacy base colour ID; append six extra shades per family.
public static class PaintPaletteIndex {
    public const int Shades=7,BaseShade=3;
    public static int Count(int bases)=>bases+(bases-1)*(Shades-1);
    static int Valid(int id,int bases)=>Math.Max(0,Math.Min(Count(bases)-1,id));
    public static int Family(int id,int bases){id=Valid(id,bases);return id<bases?id:1+(id-bases)/(Shades-1);}
    public static int Shade(int id,int bases){id=Valid(id,bases);if(id<bases)return BaseShade;int n=(id-bases)%(Shades-1);return n<BaseShade?n:n+1;}
    public static int Id(int family,int shade,int bases){family=Math.Max(0,Math.Min(bases-1,family));shade=Math.Max(0,Math.Min(Shades-1,shade));return family==0||shade==BaseShade?family:bases+(family-1)*(Shades-1)+(shade<BaseShade?shade:shade-1);}
}
}
