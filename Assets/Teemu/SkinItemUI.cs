using UnityEngine;
using UnityEngine.UI;

public class SkinItemUI : MonoBehaviour
{
    [SerializeField] public Text nameText;
    [SerializeField] public Text priceText;
    [SerializeField] public Image previewImage;
    [SerializeField] public Button buyButton;

    private string skinName;
    private int price;
    private string storagePath;

    // Configure skin item UI
    public void Setup(string name, int price, string storagePath, bool isOwned = false)
    {
        this.skinName = name;
        this.price = price;
        this.storagePath = storagePath;

        nameText.text = name;
        priceText.text = isOwned ? "Owned" : $"{price} Gems";
        buyButton.onClick.AddListener(() => DLCStoreManager.Instance.PurchaseSkin(skinName, price, storagePath));
        buyButton.interactable = !isOwned;
    }

    // Set preview image sprite
    public void SetPreviewSprite(Sprite sprite)
    {
        previewImage.sprite = sprite;
    }
}