using System.Collections;
using Photon.Pun;
using UnityEngine;

// PieceRoom 프리팹 안, MonsterRoom과 연결되는 문에 부착.
// RoomGenerator가 실제로 이 방 뒤에 MonsterRoom을 생성했을 때만 Activate()를 호출해 활성화한다.
// (같은 프리팹을 쓰는 다른 라운드의 Piece 방은 문 뒤에 아무것도 없으므로 비활성 상태로 남는다)
public class MonsterDoor : MonoBehaviourPun
{
    [Header("문 연출")]
    [SerializeField] private Transform doorChild;
    [SerializeField] private float openHeight = 4f;
    [SerializeField] private float openDuration = 1f;

    [Tooltip("라운드 시작 후 이 시간(초)이 지나면 문이 열린다. MonsterAI의 startRoomDoorDelay와 같은 값으로 맞춰야 함")]
    [SerializeField] private float openDelay = 60f;

    Vector3 closedPos;
    Vector3 openPos;
    bool activated;
    bool isOpen;

    void Awake()
    {
        if (doorChild != null)
        {
            closedPos = doorChild.position;
            openPos = closedPos + new Vector3(0f, openHeight, 0f);
        }
    }

    void OnDestroy()
    {
        if (RoundManager.Instance != null)
            RoundManager.Instance.OnRoundStarted -= HandleRoundStarted;
    }

    // RoomGenerator가 이 문 바로 뒤에 실제 MonsterRoom을 생성했을 때만 호출 (마스터에서 호출, RPC로 전체 클라이언트에 전파)
    public void Activate()
    {
        photonView.RPC(nameof(RPC_Activate), RpcTarget.All);
    }

    [PunRPC]
    void RPC_Activate()
    {
        if (activated) return;
        activated = true;

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnRoundStarted += HandleRoundStarted;
        else
            StartCoroutine(OpenAfterDelay()); // RoundManager가 없는 테스트 씬 폴백
    }

    void HandleRoundStarted()
    {
        RoundManager.Instance.OnRoundStarted -= HandleRoundStarted;
        StartCoroutine(OpenAfterDelay());
    }

    IEnumerator OpenAfterDelay()
    {
        yield return new WaitForSeconds(openDelay);

        if (isOpen) yield break;
        isOpen = true;

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            if (doorChild != null)
                doorChild.position = Vector3.Lerp(closedPos, openPos, t);
            yield return null;
        }

        if (doorChild != null)
            doorChild.position = openPos;
    }
}
