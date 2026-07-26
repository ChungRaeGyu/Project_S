using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬에 하나만 존재하는 공용 상점 UI 컨트롤러.
/// ShopRoom마다 있는 ShopInteractable이 상호작용 시 이걸 통해 열림 - 구매 목록은 그 상점 것으로,
/// 판매 목록은 상호작용한 플레이어의 장비(equips)로 채운다.
/// (파일명/클래스명은 기존 씬에 이미 연결돼있을 Inspector 참조가 깨지지 않도록 ShopManager로 유지)
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("상점 UI")]
    [SerializeField] private GameObject shopUI;

    [Header("구매 목록")]
    [SerializeField] private Transform buyGridContent;
    [SerializeField] private ShopItemSlot buySlotPrefab;

    [Header("판매 목록")]
    [SerializeField] private Transform sellGridContent;
    [SerializeField] private SellItemSlot sellSlotPrefab;

    private readonly List<ShopItemSlot> buySlots = new List<ShopItemSlot>();
    private readonly List<SellItemSlot> sellSlots = new List<SellItemSlot>();

    private ShopInteractable currentShop;
    private GameObject[] currentEquips;

    public static ShopManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (shopUI != null)
            shopUI.SetActive(false);
    }

    // -----------------------------------------------
    // ShopInteractable.OnInteract에서 호출
    // -----------------------------------------------
    public bool IsOpenFor(ShopInteractable shop)
    {
        return shopUI != null && shopUI.activeSelf && currentShop == shop;
    }

    public void Open(ShopInteractable shop, GameObject[] equips)
    {
        currentShop = shop;
        currentEquips = equips;

        BuildBuySlots();
        RefreshSellSlots();

        if (shopUI != null)
            shopUI.SetActive(true);

        // UI 클릭이 가능하도록 커서 잠금 해제
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        currentShop = null;
        currentEquips = null;

        if (shopUI != null)
            shopUI.SetActive(false);

        // CharacterLook.Set()과 동일한 평상시(게임플레이) 커서 상태로 복귀
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
    }

    // -----------------------------------------------
    // 구매 목록 - 현재 상점(currentShop)의 판매 아이템으로 채움
    // -----------------------------------------------
    private void BuildBuySlots()
    {
        foreach (ShopItemSlot slot in buySlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        buySlots.Clear();

        if (currentShop == null || buySlotPrefab == null) return;

        foreach (ItemData item in currentShop.ShopItems)
        {
            if (item == null) continue;

            ShopItemSlot slot = Instantiate(buySlotPrefab, buyGridContent);
            slot.Init(item, this);
            buySlots.Add(slot);
        }
    }

    // -----------------------------------------------
    // 판매 목록 - 상호작용한 플레이어의 equips로 채움. 판매/구매 후 갱신용으로 공개.
    // -----------------------------------------------
    public void RefreshSellSlots()
    {
        foreach (SellItemSlot slot in sellSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        sellSlots.Clear();

        if (currentEquips == null || sellSlotPrefab == null) return;

        foreach (GameObject equip in currentEquips)
        {
            if (equip == null) continue;

            ItemBasic item = equip.GetComponent<ItemBasic>();
            if (item == null || item.itemData == null) continue;

            SellItemSlot slot = Instantiate(sellSlotPrefab, sellGridContent);
            slot.Init(equip, item.itemData, this);
            sellSlots.Add(slot);
        }
    }

    // -----------------------------------------------
    // ShopItemSlot(구매 버튼)에서 호출
    // -----------------------------------------------
    public void BuyItem(ItemData itemData)
    {
        if (itemData == null || currentShop == null) return;
        if (GoldManager.Instance == null) return;

        bool success = GoldManager.Instance.SpendGold(itemData.price);
        if (!success)
        {
            Debug.Log($"[ShopManager] 골드 부족! '{itemData.itemName}' 구매 실패.");
            return;
        }

        Transform dropPoint = currentShop.DropPoint;
        if (dropPoint != null && SpawnManager.Instance != null)
        {
            SpawnManager.Instance.ItemSpawn(itemData.itemName, dropPoint);
            Debug.Log($"[ShopManager] '{itemData.itemName}' 구매 완료, 상점 앞에 생성.");
        }
        else
        {
            Debug.LogWarning("[ShopManager] DropPoint 또는 SpawnManager가 없어 구매한 아이템을 생성하지 못했습니다.");
        }
    }

    // -----------------------------------------------
    // SellItemSlot(판매 버튼)에서 호출
    // -----------------------------------------------
    public void SellItem(GameObject equippedItem, ItemData itemData)
    {
        if (equippedItem == null || itemData == null || currentEquips == null) return;
        if (GoldManager.Instance == null) return;

        for (int i = 0; i < currentEquips.Length; i++)
        {
            if (currentEquips[i] == equippedItem)
            {
                currentEquips[i] = null;
                break;
            }
        }

        GoldManager.Instance.AddGold(itemData.price);
        Photon.Pun.PhotonNetwork.Destroy(equippedItem);

        Debug.Log($"[ShopManager] '{itemData.itemName}' 판매 완료.");
        RefreshSellSlots();
    }
}
