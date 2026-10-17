using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using Newtonsoft.Json;
using UnityEngine;
namespace NeedForValheim {
public partial class Plugin {
    ConfigEntry<bool> garageCoinEconomy,garageEconomyMigrated;
    ConfigEntry<string> garageAccounts;
    GarageProgression garageProgression;
    void InitializeGarageEconomy() {
        garageCoinEconomy=Config.Bind("Garage","TestCoinEconomy",true,"Prototype economy: buy each model once for 1 Coin; each new performance level costs 1 Coin. Purchased levels and recall are free. Disable for the existing sandbox.");
        garageAccounts=Config.Bind("Garage","CharacterAccounts","{}","Character-specific vehicle ownership and purchased performance levels.");
        garageEconomyMigrated=Config.Bind("Garage","LegacyEconomyMigrated",false,"Preserves the first existing character's saved garage and current tuning when enabling the prototype economy.");
        try {if(garageAccounts.Value.Length>1048576)throw new ArgumentException("Garage accounts too large");garageProgression=JsonConvert.DeserializeObject<GarageProgression>(garageAccounts.Value)??new GarageProgression();garageProgression.Validate();}
        catch(Exception error){garageProgression=new GarageProgression();Logger.LogWarning("Garage accounts unavailable: "+error.Message);}
    }
    bool CoinEconomy=>!NetworkSessionActive&&garageCoinEconomy!=null&&garageCoinEconomy.Value;
    ConfigEntry<int>[] PerformanceEntries()=>new[]{engineStage,turboStage,gearStage,tyreStage,brakeStage,nosStage};
    ConfigEntry<int>[] PerformanceEntries(CarProfile profile)=>new[]{baseConfig_engineStage,baseConfig_turboStage,baseConfig_gearStage,baseConfig_tyreStage,baseConfig_brakeStage,baseConfig_nosStage}
        .Select(entry=>(ConfigEntry<int>)vehicleSettings[entry][profile.ConfigPrefix]).ToArray();
    GarageAccount CharacterGarage(Player player) {
        if(player==null)return null;string key=player.GetPlayerID().ToString(CultureInfo.InvariantCulture);if(key=="0")return null;
        if(garageProgression.players.TryGetValue(key,out var account))return account;
        account=new GarageAccount();account.cars[CarCatalog.StockId]=new GarageCarProgress{owned=true};
        if(!garageEconomyMigrated.Value) {
            var legacy=new HashSet<string>(SavedGarageIds());foreach(var vehicle in fleet)legacy.Add(vehicle.Profile.id);legacy.Add(CarCatalog.StockId);
            foreach(var profile in vehicleCatalog.Profiles)account.cars[profile.id]=new GarageCarProgress{owned=legacy.Contains(profile.id),stages=PerformanceEntries(profile).Select(e=>PerformanceTuning.Stage(e.Value)).ToArray()};
            garageEconomyMigrated.Value=true;
        }
        garageProgression.players[key]=account;SaveGarageAccounts();return account;
    }
    GarageCarProgress CarProgress(Player player,string id) {
        var account=CharacterGarage(player);if(account==null)return null;
        if(!account.cars.TryGetValue(id,out var progress)){progress=new GarageCarProgress();account.cars[id]=progress;}return progress;
    }
    void SaveGarageAccounts(){garageAccounts.Value=JsonConvert.SerializeObject(garageProgression);Config.Save();}
    ItemDrop CoinItem=>ObjectDB.instance?.GetItemPrefab("Coins")?.GetComponent<ItemDrop>();
    int CoinCount(Player player){var coin=CoinItem;return player!=null&&coin!=null?player.GetInventory().CountItems(coin.m_itemData.m_shared.m_name):0;}
    bool SpendGarageCoins(Player player,int amount) {
        if(amount<=0)return true;var coin=CoinItem;
        if(player==null||coin==null||CoinCount(player)<amount){workshopMessage="Requires "+amount+" Coin"+(amount==1?"":"s")+". Inventory: "+CoinCount(player)+".";return false;}
        int before=CoinCount(player);player.GetInventory().RemoveItem(coin.m_itemData.m_shared.m_name,amount);
        if(CoinCount(player)!=before-amount)throw new InvalidOperationException("Garage coin transaction did not complete");return true;
    }
    bool OwnsGarageCar(string id)=>!CoinEconomy||CarProgress(LocalPlayer() as Player,id)?.owned==true;
    void RefundGarageCoins(Player player,int amount) {
        if(amount<=0)return;
        garageAccounts.Value=JsonConvert.SerializeObject(garageProgression);
        var coin=CoinItem;
        if(coin==null)throw new InvalidOperationException("Coin prefab unavailable during garage refund");
        if(player!=null&&player.GetInventory().AddItem(coin.gameObject,amount))return;
        ItemDrop.DropItem(coin.m_itemData.Clone(),amount,player!=null?player.transform.position+Vector3.up:Vector3.up,Quaternion.identity);
    }
    bool CheckGaragePurchase(MonoBehaviour local,CarProfile profile) {
        if(!CoinEconomy||CarProgress(local as Player,profile.id)?.owned==true)return true;
        if(CoinCount(local as Player)>=1)return true;
        workshopMessage="Buy "+profile.displayName+": 1 Coin required.";ShowVehicleWarning(workshopMessage);return false;
    }
    void CompleteGaragePurchase(MonoBehaviour local,CarProfile profile) {
        if(!CoinEconomy)return;var progress=CarProgress(local as Player,profile.id);
        if(progress==null)throw new InvalidOperationException("Player garage account unavailable");if(progress.owned)return;
        if(!SpendGarageCoins(local as Player,1))throw new GaragePurchaseException(workshopMessage);
        bool autoSave=Config.SaveOnConfigSet;Config.SaveOnConfigSet=false;
        try {GarageProgression.CommitPurchase(progress,true,progress.stages,SaveGarageAccounts,()=>RefundGarageCoins(local as Player,1));}
        catch(Exception error){Logger.LogWarning("Garage purchase rolled back: "+error);throw new GaragePurchaseException("Purchase could not be saved; 1 Coin refunded.");}
        finally {Config.SaveOnConfigSet=autoSave;}
        workshopMessage="Purchased "+profile.displayName+" · 1 Coin. Recall is free.";
    }
    sealed class GaragePurchaseException:Exception {public GaragePurchaseException(string text):base(text){}}
    bool PurchasePerformance(int[] wanted,Action apply=null) {
        GarageProgression.ValidateStages(wanted);
        var entries=PerformanceEntries();var previous=entries.Select(e=>e.Value).ToArray();bool autoSave=Config.SaveOnConfigSet;Config.SaveOnConfigSet=false;
        try {
        if(!CoinEconomy){apply?.Invoke();Config.Save();return true;}
        var progress=CarProgress(LocalPlayer() as Player,current.Profile.id);if(progress==null)return false;
        if(!progress.owned){workshopMessage="Buy this car before upgrading it.";return false;}
        int cost=GarageProgression.UpgradeCost(progress.stages,wanted);
        if(CoinCount(LocalPlayer() as Player)<cost){workshopMessage="Requires "+cost+" Coins. Inventory: "+CoinCount(LocalPlayer() as Player)+".";return false;}
        apply?.Invoke();if(!SpendGarageCoins(LocalPlayer() as Player,cost))throw new InvalidOperationException(workshopMessage);
        GarageProgression.CommitPurchase(progress,progress.owned,GarageProgression.Unlock(progress.stages,wanted),SaveGarageAccounts,()=>RefundGarageCoins(LocalPlayer() as Player,cost));
        if(cost>0)workshopMessage="Purchased "+cost+" upgrade level"+(cost==1?"":"s")+" · "+cost+" Coin"+(cost==1?"":"s")+".";return true;
        }catch(Exception error){for(int i=0;i<6;i++)entries[i].Value=previous[i];workshopMessage="Upgrade could not be saved; changes rolled back.";Logger.LogWarning(workshopMessage+" "+error);return false;}
        finally {Config.SaveOnConfigSet=autoSave;}
    }
    void SetPaidPerformance(ConfigEntry<int> entry,int stage) {
        var entries=PerformanceEntries();var wanted=entries.Select(e=>PerformanceTuning.Stage(e.Value)).ToArray();int index=Array.IndexOf(entries,entry);if(index<0)return;
        wanted[index]=PerformanceTuning.Stage(stage);PurchasePerformance(wanted,()=>entry.Value=wanted[index]);
    }
    void ClampPaidPerformance() {
        if(!CoinEconomy)return;var progress=CarProgress(LocalPlayer() as Player,current.Profile.id);if(progress==null)return;
        var entries=PerformanceEntries();for(int i=0;i<6;i++)if(entries[i].Value>progress.stages[i])entries[i].Value=progress.stages[i];
    }
    string GarageEconomySummary=>NetworkSessionActive?"Network sandbox / up to four owned cars per player":CoinEconomy?"Car: 1 Coin · each new upgrade level: 1 Coin · recall is free · wallet: "+CoinCount(LocalPlayer() as Player):"Sandbox · all cars unlocked · four parked cars";
}
}
