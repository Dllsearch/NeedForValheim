using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Audio;
namespace NeedForValheim {
public sealed class MenuData {
    public int schemaVersion;
    public ModelTexture[] icons;
    public string[] titles;
    public static MenuData Load(string path) {
        var file=new FileInfo(path);
        if(!file.Exists||file.Length>4*1024*1024)throw new InvalidDataException("Missing or oversized menu assets");
        var data=JsonConvert.DeserializeObject<MenuData>(File.ReadAllText(path),new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=12});
        if(data==null||data.schemaVersion!=1||data.icons==null||data.icons.Length!=11||data.titles==null||data.titles.Length!=11)throw new InvalidDataException("Invalid menu catalog");
        foreach(var t in data.icons)if(t!=null&&(t.width<1||t.height<1||t.width>256||t.height>256||t.rgba==null||t.rgba.Length!=(long)t.width*t.height*4))throw new InvalidDataException("Invalid menu texture");
        foreach(var title in data.titles)if(title!=null&&title.Length>40)throw new InvalidDataException("Invalid menu title");
        return data;
    }
    public static Dictionary<string,ModelTexture> LoadFrontend(string path,int count=40,int bytes=8*1024*1024){
        if(new FileInfo(path).Length>bytes)throw new InvalidDataException("Oversized frontend artwork");
        var art=JsonConvert.DeserializeObject<Dictionary<string,ModelTexture>>(File.ReadAllText(path),new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=8,CheckAdditionalContent=true});
        if(art==null||art.Count>count)throw new InvalidDataException("Invalid frontend artwork");
        long total=0;
        foreach(var pair in art){var t=pair.Value;if(pair.Key.Length>80||t==null||t.width<1||t.height<1||t.width>512||t.height>512||t.rgba==null||t.rgba.Length!=(long)t.width*t.height*4)throw new InvalidDataException("Invalid frontend texture");total+=t.rgba.Length;}
        if(total>24*1024*1024)throw new InvalidDataException("Oversized frontend texture memory");
        return art;
    }
}
internal sealed class VehicleMenuAssets : IDisposable {
    public readonly Texture2D[] Icons=new Texture2D[11];
    public readonly string[] Titles=new string[11];
    public Texture2D RustLogo {get;private set;}
    public readonly Dictionary<string,Texture2D> Frontend=new Dictionary<string,Texture2D>();
    public readonly Dictionary<string,Texture2D> MwFrontend=new Dictionary<string,Texture2D>();
    readonly List<AudioClip> clips=new List<AudioClip>();
    readonly List<AudioClip> mwClips=new List<AudioClip>();
    AudioSource source;
    GameObject audioObject;
    float lastSound=-1;
    public static VehicleMenuAssets Load(string folder,Action<string> log) {
        var result=new VehicleMenuAssets();
        try {
            var data=MenuData.Load(Path.Combine(folder,"menu.json"));
            for(int i=0;i<11;i++) {
                result.Titles[i]=data.titles[i];var t=data.icons[i];if(t==null)continue;
                var tex=new Texture2D(t.width,t.height,TextureFormat.RGBA32,false){name=t.name,wrapMode=TextureWrapMode.Clamp};result.Icons[i]=tex;
                var flipped=new byte[t.rgba.Length];int row=t.width*4;
                for(int y=0;y<t.height;y++)Array.Copy(t.rgba,y*row,flipped,(t.height-1-y)*row,row);
                tex.SetPixelData<byte>(flipped,0);tex.Apply(false,true);
            }
        }catch(Exception e){log("Original menu artwork unavailable: "+e.Message);}
        try {
            string path=Path.Combine(folder,"frontend.json");
            if(File.Exists(path)) {
                if(new FileInfo(path).Length>8*1024*1024)throw new InvalidDataException("Oversized frontend artwork");
                var art=JsonConvert.DeserializeObject<Dictionary<string,ModelTexture>>(File.ReadAllText(path),new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=8});
                if(art==null||art.Count>40)throw new InvalidDataException("Invalid frontend artwork");
                foreach(var pair in art) {
                    var t=pair.Value;if(t==null||t.width<1||t.height<1||t.width>512||t.height>512||t.rgba==null||t.rgba.Length!=(long)t.width*t.height*4)throw new InvalidDataException("Invalid frontend texture");
                    bool floor=pair.Key=="garage_floor";
                    var texture=new Texture2D(t.width,t.height,TextureFormat.RGBA32,floor){name=t.name,wrapMode=floor?TextureWrapMode.Repeat:TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=4};
                    var pixels=new byte[t.rgba.Length];int stride=t.width*4;
                    for(int y=0;y<t.height;y++)Array.Copy(t.rgba,y*stride,pixels,(t.height-1-y)*stride,stride);
                    texture.SetPixelData<byte>(pixels,0);texture.Apply(floor,true);result.Frontend[pair.Key]=texture;
                }
            }
        }catch(Exception e){log("Frontend artwork unavailable: "+e.Message);}
        try{
            string path=Path.Combine(folder,"mw-frontend.json");
            if(File.Exists(path))foreach(var pair in MenuData.LoadFrontend(path,128,24*1024*1024)){
                var t=pair.Value;var texture=new Texture2D(t.width,t.height,TextureFormat.RGBA32,false){name=t.name,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
                var pixels=new byte[t.rgba.Length];int stride=t.width*4;for(int y=0;y<t.height;y++)Array.Copy(t.rgba,y*stride,pixels,(t.height-1-y)*stride,stride);
                texture.SetPixelData<byte>(pixels,0);texture.Apply(false,true);result.MwFrontend[pair.Key]=texture;
            }
        }catch(Exception e){log("MW frontend artwork unavailable: "+e.Message);}
        try {
            var path=Path.Combine(folder,"rust-logo.json");
            if(File.Exists(path)) {
                if(new FileInfo(path).Length>512*1024)throw new InvalidDataException("Oversized Rust logo");
                var t=JsonConvert.DeserializeObject<ModelTexture>(File.ReadAllText(path),new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=8});
                if(t==null||t.width<1||t.height<1||t.width>512||t.height>128||t.rgba==null||t.rgba.Length!=(long)t.width*t.height*4)throw new InvalidDataException("Invalid Rust logo");
                result.RustLogo=new Texture2D(t.width,t.height,TextureFormat.RGBA32,false){name="Official_Rust_Branding",wrapMode=TextureWrapMode.Clamp};
                var pixels=new byte[t.rgba.Length];int row=t.width*4;
                for(int y=0;y<t.height;y++)Array.Copy(t.rgba,y*row,pixels,(t.height-1-y)*row,row);
                result.RustLogo.SetPixelData<byte>(pixels,0);result.RustLogo.Apply(false,true);
            }
        }catch(Exception e){log("Optional Rust logo unavailable: "+e.Message);}
        try {
            foreach(var role in new[]{"navigate","accept","back"}) {
                var wav=WavPcm16.Read(File.OpenRead(Path.Combine(folder,role+".wav")));
                var clip=AudioClip.Create("NFS_Menu_"+role,wav.Samples.Length/wav.Channels,wav.Channels,wav.SampleRate,false);result.clips.Add(clip);
                if(!clip.SetData(wav.Samples,0))throw new InvalidDataException("Menu audio upload failed");
            }
        }catch(Exception e){log("Original menu sounds unavailable: "+e.Message);}
        try{
            foreach(var role in new[]{"navigate","accept","back"}){
                string path=Path.Combine(folder,"mw-"+role+".wav");if(!File.Exists(path))break;
                using(var stream=File.OpenRead(path)){
                    var wav=WavPcm16.Read(stream);var clip=AudioClip.Create("MW_Menu_"+role,wav.Samples.Length/wav.Channels,wav.Channels,wav.SampleRate,false);result.mwClips.Add(clip);
                    if(!clip.SetData(wav.Samples,0))throw new InvalidDataException("MW menu audio upload failed");
                }
            }
        }catch(Exception e){log("MW menu sounds unavailable: "+e.Message);}
        if(result.clips.Count==3||result.mwClips.Count==3){
            result.audioObject=new GameObject("NFVU2_MenuAudio");UnityEngine.Object.DontDestroyOnLoad(result.audioObject);
            result.source=result.audioObject.AddComponent<AudioSource>();result.source.playOnAwake=false;result.source.spatialBlend=0;
            result.source.outputAudioMixerGroup=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
        }
        return result;
    }
    public void Play(int sound,float volume,bool mw=false) {
        var bank=mw&&mwClips.Count==3?mwClips:clips;
        if(source==null||bank.Count!=3||sound<0||sound>=3||!Application.isFocused||float.IsNaN(volume)||float.IsInfinity(volume)||volume<=0||Time.unscaledTime-lastSound<.07f)return;
        lastSound=Time.unscaledTime;source.PlayOneShot(bank[sound],Mathf.Clamp01(volume));
    }
    public void Dispose() {
        foreach(var texture in Frontend.Values)UnityEngine.Object.Destroy(texture);Frontend.Clear();
        foreach(var texture in MwFrontend.Values)UnityEngine.Object.Destroy(texture);MwFrontend.Clear();
        if(audioObject!=null)UnityEngine.Object.Destroy(audioObject);
        foreach(var clip in clips)UnityEngine.Object.Destroy(clip);
        foreach(var clip in mwClips)UnityEngine.Object.Destroy(clip);
        foreach(var icon in Icons)if(icon!=null)UnityEngine.Object.Destroy(icon);
        if(RustLogo!=null)UnityEngine.Object.Destroy(RustLogo);
    }
}
}
