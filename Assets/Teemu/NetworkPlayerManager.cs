using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerManager : NetworkBehaviour
{
    private NetworkVariable<int> playerCount = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        playerCount.Value++;
        Debug.Log($"Client {clientId} connected. Total players: {playerCount.Value}");
        if (playerCount.Value > 2)
        {
            NetworkManager.Singleton.DisconnectClient(clientId); // Limit to 2 players
            Debug.Log("Connection rejected: Max players reached.");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        playerCount.Value--;
        Debug.Log($"Client {clientId} disconnected. Total players: {playerCount.Value}");
    }

    [ClientRpc]
    private void ConnectionErrorClientRpc(string errorMessage)
    {
        Debug.LogError($"Connection error: {errorMessage}");
    }
}