using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using Newtonsoft.Json;
namespace NeedForValheim {
// Each network instance has its own settings. Binding uses canonical profile
// definitions so wire snapshots remain compatible with singleplayer builds.
// The ConfigFile is never saved, and its unique nonexistent path is not loaded.
internal sealed class NetworkVehicleSettings {
    readonly ConfigFile config;
    Dictionary<string,string> values;
    readonly Dictionary<ConfigEntryBase,string> previous=new Dictionary<ConfigEntryBase,string>();
    readonly HashSet<ConfigEntryBase> edited=new HashSet<ConfigEntryBase>();
    readonly Dictionary<ConfigEntryBase,string> runtimeSaved=new Dictionary<ConfigEntryBase,string>();
    bool applying;public long Version {get;private set;}
    public NetworkVehicleSettings(string snapshot,Func<bool> userEditing=null,Func<bool> canEdit=null,Dictionary<string,string> fallbacks=null) {
        values=Parse(snapshot);
        if(fallbacks!=null)foreach(var pair in fallbacks)if(!values.ContainsKey(pair.Key))values[pair.Key]=pair.Value;
        config=new ConfigFile(Path.Combine(Path.GetTempPath(),"nfvu2-memory-"+Guid.NewGuid().ToString("N")+".cfg"),false){SaveOnConfigSet=false};
        config.SettingChanged+=(sender,args)=>{
            if(applying)return;var entry=args.ChangedSetting;
            bool user=userEditing==null||userEditing();
            if(user&&canEdit!=null&&!canEdit()) {
                applying=true;try{if(previous.TryGetValue(entry,out var before))entry.SetSerializedValue(before);}finally{applying=false;}return;
            }
            previous[entry]=entry.GetSerializedValue();
            if(user){runtimeSaved.Remove(entry);edited.Add(entry);Version++;}
        };
    }
    public void SetRuntime<T>(ConfigEntry<T> entry,T value) {
        if(!previous.ContainsKey(entry))throw new InvalidOperationException("Runtime setting belongs to another vehicle");
        if(!runtimeSaved.ContainsKey(entry))runtimeSaved[entry]=entry.GetSerializedValue();
        if(!values.ContainsKey(Key(entry)))values[Key(entry)]=runtimeSaved[entry];
        applying=true;
        try {entry.Value=value;previous[entry]=entry.GetSerializedValue();}
        finally {applying=false;}
    }
    static Dictionary<string,string> Parse(string text) {
        if(text==null||text.Length>131072)throw new InvalidDataException("Invalid network vehicle setup size");
        Dictionary<string,string> result;
        using(var reader=new JsonTextReader(new StringReader(text))) {
            reader.MaxDepth=4;
            result=JsonSerializer.Create(new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,CheckAdditionalContent=true}).Deserialize<Dictionary<string,string>>(reader);
        }
        if(result==null||result.Count>512||result.Any(p=>p.Key.Length>256||p.Value==null||p.Value.Length>16384))throw new InvalidDataException("Invalid network vehicle setup");
        return result;
    }
    static string Key(ConfigEntryBase entry)=>entry.Definition.Section+"/"+entry.Definition.Key;
    public ConfigEntry<T> Get<T>(ConfigEntry<T> source) {
        if(source==null)return null;
        if(config.TryGetEntry<T>(source.Definition,out var existing))return existing;
        applying=true;
        try {
            var entry=config.Bind(source.Definition,(T)source.DefaultValue,source.Description);
            if(values.TryGetValue(Key(source),out var serialized))entry.SetSerializedValue(serialized);
            previous[entry]=entry.GetSerializedValue();return entry;
        }finally{applying=false;}
    }
    public IEnumerable<ConfigEntryBase> Entries=>config.Select(p=>p.Value);
    public string Snapshot() {
        var result=new Dictionary<string,string>(values);
        foreach(var entry in Entries)if(edited.Contains(entry)||!result.ContainsKey(Key(entry)))result[Key(entry)]=runtimeSaved.TryGetValue(entry,out var saved)?saved:entry.GetSerializedValue();
        return JsonConvert.SerializeObject(result);
    }
    public bool Rebase(string serverSnapshot,string baselineSnapshot) {
        var baseline=Parse(baselineSnapshot);var server=Parse(serverSnapshot);
        var draft=Parse(Snapshot());var pending=new List<ConfigEntryBase>();
        foreach(var entry in edited) {
            string key=Key(entry);
            if(draft.TryGetValue(key,out var value)&&(!baseline.TryGetValue(key,out var before)||before!=value)&&
                (!server.TryGetValue(key,out var confirmed)||confirmed!=value)) {
                server[key]=value;pending.Add(entry);
            }
        }
        // Replace remains transactional and keeps runtime-only controls intact.
        Replace(JsonConvert.SerializeObject(server));
        foreach(var entry in pending)edited.Add(entry);
        return pending.Count>0;
    }
    public void Replace(string snapshot) {
        var next=Parse(snapshot);var entries=Entries.ToArray();var before=entries.Select(e=>e.GetSerializedValue()).ToArray();
        var oldValues=values;var oldEdited=edited.ToArray();
        var oldRuntime=new Dictionary<ConfigEntryBase,string>(runtimeSaved);
        var runtime=runtimeSaved.Keys.ToDictionary(e=>e,e=>e.GetSerializedValue());
        applying=true;
        try {
            foreach(var entry in entries) {
                entry.BoxedValue=entry.DefaultValue;
                if(next.TryGetValue(Key(entry),out var serialized))entry.SetSerializedValue(serialized);
            }
            foreach(var pair in runtime) {
                runtimeSaved[pair.Key]=pair.Key.GetSerializedValue();pair.Key.SetSerializedValue(pair.Value);
            }
            values=next;edited.Clear();
            foreach(var entry in entries)previous[entry]=entry.GetSerializedValue();
        }catch {
            for(int i=0;i<entries.Length;i++){entries[i].SetSerializedValue(before[i]);previous[entries[i]]=before[i];}
            values=oldValues;edited.Clear();foreach(var entry in oldEdited)edited.Add(entry);
            runtimeSaved.Clear();foreach(var pair in oldRuntime)runtimeSaved[pair.Key]=pair.Value;
            throw;
        }
        finally{applying=false;}
    }
}
}
