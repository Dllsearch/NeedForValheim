using System;
using System.Collections.Generic;
namespace NeedForValheim {
public enum HordePact {None,Odin,Thor,Iron,Frost,Storm,Valkyrie}
public static class HordePacts {
    public const float BaseEnemyDamage=1.2f;
    public static float Reload(HordePact pact)=>pact==HordePact.Odin?.6f:pact==HordePact.Frost?1.2f:1;
    public static float Speed(HordePact pact)=>pact==HordePact.Odin?1.2f:1;
    public static float Health(HordePact pact)=>pact==HordePact.Thor?1.35f:1;
    public static float EnemyDamage(HordePact pact)=>BaseEnemyDamage*(pact==HordePact.Valkyrie?1.25f:1);
    public static float Impact(HordePact pact)=>pact==HordePact.Iron?2:1;
    public static float Turret(HordePact pact)=>pact==HordePact.Iron?.75f:pact==HordePact.Storm?.8f:1;
    public static bool Elemental(HordePact pact)=>pact==HordePact.Frost||pact==HordePact.Storm;
    public static string Name(HordePact pact)=>pact==HordePact.Odin?"Odin's Haste":pact==HordePact.Thor?"Thor's Wrath":pact==HordePact.Iron?"Iron Chariot":pact==HordePact.Frost?"Skadi's Winter":pact==HordePact.Storm?"Storm Chain":pact==HordePact.Valkyrie?"Valkyrie's Watch":"No pact";
    public static string Benefit(HordePact pact)=>pact==HordePact.Odin?"40% shorter reload time":pact==HordePact.Thor?"Explosive ammunition for the entire wave":pact==HordePact.Iron?"Double run-over damage":pact==HordePact.Frost?"Ice ammunition: briefly freezes enemies":pact==HordePact.Storm?"Lightning jumps to up to 3 nearby enemies":pact==HordePact.Valkyrie?"Automatic turret for the entire wave":"No changes";
    public static string Cost(HordePact pact)=>pact==HordePact.Odin?"Enemies move 20% faster":pact==HordePact.Thor?"Enemies have 35% more health":pact==HordePact.Iron?"25% less turret damage":pact==HordePact.Frost?"20% longer reload; bosses are only slowed":pact==HordePact.Storm?"20% less direct bullet damage":pact==HordePact.Valkyrie?"Enemies deal 25% more damage":"No additional drawback";
    public static HordePact[] Draw(Random random,IReadOnlyList<HordePact> previous,int wave) {
        var pool=wave>=5?new[]{HordePact.Odin,HordePact.Thor,HordePact.Iron,HordePact.Frost,HordePact.Storm}
            :new[]{HordePact.Odin,HordePact.Thor,HordePact.Iron,HordePact.Frost,HordePact.Storm,HordePact.Valkyrie};
        for(int i=pool.Length-1;i>0;i--){int j=random.Next(i+1);var swap=pool[i];pool[i]=pool[j];pool[j]=swap;}
        bool WasOffered(HordePact pact){if(previous!=null)foreach(var old in previous)if(old==pact)return true;return false;}
        int repeated=0;for(int i=0;i<3;i++)if(WasOffered(pool[i]))repeated++;
        for(int i=0;i<3&&repeated>1;i++)if(WasOffered(pool[i]))for(int j=3;j<pool.Length;j++)if(!WasOffered(pool[j])){var swap=pool[i];pool[i]=pool[j];pool[j]=swap;repeated--;break;}
        return new[]{pool[0],pool[1],pool[2]};
    }
}
}
