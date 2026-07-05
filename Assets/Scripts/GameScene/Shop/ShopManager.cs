using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 상점 UI를 관리하는 스크립트.
/// ItemDatabase에 등록된 아이템을 GridLayoutGroup에 자동으로 슬롯 생성.
/// 상호작용 키로 열기/닫기.
/// 새 아이템을 ItemDatabase에 추가하면 자동으로 슬롯이 늘어납니다.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("상점 UI")]
    [SerializeField] private GameObject shopUI;         // 상점 전체 UI 패널
    [SerializeField] private Transform gridContent;    // GridLayoutGroup이 붙은 오브젝트

    [Header("아이템 슬롯 프리팹")]
    [SerializeField] private ShopItemSlot slotPrefab;  // 슬롯 프리팹

    [Header("진열할 아이템 (Inspector에서 직접 추가)")]
    [SerializeField] private List<ItemData> shopItems = new List<ItemData>(); // [변경] 상점에 진열할 아이템만 수동으로 지정

    private bool isOpen = false;
    private readonly List<ShopItemSlot> spawnedSlots = new List<ShopItemSlot>();

    // 싱글톤
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
        // 시작 시 상점 UI 닫기
        if (shopUI != null)
            shopUI.SetActive(false);

        // 아이템 슬롯 자동 생성
        BuildShopSlots();
    }

    // -----------------------------------------------
    // ItemDatabase의 아이템을 GridLayout에 슬롯으로 자동 생성
    // 새 아이템 추가 시 자동으로 슬롯 늘어남
    // -----------------------------------------------
    private void BuildShopSlots()
    {
        if (slotPrefab == null)
        {
            Debug.LogWarning("[ShopManager] 슬롯 프리팹이 연결되지 않았습니다.");
            return;
        }

        // 기존 슬롯 전부 제거
        foreach (ShopItemSlot slot in spawnedSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        spawnedSlots.Clear();

        // [변경] shopItems 목록에 있는 아이템만 슬롯 생성
        foreach (ItemData item in shopItems)
        {
            if (item == null) continue;

            ShopItemSlot slot = Instantiate(slotPrefab, gridContent);
            slot.Init(item);
            spawnedSlots.Add(slot);
        }

        Debug.Log($"[ShopManager] 슬롯 {spawnedSlots.Count}개 생성 완료.");
    }

    // -----------------------------------------------
    // 상점 열기/닫기 토글
    // -----------------------------------------------
    private void ToggleShop()
    {
        if (isOpen)
            CloseShop();
        else
            OpenShop();
    }

    public void OpenShop()
    {
        if (shopUI == null) return;
        isOpen = true;
        shopUI.SetActive(true);
        Debug.Log("[ShopManager] 상점 열림.");
    }

    public void CloseShop()
    {
        if (shopUI == null) return;
        isOpen = false;
        shopUI.SetActive(false);
        Debug.Log("[ShopManager] 상점 닫힘.");
    }

    // -----------------------------------------------
    // 플레이어가 상호작용 키를 눌렀을 때 외부(HitRay 등)에서 호출
    // -----------------------------------------------
    public void Interact()
    {
        ToggleShop();
    }
}