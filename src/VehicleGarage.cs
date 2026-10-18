using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using BepInEx.Configuration;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    ConfigEntry<string> spawnModel;
    ConfigEntry<string> garageRoster;
    Queue<string> garageRestore;
    bool leavingGarage;
    CarCatalog vehicleCatalog=CarCatalog.CreateStock();
    MonoBehaviour fleetOwner;
    CarProfile SelectedProfile=>vehicleCatalog.Get(spawnModel.Value);
    void InitializeGarage() {
        try { vehicleCatalog=CarCatalog.Load(Path.GetDirectoryName(Info.Location)); }
        catch (Exception e) { vehicleCatalog=CarCatalog.CreateStock(); Logger.LogWarning("cars.json ignored: "+e); }
        spawnModel=Config.Bind("Garage","SpawnModel",CarCatalog.StockId,
            new ConfigDescription("Vehicle selected through the garage.",
                new AcceptableValueList<string>(vehicleCatalog.Profiles.Select(p=>p.id).ToArray())));
        garageRoster=Config.Bind("Garage","ParkedCars","[]","Saved garage roster (up to four model ids). Recall it from My garage after loading a world; World vehicle positions, setup and cargo are saved separately in the world. This list is a legacy recall roster.");
    }
    void SaveGarageRoster(){if(!leavingGarage&&garageRestore==null){garageRoster.Value=JsonConvert.SerializeObject(fleet.Select(v=>v.Profile.id));Config.Save();}}
    string[] SavedGarageIds() {
        try{return (JsonConvert.DeserializeObject<string[]>(garageRoster.Value)??Array.Empty<string>()).Where(id=>vehicleCatalog.Profiles.Any(p=>p.id==id)).Distinct().Take(4).ToArray();}
        catch{return Array.Empty<string>();}
    }
    void RecallGarage() {
        try {
            if(garageRoster.Value.Length>1024)throw new InvalidOperationException("Invalid garage roster");
            var ids=JsonConvert.DeserializeObject<string[]>(garageRoster.Value);
            if(ids==null||ids.Length>4||ids.Any(id=>!vehicleCatalog.Profiles.Any(p=>p.id==id)))throw new InvalidOperationException("Saved garage contains an unavailable car");
            garageRestore=new Queue<string>(ids.Distinct().Where(id=>!fleet.Any(v=>v.Profile.id==id)));CloseVehicleMenu();
        }catch(Exception error){workshopMessage=error.Message;garageRestore=null;}
    }
    void TickGarageRestore(MonoBehaviour local) {
        if(garageRestore==null||SpawnBusy)return;
        if(garageRestore.Count==0){garageRestore=null;SaveGarageRoster();return;}
        spawnModel.Value=garageRestore.Dequeue();reopenGarage=garageRestore.Count==0;
        Spawn(local);
        if(!SpawnBusy){garageRestore=null;reopenGarage=false;ShowVehicleWarning("Garage recall stopped: "+status);}
    }
    bool CanAccessCurrent(MonoBehaviour local)=>car!=null&&local!=null&&(driving||Vector3.Distance(local.transform.position,car.transform.position)<5);
    bool GarageCanPark=>!driving||!Flying&&DriverInputReady&&body!=null&&body.linearVelocity.magnitude<.5f;
    void TakeGarageCar(CarProfile profile) {
        if(SpawnBusy)return;
        if(!GarageCanPark){workshopMessage="Park before changing cars.";return;}
        var existing=fleet.FirstOrDefault(v=>v.Profile.id==profile.id&&(!NetworkSessionActive||OwnNetworkVehicle(v)));
        if(driving)Exit();
        spawnModel.Value=profile.id;
        if(existing!=null){current=existing;garageView=1;SelectFrontendProfile(profile);workshopMessage="Selected your parked "+profile.displayName+".";SetVehicleMenu(true);return;}
        if(NetworkSessionActive&&networkClient.OwnedVehicles.Any(v=>v.Profile==profile.id)){garageView=1;workshopMessage="Your saved car is listed in My garage. Return to its area to load it.";return;}
        reopenGarage=true;Spawn(LocalPlayer());if(!SpawnBusy)reopenGarage=false;
    }
    void RemoveGarageCar(VehicleRuntime vehicle) {
        if(vehicle==null||!fleet.Contains(vehicle)||SpawnBusy)return;
        if(vehicle.networkData!=null){RequestNetworkRemoval(vehicle);return;}
        if(hordeWave.Active||vehicle.driving&&!GarageCanPark){workshopMessage="Park before removing the occupied car.";return;}
        var selected=current;
        if(vehicle==current&&driving)Exit();else CloseVehicleMenu();
        current=vehicle;Cleanup();garageView=1;menuTab=10;
        current=selected!=vehicle&&fleet.Contains(selected)?selected:fleet.FirstOrDefault()??new VehicleRuntime();
        workshopMessage="Removed "+vehicle.Profile.displayName+" from your garage.";
        SetVehicleMenu(true);
    }
    void SelectNearest(MonoBehaviour local) {
        var nearest=fleet.Nearest(v=>v.car!=null?(v.car.transform.position-local.transform.position).sqrMagnitude:float.PositiveInfinity);
        if(nearest!=null)current=nearest;
    }
    void ForEachVehicle(Action action)=>fleet.Visit(action);
    void CleanupAll() {
        CaptureWorldVehicles();
        garageRestore=null;leavingGarage=true;
        ClearHorde();
        ClearNitroVisuals();
        if(spawnCacheUsed){spawnCache=new SpawnAssetCache();spawnCacheUsed=false;}
        CancelSpawn(null);
        if(driving||player!=null)Exit();
        CloseVehicleMenu();
        foreach(var vehicle in fleet.ToArray()){current=vehicle;Cleanup();}
        current=new VehicleRuntime();fleetOwner=null;orbitCamera?.End();fleetMusic?.Tick(false);
        leavingGarage=false;
    }
    bool FindSpawnPoint(MonoBehaviour local,CarProfile profile,out Vector3 result) {
        if(TryGarageStationSpawnAnchor(local,out var origin,out var yaw))return FindSpawnPoint(origin,yaw,profile,out result,true);
        return FindSpawnPoint(local.transform.position,SpawnRotation(local),profile,out result);
    }
    const int GaragePageSize=6;
    int garagePage,garageView,garageSource;
    void SelectGaragePage(CarProfile[] profiles,int page) {
        if(profiles.Length==0)return;
        garagePage=Mathf.Clamp(page,0,(profiles.Length-1)/GaragePageSize);
        var profile=profiles[garagePage*GaragePageSize];
        spawnModel.Value=profile.id;SelectFrontendProfile(profile);
    }
    bool reopenGarage;
    void DrawGarageMenu() {
        garageView=UiToolbar(new Rect(25,141,510,31),garageView,new[]{"Car lot","My garage · "+WorldVehicleCount+" / 4"});
        if(garageView==1){DrawGarageFleet();return;}
        int source=UiToolbar(new Rect(25,178,510,28),garageSource,new[]{"Underground 2","Most Wanted"});
        if(source!=garageSource){garageSource=source;garagePage=0;}
        var profiles=vehicleCatalog.ForSource(garageSource==0?CarCatalog.Underground2:CarCatalog.MostWanted);
        if(profiles.Length==0){UiLabel(new Rect(25,224,510,90),"No cars imported from this game yet.",labelStyle);return;}
        if(!profiles.Any(p=>p.id==spawnModel.Value)){spawnModel.Value=profiles[0].id;SelectFrontendProfile(profiles[0]);}
        const int pageSize=GaragePageSize;
        garagePage=Mathf.Clamp(garagePage,0,(profiles.Length-1)/pageSize);
        var page=profiles.Skip(garagePage*pageSize).Take(pageSize).ToArray();
        int selected=Array.FindIndex(page,p=>p.id==spawnModel.Value);
        int next=UiGrid(new Rect(25,215,510,106),selected,page.Select(p=>p.displayName).ToArray(),2);
        if(next>=0&&next!=selected){spawnModel.Value=page[next].id;SelectFrontendProfile(page[next]);}
        GUI.enabled=garagePage>0;if(MenuButton(new Rect(25,332,120,28),"‹ Previous"))SelectGaragePage(profiles,garagePage-1);GUI.enabled=true;
        UiLabel(new Rect(162,334,230,26),(garageSource==0?"NFSU2":"NFSMW")+" CARS   "+(garagePage+1)+" / "+((profiles.Length+pageSize-1)/pageSize),labelStyle);
        GUI.enabled=(garagePage+1)*pageSize<profiles.Length;if(MenuButton(new Rect(410,332,120,28),"Next ›"))SelectGaragePage(profiles,garagePage+1);GUI.enabled=true;
        var profile=SelectedProfile;
        UiLabel(new Rect(25,375,510,32),profile.displayName,titleStyle);
        UiLabel(new Rect(25,412,510,43),Mathf.RoundToInt(profile.rig.mass)+" kg · "+profile.sound.gearRatios.Length+" gears · "+profile.rig.rimNominal+" inch rims\n"+(profile.capabilities.drift?"Road / off-road / drift":"Road / off-road")+(profile.capabilities.flight?" / flight":""),mutedStyle);
        bool owned=fleet.Any(v=>v.Profile.id==profile.id&&(!NetworkSessionActive||OwnNetworkVehicle(v)));
        GUI.enabled=!SpawnBusy&&GarageCanPark&&(owned||WorldVehicleCount<VehicleFleet<VehicleRuntime>.Capacity);
        if(MenuButton(new Rect(25,465,510,35),owned?"SELECT PARKED CAR":OwnsGarageCar(profile.id)?"RECALL OWNED CAR":CoinEconomy?"BUY CAR · 1 COIN":"TAKE CAR")){TakeGarageCar(profile);return;}
        GUI.enabled=true;
        UiLabel(new Rect(25,503,510,20),string.IsNullOrEmpty(workshopMessage)?GarageEconomySummary:workshopMessage,mutedStyle);
    }
    void DrawGarageFleet() {
        var local=LocalPlayer();int row=0;
        foreach(var vehicle in fleet.Where(v=>!NetworkSessionActive||OwnNetworkVehicle(v)).ToArray()) {
            float y=190+row++*46;
            int distance=Mathf.RoundToInt(Vector3.Distance(local.transform.position,vehicle.car.transform.position));
            GUI.enabled=GarageCanPark;
            if(MenuButton(new Rect(25,y,NetworkSessionActive?270:365,36),vehicle.Profile.displayName+"   ·   "+distance+" m",vehicle==current?choiceSelectedStyle:null)){if(driving&&vehicle!=current)Exit();current=vehicle;spawnModel.Value=vehicle.Profile.id;SelectFrontendProfile(vehicle.Profile);SetVehicleMenu(true);}
            GUI.enabled=GarageCanPark&&!SpawnBusy&&!hordeWave.Active&&(!NetworkSessionActive||OwnNetworkVehicle(vehicle));
            if(NetworkSessionActive&&MenuButton(new Rect(300,y,92,36),"RECOVER")){RecoverNetworkVehicle(vehicle.networkData.m_uid,vehicle.Profile.id);return;}
            if(MenuButton(new Rect(400,y,130,36),"REMOVE")){RemoveGarageCar(vehicle);return;}
            GUI.enabled=true;
        }
        var distant=SavedWorldVehicles.Where(d=>!fleet.Any(v=>v.storage?.Data==d)).ToArray();
        foreach(var data in distant) {
            float y=190+row++*46;
            string id=VehiclePersistence.Profile(data);var profile=vehicleCatalog.Profiles.FirstOrDefault(p=>p.id==id);
            int distance=Mathf.RoundToInt(Vector3.Distance(local.transform.position,VehiclePersistence.Location(data)));
            string state=deferredVehicles.Contains(data.m_uid)?"unavailable":"saved";
            UiLabel(new Rect(25,y,365,36),(profile?.displayName??id)+" / "+state+" / "+distance+" m",labelStyle);
            GUI.enabled=!SpawnBusy&&GarageCanPark&&!hordeWave.Active;
            if(MenuButton(new Rect(400,y,130,36),"REMOVE")){RemoveDistantWorldVehicle(data);return;}
            GUI.enabled=true;
        }
        var networkSaved=NetworkSessionActive&&networkClient!=null?networkClient.OwnedVehicles.Where(d=>!fleet.Any(v=>v.networkData!=null&&v.networkData.m_uid.Equals(d.Id))).ToArray():Array.Empty<NetworkVehicleSummary>();
        foreach(var saved in networkSaved) {
            float y=190+row++*46;var profile=vehicleCatalog.Profiles.FirstOrDefault(p=>p.id==saved.Profile);
            int distance=Mathf.RoundToInt(Vector3.Distance(local.transform.position,saved.Position));
            UiLabel(new Rect(25,y,270,36),(profile?.displayName??saved.Profile)+" / saved / "+distance+" m",labelStyle);
            GUI.enabled=!SpawnBusy&&GarageCanPark&&saved.Driver==0;
            if(MenuButton(new Rect(300,y,92,36),"RECOVER")){RecoverNetworkVehicle(saved.Id,saved.Profile);return;}
            if(MenuButton(new Rect(400,y,130,36),"REMOVE")){RemoveDistantNetworkVehicle(saved);return;}GUI.enabled=true;
        }
        if(fleet.Count==0&&distant.Length==0&&networkSaved.Length==0&&!NetworkSessionActive) {
            var saved=SavedGarageIds().Where(id=>!SavedWorldProfile(id)).ToArray();
            foreach(var id in saved) {
                var profile=vehicleCatalog.Get(id);float y=190+row++*46;
                if(MenuButton(new Rect(25,y,365,36),profile.displayName+" · saved")){spawnModel.Value=id;SelectFrontendProfile(profile);}
                if(MenuButton(new Rect(400,y,130,36),"REMOVE")){garageRoster.Value=JsonConvert.SerializeObject(saved.Where(value=>value!=id));Config.Save();workshopMessage="Removed "+profile.displayName+" from saved garage.";return;}
            }
            if(saved.Length==0)UiLabel(new Rect(25,201,510,68),"Your garage is empty. Choose a car in the car lot.",labelStyle);
            GUI.enabled=!SpawnBusy&&saved.Length>0;
            if(MenuButton(new Rect(25,399,510,35),"RECALL SAVED GARAGE")){RecallGarage();return;}GUI.enabled=true;
        }
        if(NetworkSessionActive&&row==0)UiLabel(new Rect(25,201,510,68),"Your garage is empty. Choose a car in the car lot.",labelStyle);
        GUI.enabled=car!=null;
        if(fleet.Count>0) {
            if(MenuButton(new Rect(25,399,245,35),"Performance"))ChangeMenuTab(13);
            if(MenuButton(new Rect(285,399,245,35),"Styling"))ChangeMenuTab(14);
        }
        GUI.enabled=CanAccessCurrent(local)&&(!driving||DriverInputReady);
        if(MenuButton(new Rect(25,449,245,35),driving?"EXIT CAR":"DRIVE")){CloseVehicleMenu();if(driving)RequestExit();else Enter(local);return;}
        GUI.enabled=car!=null&&GarageCanPark&&!SpawnBusy&&!HordeVehicle;
        if(MenuButton(new Rect(285,449,245,35),NetworkSessionActive&&!OwnNetworkVehicle(current)?"REMOVE IF OWNER OFFLINE":"REMOVE FROM GARAGE")){RemoveGarageCar(current);return;}
        GUI.enabled=true;
        if(NetworkSessionActive)UiLabel(new Rect(25,491,510,30),"Network sandbox / four owned cars per player / server enforces limits",mutedStyle);
        else {GUI.enabled=!SpawnBusy&&!NetworkSessionActive;garageCoinEconomy.Value=UiToggle(new Rect(25,491,510,30),garageCoinEconomy.Value,"Prototype economy: 1 Coin per car / new upgrade level");GUI.enabled=true; }
    }
}
}
