using UnityEngine;
using System.Linq;
namespace NeedForValheim {
public partial class Plugin {
#if NFVU2_EXPERIMENTAL_NETWORK
    static readonly bool ExperimentalNetwork = true;
#else
    static readonly bool ExperimentalNetwork = false;
#endif
    VehicleNetworkClient networkClient;
    VehicleNetworkServer hostNetwork;
    VehicleServerCollisions hostCollisions;
    bool networkWasReady;
    void TickVehicleNetwork() {
        if(!ExperimentalNetwork||ZNet.instance==null||ZNet.IsSinglePlayer) {
            hostCollisions?.Dispose();hostCollisions=null;networkClient?.Reset("Vehicle connection closed");networkWasReady=false;return;
        }
        if(ZNet.instance.IsServer()) {
            if(hostNetwork==null) {
                hostNetwork=new VehicleNetworkServer(vehicleCatalog,message=>Logger.LogWarning(message),container=>{if(container!=null)VehicleStorage.Hide(container.gameObject);},VehicleNetworkPlacement.Clear);
            }
            hostNetwork.Tick();
            if(hostCollisions==null)hostCollisions=new VehicleServerCollisions(vehicleCatalog,data=>fleet.Any(v=>v.car!=null&&v.car.activeSelf&&(v.networkData==data||v.storage?.Data==data)));
            hostCollisions.Tick();
        }
        if(networkClient==null)networkClient=new VehicleNetworkClient(message=>Logger.LogWarning(message));
        networkClient.Tick();
        if(networkClient.Ready!=networkWasReady) {
            networkWasReady=networkClient.Ready;
            Logger.LogInfo(networkWasReady?"NFVU2 server protocol connected; network sandbox available.":"NFVU2 server protocol disconnected.");
        }
    }
}
}
