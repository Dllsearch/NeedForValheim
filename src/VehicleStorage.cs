using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
namespace NeedForValheim {
// A real vanilla chest owns the items and saves them to the world, independently
// of the temporary car. Never parent it to the disposable vehicle object.
internal sealed class VehicleStorage {
    static readonly System.Reflection.MethodInfo Load=AccessTools.Method(typeof(Container),"Load");
    static readonly System.Reflection.FieldInfo InventoryName=AccessTools.Field(typeof(Inventory),"m_name");
    readonly bool suv;
    readonly float approachZ;
    readonly GameObject vehicle,prefab;
    readonly ZDO data;
    readonly ZDOMan world=ZDOMan.instance;
    readonly ZNetScene scene=ZNetScene.instance;
    bool SameWorld=>world!=null&&scene!=null&&world==ZDOMan.instance&&scene==ZNetScene.instance;
    Container container;
    ZNetView view;
    VehicleCargoVisibility visibility;
    bool released;
    public ZDO Data=>data;
    public int Layout {get;private set;}
    public int Capacity=>VehicleStorageRules.Capacity(suv,Layout);
    public int Count=>container!=null?container.GetInventory().NrOfItems():-1;
    public bool InUse=>container!=null&&container.IsInUse();
    public VehicleStorage(GameObject vehicle,CarProfile profile,ZDO saved) {
        this.vehicle=vehicle;data=saved;suv=!profile.capabilities.showroom;approachZ=profile.rig.trunkApproachZ;
        Layout=VehiclePersistence.Trunk(saved);
        prefab=scene.GetPrefab(suv?"piece_chest_blackmetal":"piece_chest");
        if(prefab==null)throw new InvalidOperationException("Native cargo chest unavailable");
        if(!data.IsOwner())data.SetOwner(ZDOMan.GetSessionID());
        if(!Resolve())throw new InvalidOperationException("Saved vehicle anchor is not loaded");
    }
    public VehicleStorage(GameObject vehicle,CarProfile profile,int layout):this(vehicle,!profile.capabilities.showroom,layout,profile.rig.trunkApproachZ){}
    public VehicleStorage(GameObject vehicle,bool suv,int layout):this(vehicle,suv,layout,suv?1.8f:1.3f){}
    public VehicleStorage(GameObject vehicle,bool suv,int layout,float approachZ) {
        this.vehicle=vehicle;this.suv=suv;this.approachZ=approachZ;Layout=VehicleStorageRules.Layout(layout);
        prefab=ZNetScene.instance.GetPrefab(suv?"piece_chest_blackmetal":"piece_chest");
        if(prefab==null||prefab.GetComponent<Container>()==null)throw new InvalidOperationException("Native cargo chest unavailable");
        var original=prefab.GetComponent<Container>();
        if(original.m_width!=VehicleStorageRules.Width(suv)||original.m_height!=4)throw new InvalidOperationException("Unexpected native chest dimensions");
        var root=UnityEngine.Object.Instantiate(prefab,vehicle.transform.position,Quaternion.identity);
        view=root.GetComponent<ZNetView>();data=view.GetZDO();
        if(data==null){UnityEngine.Object.Destroy(root);throw new InvalidOperationException("Cargo needs an active world");}
        data.Persistent=true;
        Attach(root);
        Tick();
    }
    void Attach(GameObject root) {
        view=root.GetComponent<ZNetView>();container=root.GetComponent<Container>();
        // Container normally loads on its next CheckForChanges tick. Load first,
        // before changing dimensions, especially after a parked zone reload.
        Load.Invoke(container,null);
        visibility=Hide(root);
        // If another native interaction filled a reloaded chest before this
        // frame, preserve that cargo and fall back to the stock trunk capacity.
        if(!Resize(Layout)&&!suv){Layout=0;Resize(Layout);}
    }
    public static VehicleCargoVisibility Hide(GameObject root) {
        var visibility=root.GetComponent<VehicleCargoVisibility>()??root.AddComponent<VehicleCargoVisibility>();
        visibility.Hide();return visibility;
    }
    bool Resolve() {
        if(released||!SameWorld)return false;
        if(container!=null&&view!=null&&view.IsValid())return true;
        container=null;view=null;
        var root=ZNetScene.instance.FindInstance(data.m_uid);
        if(root==null)return false;
        Attach(root);return true;
    }
    public bool CanResize(int layout) {
        if(layout==Layout)return true;
        return Resolve()&&VehicleStorageRules.CanResize(suv,layout,Count,InUse);
    }
    public bool Resize(int layout) {
        layout=VehicleStorageRules.Layout(layout);
        if(container==null||!VehicleStorageRules.CanResize(suv,layout,Count,InUse))return false;
        var inventory=container.GetInventory();
        int rows=VehicleStorageRules.Rows(suv,layout);
        if(inventory.GetHeight()!=rows) {
            // Repack existing instances, retaining stacks, durability and custom data.
            var items=inventory.GetAllItems();
            for(int i=0;i<items.Count;i++)items[i].m_gridPos=new Vector2i(i%inventory.GetWidth(),i/inventory.GetWidth());
            container.m_height=rows;inventory.SetHeight(rows);inventory.m_onChanged?.Invoke();
        }
        Layout=layout;return true;
    }
    public bool Open(string name) {
        if(!Resolve()||!view.IsOwner()||InventoryGui.instance==null)return false;
        InventoryName.SetValue(container.GetInventory(),name);
        container.SetInUse(true);InventoryGui.instance.Show(container);return true;
    }
    public void Tick() {
        if(vehicle==null||released||!SameWorld)return;
        bool loaded=Resolve();
        var position=vehicle.transform.TransformPoint(new Vector3(0,.5f,-approachZ));
        var rotation=Quaternion.Euler(0,vehicle.transform.eulerAngles.y,0);
        if(loaded)container.transform.SetPositionAndRotation(position,rotation);
        // Save a grounded recovery position even when the vehicle is flying.
        var saved=position;
        if(ZoneSystem.instance!=null&&ZoneSystem.instance.GetGroundHeight(position,out var ground))saved.y=ground+.15f;
        // Cargo can unload while the local car remains active. Its persistent
        // record must continue following the vehicle into the next sector.
        if(data.IsOwner()){data.SetPosition(saved);data.SetRotation(rotation);}
    }
    public static void RemoveSaved(ZDO data) {
        if(data==null||!data.IsOwner()||ZNetScene.instance==null)return;
        var root=ZNetScene.instance.FindInstance(data.m_uid);
        var container=root!=null?root.GetComponent<Container>():null;
        var view=root!=null?root.GetComponent<ZNetView>():null;
        if(container!=null) {
            if(view==null||!view.IsValid()||!view.IsOwner()||container.IsInUse())throw new InvalidOperationException("Saved cargo is busy");
            Load.Invoke(container,null);
        }
        // Unloaded inventory remains in the same vanilla ZDO. Never delete a
        // distant record based on an unavailable local inventory/count.
        VehiclePersistence.Forget(data);
        if(container==null)return;
        if(container.GetInventory().NrOfItems()==0){view.Destroy();return;}
        container.m_height=4;container.GetInventory().SetHeight(4);
        InventoryName.SetValue(container.GetInventory(),container.m_name);
        container.transform.SetPositionAndRotation(data.GetPosition(),data.GetRotation());
        root.GetComponent<VehicleCargoVisibility>()?.Show();
    }
    public void Release(bool preserveVehicle=false) {
        if(released)return;
        Tick();
        if(SameWorld&&InUse&&InventoryGui.instance!=null&&Player.m_localPlayer!=null)InventoryGui.instance.Hide();
        if(preserveVehicle&&VehiclePersistence.IsVehicle(data)){released=true;return;}
        if(SameWorld&&data.IsOwner())VehiclePersistence.Forget(data);
        if(SameWorld&&container!=null&&view!=null&&view.IsValid()) {
            if(InUse&&InventoryGui.instance!=null&&Player.m_localPlayer!=null)InventoryGui.instance.Hide();
            if(Count==0&&view.IsOwner())view.Destroy();
            else {
                // Restore the original vanilla chest in place; its existing ZDO
                // and inventory stay intact. No cloning or cross-save transfer.
                container.m_height=4;container.GetInventory().SetHeight(4);
                InventoryName.SetValue(container.GetInventory(),prefab.GetComponent<Container>().m_name);
                container.transform.SetPositionAndRotation(data.GetPosition(),data.GetRotation());
                visibility?.Show();
            }
        }
        // An unloaded chest already has a persistent vanilla ZDO; leave it alone.
        released=true;
    }
}
internal sealed class VehicleCargoVisibility : MonoBehaviour {
    Renderer[] renderers;Collider[] colliders;bool[] rendering,collision;
    WearNTear wear;bool roofWear,supportWear;
    public void Hide() {
        if(renderers!=null)return;
        renderers=gameObject.GetComponentsInChildren<Renderer>(true);rendering=renderers.Select(r=>r.enabled).ToArray();
        colliders=gameObject.GetComponentsInChildren<Collider>(true);collision=colliders.Select(c=>c.enabled).ToArray();
        foreach(var renderer in renderers)renderer.enabled=false;
        foreach(var collider in colliders)collider.enabled=false;
        wear=GetComponent<WearNTear>();
        if(wear!=null){roofWear=wear.m_noRoofWear;supportWear=wear.m_noSupportWear;wear.m_noRoofWear=wear.m_noSupportWear=false;}
    }
    void Update(){if(renderers!=null&&!VehiclePersistence.IsVehicle(GetComponent<ZNetView>()?.GetZDO()))Show();}
    public void Show() {
        if(renderers==null)return;
        for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].enabled=rendering[i];
        for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=collision[i];
        if(wear!=null){wear.m_noRoofWear=roofWear;wear.m_noSupportWear=supportWear;}
        renderers=null;colliders=null;
    }
}

}
