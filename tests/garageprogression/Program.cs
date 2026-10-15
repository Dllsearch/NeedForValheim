using System;
using System.Linq;
using NeedForValheim;
class Program {
    static int checks;
    static void Check(bool condition,string text){checks++;if(!condition)throw new Exception(text);}
    static void Reject(Action action){bool failed=false;try{action();}catch(ArgumentException){failed=true;}Check(failed,"invalid progression rejected");}
    static void Main() {
        var paid=new[]{1,0,3,2,0,1};
        Check(GarageProgression.UpgradeCost(paid,new[]{1,0,3,2,0,1})==0,"reapply paid levels free");
        Check(GarageProgression.UpgradeCost(paid,new int[6])==0,"downgrade free");
        Check(GarageProgression.UpgradeCost(paid,new[]{2,1,3,2,0,1})==2,"one coin for each newly unlocked level");
        var bought=GarageProgression.Unlock(paid,new[]{3,0,1,3,0,1});
        Check(bought.SequenceEqual(new[]{3,0,3,3,0,1}),"downgrading does not erase purchased rights");
        Check(paid.SequenceEqual(new[]{1,0,3,2,0,1}),"quote/commit do not mutate previous ledger");
        var full=Enumerable.Repeat(3,6).ToArray();var empty=new int[6];var middle=new[]{1,2,0,1,0,2};
        Check(GarageProgression.UpgradeCost(empty,full)==18,"complete six-system package has eighteen new levels");
        Check(GarageProgression.UpgradeCost(empty,middle)+GarageProgression.UpgradeCost(GarageProgression.Unlock(empty,middle),full)==18,"incremental purchases and one package cost the same");
        Check(GarageProgression.UpgradeCost(GarageProgression.Unlock(empty,full),middle)==0,"downgrading after a full package is free");
        Check(GarageProgression.UpgradeCost(GarageProgression.Unlock(paid,new int[6]),paid)==0,"stock reset retains paid tuning");
        Reject(()=>GarageProgression.UpgradeCost(null,new int[6]));Reject(()=>GarageProgression.Unlock(new int[5],new int[6]));Reject(()=>GarageProgression.UpgradeCost(new int[6],new[]{0,0,0,0,0,4}));
        var state=new GarageProgression();state.Validate();state.players["123"]=new GarageAccount();state.players["123"].cars["mw-bmwm3"]=new GarageCarProgress{owned=true,stages=bought};state.Validate();
        state.players["456"]=new GarageAccount();Check(state.players["456"].cars.Count==0,"characters have separate ownership");
        state.players["123"].cars["mw-bmwm3"].stages[2]=-1;Reject(state.Validate);
        var car=new GarageCarProgress();int wallet=5;wallet--;
        bool saveFailed=false;
        try {GarageProgression.CommitPurchase(car,true,new int[6],()=>{throw new InvalidOperationException("disk unavailable");},()=>wallet++);}catch(InvalidOperationException){saveFailed=true;}
        Check(saveFailed&&!car.owned&&wallet==5,"failed ownership save refunds the coin and rolls back ownership");
        car.stages=new[]{1,0,0,0,0,0};wallet-=2;
        try {GarageProgression.CommitPurchase(car,false,new[]{3,0,0,0,0,0},()=>{throw new InvalidOperationException();},()=>wallet+=2);}catch(InvalidOperationException){}
        Check(car.stages[0]==1&&wallet==5,"failed upgrade save refunds the whole basket and retains old rights");
        wallet--;GarageProgression.CommitPurchase(car,true,new[]{2,0,0,0,0,0},()=>{},()=>wallet++);
        Check(car.owned&&car.stages[0]==2&&wallet==4,"successful save commits once without refund");
        Console.WriteLine(checks+" garage progression checks passed; native inventory and station placement require game acceptance.");
    }
}
