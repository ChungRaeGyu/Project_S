using UnityEngine;

/// <summary>
/// 아이템 하나의 데이터를 담는 ScriptableObject.
/// 우클릭 -> Create -> ItemDatabase -> Item 으로 생성 가능.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "ItemDatabase/Item")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public int    itemID;       // 아이템 고유 ID
    public string itemName;     // 아이템 이름
    [TextArea(2, 5)]
    public string description;  // 아이템 설명
    public int    price;        // 구매 가격 (골드)
    public Sprite icon;         // 아이템 아이콘 이미지

    public Vector3[] position;
    public Vector3[] rotation;
}
