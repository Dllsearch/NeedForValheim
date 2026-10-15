using System;
using System.Collections.Generic;
using System.Linq;
namespace NeedForValheim {
// Shared across parked cars: station and TRAX choices belong to the listener.
public sealed class RadioTrax {
    public static RadioTrax Current=new RadioTrax();
    readonly HashSet<string> disabled=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public string Station {get;private set;}="mixed";
    public bool U2 {get;private set;}=true;
    public bool MW {get;private set;}=true;
    public string LastPlayed {get;private set;}="";
    public int Revision {get;private set;}
    public Action<RadioTrax> Save;
    public string Disabled=>string.Join(",",disabled.OrderBy(x=>x,StringComparer.Ordinal));
    public bool TrackEnabled(string id)=>!disabled.Contains(id);
    public bool Allows(RadioTrack track)=>TrackEnabled(track.Id)&&(track.Source=="mw"?MW:U2)&&(Station=="mixed"||Station==track.Source);
    public void Restore(string station,bool u2,bool mw,string excluded,string last){
        Station=station=="u2"||station=="mw"?station:"mixed";U2=u2;MW=mw;LastPlayed=last??"";disabled.Clear();
        foreach(string id in (excluded??"").Split(','))if(id.Length>0&&id.Length<=160)disabled.Add(id);Revision++;
    }
    public void SetStation(string station){if(station!="mixed"&&station!="u2"&&station!="mw"||station==Station)return;Station=station;Changed();}
    public void SetSource(string source,bool value){if(source=="mw"){if(MW==value)return;MW=value;}else if(source=="u2"){if(U2==value)return;U2=value;}else return;Changed();}
    public void SetTrack(string id,bool value){if(string.IsNullOrEmpty(id)||id.Length>160)return;if(value?!disabled.Remove(id):!disabled.Add(id))return;Changed();}
    public void Played(string id){if(id==LastPlayed)return;LastPlayed=id;Save?.Invoke(this);}
    void Changed(){Revision++;Save?.Invoke(this);}
}
}
