using UnityEngine;
using UnityEngine.UI;

public class SkinItemUI : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Text priceText;
    [SerializeField] private Image previewImage;
    [SerializeField] private Button buyButton;

    private string skinName;
    private int price;
    private string storagePath;

    public void Setup(string name, int price, string storagePath)
    {
        this.skinName = name;
        this.price = price;
        this.storagePath = storagePath;

        nameText.text = name;
        priceText.text = $"{price} Gems";
        buyButton.onClick.AddListener(() => DLCStoreManager.Instance.PurchaseSkin(skinName, price, storagePath));
    }
}