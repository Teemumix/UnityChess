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
        new SkinData { Name = "Knight Avatar", Price = 20, StoragePath = "skins/knight_avatar.png" },
        new SkinData { Name = "Wizard Avatar", Price = 30, StoragePath = "skins/wizard_avatar.png" },
        new SkinData { Name = "Dragon Avatar", Price = 50, StoragePath = "skins/dragon_avatar.png" }
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
        foreach (SkinData skin in availableSkins)
        {
            GameObject item = Instantiate(skinItemPrefab, contentParent);
            SkinItemUI itemUI = item.GetComponent<SkinItemUI>();
            itemUI.Setup(skin.Name, skin.Price, skin.StoragePath);
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