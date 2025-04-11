using UnityEngine;
using Unity.Netcode;

public class NetworkBootstrap : MonoBehaviour
{
    public GameObject networkGameControllerPrefab;

    // Start network as host
    public void StartHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            NetworkObject ngo = Instantiate(networkGameControllerPrefab).GetComponent<NetworkObject>();
            ngo.Spawn();
        }
    }

    // Start network as client
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    // Resign from the game
    public void Resign()
    {
        if (NetworkGameController.Instance != null)
        {
            NetworkGameController.Instance.ResignServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    // Toggle store visibility
    public void ToggleStore()
    {
        DLCStoreManager.Instance.ToggleStore(!DLCStoreManager.Instance.storePanel.activeSelf);
    }
}