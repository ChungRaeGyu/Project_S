using UnityEngine;

/// <summary>
/// 씬에 배치되는 골드 아이템 스크립트.
/// 플레이어가 HitRay로 감지 후 상호작용 키를 누르면 Interact() 호출.
/// </summary>
public class GoldItem : MonoBehaviour
{
    [Header("골드 설정")]
    [SerializeField] private int goldAmount = 10; // 이 아이템이 주는 골드량

    private bool isCollected = false; // 중복 획득 방지

    // -----------------------------------------------
    // 플레이어가 상호작용 키를 눌렀을 때 외부에서 호출
    // -----------------------------------------------
    public void Interact()
    {
        if (isCollected) return;
        if (GoldManager.Instance == null) return;

        isCollected = true;
        GoldManager.Instance.AddGold(goldAmount);
        gameObject.SetActive(false);
    }
}