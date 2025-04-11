using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine.Analytics;
using Firebase.Analytics;

public class DLCStoreManager : NetworkBehaviour
{
    public static DLCStoreManager Instance { get; private set; }

    [SerializeField] public GameObject storePanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject skinItemPrefab;
    [SerializeField] private Text gemsText;

    private int playerGems = 100;
    private List<SkinData> availableSkins = new List<SkinData>
    {
        new SkinData { Name = "Chest", Price = 17, StoragePath = "dlc/chest1.png" },
        new SkinData { Name = "Diamond", Price = 12, StoragePath = "dlc/diamonds1.png" },
        new SkinData { Name = "Crystal", Price = 7, StoragePath = "dlc/dlc1.png" },
        new SkinData { Name = "Red Crystal", Price = 2, StoragePath = "dlc/dlc2.png" },
        new SkinData { Name = "River", Price = 11, StoragePath = "dlc/dlc3.png" },
        new SkinData { Name = "Acid Pool", Price = 26, StoragePath = "dlc/dlc4.png" },
        new SkinData { Name = "Desert", Price = 16, StoragePath = "dlc/dlc5.png" },
        new SkinData { Name = "Volcano", Price = 7, StoragePath = "dlc/dlc6.png" },
        new SkinData { Name = "Pyramid", Price = 4, StoragePath = "dlc/dlc7.png" },
        new SkinData { Name = "Forest", Price = 1, StoragePath = "dlc/dlc8.png" }
    };

    // Set up singleton
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Initialize store and load skins
    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null - ensure NetworkManager is in the scene");
            return;
        }
        if (NetworkManager.Singleton.IsServer)
        {
            PlayerPrefs.DeleteAll();
        }
        playerGems = PlayerPrefs.GetInt("PlayerGems", 100);
        UpdateGemsUI();
        StartCoroutine(PopulateStoreWithDelay());
    }

    // Show or hide store panel
    public void ToggleStore(bool isActive)
    {
        storePanel.SetActive(isActive);
    }

    // Populate store with skin items
    private IEnumerator PopulateStoreWithDelay()
    {
        yield return new WaitUntil(() => SkinLoader.Instance != null && SkinLoader.Instance.isInitialized);
        foreach (SkinData skin in availableSkins)
        {
            GameObject item = Instantiate(skinItemPrefab, contentParent);
            SkinItemUI itemUI = item.GetComponent<SkinItemUI>();
            bool isOwned = PlayerPrefs.GetInt($"Skin_{skin.Name}_Owned", 0) == 1;
            itemUI.Setup(skin.Name, skin.Price, skin.StoragePath, isOwned);
            SkinLoader.Instance.LoadSkin(skin.Name, skin.StoragePath);
        }

        string equippedSkin = PlayerPrefs.GetString("EquippedSkin", "");
        if (!string.IsNullOrEmpty(equippedSkin) && NetworkPlayerSkinManager.Instance != null)
        {
            NetworkPlayerSkinManager.Instance.SetPlayerSkin(equippedSkin);
        }
    }

    // Purchase a skin
    public void PurchaseSkin(string skinName, int cost, string storagePath)
    {
        if (!NetworkManager.Singleton.IsClient)
            return;

        if (playerGems >= cost && PlayerPrefs.GetInt($"Skin_{skinName}_Owned", 0) == 0)
        {
            playerGems -= cost;
            PlayerPrefs.SetInt("PlayerGems", playerGems);
            PlayerPrefs.SetInt($"Skin_{skinName}_Owned", 1);
            PlayerPrefs.SetString("EquippedSkin", skinName);
            PlayerPrefs.Save();

            UpdateGemsUI();
            LogDLCPurchase(skinName, cost);

            SyncPurchaseServerRpc(skinName, cost);
            if (NetworkPlayerSkinManager.Instance != null)
            {
                NetworkPlayerSkinManager.Instance.SetPlayerSkin(skinName);
            }
        }
    }

    // Log skin purchase event
    private void LogDLCPurchase(string skinName, int cost)
    {
        Analytics.CustomEvent("DLCPurchase", new Dictionary<string, object>
        {
            { "SkinName", skinName },
            { "Cost", cost },
            { "Timestamp", System.DateTime.UtcNow.ToString("o") },
            { "ClientID", NetworkManager.Singleton.LocalClientId.ToString() }
        });
        FirebaseAnalytics.LogEvent("DLCPurchase", new Parameter[]
        {
            new Parameter("SkinName", skinName),
            new Parameter("Cost", cost.ToString()),
            new Parameter("Timestamp", System.DateTime.UtcNow.ToString("o")),
            new Parameter("ClientID", NetworkManager.Singleton.LocalClientId.ToString())
        });
    }

    // Sync purchase with server
    [ServerRpc(RequireOwnership = false)]
    private void SyncPurchaseServerRpc(string skinName, int cost)
    {
        SyncPurchaseClientRpc(skinName, cost);
    }

    // Sync purchase across clients
    [ClientRpc]
    private void SyncPurchaseClientRpc(string skinName, int cost)
    {
        if (PlayerPrefs.GetInt($"Skin_{skinName}_Owned", 0) == 0)
        {
            playerGems -= cost;
            PlayerPrefs.SetInt("PlayerGems", playerGems);
            PlayerPrefs.SetInt($"Skin_{skinName}_Owned", 1);
            PlayerPrefs.SetString("EquippedSkin", skinName);
            PlayerPrefs.Save();
            UpdateGemsUI();
            if (NetworkPlayerSkinManager.Instance != null)
            {
                NetworkPlayerSkinManager.Instance.SetPlayerSkin(skinName);
            }
        }
    }

    // Update gems display
    private void UpdateGemsUI()
    {
        playerGems = PlayerPrefs.GetInt("PlayerGems", 100);
        gemsText.text = $"Gems: {playerGems}";
    }
}

public class SkinData
{
    public string Name;
    public int Price;
    public string StoragePath;
}