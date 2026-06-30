using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// Piece, ShopRoom, ReadyRoom, StartRoom 문에 모두 사용 가능한 범용 문 스크립트.
/// RoomGenerator가 생성 직후 Init()을 호출해 버튼을 연결합니다.
/// </summary>
public class PieceDoor : MonoBehaviourPun
{
    [Header("문 설정")]
    [SerializeField] private float openHeight = 4f;
    [SerializeField] private float openDuration = 1f;
    [SerializeField] private float closeDelay = 10f;

    private Button linkedButton;
    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;
    private bool isMoving = false;

    // -----------------------------------------------
    // DoorManager에서 호출 - 버튼 연결 및 위치 초기화
    // -----------------------------------------------
    public void Init(Button button)
    {
        closedPos = transform.position;
        openPos = closedPos + new Vector3(0f, openHeight, 0f);

        linkedButton = button;

        if (linkedButton != null)
        {
            linkedButton.onClick.AddListener(OnButtonClick);
            Debug.Log($"[PieceDoor] '{gameObject.name}' 버튼 연결 성공.");
        }
        else
        {
            Debug.LogWarning($"[PieceDoor] '{gameObject.name}'에 연결된 버튼이 없습니다.");
        }
    }

    // -----------------------------------------------
    // 버튼 클릭 시 - RPC로 모든 클라이언트에 전달
    // [변경] RpcTarget.AllViaServer 사용 - 클라이언트도 마스터 경유로 안정적으로 전송
    // -----------------------------------------------
    private void OnButtonClick()
    {
        Debug.Log($"[PieceDoor] 버튼 클릭됨. isOpen={isOpen} isMoving={isMoving}");
        if (isOpen || isMoving) return;

        photonView.RPC("RPC_OpenDoor", RpcTarget.AllViaServer); // [변경]
    }

    // -----------------------------------------------
    // 모든 클라이언트에서 실행되는 RPC
    // -----------------------------------------------
    [PunRPC]
    private void RPC_OpenDoor()
    {
        Debug.Log($"[PieceDoor] RPC_OpenDoor 수신. '{gameObject.name}'");
        if (isOpen || isMoving) return;
        StartCoroutine(OpenThenClose());
    }

    // -----------------------------------------------
    // 열기 -> 대기 -> 닫기
    // -----------------------------------------------
    private IEnumerator OpenThenClose()
    {
        yield return StartCoroutine(SlideDoor(closedPos, openPos));
        yield return new WaitForSeconds(closeDelay);
        yield return StartCoroutine(SlideDoor(openPos, closedPos));

        if (linkedButton != null)
            linkedButton.interactable = true;
    }

    // -----------------------------------------------
    // 문 슬라이드
    // -----------------------------------------------
    private IEnumerator SlideDoor(Vector3 from, Vector3 to)
    {
        isMoving = true;
        isOpen = (to == openPos);

        if (linkedButton != null)
            linkedButton.interactable = false;

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.position = to;
        isMoving = false;
    }

    // -----------------------------------------------
    // 코드로 직접 열 때 사용 (캐릭터 시스템 연동용)
    // -----------------------------------------------
    public void OpenDoor()
    {
        if (isOpen || isMoving) return;
        photonView.RPC("RPC_OpenDoor", RpcTarget.AllViaServer);
    }
}