using System;
using System.Collections.Generic;
namespace NeedForValheim {
public sealed class GarageCarProgress {public bool owned;public int[] stages=new int[6];}
public sealed class GarageAccount {public Dictionary<string,GarageCarProgress> cars=new Dictionary<string,GarageCarProgress>();}
public sealed class GarageProgression {
    public int version=1;
    public Dictionary<string,GarageAccount> players=new Dictionary<string,GarageAccount>();
    public static int UpgradeCost(int[] paid,int[] wanted) {
        ValidateStages(paid);ValidateStages(wanted);int cost=0;
        for(int i=0;i<6;i++)cost+=Math.Max(0,wanted[i]-paid[i]);return cost;
    }
    public static int[] Unlock(int[] paid,int[] wanted) {
        UpgradeCost(paid,wanted);var result=new int[6];for(int i=0;i<6;i++)result[i]=Math.Max(paid[i],wanted[i]);return result;
    }
    public static void CommitPurchase(GarageCarProgress car,bool owned,int[] stages,Action save,Action refund) {
        ValidateStages(stages);bool oldOwned=car.owned;var oldStages=car.stages;
        try {car.owned=owned;car.stages=stages;save();}
        catch {car.owned=oldOwned;car.stages=oldStages;refund();throw;}
    }
    public static void ValidateStages(int[] stages){if(stages==null||stages.Length!=6||Array.Exists(stages,v=>v<0||v>3))throw new ArgumentException("Invalid garage upgrade levels");}
    public void Validate() {
        if(version!=1||players==null||players.Count>128)throw new ArgumentException("Invalid garage account data");
        foreach(var player in players) {
            if(!long.TryParse(player.Key,out var id)||id==0||player.Value?.cars==null||player.Value.cars.Count>128)throw new ArgumentException("Invalid garage character");
            foreach(var car in player.Value.cars){if(string.IsNullOrEmpty(car.Key)||car.Key.Length>64||car.Value==null)throw new ArgumentException("Invalid garage car");ValidateStages(car.Value.stages);}
        }
    }
}
}
