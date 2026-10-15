using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
namespace NeedForValheim {
public sealed class VehicleGarageStation:MonoBehaviour,Hoverable,Interactable {
    public string GetHoverName()=>"NFS Garage";
    public float GetHoverOffset()=>1f;
    public string GetHoverText()=>Localization.instance.Localize("NFS Garage\n[<color=yellow><b>$KEY_Use</b></color>] Open garage");
    public bool Interact(Humanoid user,bool hold,bool alt)=>!hold&&Plugin.ControlsOwner!=null&&Plugin.ControlsOwner.OpenGarageStation(this,user);
    public bool UseItem(Humanoid user,ItemDrop.ItemData item)=>false;
}
public partial class Plugin {
    const string GarageStationPrefab="nfvu2_garage_station";
    GameObject garagePrefabRoot,garageStationPrefab;
    VehicleGarageStation garageStationContext;
    VehicleGarageStation garageSpawnStation;
    bool garageSpawnAnchorCaptured;
    Vector3 garageSpawnOrigin;
    Quaternion garageSpawnYaw;
    Material garageStationMaterial;
    void InitializeGarageStation(){controlsHarmony.PatchAll(typeof(GarageStationSceneHook));controlsHarmony.PatchAll(typeof(GarageStationDatabaseHook));}
    void RegisterGarageStation(ZNetScene scene) {
        if(garageStationPrefab==null) {
            var source=scene.m_prefabs.FirstOrDefault(p=>p!=null&&p.name=="piece_workbench");if(source==null){Logger.LogWarning("Garage station: native workbench prefab unavailable");return;}
            garagePrefabRoot=new GameObject("NFVU2 garage prefab storage");garagePrefabRoot.SetActive(false);DontDestroyOnLoad(garagePrefabRoot);
            garageStationPrefab=Instantiate(source,garagePrefabRoot.transform,false);garageStationPrefab.name=GarageStationPrefab;
            foreach(var station in garageStationPrefab.GetComponentsInChildren<CraftingStation>(true)) {
                if(station.m_areaMarker!=null)station.m_areaMarker.SetActive(false);if(station.m_inUseObject!=null)station.m_inUseObject.SetActive(false);
                DestroyImmediate(station);
            }
            var piece=garageStationPrefab.GetComponent<Piece>();piece.m_name="NFS Garage";piece.m_description="Call and customize your cars. Prototype prices: 1 Coin per car and per new performance level.";
            piece.m_category=Piece.PieceCategory.Crafting;piece.m_craftingStation=null;piece.m_enabled=true;
            var wood=ObjectDB.instance?.GetItemPrefab("Wood")?.GetComponent<ItemDrop>();
            if(wood!=null)piece.m_resources=new[]{new Piece.Requirement{m_resItem=wood,m_amount=10,m_recover=true}};
            garageStationPrefab.AddComponent<VehicleGarageStation>();
            var sourceMaterial=garageStationPrefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m!=null);
            if(sourceMaterial!=null)garageStationMaterial=new Material(sourceMaterial){name="NFVU2 garage timber"};
            // A real buildable service bay; inherited Piece/WearNTear/ZNetView
            // provide placement, hammer removal/refund and normal world saving.
            void Beam(string name,Vector3 position,Vector3 size) {
                var node=GameObject.CreatePrimitive(PrimitiveType.Cube);node.name=name;node.transform.SetParent(garageStationPrefab.transform,false);node.transform.localPosition=position;node.transform.localScale=size;
                node.layer=garageStationPrefab.layer;if(garageStationMaterial!=null)node.GetComponent<Renderer>().sharedMaterial=garageStationMaterial;
            }
            Beam("Service bay",new Vector3(0,.02f,4.8f),new Vector3(5.8f,.08f,8));
            foreach(float x in new[]{-2.8f,2.8f})foreach(float z in new[]{1.3f,8.3f})Beam("Garage post",new Vector3(x,2.25f,z),new Vector3(.2f,4.5f,.2f));
            Beam("Garage roof",new Vector3(0,4.6f,4.8f),new Vector3(6.2f,.2f,8.4f));
            Beam("Front lintel",new Vector3(0,4.25f,1.3f),new Vector3(5.8f,.3f,.2f));
            garageStationPrefab.SetActive(true);
            Logger.LogInfo("Garage station prefab registered: hammer / Crafting / NFS Garage, 10 Wood");
        }
        if(!scene.m_prefabs.Contains(garageStationPrefab))scene.m_prefabs.Add(garageStationPrefab);
    }
    void RegisterGarageHammerPiece() {
        if(garageStationPrefab==null||ObjectDB.instance==null)return;
        var hammer=ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();var table=hammer?.m_itemData.m_shared.m_buildPieces;
        if(table!=null&&!table.m_pieces.Contains(garageStationPrefab))table.m_pieces.Add(garageStationPrefab);
    }
    internal bool OpenGarageStation(VehicleGarageStation station,Humanoid user) {
        if(station==null||user!=LocalPlayer()||Multiplayer()||!CanUseVehicleMenu(user)||SpawnBusy)return false;
        garageStationContext=station;InventoryGui.instance?.Hide();garageView=0;menuTab=10;SetVehicleMenu(true);
        workshopMessage=GarageEconomySummary;Logger.LogInfo("Garage station opened");return true;
    }
    bool TryGarageStationSpawnAnchor(MonoBehaviour local,out Vector3 origin,out Quaternion yaw) {
        origin=Vector3.zero;yaw=Quaternion.identity;
        if(garageSpawnAnchorCaptured){origin=garageSpawnOrigin;yaw=garageSpawnYaw;return true;}
        if(local==null||garageStationContext==null||Vector3.Distance(local.transform.position,garageStationContext.transform.position)>12)return false;
        origin=garageStationContext.transform.TransformPoint(new Vector3(0,.15f,4.8f));yaw=Quaternion.Euler(0,garageStationContext.transform.eulerAngles.y,0);return true;
    }
    void CaptureGarageStationSpawn(MonoBehaviour local) {
        garageSpawnAnchorCaptured=false;garageSpawnStation=null;
        if(TryGarageStationSpawnAnchor(local,out garageSpawnOrigin,out garageSpawnYaw)){garageSpawnStation=garageStationContext;garageSpawnAnchorCaptured=true;}
    }
    void FinishGarageStationSpawn(bool reopen) {
        if(reopen&&garageSpawnStation!=null)garageStationContext=garageSpawnStation;
        garageSpawnStation=null;garageSpawnAnchorCaptured=false;
    }
    void DisposeGarageStation() {
        garageStationContext=null;
        FinishGarageStationSpawn(false);
        if(garageStationPrefab!=null&&ObjectDB.instance!=null){var table=ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces;table?.m_pieces.Remove(garageStationPrefab);}
        if(garagePrefabRoot!=null)Destroy(garagePrefabRoot);if(garageStationMaterial!=null)Destroy(garageStationMaterial);
        garagePrefabRoot=garageStationPrefab=null;garageStationMaterial=null;
    }
}
[HarmonyPatch(typeof(ZNetScene),"Awake")]
static class GarageStationSceneHook {
    static void Prefix(ZNetScene __instance){Plugin.ControlsOwner?.RegisterGarageScene(__instance);}
    static void Postfix(){Plugin.ControlsOwner?.RegisterGarageDatabase();}
}
[HarmonyPatch(typeof(ObjectDB),"Awake")]
static class GarageStationDatabaseHook {static void Postfix(){Plugin.ControlsOwner?.RegisterGarageDatabase();}}
public partial class Plugin {
    internal void RegisterGarageScene(ZNetScene scene){try{RegisterGarageStation(scene);}catch(Exception error){Logger.LogError("Garage station registration failed: "+error);}}
    internal void RegisterGarageDatabase(){try{RegisterGarageHammerPiece();}catch(Exception error){Logger.LogWarning("Garage hammer registration failed: "+error.Message);}}
}
}
