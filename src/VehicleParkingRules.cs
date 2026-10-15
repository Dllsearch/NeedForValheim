namespace NeedForValheim {
public static class VehicleParkingRules {
    public static bool Freeze(bool occupied,bool flying,float horizontalDistance,float drop,float unsupportedSeconds)=>
        Freeze(occupied,flying,horizontalDistance,drop,unsupportedSeconds,true);
    public static bool Freeze(bool occupied,bool flying,float horizontalDistance,float drop,float unsupportedSeconds,bool terrainLoaded)=>
        !occupied&&!flying&&(!terrainLoaded||horizontalDistance>80||drop>1||unsupportedSeconds>.75f);
    public static bool Thaw(bool occupied,float horizontalDistance,bool terrainPresent)=>
        terrainPresent&&(occupied||horizontalDistance<60);
    public static bool Thaw(bool occupied,float horizontalDistance,bool terrainPresent,float readySeconds)=>
        readySeconds>=.3f&&Thaw(occupied,horizontalDistance,terrainPresent);
}
}
