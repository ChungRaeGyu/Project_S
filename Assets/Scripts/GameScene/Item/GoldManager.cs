using UnityEngine;
using TMPro;

/// <summary>
/// 플레이어 개인 골드를 로컬에서 관리하는 스크립트.
/// 골드 아이템과 상호작용 시 AddGold()를 호출하면 자동으로 골드가 추가됨.
/// 상점 구매 시 SpendGold()를 호출해 골드 차감.
/// </summary>
public class GoldManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text goldText; // 현재 골드를 표시할 TMP 텍스트

    [Header("시작 골드")]
    [SerializeField] private int startingGold = 0;

    // 현재 골드
    public int CurrentGold { get; private set; } = 0;

    // 싱글톤 (로컬 플레이어 한 명만 관리)
    public static GoldManager Instance { get; private set; }

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
        // 시작 골드 설정
        CurrentGold = startingGold;
        UpdateGoldText();
    }

    // -----------------------------------------------
    // 골드 추가 - 돈 아이템 습득 시 호출
    // -----------------------------------------------
    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        CurrentGold += amount;
        UpdateGoldText();
        Debug.Log($"[GoldManager] 골드 +{amount} 획득. 현재 골드: {CurrentGold}");
    }

    // -----------------------------------------------
    // 골드 차감 - 상점 구매 시 호출
    // 골드가 부족하면 false 반환
    // -----------------------------------------------
    public bool SpendGold(int amount)
    {
        if (amount <= 0) return false;

        if (CurrentGold < amount)
        {
            Debug.Log($"[GoldManager] 골드 부족. 필요: {amount}, 보유: {CurrentGold}");
            return false;
        }

        CurrentGold -= amount;
        UpdateGoldText();
        Debug.Log($"[GoldManager] 골드 -{amount} 사용. 현재 골드: {CurrentGold}");
        return true;
    }

    // -----------------------------------------------
    // 골드가 충분한지 확인만 할 때 사용 (차감 X)
    // -----------------------------------------------
    public bool HasEnoughGold(int amount)
    {
        return CurrentGold >= amount;
    }

    // -----------------------------------------------
    // TMP 텍스트 갱신
    // -----------------------------------------------
    private void UpdateGoldText()
    {
        if (goldText == null) return;
        goldText.text = $"{CurrentGold:N0} G"; // 예: 1,000 G
    }
}
