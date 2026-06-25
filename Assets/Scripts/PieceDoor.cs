using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Piece, ShopRoom, ReadyRoom 문에 모두 사용 가능한 범용 문 스크립트.
/// DoorManager에서 Init(button)을 호출해 버튼을 연결합니다.
/// 버튼 클릭 시 문이 위로 열리고, closeDelay초 후 자동으로 닫힙니다.
/// </summary>
public class PieceDoor : MonoBehaviour
{
    [Header("문 설정")]
    [SerializeField] private float openHeight = 4f;   // 문이 올라가는 높이
    [SerializeField] private float openDuration = 1f;   // 열리고 닫히는 데 걸리는 시간
    [SerializeField] private float closeDelay = 10f;  // 열린 후 닫히기까지 대기 시간

    private Button linkedButton;  // 연결된 UI 버튼
    private Vector3 closedPos;    // 닫힌 위치
    private Vector3 openPos;      // 열린 위치
    private bool isOpen = false;
    private bool isMoving = false;

    // -----------------------------------------------
    // DoorManager에서 생성 직후 호출해 버튼을 연결
    // -----------------------------------------------
    public void Init(Button button)
    {
        closedPos = transform.position;
        openPos = closedPos + new Vector3(0f, openHeight, 0f);

        linkedButton = button;

        if (linkedButton != null)
            linkedButton.onClick.AddListener(OnButtonClick);
        else
            Debug.LogWarning($"[PieceDoor] '{gameObject.name}'에 연결된 버튼이 없습니다.");
    }

    // -----------------------------------------------
    // 버튼 클릭 시 호출
    // -----------------------------------------------
    private void OnButtonClick()
    {
        if (isOpen || isMoving) return;
        StartCoroutine(OpenThenClose());
    }

    // -----------------------------------------------
    // 열기 -> 대기 -> 닫기 순서로 실행
    // -----------------------------------------------
    private IEnumerator OpenThenClose()
    {
        // 1) 문 열기
        yield return StartCoroutine(SlideDoor(closedPos, openPos));

        // 2) closeDelay초 대기
        yield return new WaitForSeconds(closeDelay);

        // 3) 문 닫기
        yield return StartCoroutine(SlideDoor(openPos, closedPos));

        // 4) 버튼 다시 활성화
        if (linkedButton != null)
            linkedButton.interactable = true;
    }

    // -----------------------------------------------
    // 문을 from 에서 to 로 슬라이드
    // -----------------------------------------------
    private IEnumerator SlideDoor(Vector3 from, Vector3 to)
    {
        isMoving = true;
        isOpen = (to == openPos);

        // 이동 중 버튼 비활성화 (중복 클릭 방지)
        if (linkedButton != null)
            linkedButton.interactable = false;

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            t = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.position = to;
        isMoving = false;
    }

    // -----------------------------------------------
    // 캐릭터 시스템 구현 후 코드로 직접 열 때 사용
    // -----------------------------------------------
    public void OpenDoor()
    {
        if (isOpen || isMoving) return;
        StartCoroutine(OpenThenClose());
    }
}