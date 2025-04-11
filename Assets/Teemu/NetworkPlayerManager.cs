using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerManager : NetworkBehaviour
{
    private NetworkVariable<int> playerCount = new NetworkVariable<int>(0);

    // Set up network event listeners
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    // Handle client connection
    private void OnClientConnected(ulong clientId)
    {
        playerCount.Value++;
        if (playerCount.Value > 2)
        {
            NetworkManager.Singleton.DisconnectClient(clientId);
        }
    }

    // Handle client disconnection
    private void OnClientDisconnected(ulong clientId)
    {
        playerCount.Value--;
    }

    // Notify clients of connection errors
    [ClientRpc]
    private void ConnectionErrorClientRpc(string errorMessage)
    {
        Debug.LogError($"Connection error: {errorMessage}");
    }
}