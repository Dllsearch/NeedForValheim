using System;
namespace NeedForValheim {
public static class VehicleStorageRules {
    public static int Layout(int layout)=>Math.Max(0,Math.Min(3,layout));
    // A showroom trades cargo rows for audio; cars without one keep the full floor.
    public static int Width(CarProfile profile)=>profile!=null&&profile.capabilities.showroom?6:8;
    public static int Rows(CarProfile profile,int layout)=>profile!=null&&profile.capabilities.showroom?4-Layout(layout):4;
    public static int Capacity(CarProfile profile,int layout)=>Width(profile)*Rows(profile,layout);
    public static bool CanResize(CarProfile profile,int layout,int stacks,bool inUse)=>!inUse&&stacks>=0&&stacks<=Capacity(profile,layout);
    public static int Width(bool suv)=>Width(suv?CarCatalog.BuiltInH2():CarCatalog.Nissan350Z());
    public static int Rows(bool suv,int layout)=>Rows(suv?CarCatalog.BuiltInH2():CarCatalog.Nissan350Z(),layout);
    public static int Capacity(bool suv,int layout)=>Capacity(suv?CarCatalog.BuiltInH2():CarCatalog.Nissan350Z(),layout);
    public static bool CanResize(bool suv,int layout,int stacks,bool inUse)=>CanResize(suv?CarCatalog.BuiltInH2():CarCatalog.Nissan350Z(),layout,stacks,inUse);
}
}
