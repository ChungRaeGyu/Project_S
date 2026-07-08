using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// Piece, ShopRoom, ReadyRoom, StartRoom 문에 모두 사용 가능한 범용 문 스크립트.
/// 부모 오브젝트에 부착. 자식 오브젝트가 실제로 슬라이드되는 문.
/// OnInteract() 호출 시:
///   - 부모의 BoxCollider 로컬로 비활성화 (다시 켜지지 않음)
///   - 자식 문 오브젝트 위로 슬라이드 (RPC로 전체 동기화)
/// </summary>
public class PieceDoor : MonoBehaviourPun, IInteractable
{
    [Header("문 설정")]
    [SerializeField] private float openHeight = 4f;
    [SerializeField] private float openDuration = 1f;
    [SerializeField] private float closeDelay = 10f;

    // [변경] 슬라이드할 자식 오브젝트
    [Header("슬라이드할 자식 문 오브젝트")]
    [SerializeField] private Transform doorChild;

    private Button linkedButton;
    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;
    private bool isMoving = false;

    // [변경] 부모의 BoxCollider 참조
    private BoxCollider doorCollider;

    // -----------------------------------------------
    // DoorManager에서 호출 - 버튼 연결 및 위치 초기화
    // -----------------------------------------------
    public void Init(Button button)
    {
        // [변경] 자식 기준으로 위치 초기화
        if (doorChild != null)
        {
            closedPos = doorChild.position;
            openPos = closedPos + new Vector3(0f, openHeight, 0f);
        }
        else
        {
            Debug.LogWarning($"[PieceDoor] '{gameObject.name}' doorChild가 연결되지 않았습니다.");
        }

        // [변경] 부모의 BoxCollider 캐싱
        doorCollider = GetComponent<BoxCollider>();

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
    // -----------------------------------------------
    private void OnButtonClick()
    {
        Debug.Log($"[PieceDoor] 버튼 클릭됨. isOpen={isOpen} isMoving={isMoving}");
        if (isOpen || isMoving) return;

        photonView.RPC("RPC_OpenDoor", RpcTarget.AllViaServer);
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
    // 자식 문 슬라이드
    // [변경] transform 대신 doorChild 기준으로 이동
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
            if (doorChild != null)
                doorChild.position = Vector3.Lerp(from, to, t); // [변경]
            yield return null;
        }

        if (doorChild != null)
            doorChild.position = to;
        isMoving = false;
    }

    // -----------------------------------------------
    // 상호작용 시 호출
    // [변경] 부모 BoxCollider 로컬로 끄기 + RPC로 자식 문 열기
    // -----------------------------------------------
    public void OnInteract(GameObject[] obj = null)
    {
        Debug.Log($"[PieceDoor] OnInteract 호출. isOpen={isOpen} isMoving={isMoving}");
        if (isOpen || isMoving) return;

        // [변경] 부모 BoxCollider 로컬로 비활성화 (다시 켜지지 않음)
        if (doorCollider != null)
            doorCollider.enabled = false;

        photonView.RPC("RPC_OpenDoor", RpcTarget.AllViaServer);
    }

    // -----------------------------------------------
    // 코드로 직접 열 때 사용 (캐릭터 시스템 연동용)
    // -----------------------------------------------
    public void OpenDoor()
    {
        if (isOpen || isMoving) return;
        photonView.RPC("RPC_OpenDoor", RpcTarget.AllViaServer);
    }

    public void OnInteract(GameObject[] obj=null)
    {
        Debug.Log($"[PieceDoor] 버튼 클릭됨. isOpen={isOpen} isMoving={isMoving}");
        if (isOpen || isMoving) return;

        photonView.RPC("RPC_OpenDoor", RpcTarget.AllViaServer); // [변경]
    }
}