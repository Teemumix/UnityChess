using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerManager : NetworkBehaviour
{
    private NetworkVariable<int> playerCount = new NetworkVariable<int>(0);

    void Start()
    {
        if (!IsSpawned) Debug.LogWarning("NetworkPlayerManager not spawned yet!");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"NetworkPlayerManager spawned. IsServer: {IsServer}");
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
            NetworkManager.Singleton.DisconnectClient(clientId);
            Debug.Log("Connection rejected: Max players reached.");
        }
        else
        {
            Debug.Log($"Successfully connected client {clientId}");
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