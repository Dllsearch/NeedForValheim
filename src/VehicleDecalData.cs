using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
namespace NeedForValheim {
public sealed class DecalArtwork {
    public int id,width,height;public string name,title,kind;public byte[] data;
}
public sealed class DecalArtworkCatalog {
    public int schemaVersion;public string sourceGame;public DecalArtwork[] entries;
    public static DecalArtworkCatalog Load(string path) {
        var file=new FileInfo(path);if(file.Length<1||file.Length>32*1024*1024)throw new InvalidDataException("Invalid decal artwork file size");
        var result=JsonConvert.DeserializeObject<DecalArtworkCatalog>(File.ReadAllText(path),new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=8,CheckAdditionalContent=true});
        if(result==null||result.schemaVersion!=1||result.sourceGame!="mw"||result.entries==null||result.entries.Length<1||result.entries.Length>1024)throw new InvalidDataException("Invalid decal artwork catalog");
        var ids=new HashSet<int>();foreach(var item in result.entries)if(item==null||item.id<100||!ids.Add(item.id)||item.width<1||item.height<1||item.width>512||item.height>512||item.data==null||item.data.Length>512*512*4||string.IsNullOrEmpty(item.title)||item.title.Length>100||(item.kind!="RECT"&&item.kind!="WIDE"))throw new InvalidDataException("Invalid original decal artwork");
        return result;
    }
}
}
