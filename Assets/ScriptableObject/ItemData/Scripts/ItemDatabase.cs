using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 아이템을 한 곳에서 관리하는 ScriptableObject 데이터베이스.
/// 우클릭 -> Create -> ItemDatabase -> Database 로 생성 가능.
/// ID로 아이템을 빠르게 검색할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "ItemDatabase/Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("아이템 목록 (Inspector에서 추가/삭제)")]
    public List<ItemData> items = new List<ItemData>();

    // 빠른 검색을 위한 캐시 딕셔너리 (ID -> ItemData)
    private Dictionary<int, ItemData> itemCache;

    // -----------------------------------------------
    // 캐시 초기화 - 처음 검색 시 자동으로 빌드
    // -----------------------------------------------
    private void BuildCache()
    {
        itemCache = new Dictionary<int, ItemData>();
        foreach (ItemData item in items)
        {
            if (item == null) continue;

            if (itemCache.ContainsKey(item.itemID))
                Debug.LogWarning($"[ItemDatabase] 중복된 아이템 ID: {item.itemID} ({item.itemName})");
            else
                itemCache[item.itemID] = item;
        }
    }

    // -----------------------------------------------
    // ID로 아이템 검색
    // -----------------------------------------------
    public ItemData GetItemByID(int id)
    {
        if (itemCache == null) BuildCache();

        if (itemCache.TryGetValue(id, out ItemData item))
            return item;

        Debug.LogWarning($"[ItemDatabase] ID {id}에 해당하는 아이템을 찾을 수 없습니다.");
        return null;
    }

    // -----------------------------------------------
    // 이름으로 아이템 검색
    // -----------------------------------------------
    public ItemData GetItemByName(string name)
    {
        foreach (ItemData item in items)
        {
            if (item != null && item.itemName == name)
                return item;
        }

        Debug.LogWarning($"[ItemDatabase] '{name}' 이름의 아이템을 찾을 수 없습니다.");
        return null;
    }

    // -----------------------------------------------
    // 에디터에서 아이템 추가/삭제 시 캐시 갱신
    // -----------------------------------------------
    private void OnValidate()
    {
        BuildCache();
    }
}
