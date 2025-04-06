using UnityEngine;
using Unity.Netcode;

public class NetworkBootstrap : MonoBehaviour
{
    public GameObject networkGameControllerPrefab; // Assign in Inspector

    public void StartHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Host started: true");
            NetworkObject ngo = Instantiate(networkGameControllerPrefab).GetComponent<NetworkObject>();
            ngo.Spawn();
        }
        else
        {
            Debug.Log("Host started: false");
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Client started: true");
        }
        else
        {
            Debug.Log("Client started: false");
        }
    }

    public void Resign()
    {
        if (NetworkGameController.Instance != null)
        {
            NetworkGameController.Instance.ResignServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    public void ToggleStore()
    {
        DLCStoreManager.Instance.ToggleStore(!DLCStoreManager.Instance.storePanel.activeSelf);
    }
}