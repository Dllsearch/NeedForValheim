using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NeedForValheim {
public sealed class RadioTrack {
    public string Path {get;private set;}
    public string Title {get;private set;}
    public string Source {get;private set;}
    public string Id {get;private set;}
    public RadioTrack(string path,string title,string source="u2"){Path=path;Title=title;Source=source;Id=source+":"+System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();}
}
public static class RadioPlaylist {
    public static RadioTrack[] Read(string folder,Action<string> log,string source="u2") {
        var tracks=new List<RadioTrack>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if(!Directory.Exists(folder))return tracks.ToArray();
        bool manifestValid=false;
        string manifest=System.IO.Path.Combine(folder,"tracks.json");
        if(File.Exists(manifest))try {
            foreach(var entry in JArray.Parse(File.ReadAllText(manifest))) {
                string file=(string)entry["file"],title=(string)entry["title"];
                if(string.IsNullOrEmpty(file)||file!=System.IO.Path.GetFileName(file)||file.IndexOfAny(new[]{'/', '\\', ':'})>=0)continue;
                Add(System.IO.Path.Combine(folder,file),title,tracks,seen,source);
            }
            manifestValid=true;
        }catch(Exception e){log("Vehicle radio manifest ignored: "+e.Message);}
        if(!manifestValid)foreach(string path in Directory.GetFiles(folder).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase))Add(path,null,tracks,seen,source);
        return tracks.ToArray();
    }
    static void Add(string path,string title,List<RadioTrack> tracks,HashSet<string> seen,string source) {
        string ext=System.IO.Path.GetExtension(path).ToLowerInvariant();
        if((ext!=".wav"&&ext!=".ogg")||!File.Exists(path)||!seen.Add(System.IO.Path.GetFullPath(path)))return;
        tracks.Add(new RadioTrack(path,string.IsNullOrWhiteSpace(title)?System.IO.Path.GetFileNameWithoutExtension(path):title,source));
    }
}
// No Unity dependencies: selection, failed-track recovery and async load identity.
public sealed class RadioPlaybackState {
    readonly bool[] failed;
    readonly bool[] enabled;
    readonly bool shuffle;
    readonly Random random;
    readonly List<int> bag=new List<int>(),history=new List<int>();
    int historyCursor=-1;
    public int Count { get { return failed.Length; } }
    public int Index { get; private set; }
    public int Revision { get; private set; }
    public bool Paused { get; private set; }
    public bool HasPlayable { get { return Index >= 0; } }
    public RadioPlaybackState(int count,bool shuffle=false,int? seed=null,int avoid=-1,bool[] eligible=null) {
        if(count<0)throw new ArgumentOutOfRangeException(nameof(count));
        failed=new bool[count];enabled=Enumerable.Repeat(true,count).ToArray();this.shuffle=shuffle;random=seed.HasValue?new Random(seed.Value):new Random(Guid.NewGuid().GetHashCode());Index=-1;
        if(eligible!=null){if(eligible.Length!=count)throw new ArgumentException("Radio eligibility size");Array.Copy(eligible,enabled,count);}
        if(shuffle){Refill(avoid);NewTrack();}else Index=count>0?0:-1;
    }
    bool Playable(int index)=>index>=0&&index<Count&&enabled[index]&&!failed[index];
    void Refill(int avoid){
        bag.Clear();for(int i=0;i<Count;i++)if(Playable(i))bag.Add(i);
        for(int i=bag.Count-1;i>0;i--){int j=random.Next(i+1);int v=bag[i];bag[i]=bag[j];bag[j]=v;}
        if(bag.Count>1&&bag[0]==avoid){int j=random.Next(1,bag.Count);int v=bag[0];bag[0]=bag[j];bag[j]=v;}
    }
    bool NewTrack(){
        if(bag.Count==0)Refill(Index);
        while(bag.Count>0){int next=bag[0];bag.RemoveAt(0);if(!Playable(next))continue;Index=next;
            if(historyCursor+1<history.Count)history.RemoveRange(historyCursor+1,history.Count-historyCursor-1);
            history.Add(next);if(history.Count>512)history.RemoveAt(0);historyCursor=history.Count-1;Revision++;return true;}
        Index=-1;Revision++;return false;
    }
    public bool SetEligible(bool[] choices){
        if(choices==null||choices.Length!=Count)throw new ArgumentException("Radio eligibility size");
        if(enabled.SequenceEqual(choices))return false;
        Array.Copy(choices,enabled,Count);bag.RemoveAll(i=>!Playable(i));
        // New choices can rejoin the next shuffle cycle; a muted current song stops now.
        if(Playable(Index))return false;
        int old=Index;Move(1);return old!=Index;
    }
    public bool Select(int index) {
        if(index<0||index>=Count||!enabled[index])return false;
        failed[index]=false;Index=index;Revision++;
        if(shuffle){bag.Remove(index);if(historyCursor+1<history.Count)history.RemoveRange(historyCursor+1,history.Count-historyCursor-1);history.Add(index);if(history.Count>512)history.RemoveAt(0);historyCursor=history.Count-1;}
        return true;
    }
    public bool Move(int direction) {
        if(Count==0)return false;
        if(shuffle){
            int step=direction<0?-1:1;
            for(int h=historyCursor+step;h>=0&&h<history.Count;h+=step)if(Playable(history[h])){historyCursor=h;Index=history[h];Revision++;return true;}
            return direction<0?false:NewTrack();
        }
        int start=Index<0?0:Index;
        for(int step=1;step<=Count;step++) {
            int candidate=((start+(direction<0?-step:step))%Count+Count)%Count;
            if(Playable(candidate)) {Index=candidate;Revision++;return true;}
        }
        Index=-1;Revision++;return false;
    }
    public bool MarkFailed(int revision) {
        if(revision!=Revision||Index<0)return false;
        failed[Index]=true;Move(1);return true;
    }
    public bool IsCurrent(int revision) {return revision==Revision&&Index>=0;}
    public void StartSession(bool autoPlay){Paused=!autoPlay;}
    public void TogglePause(){Paused=!Paused;}
}
}
