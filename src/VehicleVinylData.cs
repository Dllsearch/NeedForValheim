using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
namespace NeedForValheim {
public sealed class VinylLayerState {
    public int design,primary=2,secondary=5,tertiary=1;public float opacity=1;public bool originalColors;
    public string Signature=>design+":"+primary+":"+secondary+":"+tertiary+":"+opacity.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+originalColors;
}
public sealed class VinylEntry {
    public int id,width,height;public string name,title,category;public bool fixedColors;public byte[] data,thumbnail;
}
public sealed class VinylCatalog {
    public int schemaVersion;public string preset,sourceGame;public VinylEntry[] entries;
    static string SourceFor(string preset)=>preset!=null&&preset.StartsWith("NFVU2_MW_",StringComparison.Ordinal)?"mw":"u2";
    public static VinylCatalog Empty(string preset)=>new VinylCatalog{schemaVersion=1,preset=preset,sourceGame=SourceFor(preset),entries=Array.Empty<VinylEntry>()};
    public static VinylCatalog Load(string path,string expected){
        if(!File.Exists(path))return Empty(expected);
        if(new FileInfo(path).Length>48*1024*1024)throw new InvalidDataException("Oversized vinyl catalog");
        var c=JsonConvert.DeserializeObject<VinylCatalog>(File.ReadAllText(path),new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=8,CheckAdditionalContent=true});
        if(c==null||c.schemaVersion!=1||c.preset!=expected||c.sourceGame!=SourceFor(expected)||c.entries==null||c.entries.Length>1024)throw new InvalidDataException("Invalid vinyl catalog");
        var ids=new HashSet<int>();
        foreach(var e in c.entries)if(e==null||e.id<100||!ids.Add(e.id)||e.width!=512||e.height!=512||string.IsNullOrWhiteSpace(e.title)||e.title.Length>100||e.category==null||e.category.Length>32||e.data==null||e.data.Length>2*1024*1024||e.thumbnail==null||e.thumbnail.Length>64*64*8)throw new InvalidDataException("Invalid vinyl entry");
        return c;
    }
    public static byte[] Unpack(byte[] packed,int size){
        var result=new byte[size];using(var input=new MemoryStream(packed))using(var z=new DeflateStream(input,CompressionMode.Decompress)){
            int count=0;while(count<size){int n=z.Read(result,count,size-count);if(n==0)throw new InvalidDataException("Truncated vinyl pixels");count+=n;}
            if(z.ReadByte()!=-1)throw new InvalidDataException("Oversized vinyl pixels");
        }return result;
    }
}
public struct VinylPoint {public float x,y,z,u,v,nx,ny,nz;}
public static class VinylComposite {
    public const int Size=512;
    public static void Blend(byte[] canvas,byte[] artwork,byte[] colors,bool fixedColors,float opacity){
        if(canvas.Length!=artwork.Length||colors.Length!=9)throw new ArgumentException("Vinyl blend size");
        opacity=float.IsNaN(opacity)?1:Math.Max(0,Math.Min(1,opacity));
        for(int p=0;p<canvas.Length;p+=4){float a=artwork[p+3]/255f*opacity;if(a<=0)continue;
            float sum=artwork[p]+artwork[p+1]+artwork[p+2];
            for(int ch=0;ch<3;ch++){
                float tint=fixedColors?artwork[p+ch]:sum>0?(artwork[p]*colors[ch]+artwork[p+1]*colors[3+ch]+artwork[p+2]*colors[6+ch])/sum:colors[ch];
                canvas[p+ch]=(byte)Math.Max(0,Math.Min(255,Math.Round(canvas[p+ch]*(1-a)+tint*a)));
            }canvas[p+3]=255;
        }
    }
    static float Edge(float ax,float ay,float bx,float by,float x,float y)=>(x-ax)*(by-ay)-(y-ay)*(bx-ax);
    public static byte[] LegacyMask(int design,VinylPoint[][] triangles){
        var pixels=new byte[Size*Size*4];
        foreach(var t in triangles){if(t.Length!=3)continue;var a=t[0];var b=t[1];var c=t[2];
            float ax=a.u*Size,ay=a.v*Size,bx=b.u*Size,by=b.v*Size,cx=c.u*Size,cy=c.v*Size;
            float area=Edge(ax,ay,bx,by,cx,cy);if(Math.Abs(area)<.01f)continue;
            int minx=Math.Max(0,(int)Math.Floor(Math.Min(ax,Math.Min(bx,cx)))),maxx=Math.Min(Size-1,(int)Math.Ceiling(Math.Max(ax,Math.Max(bx,cx))));
            int miny=Math.Max(0,(int)Math.Floor(Math.Min(ay,Math.Min(by,cy)))),maxy=Math.Min(Size-1,(int)Math.Ceiling(Math.Max(ay,Math.Max(by,cy))));
            for(int y=miny;y<=maxy;y++)for(int x=minx;x<=maxx;x++){
                float wa=Edge(bx,by,cx,cy,x+.5f,y+.5f)/area,wb=Edge(cx,cy,ax,ay,x+.5f,y+.5f)/area,wc=1-wa-wb;
                if(wa<-.001f||wb<-.001f||wc<-.001f)continue;
                float px=wa*a.x+wb*b.x+wc*c.x,py=wa*a.y+wb*b.y+wc*c.y,pz=wa*a.z+wb*b.z+wc*c.z;
                float nx=wa*a.nx+wb*b.nx+wc*c.nx,ny=wa*a.ny+wb*b.ny+wc*c.ny,nz=wa*a.nz+wb*b.nz+wc*c.nz;
                bool mark;
                if(design==2)mark=ny>.35f&&(Math.Abs(px-.18f)<.055f||Math.Abs(px+.18f)<.055f);
                else {float h=Math.Abs(ny)>=Math.Abs(nx)&&Math.Abs(ny)>=Math.Abs(nz)?px:Math.Abs(nx)>=Math.Abs(nz)?pz:px;float v=Math.Abs(ny)>=Math.Abs(nx)&&Math.Abs(ny)>=Math.Abs(nz)?pz:py;mark=(((int)Math.Floor(h/.22f)+(int)Math.Floor(v/.22f))&1)==0;}
                if(!mark)continue;int p=((Size-1-y)*Size+x)*4;pixels[p]=255;pixels[p+3]=255;
            }
        }return pixels;
    }
}
}
