using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkPlayerSkinManager : NetworkBehaviour
{
    public static NetworkPlayerSkinManager Instance { get; private set; }

    [SerializeField] private Image whitePlayerIcon; // Host (White player)
    [SerializeField] private Image blackPlayerIcon; // Client (Black player)

    private NetworkVariable<int> playerCount = new NetworkVariable<int>(0);
    private NetworkVariable<ulong> whitePlayerId = new NetworkVariable<ulong>(0);
    private NetworkVariable<ulong> blackPlayerId = new NetworkVariable<ulong>(0);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        Debug.Log($"Network spawn - WhiteIcon: {whitePlayerIcon != null}, BlackIcon: {blackPlayerIcon != null}");
        UpdatePlayerIcons();
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
            string equippedSkin = PlayerPrefs.GetString("EquippedSkin", "");
            if (!string.IsNullOrEmpty(equippedSkin))
            {
                SetPlayerSkinServerRpc(equippedSkin, whitePlayerId.Value);
            }
        }
        UpdatePlayerIcons();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        playerCount.Value--;
        if (clientId == whitePlayerId.Value) whitePlayerId.Value = 0;
        if (clientId == blackPlayerId.Value) blackPlayerId.Value = 0;
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
        if (NetworkManager.Singleton.IsClient)
        {
            SetPlayerSkinServerRpc(skinName, NetworkManager.Singleton.LocalClientId);
        }
        UpdateLocalIcon(skinName, sprite);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerSkinServerRpc(string skinName, ulong clientId)
    {
        UpdatePlayerSkinClientRpc(skinName, clientId);
    }

    [ClientRpc]
    private void UpdatePlayerSkinClientRpc(string skinName, ulong clientId)
    {
        Sprite sprite = SkinLoader.Instance.GetSkinSprite(skinName);
        if (sprite == null)
        {
            Debug.LogError($"Client failed to get sprite for {skinName}");
            return;
        }
        if (clientId == NetworkManager.Singleton.LocalClientId) // Local player’s skin
        {
            Image targetIcon = NetworkManager.Singleton.IsHost ? whitePlayerIcon : blackPlayerIcon;
            if (targetIcon != null)
            {
                targetIcon.sprite = sprite;
                Debug.Log($"Updated local skin for client {clientId} to {skinName}");
            }
        }
        else // Remote player’s skin
        {
            Image targetIcon = NetworkManager.Singleton.IsHost ? blackPlayerIcon : whitePlayerIcon;
            if (targetIcon != null)
            {
                targetIcon.sprite = sprite;
                Debug.Log($"Updated remote skin for client {clientId} to {skinName}");
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
        string whiteSkin = PlayerPrefs.GetString("EquippedSkin", "");
        if (!string.IsNullOrEmpty(whiteSkin) && whitePlayerId.Value != 0)
        {
            Sprite sprite = SkinLoader.Instance.GetSkinSprite(whiteSkin);
            if (sprite != null && whitePlayerIcon != null) whitePlayerIcon.sprite = sprite;
        }
    }
}