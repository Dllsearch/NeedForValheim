using System.Collections.Generic;
using BepInEx.Configuration;
namespace NeedForValheim {
public partial class Plugin {
    // One settings entry per catalog vehicle: the stock entry keeps its plain section,
    // every other car gets the section prefixed with its id (h2.Driving for the H2).
    readonly Dictionary<ConfigEntryBase,Dictionary<string,ConfigEntryBase>> vehicleSettings=new Dictionary<ConfigEntryBase,Dictionary<string,ConfigEntryBase>>();
    ConfigEntry<T> BindVehicle<T>(string section,string key,T value,string description)=>BindVehicle(section,key,value,new ConfigDescription(description));
    ConfigEntry<T> BindVehicle<T>(string section,string key,T value,ConfigDescription description) {
        var stock=Config.Bind(section,key,value,description);
        var byVehicle=new Dictionary<string,ConfigEntryBase>{{string.Empty,stock}};
        foreach(var profile in vehicleCatalog.Profiles) {
            string prefix=profile.ConfigPrefix;
            if(prefix.Length==0 || byVehicle.ContainsKey(prefix))continue;
            byVehicle[prefix]=Config.Bind(prefix+section,key,value,description);
        }
        vehicleSettings[stock]=byVehicle;return stock;
    }
    ConfigEntry<T> VehicleConfig<T>(ConfigEntry<T> stock) {
        if(stock==null || !vehicleSettings.TryGetValue(stock,out var byVehicle))return stock;
        return !byVehicle.TryGetValue(current.Profile.ConfigPrefix,out var other)?stock:(ConfigEntry<T>)other;
    }
    ConfigEntry<bool> baseConfig_terrainMode;
    ConfigEntry<bool> terrainMode {get=>VehicleConfig(baseConfig_terrainMode);set=>baseConfig_terrainMode=value;}
    ConfigEntry<float> baseConfig_soundVolume;
    ConfigEntry<float> soundVolume {get=>VehicleConfig(baseConfig_soundVolume);set=>baseConfig_soundVolume=value;}
    ConfigEntry<bool> baseConfig_radioEnabled;
    ConfigEntry<bool> radioEnabled {get=>VehicleConfig(baseConfig_radioEnabled);set=>baseConfig_radioEnabled=value;}
    ConfigEntry<bool> baseConfig_neonEnabled;
    ConfigEntry<bool> neonEnabled {get=>VehicleConfig(baseConfig_neonEnabled);set=>baseConfig_neonEnabled=value;}
    ConfigEntry<bool> baseConfig_neonPulsing;
    ConfigEntry<bool> neonPulsing {get=>VehicleConfig(baseConfig_neonPulsing);set=>baseConfig_neonPulsing=value;}
    ConfigEntry<bool> baseConfig_nitroEnabled;
    ConfigEntry<bool> nitroEnabled {get=>VehicleConfig(baseConfig_nitroEnabled);set=>baseConfig_nitroEnabled=value;}
    ConfigEntry<bool> baseConfig_backfireEnabled;
    ConfigEntry<bool> backfireEnabled {get=>VehicleConfig(baseConfig_backfireEnabled);set=>baseConfig_backfireEnabled=value;}
    ConfigEntry<bool> baseConfig_keepRunning;
    ConfigEntry<bool> keepRunning {get=>VehicleConfig(baseConfig_keepRunning);set=>baseConfig_keepRunning=value;}
    ConfigEntry<float> baseConfig_radioVolume;
    ConfigEntry<float> radioVolume {get=>VehicleConfig(baseConfig_radioVolume);set=>baseConfig_radioVolume=value;}
    ConfigEntry<string> baseConfig_neonColor;
    ConfigEntry<string> neonColor {get=>VehicleConfig(baseConfig_neonColor);set=>baseConfig_neonColor=value;}
    ConfigEntry<string> baseConfig_headlightPreset;
    ConfigEntry<string> headlightPreset {get=>VehicleConfig(baseConfig_headlightPreset);set=>baseConfig_headlightPreset=value;}
    ConfigEntry<float> baseConfig_headlightStrength;
    ConfigEntry<float> headlightStrength {get=>VehicleConfig(baseConfig_headlightStrength);set=>baseConfig_headlightStrength=value;}
    ConfigEntry<bool> baseConfig_headlightShowOff;
    ConfigEntry<bool> headlightShowOff {get=>VehicleConfig(baseConfig_headlightShowOff);set=>baseConfig_headlightShowOff=value;}
    ConfigEntry<float> baseConfig_frontCamber;
    ConfigEntry<float> frontCamber {get=>VehicleConfig(baseConfig_frontCamber);set=>baseConfig_frontCamber=value;}
    ConfigEntry<float> baseConfig_rearCamber;
    ConfigEntry<float> rearCamber {get=>VehicleConfig(baseConfig_rearCamber);set=>baseConfig_rearCamber=value;}
    ConfigEntry<float> baseConfig_frontWheelOffset;
    ConfigEntry<float> frontWheelOffset {get=>VehicleConfig(baseConfig_frontWheelOffset);set=>baseConfig_frontWheelOffset=value;}
    ConfigEntry<float> baseConfig_rearWheelOffset;
    ConfigEntry<float> rearWheelOffset {get=>VehicleConfig(baseConfig_rearWheelOffset);set=>baseConfig_rearWheelOffset=value;}
    ConfigEntry<bool> baseConfig_driftMode;
    ConfigEntry<bool> driftMode {get=>VehicleConfig(baseConfig_driftMode);set=>baseConfig_driftMode=value;}
}
}
