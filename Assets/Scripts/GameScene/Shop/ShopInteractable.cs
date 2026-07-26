using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ShopRoom 프리팹 안, 실제 상점 기계에 부착. 플레이어가 바라보고 상호작용하면
/// 공용 ShopManager UI를 이 상점 기준(판매 아이템 목록 + 구매품 드롭 위치)으로 연다.
/// ShopRoom이 라운드마다 여러 개 생성되므로, 이 컴포넌트는 그 방 하나에 고유한 데이터만 들고 있다.
/// </summary>
public class ShopInteractable : MonoBehaviour, IInteractable
{
    [Header("이 상점에서 파는 아이템")]
    [SerializeField] private List<ItemData> shopItems = new List<ItemData>();

    [Header("구매한 아이템이 생성될 위치 (상점 기계 앞 바닥)")]
    [SerializeField] private Transform dropPoint;

    public IReadOnlyList<ItemData> ShopItems => shopItems;
    public Transform DropPoint => dropPoint;

    public void OnInteract(GameObject[] obj = null)
    {
        if (ShopManager.Instance == null) return;

        if (ShopManager.Instance.IsOpenFor(this))
            ShopManager.Instance.Close();
        else
            ShopManager.Instance.Open(this, obj);
    }
}
