using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace NeedForValheim {
// Chassis numbers, capabilities and asset layout for one vehicle. Plain fields keep the
// catalog testable without Unity and loadable from a JSON file next to the plugin.
public sealed class CarRig {
    public float boxCenterX, boxCenterY, boxCenterZ;
    public float boxSizeX, boxSizeY, boxSizeZ;
    public float mass;
    public float trackWidth, wheelHeight, frontAxleZ, rearAxleZ, wheelRadius;
    public float rearTrackWidth,rearWheelRadius,rearWheelHeight,tyreWidth,rearTyreWidth,extraRearAxleOffset;
    public float WheelRadius(int index)=>index>=2&&rearWheelRadius>0?rearWheelRadius:wheelRadius;
    public float WheelHeight(int index)=>index>=2&&rearWheelHeight>0?rearWheelHeight:wheelHeight;
    public float TrackWidth(int index) => index>=2&&rearTrackWidth>0?rearTrackWidth:trackWidth;
    public float spawnLift, clearanceX, clearanceY, clearanceZ;
    public float trunkApproachZ = 1.3f;
    public float rimNominal, rimMin, rimMax;
    public bool spinners;
}
public sealed class CarCapabilities {
    public bool showroom, interior, radio, neon, nitro, drift, flight, turret, exhaust;
    public bool turbo, suvEngine, suvTuning;
}
public sealed class CarSound {
    public float[] gearRatios;
    public int[] rpmBands;
    public float upShiftRpm, downShiftRpm, minRpm, maxRpm;
}
public sealed class CarAssets {
    public string modelFile,decalFile;
    public string[] decalSlots;
    public string[] engineStageFolders;
    public bool engineCoast;
    public string[] audioFolders;
    public string interiorFile, hudFile, turretFile, turretFolder, engineFolder, flameModelFile;
}
public sealed class CarProfile {
    public string id, displayName, shortName, prefabName, configPrefix, summary;
    // Missing source in existing catalogs remains Underground 2.
    public string sourceGame = CarCatalog.Underground2;
    public CarRig rig = new CarRig();
    public CarCapabilities capabilities = new CarCapabilities();
    public CarSound sound = new CarSound();
    public CarAssets assets = new CarAssets();
    public string ConfigPrefix => string.IsNullOrEmpty(configPrefix) ? (id == CarCatalog.StockId ? "" : id + ".") : configPrefix;
    public bool IsSuv => capabilities.suvTuning;
}

public sealed class CarCatalog {
    public const string StockId = "350z";
    public const string H2Id = "h2";
    public const string Underground2 = "u2", MostWanted = "mw";
    public const int MaxProfiles = 128;
    readonly List<CarProfile> items;
    public CarCatalog(IEnumerable<CarProfile> profiles) {
        items = profiles == null ? new List<CarProfile>() : profiles.ToList();
        if (items.Count == 0) throw new InvalidDataException("Vehicle catalog is empty");
        if (items.Count > MaxProfiles) throw new InvalidDataException("Vehicle catalog lists more than " + MaxProfiles + " cars");
        var seen = new HashSet<string>();
        foreach (var profile in items) {
            Validate(profile);
            if (!seen.Add(profile.id)) throw new InvalidDataException("Duplicate vehicle id: " + profile.id);
        }
        if (!seen.Contains(StockId)) throw new InvalidDataException("Vehicle catalog has no " + StockId + " entry");
    }
    public IReadOnlyList<CarProfile> Profiles => items;
    public CarProfile[] ForSource(string source) => items.Where(p => p.sourceGame == source).ToArray();
    public int Count => items.Count;
    public CarProfile Get(string id) => items.FirstOrDefault(p => p.id == id) ?? items[0];
    public CarProfile GetByDisplayName(string name) => items.FirstOrDefault(p => p.displayName == name) ?? items[0];

    // cars.json next to the plugin overrides the built-in entries by id and appends the rest.
    public static CarCatalog CreateStock() => new CarCatalog(new[] { Nissan350Z(), BuiltInH2() });
    public static CarCatalog Load(string folder) {
        var builtins = CreateStock().Profiles.ToList();
        var path = Path.Combine(folder, "cars.json");
        if (!File.Exists(path)) return new CarCatalog(builtins);
        var text = new FileInfo(path).Length > 4 * 1024 * 1024 ? null : File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(text)) return new CarCatalog(builtins);
        CarProfile[] extra;
        using (var stream = File.OpenText(path))
        using (var reader = new JsonTextReader(stream))
            extra = JsonSerializer.Create(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, MaxDepth = 32, CheckAdditionalContent = true })
                .Deserialize<CarProfile[]>(reader) ?? Array.Empty<CarProfile>();
        var merged = builtins.ToList();
        foreach (var profile in extra) {
            int index = merged.FindIndex(p => p.id == profile.id);
            if (index >= 0) merged[index] = profile;
            else merged.Add(profile);
        }
        return new CarCatalog(merged);
    }

    static void Validate(CarProfile profile) {
        if (profile == null || string.IsNullOrWhiteSpace(profile.id)) throw new InvalidDataException("Vehicle profile without id");
        if (profile.id.Length > 32 || profile.id.Any(c => !(char.IsLetterOrDigit(c) || c == '-' || c == '_')))
            throw new InvalidDataException("Vehicle id must be a short file-safe name: " + profile.id);
        profile.displayName = string.IsNullOrWhiteSpace(profile.displayName) ? profile.id : profile.displayName;
        if (string.IsNullOrWhiteSpace(profile.sourceGame)) profile.sourceGame = Underground2;
        profile.sourceGame = profile.sourceGame.ToLowerInvariant();
        if (profile.sourceGame != Underground2 && profile.sourceGame != MostWanted)
            throw new InvalidDataException("Unsupported vehicle source: " + profile.sourceGame);
        profile.shortName = string.IsNullOrWhiteSpace(profile.shortName) ? profile.id : profile.shortName;
        profile.prefabName = string.IsNullOrWhiteSpace(profile.prefabName) ? profile.id + "_Local_Prototype" : profile.prefabName;
        var rig = profile.rig;
        if (rig.boxSizeX <= 0 || rig.boxSizeY <= 0 || rig.boxSizeZ <= 0 || rig.mass <= 0 ||
            rig.trackWidth <= 0 || rig.frontAxleZ <= 0 || rig.rearAxleZ <= 0 || rig.wheelRadius <= 0)
            throw new InvalidDataException("Vehicle " + profile.id + " has a non-positive chassis value");
        foreach (var value in new[] { rig.boxCenterX, rig.boxCenterY, rig.boxCenterZ, rig.wheelHeight, rig.spawnLift,
            rig.clearanceX, rig.clearanceY, rig.clearanceZ, rig.trunkApproachZ })
            if (float.IsNaN(value) || float.IsInfinity(value) || Math.Abs(value) > 10)
                throw new InvalidDataException("Vehicle " + profile.id + " has an out-of-range chassis offset");
        if (rig.spawnLift < 0) throw new InvalidDataException("Vehicle " + profile.id + " spawns below the ground");
        if (float.IsNaN(rig.rearTrackWidth)||float.IsInfinity(rig.rearTrackWidth)||rig.rearTrackWidth<0||rig.rearTrackWidth>2)
            throw new InvalidDataException("Vehicle " + profile.id + " has an invalid rear track width");
        foreach(var value in new[]{rig.rearWheelRadius,rig.rearWheelHeight})if(float.IsNaN(value)||float.IsInfinity(value)||value<0||value>2)throw new InvalidDataException("Invalid rear wheel dimensions");
        if (rig.trunkApproachZ <= 0) rig.trunkApproachZ = 1.3f;
        if (rig.rimMin > 0 && !(rig.rimMin <= rig.rimNominal && rig.rimNominal <= rig.rimMax) &&
            !(rig.rimMax <= rig.rimNominal && rig.rimNominal <= rig.rimMin))
            throw new InvalidDataException("Vehicle " + profile.id + " has a rim size outside its range");
        var sound = profile.sound;
        if (sound.gearRatios == null || sound.gearRatios.Length < 1 || sound.gearRatios.Length > 8 ||
            sound.gearRatios.Any(r => float.IsNaN(r) || float.IsInfinity(r) || r <= 0))
            throw new InvalidDataException("Vehicle " + profile.id + " has an invalid gear table");
        if (sound.rpmBands == null || sound.rpmBands.Length < 2 || sound.rpmBands.Length > 16)
            throw new InvalidDataException("Vehicle " + profile.id + " has an invalid rpm band table");
        for (int i = 1; i < sound.rpmBands.Length; i++)
            if (sound.rpmBands[i] <= sound.rpmBands[i - 1]) throw new InvalidDataException("Vehicle " + profile.id + " has unsorted rpm bands");
        if (sound.minRpm < 0 || sound.minRpm >= sound.maxRpm || sound.downShiftRpm < sound.minRpm ||
            sound.upShiftRpm <= sound.downShiftRpm || sound.upShiftRpm > sound.maxRpm)
            throw new InvalidDataException("Vehicle " + profile.id + " has an invalid rpm range");
        if (string.IsNullOrWhiteSpace(profile.assets.modelFile)) profile.assets.modelFile = "body.json";
        if (profile.assets.audioFolders == null || profile.assets.audioFolders.Length == 0)
            profile.assets.audioFolders = new[] { "audio/effects", "audio/turbo" };
        if (string.IsNullOrWhiteSpace(profile.summary)) profile.summary = profile.displayName;
        if (profile.capabilities.interior && string.IsNullOrWhiteSpace(profile.assets.interiorFile))
            profile.assets.interiorFile = "interior/interior.json";
        if (profile.capabilities.turret) {
            if (string.IsNullOrWhiteSpace(profile.assets.turretFile)) profile.assets.turretFile = "turret/turret.json";
            if (string.IsNullOrWhiteSpace(profile.assets.turretFolder)) profile.assets.turretFolder = "turret";
        }
        if (string.IsNullOrWhiteSpace(profile.assets.hudFile)) profile.assets.hudFile = "hud/hud.json";
    }

    public static CarProfile Nissan350Z() => new CarProfile {
        id = StockId, displayName = "Nissan 350Z", shortName = "350Z", prefabName = "350Z_Local_Prototype", configPrefix = "",
        summary = "350Z with your existing features and settings.",
        rig = new CarRig {
            boxCenterY = .76f, boxSizeX = 1.62f, boxSizeY = .8f, boxSizeZ = 3.8f, mass = 1450,
            trackWidth = .8f, wheelHeight = .34f, frontAxleZ = 1.3f, rearAxleZ = 1.35f, wheelRadius = .34f,
            spawnLift = .35f, clearanceX = .29f, clearanceZ = .5f,
            rimNominal = 20, rimMin = 16, rimMax = 22
        },
        capabilities = new CarCapabilities { showroom = true, interior = true, radio = true, neon = true, nitro = true, drift = true, flight = true, turret = true, exhaust = true, turbo = true },
        sound = new CarSound {
            gearRatios = new[] { 4.15f, 2.32f, 1.62f, 1.27f, 1f, .79f },
            rpmBands = new[] { 1400, 2100, 2800, 3500, 4300, 5100, 5900, 6500 },
            upShiftRpm = 5600, downShiftRpm = 2000, minRpm = 950, maxRpm = 6700
        },
        assets = new CarAssets {
            modelFile = "body.json", audioFolders = new[] { "audio/effects", "audio/turbo", "turret" },
            interiorFile = "interior/interior.json", hudFile = "hud/hud.json", turretFolder = "turret"
        }
    };

    public static CarProfile BuiltInH2() => new CarProfile {
        id = H2Id, displayName = "Hummer H2", shortName = "H2", prefabName = "H2_Local_Prototype", configPrefix = "h2.",
        summary = "Yellow H2 \u00b7 26-inch spinners \u00b7 SUV audio\nNo engine bay or trunk customisation.",
        rig = new CarRig {
            boxCenterY = 1.02f, boxSizeX = 1.92f, boxSizeY = 1.42f, boxSizeZ = 4.35f, mass = 2900,
            trackWidth = .881f, wheelHeight = .5f, frontAxleZ = 1.521f, rearAxleZ = 1.58538f, wheelRadius = .4364f,
            spawnLift = .3f, clearanceX = .22f, clearanceY = -.01f, clearanceZ = .375f, trunkApproachZ = 1.8f,
            rimNominal = 26, rimMin = 22, rimMax = 28, spinners = true
        },
        capabilities = new CarCapabilities { radio = true, neon = true, nitro = true, flight = true, suvEngine = true, suvTuning = true },
        sound = new CarSound {
            gearRatios = new[] { 3.06f, 1.63f, 1f, .7f },
            rpmBands = new[] { 1000, 1500, 2000, 2500, 3000, 3600, 4200, 4782 },
            upShiftRpm = 4100, downShiftRpm = 1500, minRpm = 1000, maxRpm = 4782
        },
        assets = new CarAssets {
            modelFile = "h2/h2.json", audioFolders = new[] { "audio/effects", "audio/h2" },
            engineFolder = "audio/h2", flameModelFile = "body.json", turretFolder = null
        }
    };
}
}
