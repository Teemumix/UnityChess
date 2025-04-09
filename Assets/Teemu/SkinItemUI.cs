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