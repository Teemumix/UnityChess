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

    public void Setup(string name, int price, string storagePath, bool isOwned = false)
    {
        this.skinName = name;
        this.price = price;
        this.storagePath = storagePath;

        nameText.text = name;
        priceText.text = isOwned ? "Owned" : $"{price} Gems";
        buyButton.onClick.AddListener(() => DLCStoreManager.Instance.PurchaseSkin(skinName, price, storagePath));
        buyButton.interactable = !isOwned;
        Debug.Log($"SkinItemUI Setup: {name}, Owned: {isOwned}, PreviewImage: {previewImage != null}");
    }

    public void SetPreviewSprite(Sprite sprite) // New method to set sprite
    {
        previewImage.sprite = sprite;
        Debug.Log($"Set preview sprite for {skinName}: {sprite != null}");
    }
}