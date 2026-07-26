<<<<<<< Updated upstream
=======
using TMPro;
>>>>>>> Stashed changes
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상점 아이템 슬롯 하나의 UI를 담당하는 스크립트.
/// ShopManager가 자동으로 생성하고 Init()으로 데이터를 채웁니다.
/// 슬롯 프리팹에 이 스크립트를 부착하세요.
/// </summary>
public class ShopItemSlot : MonoBehaviour
{
    [Header("슬롯 UI 요소 (프리팹에서 연결)")]
    [SerializeField] private Image iconImage; // 아이템 아이콘
    [SerializeField] private TMP_Text nameText;  // 아이템 이름
    [SerializeField] private TMP_Text descText;  // 아이템 설명
    [SerializeField] private TMP_Text priceText; // 아이템 가격
    [SerializeField] private Button buyButton; // 구매 버튼

    private ItemData itemData;
    private ShopManager shopManager;

    // -----------------------------------------------
    // ShopManager에서 슬롯 생성 직후 호출 - 데이터 채우기
    // -----------------------------------------------
    public void Init(ItemData data, ShopManager manager)
    {
        itemData = data;
        shopManager = manager;

        if (iconImage != null) iconImage.sprite = data.icon;
        if (nameText != null) nameText.text = data.itemName;
        if (descText != null) descText.text = data.description;
        if (priceText != null) priceText.text = $"{data.price:N0} G";

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
    }

    // -----------------------------------------------
    // 구매 버튼 클릭 시 호출 - 실제 골드 차감/아이템 생성은 ShopManager가 처리
    // (어느 상점에서 샀는지 알아야 드롭 위치를 알 수 있어서)
    // -----------------------------------------------
    private void OnBuyClicked()
    {
<<<<<<< Updated upstream
        if (itemData == null) return;
        if (GoldManager.Instance == null) return;

        bool success = GoldManager.Instance.SpendGold(itemData.price);

        if (success)
        {
            Debug.Log($"[ShopItemSlot] '{itemData.itemName}' 구매 완료!");
            // TODO: 인벤토리에 아이템 추가 로직 (인벤토리 구현 후 연동)
        }
        else
        {
            Debug.Log($"[ShopItemSlot] 골드 부족! '{itemData.itemName}' 구매 실패.");
            // TODO: 골드 부족 UI 피드백
        }
=======
        shopManager.BuyItem(itemData);
>>>>>>> Stashed changes
    }
}