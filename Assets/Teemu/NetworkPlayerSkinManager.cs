using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class NetworkPlayerSkinManager : NetworkBehaviour
{
    public static NetworkPlayerSkinManager Instance { get; private set; }

    [SerializeField] private Image whitePlayerIcon;
    [SerializeField] private Image blackPlayerIcon;

    private NetworkVariable<int> playerCount = new NetworkVariable<int>(0);
    private NetworkVariable<ulong> whitePlayerId = new NetworkVariable<ulong>(0);
    private NetworkVariable<ulong> blackPlayerId = new NetworkVariable<ulong>(0);
    private NetworkVariable<string> whitePlayerSkin = new NetworkVariable<string>("");
    private NetworkVariable<string> blackPlayerSkin = new NetworkVariable<string>("");

    // Set up singleton and spawn network object
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

        if (NetworkManager.Singleton.IsServer && !netObj.IsSpawned)
        {
            netObj.Spawn();
        }
    }

    // Wait for network spawn
    private void Start()
    {
        if (!IsSpawned)
        {
            StartCoroutine(WaitForSpawn());
        }
    }

    // Allow manual spawning with key press
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && NetworkManager.Singleton.IsServer && !IsSpawned)
        {
            GetComponent<NetworkObject>().Spawn();
        }
    }

    // Wait until network object is spawned
    private IEnumerator WaitForSpawn()
    {
        yield return new WaitUntil(() => IsSpawned);
        UpdatePlayerIcons();
    }

    // Set up network callbacks and initial skin
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        UpdatePlayerIcons();
        if (IsHost)
        {
            string equippedSkin = PlayerPrefs.GetString("EquippedSkin", "");
            if (!string.IsNullOrEmpty(equippedSkin))
            {
                SetPlayerSkin(equippedSkin);
            }
        }
    }

    // Update player count and skins on connect
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

    // Update player count and skins on disconnect
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

    // Set player's skin locally and sync
    public void SetPlayerSkin(string skinName)
    {
        if (!NetworkManager.Singleton.IsClient)
            return;

        Sprite sprite = SkinLoader.Instance.GetSkinSprite(skinName);
        if (sprite == null)
            return;

        UpdateLocalIcon(skinName, sprite);

        if (NetworkManager.Singleton.IsServer)
        {
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

    // Sync skin when network is ready
    private IEnumerator SyncSkinWhenReady(string skinName)
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && IsSpawned);
        SyncSkinServerRpc(skinName);
    }

    // Sync skin with server
    [ServerRpc(RequireOwnership = false)]
    private void SyncSkinServerRpc(string skinName)
    {
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

    // Sync skin across clients
    [ClientRpc]
    private void SyncSkinClientRpc(string skinName)
    {
        Sprite sprite = SkinLoader.Instance.GetSkinSprite(skinName);
        if (sprite != null)
        {
            UpdateLocalIcon(skinName, sprite);
        }
    }

    // Update local player icon
    private void UpdateLocalIcon(string skinName, Sprite sprite)
    {
        Image targetIcon = NetworkManager.Singleton.IsHost ? whitePlayerIcon : blackPlayerIcon;
        if (targetIcon != null)
        {
            targetIcon.sprite = sprite;
        }
    }

    // Update all player icons
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