using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

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
        new SkinData { Name = "Chest", Price = 20, StoragePath = "dlc/chest1.png" },
        new SkinData { Name = "Diamond", Price = 30, StoragePath = "dlc/diamonds1.png" },
        new SkinData { Name = "Crystal", Price = 50, StoragePath = "dlc/dlc1.png" },
        new SkinData { Name = "Red Crystal", Price = 13, StoragePath = "dlc/dlc2.png" },
        new SkinData { Name = "River", Price = 50, StoragePath = "dlc/dlc3.png" },
        new SkinData { Name = "Acid Pool", Price = 50, StoragePath = "dlc/dlc4.png" },
        new SkinData { Name = "Desert", Price = 50, StoragePath = "dlc/dlc5.png" },
        new SkinData { Name = "Volcano", Price = 48, StoragePath = "dlc/dlc6.png" },
        new SkinData { Name = "Pyramid", Price = 50, StoragePath = "dlc/dlc7.png" },
        new SkinData { Name = "Forest", Price = 46, StoragePath = "dlc/dlc8.png" }
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        playerGems = PlayerPrefs.GetInt("PlayerGems", 100);
        UpdateGemsUI();
        StartCoroutine(PopulateStoreWithDelay()); // Use coroutine for delay
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
    }

    public void PurchaseSkin(string skinName, int price, string storagePath)
    {
        if (playerGems >= price)
        {
            playerGems -= price;
            PlayerPrefs.SetInt("PlayerGems", playerGems);
            PlayerPrefs.SetInt($"Skin_{skinName}_Owned", 1);
            PlayerPrefs.Save();
            UpdateGemsUI();
            Debug.Log($"Purchased {skinName} for {price} Gems!");
            // No need to load here since all skins are loaded at start
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