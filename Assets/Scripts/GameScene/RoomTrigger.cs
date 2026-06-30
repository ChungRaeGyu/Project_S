using UnityEngine;

/// <summary>
/// 모든 종류의 방(StartRoom, Piece, ShopRoom, ReadyRoom, EndRoom)에 공통으로 사용하는 트리거.
/// RoomGenerator가 생성 시 SetStepIndex()로 순서 번호를 부여합니다.
/// 플레이어 진입/퇴장 시 RoundManager에 직접 알립니다.
/// 이 방의 Collider에 Is Trigger 체크 필요.
/// </summary>
public class RoomTrigger : MonoBehaviour
{
    // 이 방의 전체 순서 번호 (0부터 시작, RoomGenerator가 자동으로 부여)
    public int StepIndex { get; private set; } = -1;

    // -----------------------------------------------
    // RoomGenerator에서 생성 직후 호출 - 순서 번호 부여
    // -----------------------------------------------
    public void SetStepIndex(int index)
    {
        StepIndex = index;
    }

    // -----------------------------------------------
    // 플레이어가 이 방 안으로 들어왔을 때
    // -----------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnPlayerEnterRoom(other.gameObject, StepIndex);
    }

    // -----------------------------------------------
    // 플레이어가 이 방 밖으로 나갔을 때
    // -----------------------------------------------
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnPlayerExitRoom(other.gameObject, StepIndex);
    }
}
