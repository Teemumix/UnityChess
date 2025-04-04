using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkBootstrap : MonoBehaviour
{
public void StartHost()
{
    bool success = NetworkManager.Singleton.StartHost();
    Debug.Log($"Host started: {success}");
}

public void StartClient()
{
    bool success = NetworkManager.Singleton.StartClient();
    Debug.Log($"Client started: {success}");
}

    public void Resign(){
        if (NetworkGameController.Instance != null)
        {
            NetworkGameController.Instance.ResignServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }
}
