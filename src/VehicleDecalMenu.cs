using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    readonly Dictionary<string,Dictionary<string,ConfigEntry<int>>> decalSettings=new Dictionary<string,Dictionary<string,ConfigEntry<int>>>();
    int decalSlot,decalPage;
    void InitializeDecals() {
        foreach(var profile in vehicleCatalog.Profiles) {
            var slots=new Dictionary<string,ConfigEntry<int>>();decalSettings[profile.id]=slots;
            foreach(var key in profile.assets.decalSlots??Array.Empty<string>())slots[key]=Config.Bind(profile.ConfigPrefix+"Decals",key,0,new ConfigDescription("Original decal artwork for this projection slot. Zero removes it.",new AcceptableValueRange<int>(0,int.MaxValue)));
        }
    }
    void TickDecals() {
        if(current.decals==null)return;
        var kit=current.styling?.ActiveOption("Bodykit");
        current.decals.Tick(decalSettings[current.Profile.id].ToDictionary(p=>p.Key,p=>p.Value.Value),kit!=null&&kit.sourceKit>=0?kit.sourceKit:0);
    }
    IEnumerable<ConfigEntryBase> CurrentDecalSettings()=>decalSettings.TryGetValue(current.Profile.id,out var entries)?entries.Values.Cast<ConfigEntryBase>():Enumerable.Empty<ConfigEntryBase>();
    void RestoreMissingDecalSettings(Dictionary<string,string> saved) {
        foreach(var entry in CurrentDecalSettings().Concat(new ConfigEntryBase[]{glassColour,glassShade}))if(!saved.ContainsKey(entry.Definition.Section+"/"+entry.Definition.Key))entry.BoxedValue=entry.DefaultValue;
    }
    void DrawDecalWorkshop() {
        var catalog=current.decals;
        if(catalog==null||catalog.Slots.Length==0){UiLabel(new Rect(25,190,510,70),(current.Profile.assets.decalSlots?.Length??0)>0?"Native decal assets are unavailable for this car.":"This model has no original decal projection slots.",mutedStyle);return;}
        decalSlot=Math.Max(0,Math.Min(decalSlot,catalog.Slots.Length-1));
        GUI.enabled=WorkshopParked&&decalSlot>0;if(MenuButton(new Rect(25,185,120,30),"‹ Slot")){decalSlot--;decalPage=0;}
        GUI.enabled=WorkshopParked&&decalSlot+1<catalog.Slots.Length;if(MenuButton(new Rect(410,185,120,30),"Slot ›")){decalSlot++;decalPage=0;}
        GUI.enabled=true;var slot=catalog.Slots[decalSlot];UiLabel(new Rect(157,187,245,26),(decalSlot+1)+" / "+catalog.Slots.Length+" decal slots",labelStyle);
        UiLabel(new Rect(25,225,510,28),slot.title.Replace(" RECT MEDIUM","").Replace(" WIDE MEDIUM",""),labelStyle);
        var options=new[]{new DecalArtwork{id=0,title="None"}}.Concat(catalog.Artwork.Where(e=>e.kind==slot.kind).OrderBy(e=>e.title)).ToArray();
        decalPage=Math.Max(0,Math.Min(decalPage,(options.Length-1)/9));var page=options.Skip(decalPage*9).Take(9).ToArray();var setting=decalSettings[current.Profile.id][slot.key];int selected=Array.FindIndex(page,e=>e.id==setting.Value);
        GUI.enabled=WorkshopParked;int next=UiGrid(new Rect(25,267,510,162),selected,page.Select(e=>e.title).ToArray(),3);if(next>=0&&next!=selected)setting.Value=page[next].id;
        GUI.enabled=decalPage>0;if(MenuButton(new Rect(25,448,120,30),"‹ Previous"))decalPage--;
        GUI.enabled=true;UiLabel(new Rect(159,451,240,27),"Artwork "+(decalPage+1)+" / "+((options.Length+8)/9),labelStyle);
        GUI.enabled=(decalPage+1)*9<options.Length;if(MenuButton(new Rect(410,448,120,30),"Next ›"))decalPage++;
        GUI.enabled=true;
    }
}
}
