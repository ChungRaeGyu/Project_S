using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// Piece, ShopRoom, ReadyRoom, StartRoom 문에 모두 사용 가능한 범용 문 스크립트.
/// 부모 오브젝트에 부착. 자식 오브젝트가 실제로 슬라이드되는 문.
/// OnInteract() 호출 시:
///   - 최초로 열릴 때 부모의 BoxCollider를 Is Trigger로 전환 (그 이후로는 계속 통과 가능)
///   - 자식 문 오브젝트 위로 슬라이드 (RPC로 전체 동기화, closeDelay 후 자동으로 다시 닫힘)
/// </summary>
public class PieceDoor : MonoBehaviourPun, IInteractable
{
    [Header("문 설정")]
    [SerializeField] private float openHeight = 4f;
    [SerializeField] private float openDuration = 1f;
    [SerializeField] private float closeDelay = 10f;

    [Header("필요 아이템 (비워두면 아이템 없이 상호작용만으로 열림)")]
    [SerializeField] private ItemData requiredItem;

    [Tooltip("체크하면 RoundManager의 현재 라운드 타이머가 끝나기 전까지 상호작용/버튼으로 열 수 없다 (ReadyRoom 문 등)")]
    [SerializeField] private bool requireRoundTimeUp = false;

    // [변경] 슬라이드할 자식 오브젝트
    [Header("슬라이드할 자식 문 오브젝트")]
    [SerializeField] private Transform doorChild;

    private Button linkedButton;
    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;
    private bool isMoving = false;

    // 이 문이 속한 방의 stepIndex. RoomGenerator가 생성 시 SetStepIndex()로 부여한다.
    private int stepIndex = -1;

    // 문은 이중 구조다: doorChild(보이는 문짝, RPC로 전체 클라이언트에 슬라이드 동기화)와
    // doorCollider(안 보이는 문틀 콜라이더, 물리적으로 통행을 막음 - 부모 오브젝트에 있음).
    // doorCollider.isTrigger는 절대 RPC로 동기화하지 않고 각 클라이언트가 로컬로만 관리한다.
    // 그래서 requiredItem이 있는 문은: 문짝이 열리는 건 다같이 보이지만(RPC), 실제로 통과 가능한지는
    // "본인이 그 상호작용에서 아이템 체크를 통과해서 OnInteract()가 이 줄까지 왔는가"에 달려있다 -
    // 즉 아이템 없는 플레이어는 자기 화면에서 doorCollider.isTrigger가 계속 false로 남아 못 지나간다.
    private BoxCollider doorCollider;

    // doorCollider와 같은 오브젝트에 붙어있는 반투명 벽 메시. isTrigger가 로컬로 켜질 때
    // (이 클라이언트가 통과 가능해질 때) 같이 로컬로 꺼줘야 안 보이는 게 맞다.
    private Renderer wallRenderer;

    // 이 문이 열리기 시작할 때 발생 (예: DoorManager가 StartRoom 문에 구독해서 라운드 시작 트리거로 사용)
    public event System.Action OnOpened;

    // Init()은 RoomGenerator -> DoorManager 등록 경로를 통해서만 호출되는데, 그 경로는 마스터에서만 실행된다.
    // closedPos/openPos/doorCollider는 모든 클라이언트에서 각자 필요하므로(RPC로 문 여닫힘이 전체 동기화되니까),
    // 마스터 전용 경로에 의존하지 않고 오브젝트가 생성되는 모든 클라이언트에서 실행되는 Awake()에서 초기화한다.
    // (예전엔 Init()에서만 계산해서, 클라이언트에서는 closedPos/openPos가 기본값(0,0,0)으로 남아
    //  문이 열릴 때 방 중앙(월드 원점)으로 순간이동하는 버그가 있었다.)
    void Awake()
    {
        if (doorChild != null)
        {
            closedPos = doorChild.position;
            openPos = closedPos + new Vector3(0f, openHeight, 0f);
        }
        else
        {
            Debug.LogWarning($"[PieceDoor] '{gameObject.name}' doorChild가 연결되지 않았습니다.");
        }

        doorCollider = GetComponent<BoxCollider>();
        wallRenderer = GetComponent<Renderer>();
    }

    // -----------------------------------------------
    // DoorManager에서 호출 - 버튼 연결
    // -----------------------------------------------
    public void Init(Button button)
    {
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

    // RoomGenerator가 생성 직후(마스터에서만) 호출 - 이 문이 속한 방의 순서 번호 부여.
    // RoomGenerator의 방 생성 코루틴 자체가 마스터에서만 실행되므로, 여기서 로컬로만 값을 설정하면
    // 클라이언트의 stepIndex/RoomTrigger.StepIndex/RoomGenerator.roomsByStepIndex가 전부 비어있게 되어
    // "라운드 끝나도 마스터만 이동하고 클라이언트는 그대로 남는" 버그가 생긴다. 그래서 RPC로 전파한다.
    public void SetStepIndex(int index)
    {
        photonView.RPC("RPC_SetStepIndex", RpcTarget.AllViaServer, index);
    }

    [PunRPC]
    private void RPC_SetStepIndex(int index)
    {
        stepIndex = index;

        // 같은 방 안의 RoomTrigger에도 동일한 stepIndex 전달 (플레이어 위치 추적용) - RoomTrigger는
        // PhotonView가 없는 순수 로컬 컴포넌트라 이 RPC 콜백을 통해서만 클라이언트마다 값을 채울 수 있다.
        RoomTrigger trigger = transform.root.GetComponentInChildren<RoomTrigger>();
        if (trigger != null)
            trigger.SetStepIndex(index);

        // 이 클라이언트 자신의 RoomGenerator에도 "stepIndex -> 방 오브젝트" 매핑을 등록
        if (RoomGenerator.Instance != null)
            RoomGenerator.Instance.RegisterRoomByStepIndex(index, transform.root.gameObject);
    }

    // -----------------------------------------------
    // 버튼 클릭 시 - RPC로 모든 클라이언트에 전달
    // -----------------------------------------------
    // 테스트용 버튼 경로는 라운드 타이머 체크를 거치지 않는다 (일부러 언제든 열 수 있게 둠).
    private void OnButtonClick()
    {
        Debug.Log($"[PieceDoor] 버튼 클릭됨. isOpen={isOpen} isMoving={isMoving}");
        if (isOpen || isMoving) return;

        photonView.RPC("RPC_OpenDoor", RpcTarget.AllViaServer);
    }

    // 라운드 타이머가 아직 돌아가는 중이면(시간 안 끝났으면) true - requireRoundTimeUp 체크된 문에만 적용
    private bool IsBlockedByRoundTimer()
    {
        if (!requireRoundTimeUp) return false;
        if (RoundManager.Instance == null) return false;
        if (!RoundManager.Instance.IsRoundRunning) return false; // 시간 다 됐으면 당연히 열림

        // 완전 탈락한 플레이어를 제외한 모든 플레이어가 이미 이 방에 모여있으면 시간이 남아있어도 조기 개방
        if (stepIndex >= 0 && RoundManager.Instance.AreAllActivePlayersInRoom(stepIndex))
            return false;

        Debug.Log($"[PieceDoor] '{gameObject.name}' 아직 라운드 시간이 남아있어 열 수 없습니다.");
        return true;
    }

    // -----------------------------------------------
    // 모든 클라이언트에서 실행되는 RPC
    // -----------------------------------------------
    [PunRPC]
    private void RPC_OpenDoor()
    {
        Debug.Log($"[PieceDoor] RPC_OpenDoor 수신. '{gameObject.name}'");
        if (isOpen || isMoving) return;
        OnOpened?.Invoke();
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
    // -----------------------------------------------
    public void OnInteract(GameObject[] obj = null)
    {
        Debug.Log($"[PieceDoor] OnInteract 호출. isOpen={isOpen} isMoving={isMoving}");
        if (isOpen || isMoving) return;
        if (IsBlockedByRoundTimer()) return;

        if (requiredItem != null)
        {
            GameObject matchedItem = FindRequiredItem(obj);
            if (matchedItem == null)
            {
                Debug.Log($"[PieceDoor] '{requiredItem.itemName}'이(가) 없어 문을 열 수 없습니다.");
                return;
            }

            // 상호작용으로 문을 여는 데 성공했으니 아이템 소모 (로컬 클라이언트가 자기 아이템을 파괴)
            PhotonNetwork.Destroy(matchedItem);
        }

        // 여기 도달했다는 건 이 클라이언트에서 아이템 체크(위)를 통과했다는 뜻이므로,
        // "이 클라이언트에서만" 통과 가능하게 로컬로 트리거 전환한다 (다른 클라이언트에는 전파 안 됨).
        if (doorCollider != null)
            doorCollider.isTrigger = true;

        // 반투명 벽도 같이 로컬로 꺼준다 - 통과 가능해졌는데 벽이 계속 보이면 안 되니까.
        if (wallRenderer != null)
            wallRenderer.enabled = false;

        photonView.RPC("RPC_OpenDoor", RpcTarget.AllViaServer);
    }

    // equips(장비 슬롯) 안에서 requiredItem과 같은 ItemData를 가진 오브젝트를 찾는다.
    GameObject FindRequiredItem(GameObject[] equips)
    {
        if (equips == null) return null;

        foreach (GameObject equip in equips)
        {
            if (equip == null) continue;

            ItemBasic item = equip.GetComponent<ItemBasic>();
            if (item != null && item.itemData == requiredItem)
                return equip;
        }

        return null;
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
