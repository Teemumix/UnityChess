using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;

public class DLCStoreManager : MonoBehaviour
{
    public static DLCStoreManager Instance { get; private set; }

    [SerializeField] public GameObject storePanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject skinItemPrefab;
    [SerializeField] private Text gemsText;

    private int playerGems = 100;
    private List<SkinData> availableSkins = new List<SkinData>
    {
        new SkinData { Name = "Chest", Price = 38, StoragePath = "dlc/chest1.png" },
        new SkinData { Name = "Diamond", Price = 12, StoragePath = "dlc/diamonds1.png" },
        new SkinData { Name = "Crystal", Price = 89, StoragePath = "dlc/dlc1.png" },
        new SkinData { Name = "Red Crystal", Price = 2, StoragePath = "dlc/dlc2.png" },
        new SkinData { Name = "River", Price = 33, StoragePath = "dlc/dlc3.png" },
        new SkinData { Name = "Acid Pool", Price = 26, StoragePath = "dlc/dlc4.png" },
        new SkinData { Name = "Desert", Price = 18, StoragePath = "dlc/dlc5.png" },
        new SkinData { Name = "Volcano", Price = 7, StoragePath = "dlc/dlc6.png" },
        new SkinData { Name = "Pyramid", Price = 4, StoragePath = "dlc/dlc7.png" },
        new SkinData { Name = "Forest", Price = 1, StoragePath = "dlc/dlc8.png" }
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (NetworkManager.Singleton.IsServer) // Reset only on host
        {
            PlayerPrefs.DeleteAll(); // Run once, then comment out
        }
        playerGems = PlayerPrefs.GetInt("PlayerGems", 100);
        UpdateGemsUI();
        StartCoroutine(PopulateStoreWithDelay()); 
    }

    public void ToggleStore(bool isActive)
    {
        storePanel.SetActive(isActive);
    }

    private IEnumerator PopulateStoreWithDelay()
    {
        yield return new WaitUntil(() => SkinLoader.Instance != null && SkinLoader.Instance.isInitialized);
        Debug.Log($"Populating store with {availableSkins.Count} skins. ContentParent: {contentParent?.name}");
        foreach (SkinData skin in availableSkins)
        {
            GameObject item = Instantiate(skinItemPrefab, contentParent);
            SkinItemUI itemUI = item.GetComponent<SkinItemUI>();
            bool isOwned = PlayerPrefs.GetInt($"Skin_{skin.Name}_Owned", 0) == 1;
            itemUI.Setup(skin.Name, skin.Price, skin.StoragePath, isOwned);
            Debug.Log($"Instantiated {skin.Name}, Owned: {isOwned}");
            SkinLoader.Instance.LoadSkin(skin.Name, skin.StoragePath); // Load all skins
        }

        string equippedSkin = PlayerPrefs.GetString("EquippedSkin", "");
        if (!string.IsNullOrEmpty(equippedSkin))
        {
            NetworkPlayerSkinManager.Instance.SetPlayerSkin(equippedSkin);
        }
    }

    public void PurchaseSkin(string skinName, int price, string storagePath)
    {
    if (playerGems >= price)
        {
            playerGems -= price;
            PlayerPrefs.SetInt("PlayerGems", playerGems);
            Debug.Log($"Set PlayerGems to {playerGems}");
            PlayerPrefs.SetInt($"Skin_{skinName}_Owned", 1);
            Debug.Log($"Set Skin_{skinName}_Owned to 1");
            try
            {
                PlayerPrefs.SetString("EquippedSkin", skinName);
                Debug.Log($"Set EquippedSkin to {skinName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set EquippedSkin: {e.Message}");
            }
            PlayerPrefs.Save();
            UpdateGemsUI();
            Debug.Log($"Purchased and equipped {skinName} for {price} Gems!");
            NetworkPlayerSkinManager.Instance.SetPlayerSkin(skinName);
        }
        else
        {
            Debug.Log("Not enough Gems!");
        }
    }

    private void UpdateGemsUI()
    {
        gemsText.text = $"Gems: {playerGems}";
        Debug.Log($"Gems UI updated to: {playerGems}");
    }
}

public class SkinData
{
    public string Name;
    public int Price;
    public string StoragePath;
}