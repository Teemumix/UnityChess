using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkBootstrap : MonoBehaviour
{
    public void StartHost(){
        NetworkManager.Singleton.StartHost();
        Debug.Log("Host started");
    }

    public void StartClient(){
        NetworkManager.Singleton.StartClient();
        Debug.Log("Client started");
    }

    public void Resign(){
        if (NetworkGameController.Instance != null)
        {
            NetworkGameController.Instance.ResignServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }
}
