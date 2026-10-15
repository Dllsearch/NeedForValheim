using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeedForValheim;
class Program {
    static int checks;
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;}
    // A catalog file is rejected by validation (InvalidDataException) or by the JSON reader.
    static void Throws(Func<object> build,string name) {
        try{build();}catch(Exception){checks++;return;}
        throw new Exception(name+" was accepted");
    }
    static CarRig RoadRig()=>new CarRig{boxSizeX=1.6f,boxSizeY=.8f,boxSizeZ=3.8f,mass=1400,trackWidth=.8f,frontAxleZ=1.3f,rearAxleZ=1.35f,wheelRadius=.34f,spawnLift=.35f,rimNominal=20,rimMin=16,rimMax=22};
    static CarSound RoadSound()=>new CarSound{gearRatios=new[]{4.15f,2.32f,1f},rpmBands=new[]{1400,2100,2800,3500},upShiftRpm=5600,downShiftRpm=2000,minRpm=950,maxRpm=6700};
    static CarProfile Extra(string id)=>new CarProfile{id=id,rig=RoadRig(),sound=RoadSound()};
    static CarProfile Rig(CarProfile profile,Action<CarRig> edit){edit(profile.rig);return profile;}
    static CarProfile Sound(CarProfile profile,Action<CarSound> edit){edit(profile.sound);return profile;}
    static CarCatalog Catalog(params CarProfile[] extra)=>new CarCatalog(new[]{CarCatalog.Nissan350Z()}.Concat(extra));
    const string RoadJson=@"{""boxSizeX"":1.6,""boxSizeY"":0.8,""boxSizeZ"":3.8,""mass"":1400,""trackWidth"":0.8,""frontAxleZ"":1.3,""rearAxleZ"":1.35,""wheelRadius"":0.34,""spawnLift"":0.35,""rimNominal"":20,""rimMin"":16,""rimMax"":22}";
    const string SoundJson=@"{""gearRatios"":[4.15,2.32,1.0],""rpmBands"":[1400,2100,2800,3500],""upShiftRpm"":5600,""downShiftRpm"":2000,""minRpm"":950,""maxRpm"":6700}";

    static void Main(string[] args) {
        var stock=CarCatalog.CreateStock();
        Check(stock.Count==2,"the built-in catalog has both cars");
        Check(stock.Get(CarCatalog.StockId).shortName=="350Z","the stock id resolves to the 350Z");
        Check(stock.Get(CarCatalog.H2Id).IsSuv,"the SUV id is recognised");
        Check(stock.Get("no such car").id==CarCatalog.StockId,"an unknown spawn setting falls back to the stock car");
        Check(stock.GetByDisplayName("Hummer H2").IsSuv,"a garage label resolves to its car");
        Check(CarCatalog.Nissan350Z().ConfigPrefix.Length==0,"the stock car keeps the original setting names");
        Check(CarCatalog.BuiltInH2().ConfigPrefix=="h2.","the built-in SUV keeps its historic prefix");
        Check(Extra("silvia").ConfigPrefix=="silvia.","a new car gets its own setting prefix");

        var customSuv=Extra("custom-suv");customSuv.capabilities.suvTuning=true;
        Check(customSuv.IsSuv,"SUV behaviour follows the capability for any model id");
        var roadH2=CarCatalog.BuiltInH2();roadH2.capabilities.suvTuning=false;
        Check(!roadH2.IsSuv,"the H2 id alone does not force SUV behaviour");

        var z=CarCatalog.Nissan350Z();var h2=CarCatalog.BuiltInH2();
        Check(z.rig.boxSizeZ==3.8f&&z.rig.mass==1450&&z.rig.trunkApproachZ==1.3f,"the 350Z keeps its chassis numbers");
        Check(h2.rig.boxSizeZ==4.35f&&h2.rig.mass==2900&&h2.rig.trunkApproachZ==1.8f&&h2.rig.spinners,"the H2 keeps its chassis numbers");
        Check(z.rig.rimNominal==20&&h2.rig.rimNominal==26&&h2.rig.rimMin==22&&h2.rig.rimMax==28,"each car carries its own rim range");
        Check(z.capabilities.drift&&z.capabilities.turret&&z.capabilities.nitro&&z.capabilities.showroom&&z.capabilities.turbo,"the 350Z feature set");
        Check(!h2.capabilities.drift&&!h2.capabilities.turret&&h2.capabilities.nitro&&!h2.capabilities.showroom&&h2.capabilities.suvEngine&&h2.capabilities.suvTuning,"the H2 retains its SUV feature set and supports NOS kits");

        for(int layout=0;layout<4;layout++)
            Check(VehicleStorageRules.Capacity(z,layout)==VehicleStorageRules.Capacity(false,layout)&&
                  VehicleStorageRules.Capacity(h2,layout)==VehicleStorageRules.Capacity(true,layout),"cargo capacity follows the car at layout "+layout);
        Check(VehicleStorageRules.Width(h2)==8&&VehicleStorageRules.Rows(h2,3)==4,"a car without a showroom keeps the full floor");

        var road=z.capabilities;var suv=h2.capabilities;
        Check(ControlRules.CycleMode(1,1,road)==2&&ControlRules.CycleMode(2,1,road)==3&&ControlRules.CycleMode(3,1,road)==0,"the 350Z cycles through drift");
        Check(ControlRules.CycleMode(1,1,suv)==3&&ControlRules.CycleMode(3,-1,suv)==1&&ControlRules.CycleMode(0,1,suv)==1,"a car without drift cycles around it");
        for(int mode=0;mode<4;mode++)for(int direction=-1;direction<=1;direction+=2)
            Check(ControlRules.CycleMode(mode,direction,suv)==ControlRules.CycleMode(mode,direction,true)&&
                  ControlRules.CycleMode(mode,direction,road)==ControlRules.CycleMode(mode,direction,false),"mode cycling matches the old car switch at "+mode+"/"+direction);

        var fresh=Catalog(Extra("silvia")).Get("silvia");
        Check(fresh.assets.modelFile=="body.json","a model file path is required");
        Check(fresh.assets.audioFolders.Length==2,"the default audio folders are used");
        Check(fresh.assets.hudFile=="hud/hud.json","the gauge layout has a default path");
        Check(fresh.displayName=="silvia"&&fresh.shortName=="silvia"&&fresh.summary=="silvia","names default to the id");
        Check(fresh.prefabName=="silvia_Local_Prototype","the prefab name defaults to the id");
        Check(fresh.rig.trunkApproachZ==1.3f,"the cargo approach point gets a default");
        Check(fresh.assets.interiorFile==null&&fresh.assets.turretFile==null&&fresh.assets.turretFolder==null,"optional assets stay unset without the feature");
        var built=Catalog(new CarProfile{id="silvia",rig=RoadRig(),sound=RoadSound(),capabilities=new CarCapabilities{interior=true,turret=true}}).Get("silvia");
        Check(built.assets.interiorFile=="interior/interior.json","the cabin path defaults with the interior feature");
        Check(built.assets.turretFile=="turret/turret.json"&&built.assets.turretFolder=="turret","the turret paths default with the turret feature");

        Throws(()=>new CarCatalog(Array.Empty<CarProfile>()),"an empty catalog is refused");
        Throws(()=>new CarCatalog(new[]{Extra("silvia")}),"a catalog without the stock car is refused");
        Throws(()=>Catalog(Extra("silvia"),Extra("silvia")),"duplicate ids are refused");
        Throws(()=>Catalog(Enumerable.Range(1,CarCatalog.MaxProfiles).Select(i=>Extra("car"+i)).ToArray()),"catalog capacity overflow is refused");
        Check(Catalog(Enumerable.Range(1,CarCatalog.MaxProfiles-1).Select(i=>Extra("car"+i)).ToArray()).Count==CarCatalog.MaxProfiles,"full catalog capacity is accepted");
        var mw=Extra("mw-golf");mw.sourceGame="MW";
        var sources=Catalog(Extra("golf"),mw);
        Check(sources.ForSource("u2").Length==2&&sources.ForSource("mw").Single().id=="mw-golf","source tabs separate the same car from U2 and MW");
        Check(sources.Get("golf").ConfigPrefix!=mw.ConfigPrefix,"MW and U2 keep separate saved settings");
        var invalidSource=Extra("other");invalidSource.sourceGame="unknown";
        Throws(()=>Catalog(invalidSource),"unknown sources are refused");
        Throws(()=>Catalog(Extra("bad car!")),"a file-unsafe id is refused");
        Throws(()=>Catalog(Extra(new string('c',33))),"an over-long id is refused");
        Throws(()=>Catalog(Rig(Extra("silvia"),r=>r.mass=0)),"a car without mass is refused");
        Throws(()=>Catalog(Rig(Extra("silvia"),r=>r.wheelRadius=-1)),"a negative wheel radius is refused");
        Throws(()=>Catalog(Rig(Extra("silvia"),r=>r.boxCenterY=float.NaN)),"a NaN chassis offset is refused");
        Throws(()=>Catalog(Rig(Extra("silvia"),r=>r.spawnLift=-1)),"a car spawning below the ground is refused");
        Throws(()=>Catalog(Rig(Extra("silvia"),r=>r.rimNominal=40)),"a rim outside its own range is refused");
        Throws(()=>Catalog(Rig(Extra("silvia"),r=>r.trunkApproachZ=float.NaN)),"a NaN cargo distance is refused");
        Throws(()=>Catalog(Sound(Extra("silvia"),s=>s.gearRatios=new float[0])),"a car without gears is refused");
        Throws(()=>Catalog(Sound(Extra("silvia"),s=>s.gearRatios=Enumerable.Repeat(1f,9).ToArray())),"more than eight gears are refused");
        Throws(()=>Catalog(Sound(Extra("silvia"),s=>s.rpmBands=new[]{3500,1400})),"unsorted rpm bands are refused");
        Throws(()=>Catalog(Sound(Extra("silvia"),s=>s.minRpm=7000)),"an rpm range that never shifts up is refused");
        Throws(()=>Catalog(Sound(Extra("silvia"),s=>s.upShiftRpm=9000)),"an upshift above the limiter is refused");

        var folder=Path.Combine(Path.GetTempPath(),"nfvu2-cars-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var file=Path.Combine(folder,"cars.json");
        try {
            Check(CarCatalog.Load(folder).Count==2,"a missing cars.json keeps the built-in cars");
            File.WriteAllText(file,"  ");
            Check(CarCatalog.Load(folder).Count==2,"an empty cars.json keeps the built-in cars");
            File.WriteAllText(file,"[{\"id\":\"h2\",\"rig\":"+RoadJson+",\"sound\":"+SoundJson+"},{\"id\":\"silvia\",\"displayName\":\"Silvia S15\",\"shortName\":\"S15\",\"rig\":"+RoadJson+",\"sound\":"+SoundJson+"}]");
            var merged=CarCatalog.Load(folder);
            Check(merged.Count==3,"cars.json adds a new car next to the built-ins");
            Check(merged.Get("silvia").displayName=="Silvia S15"&&merged.Get("silvia").rig.mass==1400,"the new car keeps its own numbers");
            Check(merged.Get("silvia").assets.modelFile=="body.json","the new car gets default asset paths");
            Check(merged.Get("h2").rig.mass==1400&&merged.Get("h2").ConfigPrefix=="h2.","cars.json overrides a built-in car by id");
            Check(merged.Get(CarCatalog.StockId).rig.mass==1450,"an override never touches the untouched stock car");
            File.WriteAllText(file,"[{\"id\":\"silvia\"}]");
            Throws(()=>CarCatalog.Load(folder).Count,"a car without chassis numbers is refused");
            File.WriteAllText(file,"[{\"id\":\"350z\",\"rig\":"+RoadJson+",\"sound\":"+SoundJson+"}] trailing");
            Throws(()=>CarCatalog.Load(folder).Count,"text after the car list is refused");
        } finally {Directory.Delete(folder,true);}

        // The catalog the build ships is the one the game reads next to the plugin,
        // so every car it lists must also have a model the runtime loader accepts.
        if (args.Length == 1 && Directory.Exists(args[0])) {
            var shipped = CarCatalog.Load(args[0]);
            Check(shipped.Count > 2, "the shipped cars.json adds an imported car");
            foreach (var car in shipped.Profiles) {
                var model = Path.Combine(args[0], car.assets.modelFile);
                if (!File.Exists(model)) throw new Exception("catalog car has no model file: " + car.id);
                var data = ModelFile.Load(model);
                if(car.sourceGame==CarCatalog.Underground2) {
                Check(car.capabilities.nitro,car.id+" allows the garage NOS kit");
                Check(data.nitroFlameTexture>=0&&data.effectEmitters!=null&&data.effectEmitters.Any(e=>e.role=="purge"),car.id+" has original NOS artwork and fitted emitters");
                Check(data.lights.Any(l=>l.role=="headlight")&&data.lights.Any(l=>l.role=="taillight"),car.id+" has front projection and rear light emitters");
                Check(data.lights.Any(l=>l.role=="headlight"&&l.position.x<-.2f)&&data.lights.Any(l=>l.role=="headlight"&&l.position.x>.2f),car.id+" projects from headlamps on both sides of the car");
                if(car.id=="imprezawrx"||car.id=="lancerevo8")Check(data.parts.Any(p=>p.name.Contains("HEADLIGHT_LEFT")&&p.lightRole=="headlight")&&data.parts.Any(p=>p.name.Contains("HEADLIGHT_RIGHT")&&p.lightRole=="headlight"),car.id+" preserves distinct original left and right lamp meshes");
                Check(data.effectEmitters.Any(e=>e.role=="exhaust")&&data.effectEmitters.Where(e=>e.role=="exhaust").All(e=>e.position.z < -car.rig.rearAxleZ&&e.position.y>.04f),car.id+" exhaust flames are behind the rear axle and above ground");
                var smoke=data.textures[data.nitroPurgeTexture].rgba;
                Check(Enumerable.Range(0,smoke.Length/4).Any(i=>smoke[i*4+3]<255),car.id+" original NOS purge transparency survives import");
                if(car.id=="miata")Check(data.lights.Count(l=>l.role=="headlight")==2&&data.lights.Count(l=>l.role=="taillight")==2&&data.parts.Any(p=>p.lightRole=="headlight")&&data.parts.Any(p=>p.lightRole=="taillight"),"Miata has both headlamps, rear lamps and emissive lens surfaces");
                } else {
                    bool service=new[]{"mw-cemtr","mw-garb","mw-pizza","mw-taxi","mw-minivan","mw-pickupa"}.Contains(car.id);
                    if(!service){Check(data.lights.Count(l=>l.role=="headlight")==2&&data.lights.Count(l=>l.role=="taillight")==2,car.id+" keeps both original front and rear lamps");Check(data.parts.Any(p=>p.group=="brake")&&data.materials.Any(m=>m.rimPaint),car.id+" has original brakes and paintable rims");}
                    else Check(data.parts.Any(p=>p.group=="wheel"&&data.materials[p.material].wheelRubber),car.id+" preserves the original service tyre");
                }
                var styling=Path.Combine(args[0],"styling",car.id+".json");
                if(File.Exists(styling)) {
                    var library=ModelFile.Load(styling);
                    Check(library.styleOptions.Length>20&&library.parts.Length>0,car.id+" styling catalog validates in runtime loader");
                    Check(library.styleReplacements.All(r=>data.parts.Any(p=>p.name==r.Substring(r.IndexOf('|')+1))),car.id+" replacement targets exist in its actual model");
                    foreach(var rim in library.styleOptions.Where(o=>o.category=="Rims")) {
                        var backing=library.parts.Where(p=>p.option=="Rims:"+rim.id&&library.materials[p.material].name.StartsWith("010cb64a_")).SelectMany(p=>p.vertices).ToArray();
                        if(backing.Any(v=>Math.Sqrt(v.y*v.y+v.z*v.z)>(car.id=="350z"?.35f:car.rig.wheelRadius)*1.011f))throw new Exception(car.id+" rim retains an oversized atlas backing quad: "+rim.title);
                        for(int axle=0;axle<2;axle++) {
                            var mesh=library.parts.Where(p=>p.option=="Rims:"+rim.id&&ModelFile.ForAxle(p,axle*2)&&(library.materials[p.material].wheelRubber||library.materials[p.material].name.StartsWith("78743ca1_"))).SelectMany(p=>p.vertices).ToArray();
                            float radius=car.id=="350z"?.35f:car.rig.WheelRadius(axle*2);
                            if(mesh.Length==0||Math.Abs(mesh.Max(v=>Math.Abs(v.y))-radius)>.02f)throw new Exception(car.id+" aftermarket rim has incorrect tyre radius: "+rim.title);
                        }
                    }
                    Check(true,car.id+" aftermarket rims retain its tyre diameter");
                    Check(library.styleOptions.Where(o=>o.category=="Bodykit").All(o=>library.parts.Any(p=>p.option=="Bodykit:"+o.id&&p.name.Contains("_BODY_"))),car.id+" bodykit options include a real body shell, not placeholder windows");
                }
                Check(data.parts.Length > 0 && data.materials.Length > 0 && data.textures.Length > 0,
                      car.id + " model validates in the runtime loader");
                var wheel = data.parts.Where(p => p.group == "wheel").SelectMany(p => p.vertices).ToList();
                var body = data.parts.Where(p => p.group == "body").SelectMany(p => p.vertices).ToList();
                Check(wheel.Count > 0 && body.Count > 0, car.id + " has body and wheel meshes");
                Check(Math.Abs(wheel.Max(v => Math.Abs(v.y)) - Math.Max(car.rig.WheelRadius(0),car.rig.WheelRadius(2))) < .02f,
                      car.id + " wheel mesh radius matches its chassis");
                Check(car.rig.frontAxleZ + car.rig.rearAxleZ < body.Max(v => v.z) - body.Min(v => v.z),
                      car.id + " axles sit inside the body");
                Check(car.rig.trackWidth + wheel.Max(v => Math.Abs(v.x)) <= body.Max(v => Math.Abs(v.x)) + .15f,
                      car.id + " tyres stay within the body width");
            }
        }
        Console.WriteLine(checks+" car catalog checks passed. Spawning a new car in the game still requires a game test.");
    }
}
