using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using NeedForValheim;
class VinylTests {
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static byte[] Pack(byte[] bytes){using(var m=new MemoryStream()){using(var z=new DeflateStream(m,CompressionMode.Compress,true))z.Write(bytes,0,bytes.Length);return m.ToArray();}}
    static byte[] Base()=>new byte[]{40,50,60,255};
    static readonly byte[] Colors={240,10,20,20,230,30,30,40,220};
    static void Main(string[] args){
        var canvas=Base();VinylComposite.Blend(canvas,new byte[]{255,0,0,0},Colors,false,1);Check(canvas.SequenceEqual(Base()),"zero coverage preserves body paint");
        foreach(int channel in new[]{0,1,2}){canvas=Base();var mask=new byte[]{0,0,0,255};mask[channel]=255;VinylComposite.Blend(canvas,mask,Colors,false,1);Check(canvas.Take(3).SequenceEqual(Colors.Skip(channel*3).Take(3)),"native RGB mask uses independently selected colour "+channel);}
        canvas=Base();VinylComposite.Blend(canvas,new byte[]{91,70,153,255},Colors,true,1);Check(canvas.Take(3).SequenceEqual(new byte[]{91,70,153}),"artist/flag palette survives compositing");
        canvas=Base();VinylComposite.Blend(canvas,new byte[]{255,0,0,128},Colors,false,1);Check(canvas[0]>=139&&canvas[0]<=141&&canvas[1]>=29&&canvas[1]<=31,"coverage antialiasing blends with body paint");
        canvas=Base();VinylComposite.Blend(canvas,new byte[]{255,0,0,255},Colors,false,0);Check(canvas.SequenceEqual(Base()),"hidden layer is transparent");
        canvas=Base();VinylComposite.Blend(canvas,new byte[]{255,0,0,255},Colors,false,1);VinylComposite.Blend(canvas,new byte[]{0,0,255,255},Colors,false,1);Check(canvas.Take(3).SequenceEqual(Colors.Skip(6).Take(3)),"later vinyl layers cover earlier layers");
        Check(VinylCatalog.Unpack(Pack(new byte[]{1,2,3,4}),4).SequenceEqual(new byte[]{1,2,3,4}),"bounded Deflate roundtrip");
        canvas=Base();VinylComposite.Blend(canvas,new byte[]{91,70,153,255},Colors,true,1,true);Check(canvas.SequenceEqual(new byte[]{91,70,153,0}),"opaque artwork removes paint contribution without changing artist colours");
        canvas=Base();VinylComposite.Blend(canvas,new byte[]{255,0,0,128},Colors,false,1,true);Check(canvas[3]==127,"antialiased artwork retains uncovered paint weight");
        VinylComposite.Blend(canvas,new byte[]{0,255,0,128},Colors,false,1,true);Check(canvas[3]==63,"overlapping translucent layers multiply remaining paint weight");
        canvas=Base();VinylComposite.Blend(canvas,new byte[]{255,0,0,0},Colors,false,1,true);Check(canvas.SequenceEqual(Base()),"transparent artwork retains full paint contribution");
        foreach(var bytes in new[]{new byte[]{1,2,3},new byte[]{1,2,3,4,5}}){bool rejected=false;try{VinylCatalog.Unpack(Pack(bytes),4);}catch(InvalidDataException){rejected=true;}Check(rejected,"wrong decompressed size rejected");}
        var side=new[]{new VinylPoint{x=1,y=0,z=-1,u=0,v=0,nx=1},new VinylPoint{x=1,y=1,z=-1,u=1,v=0,nx=1},new VinylPoint{x=1,y=0,z=1,u=0,v=1,nx=1}};
        var stripes=VinylComposite.LegacyMask(2,new[]{side});Check(stripes.All(b=>b==0),"roof racing stripes do not become stretched side streaks");
        var checkers=VinylComposite.LegacyMask(3,new[]{side});Check(checkers.Where((b,i)=>i%4==3).Any(b=>b==255)&&checkers.Where((b,i)=>i%4==3).Any(b=>b==0),"side checkers use vertical height and car length");
        Check(side[1].u==1&&side[1].v==0,"procedural bake never changes source UVs");
        string identityPath=Path.Combine(Path.GetTempPath(),"nfvu2-vinyl-identity-"+Guid.NewGuid()+".json");
        try{
            foreach(string preset in new[]{"NFVU2_MIATA","NFVU2_MW_SUPRA"}){
                var identity=VinylCatalog.Empty(preset);File.WriteAllText(identityPath,JsonConvert.SerializeObject(identity));
                Check(VinylCatalog.Load(identityPath,preset).sourceGame==identity.sourceGame,"matching source accepted: "+preset);
                foreach(string source in new[]{identity.sourceGame=="mw"?"u2":"mw","unknown"}){
                    identity.sourceGame=source;File.WriteAllText(identityPath,JsonConvert.SerializeObject(identity));
                    bool rejected=false;try{VinylCatalog.Load(identityPath,preset);}catch(InvalidDataException){rejected=true;}
                    Check(rejected,"foreign source rejected: "+preset+" / "+source);
                }
            }
            File.Delete(identityPath);Check(VinylCatalog.Load(identityPath,"NFVU2_MW_SUPRA").sourceGame=="mw","missing MW catalog retains source identity");
        }finally{if(File.Exists(identityPath))File.Delete(identityPath);}
        int catalogs=0,designs=0;VinylCatalog sample=null;
        foreach(var path in Directory.GetFiles(args[0],"vinyl-*.json")){
            string preset=Path.GetFileNameWithoutExtension(path).Substring(6);var c=VinylCatalog.Load(path,preset);if(c.sourceGame!="u2")continue;catalogs++;designs+=c.entries.Length;
            if(preset.Contains("MIATA"))sample=c;
            Check(c.entries.Length>500,preset+" has a complete original design catalog");
            foreach(var entry in c.entries){var rgba=VinylCatalog.Unpack(entry.data,512*512*4);var thumb=VinylCatalog.Unpack(entry.thumbnail,64*64*4);Check(rgba.Where((b,i)=>i%4==3).Any(b=>b>0),"design has real coverage: "+entry.name);Check(thumb.Length==16384,"thumbnail valid: "+entry.name);}
        }
        Check(catalogs==30,"all 30 U2 cars have their own original UV-mapped vinyl catalog");
        if(args.Length>1&&sample!=null){
            var painted=new byte[512*512*4];for(int p=0;p<painted.Length;p+=4){painted[p]=166;painted[p+1]=173;painted[p+2]=184;painted[p+3]=255;}
            foreach(string category in new[]{"Tribal","Hood"}){var entry=sample.entries.First(e=>e.category==category);VinylComposite.Blend(painted,VinylCatalog.Unpack(entry.data,painted.Length),new byte[]{217,14,50,230,156,16,22,179,207},entry.fixedColors,1);}
            File.WriteAllBytes(args[1],painted);
        }
        Console.WriteLine(checks+" vinyl checks passed; "+designs+" original variants across "+catalogs+" cars. Unity material drawing requires game verification.");
    }
}
