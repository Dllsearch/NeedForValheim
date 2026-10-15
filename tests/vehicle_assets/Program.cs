using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NeedForValheim;
class Program {
    static int checks;
    static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
    static void CheckAudio(CarProfile profile,string root) {
        if(string.IsNullOrEmpty(profile.assets.engineFolder))return;
        var folder=new[]{Path.Combine(root,profile.assets.engineFolder),Path.Combine(root,"audio-"+profile.id),Path.Combine(root,Path.GetFileName(profile.assets.engineFolder))}.FirstOrDefault(Directory.Exists);
        Check(folder!=null,profile.id+" original engine folder exists");
        foreach(var band in profile.sound.rpmBands) {
            using(var stream=File.OpenRead(Path.Combine(folder,"engine_"+band+".wav"))) {
                var wave=WavPcm16.Read(stream);
                Check(wave.Channels>=1&&wave.Channels<=2&&wave.Samples.Length>2000&&wave.Samples.Any(v=>Math.Abs(v)>.001f),profile.id+" original engine loop is audible at "+band);
            }
        }
    }
    static void Main(string[] args) {
        var root=args[0];var paths=Directory.GetDirectories(root).Select(p=>Path.Combine(p,"profile.json")).Where(File.Exists).ToArray();
        Check(paths.Length>0,"export fixtures must be present");
        foreach(var path in paths)foreach(var profile in JsonConvert.DeserializeObject<CarProfile[]>(File.ReadAllText(path))) {
            new CarCatalog(profile.id==CarCatalog.StockId?new[]{profile}:new[]{CarCatalog.Nissan350Z(),profile});
            var file=Path.Combine(root,profile.assets.modelFile);
            if(!File.Exists(file)&&profile.assets.modelFile=="body.json")file=Path.Combine(root,"model/body.json");
            var model=ModelFile.Load(file);
            CheckAudio(profile,args.Length>1?args[1]:root);
            if(profile.sourceGame==CarCatalog.MostWanted){checks++;continue;}
            Check(model.animations!=null&&model.animations.Length==8,profile.id+" has all source panel tracks");
            Check(model.trunkLayouts!=null&&model.trunkLayouts.Length==4,profile.id+" has original trunk layouts");
            Check(model.audioComponents!=null&&model.audioComponents.Length==39,profile.id+" has shared audio components");
            foreach(var animation in model.animations) {
                var first=animation.frames[0];var q=first.rotation;var p=first.offset;
                Check(Math.Abs(q.x)+Math.Abs(q.y)+Math.Abs(q.z)<.001&&Math.Abs(q.w-1)<.001,profile.id+" closed panel has no rotation jump");
                Check(Math.Abs(p.x)+Math.Abs(p.y)+Math.Abs(p.z)<.001,profile.id+" closed panel has no translation jump");
            }
            Check(model.parts.Any(p=>p.optionGroup=="engine")&&model.parts.Any(p=>p.optionGroup=="trunk"),profile.id+" imports engine and trunk geometry");
            Check(model.parts.Any(p=>p.movingPart=="leftDoor")&&model.parts.Any(p=>p.movingPart=="rightDoor"),profile.id+" binds its own doors");
            if(profile.rig.spinners)Check(model.parts.Any(p=>p.group=="spinner"),profile.id+" retains separate spinner meshes");
            var sound=new EngineSoundModel(profile.sound,profile.rig.wheelRadius);
            for(int i=0;i<80;i++)sound.Step(.02f,true,0,0,0,false);
            Check(Math.Abs(sound.Rpm-profile.sound.minRpm)<1,profile.id+" idles using its own profile");
            for(float rpm=profile.sound.rpmBands[0];rpm<=profile.sound.rpmBands.Last();rpm+=31) {
                double power=0;for(int i=0;i<profile.sound.rpmBands.Length;i++){var gain=EngineSoundModel.BandGain(rpm,i,profile.sound.rpmBands);power+=gain*gain;}
                Check(Math.Abs(power-1)<.00001,profile.id+" engine bands blend without holes");
            }
            Console.WriteLine("PASS "+profile.id+": "+model.parts.Length+" mesh sections accepted by the runtime loader");
        }
        Console.WriteLine(checks+" generic vehicle asset checks passed; Unity runtime remains separate.");
    }
}
