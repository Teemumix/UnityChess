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

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null - ensure NetworkManager is in the scene");
            return;
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("NetworkObject component missing on NetworkPlayerSkinManager");
            return;
        }

        // Spawn on host only once
        if (NetworkManager.Singleton.IsServer && !netObj.IsSpawned)
        {
            netObj.Spawn();
            Debug.Log("Host spawned NetworkPlayerSkinManager in Awake");
        }
    }

    private void Start()
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("NetworkPlayerSkinManager not spawned yet! Waiting...");
            StartCoroutine(WaitForSpawn());
            return;
        }

        if (NetworkManager.Singleton.IsClient && !IsServer)
        {
            Debug.Log("Client waiting for NetworkPlayerSkinManager to spawn");
            StartCoroutine(WaitForSpawn());
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && NetworkManager.Singleton.IsServer && !IsSpawned)
        {
            GetComponent<NetworkObject>().Spawn();
            Debug.Log("Manually spawned NetworkPlayerSkinManager via S key");
        }
    }

    private IEnumerator WaitForSpawn()
    {
        yield return new WaitUntil(() => IsSpawned);
        Debug.Log("NetworkPlayerSkinManager confirmed spawned - IsServer: " + IsServer + ", IsClient: " + IsClient);
        UpdatePlayerIcons();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        Debug.Log($"OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, IsSpawned: {IsSpawned}");
        UpdatePlayerIcons();
        if (IsHost)
        {
            string equippedSkin = PlayerPrefs.GetString("EquippedSkin", "");
            if (!string.IsNullOrEmpty(equippedSkin))
            {
                Debug.Log($"Host forcing sync of {equippedSkin} on spawn");
                SetPlayerSkin(equippedSkin);
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
                SyncSkinServerRpc(whitePlayerSkin.Value);
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
        if (!NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("SetPlayerSkin called on a non-client instance. Ignoring.");
            return;
        }

        Sprite sprite = SkinLoader.Instance.GetSkinSprite(skinName);
        if (sprite == null)
        {
            Debug.LogError($"Failed to get sprite for {skinName}");
            return;
        }
        UpdateLocalIcon(skinName, sprite);
        Debug.Log($"SetPlayerSkin - IsClient: {NetworkManager.Singleton.IsClient}, IsSpawned: {IsSpawned}");

        // Always attempt to sync, even if not spawned yet
        if (NetworkManager.Singleton.IsServer)
        {
            if (!IsSpawned)
            {
                Debug.LogWarning("NetworkObject not spawned yet! Forcing sync anyway...");
            }
            SyncSkinServerRpc(skinName);
        }
        else if (!IsSpawned)
        {
            StartCoroutine(SyncSkinWhenReady(skinName));
        }
        else
        {
            SyncSkinServerRpc(skinName);
        }
    }

    private IEnumerator SyncSkinWhenReady(string skinName)
    {
        Debug.Log($"Waiting to sync {skinName} - Initial state - IsClient: {NetworkManager.Singleton.IsClient}, IsSpawned: {IsSpawned}");
        yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && IsSpawned);
        Debug.Log($"Syncing skin {skinName} for ClientId: {NetworkManager.Singleton.LocalClientId}");
        SyncSkinServerRpc(skinName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SyncSkinServerRpc(string skinName)
    {
        Debug.Log($"ServerRpc called - Skin: {skinName}, ClientId: {NetworkManager.Singleton.LocalClientId}");
        if (NetworkManager.Singleton.LocalClientId == whitePlayerId.Value)
        {
            whitePlayerSkin.Value = skinName;
        }
        else if (NetworkManager.Singleton.LocalClientId == blackPlayerId.Value)
        {
            blackPlayerSkin.Value = skinName;
        }
        SyncSkinClientRpc(skinName);
    }

    [ClientRpc]
    private void SyncSkinClientRpc(string skinName)
    {
        Debug.Log($"ClientRpc received - Skin: {skinName}");
        Sprite sprite = SkinLoader.Instance.GetSkinSprite(skinName);
        if (sprite == null)
        {
            Debug.LogError($"Client failed to get sprite for {skinName}");
            return;
        }
        UpdateLocalIcon(skinName, sprite);
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