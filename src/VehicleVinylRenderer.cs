using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace NeedForValheim {
public sealed class VehicleVinylRenderer:IDisposable {
    readonly Task<VinylCatalog> loading;
    readonly Dictionary<int,Texture2D> thumbnails=new Dictionary<int,Texture2D>();
    VinylCatalog catalog;Task<byte[]> rendering;CancellationTokenSource cancellation;
    Texture2D texture;string signature="";bool disposed;
    float renderingCarbon;
    public float CarbonAmount {get;private set;}
    public string Status {get;private set;}="Loading original vinyls…";
    public VinylEntry[] Entries=>catalog?.entries??Array.Empty<VinylEntry>();
    public bool Loading=>catalog==null;
    public Texture2D Texture=>texture;
    public VehicleVinylRenderer(string preset){
        string root=Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
        bool safe=!string.IsNullOrEmpty(preset)&&preset.Length<=100&&preset.All(c=>c>='A'&&c<='Z'||c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_'||c=='-');
        loading=safe?Task.Run(()=>VinylCatalog.Load(Path.Combine(root,"menu","vinyl-"+preset+".json"),preset)):Task.FromResult(VinylCatalog.Empty(preset));
        Observe(loading);
    }
    static void Observe(Task task){task.ContinueWith(t=>{var ignored=t.Exception;},CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);}
    public bool Tick(){
        bool changed=false;if(disposed)return false;
        if(catalog==null&&loading.IsCompleted){
            try{catalog=loading.GetAwaiter().GetResult();Status=catalog.entries.Length+" original "+catalog.sourceGame.ToUpperInvariant()+" vinyl variants";}
            catch(Exception e){catalog=VinylCatalog.Empty("");Status="Original vinyls unavailable: "+e.Message;}
            signature="";changed=true;
        }
        if(rendering!=null&&rendering.IsCompleted){
            var job=rendering;rendering=null;
            try{
                var bytes=job.GetAwaiter().GetResult();var fresh=new Texture2D(512,512,TextureFormat.RGBA32,true){name="NFVU2_LayeredVinyl",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=4};
                var flipped=new byte[bytes.Length];int stride=512*4;for(int y=0;y<512;y++)Array.Copy(bytes,y*stride,flipped,(511-y)*stride,stride);
                fresh.SetPixelData<byte>(flipped,0);fresh.Apply(true,true);if(texture!=null)UnityEngine.Object.Destroy(texture);texture=fresh;CarbonAmount=renderingCarbon;changed=true;
            }catch(OperationCanceledException){}catch(Exception e){Status="Vinyl preview unavailable: "+e.Message;}
        }return changed;
    }
    public void Request(VinylLayerState[] layers,Color baseColor,int revision,Func<VinylPoint[][]> geometry){
        if(disposed)return;
        string key=revision+"/"+baseColor+"/"+string.Join(";",layers.Select(l=>l.Signature));
        if(key==signature)return;signature=key;
        cancellation?.Cancel();cancellation?.Dispose();cancellation=new CancellationTokenSource();var token=cancellation.Token;
        bool active=layers.Any(l=>l.design>=2&&l.opacity>0);
        if(!active){rendering=null;if(texture!=null)UnityEngine.Object.Destroy(texture);texture=null;CarbonAmount=0;return;}
        if(Loading){signature="";return;}
        var triangles=layers.Any(l=>l.design==2||l.design==3)?geometry():Array.Empty<VinylPoint[]>();
        var palette=Plugin.PaintColors.Select(c=>new[]{Byte(c.r),Byte(c.g),Byte(c.b)}).ToArray();
        byte[] foundation={Byte(baseColor.r),Byte(baseColor.g),Byte(baseColor.b),255};
        var entries=Entries;
        renderingCarbon=layers.Where(l=>entries.Any(e=>e.id==l.design&&e.category=="Carbon")).Select(l=>Mathf.Clamp01(l.opacity)).DefaultIfEmpty(0).Max();
        rendering=Task.Run(()=>{
            var canvas=new byte[512*512*4];for(int p=0;p<canvas.Length;p+=4)Buffer.BlockCopy(foundation,0,canvas,p,4);
            foreach(var layer in layers){token.ThrowIfCancellationRequested();if(layer.design<2||layer.opacity<=0)continue;
                byte[] art;bool fixedColors=false;
                if(layer.design==2||layer.design==3)art=VinylComposite.LegacyMask(layer.design,triangles);
                else {var entry=entries.FirstOrDefault(e=>e.id==layer.design);if(entry==null)continue;art=VinylCatalog.Unpack(entry.data,512*512*4);fixedColors=entry.fixedColors||layer.originalColors;}
                var colors=palette[Math.Max(0,Math.Min(palette.Length-1,layer.primary))].Concat(palette[Math.Max(0,Math.Min(palette.Length-1,layer.secondary))]).Concat(palette[Math.Max(0,Math.Min(palette.Length-1,layer.tertiary))]).ToArray();
                VinylComposite.Blend(canvas,art,colors,fixedColors,layer.opacity);
            }token.ThrowIfCancellationRequested();return canvas;
        },token);Observe(rendering);
    }
    static byte Byte(float value)=>(byte)Mathf.Clamp(Mathf.RoundToInt(value*255),0,255);
    public Texture2D Thumbnail(VinylEntry entry){
        if(thumbnails.TryGetValue(entry.id,out var cached))return cached;
        if(thumbnails.Count>=32){foreach(var old in thumbnails.Values)UnityEngine.Object.Destroy(old);thumbnails.Clear();}
        var bytes=VinylCatalog.Unpack(entry.thumbnail,64*64*4);var flipped=new byte[bytes.Length];for(int y=0;y<64;y++)Array.Copy(bytes,y*256,flipped,(63-y)*256,256);
        var t=new Texture2D(64,64,TextureFormat.RGBA32,false){name=entry.name+"_Preview"};t.SetPixelData<byte>(flipped,0);t.Apply(false,true);thumbnails[entry.id]=t;return t;
    }
    public void Dispose(){disposed=true;cancellation?.Cancel();cancellation?.Dispose();if(texture!=null)UnityEngine.Object.Destroy(texture);foreach(var t in thumbnails.Values)UnityEngine.Object.Destroy(t);thumbnails.Clear();}
}
}
