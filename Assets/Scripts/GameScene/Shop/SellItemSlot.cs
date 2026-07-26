using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 판매 목록 슬롯 하나의 UI를 담당하는 스크립트. ShopManager.RefreshSellSlots()가
/// 플레이어의 equips(장비)를 기준으로 자동 생성/파괴한다.
/// </summary>
public class SellItemSlot : MonoBehaviour
{
    [Header("슬롯 UI 요소 (프리팹에서 연결)")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button sellButton;

    private GameObject equippedItem;
    private ItemData itemData;
    private ShopManager shopManager;

    public void Init(GameObject equip, ItemData data, ShopManager manager)
    {
        equippedItem = equip;
        itemData = data;
        shopManager = manager;

        if (iconImage != null) iconImage.sprite = data.icon;
        if (nameText != null) nameText.text = data.itemName;
        if (priceText != null) priceText.text = $"{data.price:N0} G";

        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellClicked);
    }

    private void OnSellClicked()
    {
        shopManager.SellItem(equippedItem, itemData);
    }
}
