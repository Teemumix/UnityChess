using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class NetworkPlayerSkinManager : NetworkBehaviour
{
    public static NetworkPlayerSkinManager Instance { get; private set; }

    [SerializeField] private Image whitePlayerIcon; // Host (White player)
    [SerializeField] private Image blackPlayerIcon; // Client (Black player)

    private NetworkVariable<int> playerCount = new NetworkVariable<int>(0);
    private NetworkVariable<ulong> whitePlayerId = new NetworkVariable<ulong>(0);
    private NetworkVariable<ulong> blackPlayerId = new NetworkVariable<ulong>(0);
    private NetworkVariable<string> whitePlayerSkin = new NetworkVariable<string>("");
    private NetworkVariable<string> blackPlayerSkin = new NetworkVariable<string>("");

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsServer && !IsSpawned)
            {
                GetComponent<NetworkObject>().Spawn();
                Debug.Log("Host spawned NetworkPlayerSkinManager");
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                Debug.Log("Client waiting for NetworkPlayerSkinManager to spawn");
                StartCoroutine(WaitForSpawn());
            }
        }
    }

    private IEnumerator WaitForSpawn()
    {
        yield return new WaitUntil(() => IsSpawned);
        Debug.Log("Client confirmed NetworkPlayerSkinManager spawned");
        UpdatePlayerIcons();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        Debug.Log($"Network spawn - WhiteIcon: {whitePlayerIcon != null}, BlackIcon: {blackPlayerIcon != null}, IsSpawned: {IsSpawned}");
        UpdatePlayerIcons();
        if (IsHost)
        {
            string equippedSkin = PlayerPrefs.GetString("EquippedSkin", "");
            if (!string.IsNullOrEmpty(equippedSkin))
            {
                Debug.Log($"Host forcing sync of {equippedSkin} on spawn");
                SetPlayerSkinServerRpc(equippedSkin, NetworkManager.Singleton.LocalClientId);
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        playerCount.Value++;
        if (playerCount.Value == 1)
        {
            whitePlayerId.Value = clientId;
        }
        else if (playerCount.Value == 2)
        {
            blackPlayerId.Value = clientId;
            if (!string.IsNullOrEmpty(whitePlayerSkin.Value))
            {
                SetPlayerSkinServerRpc(whitePlayerSkin.Value, whitePlayerId.Value);
            }
        }
        UpdatePlayerIcons();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        playerCount.Value--;
        if (clientId == whitePlayerId.Value)
        {
            whitePlayerId.Value = 0;
            whitePlayerSkin.Value = "";
        }
        if (clientId == blackPlayerId.Value)
        {
            blackPlayerId.Value = 0;
            blackPlayerSkin.Value = "";
        }
        UpdatePlayerIcons();
    }

    public void SetPlayerSkin(string skinName)
    {
        Sprite sprite = SkinLoader.Instance.GetSkinSprite(skinName);
        if (sprite == null)
        {
            Debug.LogError($"Failed to get sprite for {skinName}");
            return;
        }
        UpdateLocalIcon(skinName, sprite);
        Debug.Log($"SetPlayerSkin - IsClient: {NetworkManager.Singleton?.IsClient}, IsSpawned: {IsSpawned}");
        StartCoroutine(SyncSkinWhenReady(skinName));
    }

    private IEnumerator SyncSkinWhenReady(string skinName)
    {
        Debug.Log($"Waiting to sync {skinName} - Initial state - IsClient: {NetworkManager.Singleton?.IsClient}, IsSpawned: {IsSpawned}");
        yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && IsSpawned);
        Debug.Log($"Syncing skin {skinName} for ClientId: {NetworkManager.Singleton.LocalClientId}");
        SetPlayerSkinServerRpc(skinName, NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerSkinServerRpc(string skinName, ulong clientId)
    {
        Debug.Log($"ServerRpc called - Skin: {skinName}, ClientId: {clientId}");
        if (clientId == whitePlayerId.Value)
        {
            whitePlayerSkin.Value = skinName;
        }
        else if (clientId == blackPlayerId.Value)
        {
            blackPlayerSkin.Value = skinName;
        }
        UpdatePlayerSkinClientRpc(skinName, clientId);
    }

    [ClientRpc]
    private void UpdatePlayerSkinClientRpc(string skinName, ulong clientId)
    {
        Debug.Log($"ClientRpc received - Skin: {skinName}, ClientId: {clientId}");
        Sprite sprite = SkinLoader.Instance.GetSkinSprite(skinName);
        if (sprite == null)
        {
            Debug.LogError($"Client failed to get sprite for {skinName}");
            return;
        }
        if (clientId == whitePlayerId.Value)
        {
            if (whitePlayerIcon != null)
            {
                whitePlayerIcon.sprite = sprite;
                Debug.Log($"Updated whitePlayerIcon to {skinName} for client {clientId}");
            }
        }
        else if (clientId == blackPlayerId.Value)
        {
            if (blackPlayerIcon != null)
            {
                blackPlayerIcon.sprite = sprite;
                Debug.Log($"Updated blackPlayerIcon to {skinName} for client {clientId}");
            }
        }
    }

    private void UpdateLocalIcon(string skinName, Sprite sprite)
    {
        Image targetIcon = NetworkManager.Singleton.IsHost ? whitePlayerIcon : blackPlayerIcon;
        if (targetIcon != null)
        {
            targetIcon.sprite = sprite;
            Debug.Log($"Local skin updated to {skinName} on {(NetworkManager.Singleton.IsHost ? "whitePlayerIcon" : "blackPlayerIcon")}");
        }
        else
        {
            Debug.LogError($"Target icon is null for {skinName}");
        }
    }

    private void UpdatePlayerIcons()
    {
        if (!string.IsNullOrEmpty(whitePlayerSkin.Value) && whitePlayerId.Value != 0)
        {
            Sprite sprite = SkinLoader.Instance.GetSkinSprite(whitePlayerSkin.Value);
            if (sprite != null && whitePlayerIcon != null)
            {
                whitePlayerIcon.sprite = sprite;
            }
        }
        if (!string.IsNullOrEmpty(blackPlayerSkin.Value) && blackPlayerId.Value != 0)
        {
            Sprite sprite = SkinLoader.Instance.GetSkinSprite(blackPlayerSkin.Value);
            if (sprite != null && blackPlayerIcon != null)
            {
                blackPlayerIcon.sprite = sprite;
            }
        }
    }
}