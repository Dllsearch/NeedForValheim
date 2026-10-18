using System;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace NeedForValheim {
public static class VehiclePaintShader {
    static bool attempted;
    static Shader shader;
    public static Shader Get() {
        if(attempted)return shader;
        attempted=true;
        AssetBundle bundle=null;
        try {
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("NeedForValheim.CarShaders")) {
                if(stream==null)return null;
                using(var bytes=new MemoryStream()) {
                    stream.CopyTo(bytes);bundle=AssetBundle.LoadFromMemory(bytes.ToArray());
                }
            }
            if(bundle==null)throw new InvalidDataException("Cannot load embedded car shader bundle.");
            var loaded=bundle.LoadAsset<Shader>("Assets/NFVU2CarPaint.shader");
            if(loaded==null||!loaded.isSupported)throw new InvalidDataException("Car paint shader is unsupported on this graphics device.");
            shader=loaded;
        }catch(Exception error){Debug.LogWarning("NFVU2 custom paint unavailable: "+error.Message);}
        finally{if(bundle!=null)bundle.Unload(false);}
        return shader;
    }
}
}
