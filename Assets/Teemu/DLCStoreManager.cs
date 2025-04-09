using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

        PopulateStore();
    }

    public void ToggleStore(bool isActive)
    {
        storePanel.SetActive(isActive);
    }

    private void PopulateStore()
    {
        Debug.Log($"Populating store with {availableSkins.Count} skins. ContentParent: {contentParent?.name}");
        foreach (SkinData skin in availableSkins)
        {
            GameObject item = Instantiate(skinItemPrefab, contentParent);
            SkinItemUI itemUI = item.GetComponent<SkinItemUI>();
            itemUI.Setup(skin.Name, skin.Price, skin.StoragePath);
            Debug.Log($"Instantiated {skin.Name}");
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
            //SkinLoader.Instance.LoadSkin(skinName, storagePath); 
        }
        else
        {
            Debug.Log("Not enough Gems!");
        }
    }

    private void UpdateGemsUI()
    {
        gemsText.text = $"Gems: {playerGems}";
    }
}

public class SkinData
{
    public string Name;
    public int Price;
    public string StoragePath;
}