using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
namespace NeedForValheim {
// Managed data only: safe on a worker. Cache lifetime is one local world.
public sealed class SpawnAssetCache {
    sealed class Entry {public long Size,Ticks;public object Data;}
    readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();
    public T Load<T>(string path,Func<string,T> read,CancellationToken token,string cacheKey=null) where T:class {
        token.ThrowIfCancellationRequested();
        var file=new FileInfo(path);
        long size=file.Length,ticks=file.LastWriteTimeUtc.Ticks;
        string key=cacheKey??path;
        lock(entries)if(entries.TryGetValue(key,out var entry)&&entry.Size==size&&entry.Ticks==ticks)return (T)entry.Data;
        var data=read(path);
        token.ThrowIfCancellationRequested();
        lock(entries)entries[key]=new Entry{Size=size,Ticks=ticks,Data=data};
        return data;
    }
}
public sealed class SpawnAssets {
    public ModelData Model,Interior,Turret,Flames,Styling,Decals;
    public DecalArtworkCatalog DecalArtwork;
    public HudData Hud;
    public SpawnAudio Audio;
    public readonly List<string> Warnings=new List<string>();
    public static SpawnAssets Load(string folder,bool suv,SpawnAssetCache cache,CancellationToken token)=>Load(folder,suv?CarCatalog.BuiltInH2():CarCatalog.Nissan350Z(),cache,token);
    public static SpawnAssets Load(string folder,CarProfile profile,SpawnAssetCache cache,CancellationToken token) {
        var result=new SpawnAssets();
        var assets=profile.assets;
        result.Model=cache.Load(Path.Combine(folder,assets.modelFile),ModelFile.Load,token);
        string styling=Path.Combine(folder,"styling",profile.id+".json");
        // Libraries are large and runtime-owned; do not retain every browsed car
        // in the world's small base-model/audio cache after removing the car.
        if(File.Exists(styling))result.Optional(()=>{
            var library=ModelFile.Load(styling);
            if(library.styleOptions==null)throw new InvalidDataException("File is not a styling library");
            result.Styling=library;
        },"Styling",token);
        if(!string.IsNullOrEmpty(assets.decalFile))result.Optional(()=>{
            var geometry=cache.Load(Path.Combine(folder,assets.decalFile),ModelFile.Load,token);
            var artwork=cache.Load(Path.Combine(folder,"decals","mw-logos.json"),DecalArtworkCatalog.Load,token);
            result.Decals=geometry;result.DecalArtwork=artwork;
        },"Native decals",token);
        result.Flames=result.Model;
        if(assets.flameModelFile!=null)result.Optional(()=>result.Flames=cache.Load(Path.Combine(folder,assets.flameModelFile),ModelFile.Load,token),"Flight atlas",token);
        if(profile.capabilities.interior)result.Optional(()=>result.Interior=cache.Load(Path.Combine(folder,assets.interiorFile),ModelFile.Load,token),"Cabin",token);
        if(profile.capabilities.turret)result.Optional(()=>result.Turret=cache.Load(Path.Combine(folder,assets.turretFile),ModelFile.Load,token),"Turret",token);
        result.Optional(()=>result.Hud=cache.Load(Path.Combine(folder,assets.hudFile),HudFile.Load,token),"HUD",token);
        result.Audio=new SpawnAudio(folder,assets.audioFolders,cache,token);
        return result;
    }
    void Optional(Action load,string label,CancellationToken token) {
        token.ThrowIfCancellationRequested();
        try {load();}catch(OperationCanceledException){throw;}catch(Exception error){Warnings.Add(label+" unavailable: "+error.Message);}
    }
}
public sealed class SpawnAudio {
    readonly Dictionary<string,WavData> clips=new Dictionary<string,WavData>();
    readonly Dictionary<string,Exception> errors=new Dictionary<string,Exception>();
    public readonly WavData Reverse;
    public SpawnAudio(string folder,bool suv,SpawnAssetCache cache,CancellationToken token):this(folder,(suv?CarCatalog.BuiltInH2():CarCatalog.Nissan350Z()).assets.audioFolders,cache,token) {}
    public SpawnAudio(string folder,string[] subfolders,SpawnAssetCache cache,CancellationToken token) {
        var assembly=typeof(SpawnAudio).Assembly;
        foreach(var name in assembly.GetManifestResourceNames())if(name.StartsWith("NeedForValheim.Audio.",StringComparison.Ordinal)&&name.EndsWith(".wav",StringComparison.Ordinal))
            Read(name,()=>cache.Load(assembly.Location,_=>WavPcm16.Read(assembly.GetManifestResourceStream(name)),token,name),token);
        foreach(var subfolder in subfolders) {
            string path=Path.Combine(folder,subfolder);
            if(!Directory.Exists(path))continue;
            foreach(var file in Directory.GetFiles(path,"*.wav"))
                Read(file,()=>cache.Load(file,p=>WavPcm16.Read(File.OpenRead(p),subfolder=="turret"?30:15),token),token);
        }
        token.ThrowIfCancellationRequested();
        Reverse=ReverseGearSound.Create();
    }
    void Read(string key,Func<WavData> load,CancellationToken token) {
        key=Key(key);
        token.ThrowIfCancellationRequested();
        try {clips[key]=load();}catch(OperationCanceledException){throw;}catch(Exception error){errors[key]=error;}
    }
    public WavData Embedded(string name)=>FileData("NeedForValheim.Audio."+name);
    public WavData FileData(string path) {
        path=Key(path);
        if(clips.TryGetValue(path,out var clip))return clip;
        errors.TryGetValue(path,out var error);
        throw new InvalidDataException("Audio unavailable: "+path,error);
    }
    static string Key(string value)=>value.StartsWith("NeedForValheim.Audio.",StringComparison.Ordinal)?value:Path.GetFullPath(value).ToUpperInvariant();
}
}
