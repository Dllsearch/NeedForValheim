using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    ConfigEntry<string> traxStation,traxDisabled,traxLast;
    ConfigEntry<bool> traxU2,traxMW,radioAutoPlay;
    int traxPage;bool traxEditing;
    void InitializeRadioTrax(){
        radioAutoPlay=Config.Bind("EA TRAX","AutoPlay",true,"Automatically start vehicle music on spawn or entry. Manual Play remains available when disabled.");
        traxStation=Config.Bind("EA TRAX","Station","mixed",new ConfigDescription("Radio station: u2, mw or mixed.",new AcceptableValueList<string>("u2","mw","mixed")));
        traxU2=Config.Bind("EA TRAX","Underground2",true,"Include Underground 2 music.");
        traxMW=Config.Bind("EA TRAX","MostWanted",true,"Include Most Wanted 2005 music.");
        traxDisabled=Config.Bind("EA TRAX","DisabledTracks","","Excluded stable source:track IDs; retained when a source is temporarily absent.");
        traxLast=Config.Bind("EA TRAX","LastPlayed","","Avoid this track at the next random start.");
        var options=new RadioTrax();options.Restore(traxStation.Value,traxU2.Value,traxMW.Value,traxDisabled.Value,traxLast.Value);
        options.Save=t=>{traxStation.Value=t.Station;traxU2.Value=t.U2;traxMW.Value=t.MW;traxDisabled.Value=t.Disabled;traxLast.Value=t.LastPlayed;Config.Save();};RadioTrax.Current=options;
    }
    void DrawRadioTraxMenu(){
        var options=RadioTrax.Current;
        radioEnabled.Value=UiToggle(new Rect(25,140,240,28),radioEnabled.Value,"Radio ON");
        radioAutoPlay.Value=UiToggle(new Rect(25,171,240,20),radioAutoPlay.Value,"Autoplay on spawn / entry");
        UiLabel(new Rect(280,140,248,23),"VOLUME  "+Mathf.RoundToInt(radioVolume.Value*100)+" %",mutedStyle);
        radioVolume.Value=UiSlider(new Rect(280,168,245,14),radioVolume.Value,0,1);
        string[] stations={"u2","mw","mixed"};int selected=Array.IndexOf(stations,options.Station);
        int station=UiToolbar(new Rect(25,190,510,30),Math.Max(0,selected),new[]{"UNDERGROUND 2","MOST WANTED","MIXED"});
        if(station!=selected){options.SetStation(stations[station]);traxPage=0;}
        options.SetSource("u2",UiToggle(new Rect(25,227,245,26),options.U2,"U2 source"));
        options.SetSource("mw",UiToggle(new Rect(285,227,245,26),options.MW,"MW source"));
        traxEditing=UiToolbar(new Rect(25,262,510,28),traxEditing?1:0,new[]{"RADIO / PLAY","EA TRAX / ENABLE TRACKS"})==1;
        if(vehicleRadio==null||vehicleRadio.Count==0){UiLabel(new Rect(25,310,510,95),"No local soundtrack available. Import the U2 or MW music library.",labelStyle);return;}
        UiLabel(new Rect(25,300,510,45),vehicleRadio.CurrentTitle,labelStyle);
        bool canPlay=(driving||ParkedRunning||current.networkData!=null&&current.networkData.GetLong(VehicleNetworkProtocol.Driver)!=0)&&radioEnabled.Value;
        GUI.enabled=canPlay;
        if(MenuButton(new Rect(25,346,157,29),"◀ PREVIOUS"))vehicleRadio.Previous();
        if(MenuButton(new Rect(197,346,157,29),vehicleRadio.Paused?"PLAY":"PAUSE"))vehicleRadio.TogglePause();
        if(MenuButton(new Rect(369,346,157,29),"NEXT ▶"))vehicleRadio.Next();
        GUI.enabled=true;
        var tracks=vehicleRadio.Tracks;var visible=Enumerable.Range(0,tracks.Count).Where(i=>options.Station=="mixed"||tracks[i].Source==options.Station).ToArray();
        const int pageSize=4;int pages=Math.Max(1,(visible.Length+pageSize-1)/pageSize);traxPage=Math.Max(0,Math.Min(traxPage,pages-1));
        for(int row=0;row<pageSize;row++){
            int index=traxPage*pageSize+row;if(index>=visible.Length)break;int song=visible[index];var track=tracks[song];var rect=new Rect(25,384+row*29,510,27);
            string title=(track.Source=="mw"?"[MW] ":"[U2] ")+track.Title;
            if(traxEditing)options.SetTrack(track.Id,UiToggle(rect,options.TrackEnabled(track.Id),title));
            else {GUI.enabled=canPlay&&options.Allows(track);if(MenuButton(rect,(song==vehicleRadio.CurrentIndex?"▶ ":"")+title,songButtonStyle,false))vehicleRadio.Select(song);GUI.enabled=true;}
        }
        if(visible.Length==0)UiLabel(new Rect(25,392,510,65),"This station has no imported tracks.",mutedStyle);
        GUI.enabled=traxPage>0;if(MenuButton(new Rect(25,507,105,25),"◀ PAGE"))traxPage--;GUI.enabled=traxPage+1<pages;if(MenuButton(new Rect(430,507,105,25),"PAGE ▶"))traxPage++;GUI.enabled=true;
        UiLabel(new Rect(144,507,280,25),(traxPage+1)+" / "+pages+"  ·  "+tracks.Count(t=>options.Allows(t))+" ACTIVE  ·  SHUFFLE",mutedStyle);
        UiLabel(new Rect(25,538,510,21),traxEditing?"Choices saved · all tracks OFF means silence":vehicleRadio.Status,mutedStyle);
    }
}
}
