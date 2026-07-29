using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

// 플레이어 프리팹에 부착. 공포 스탯 시스템(다른 담당자 작업)이 공포 100 달성 시 EnterDimension()을 호출하면
// 이 플레이어만 로컬로 "미지 차원"에 들어간다. 네트워크 동기화가 전혀 없으므로 다른 플레이어에게는
// 아무 일도 일어나지 않는다 - 정지 화면처럼 마지막 위치에 그대로 서 있는 것처럼 보인다.
public class FearDimensionController : MonoBehaviour
{
    [Header("미지 차원 프리팹")]
    [SerializeField] private UnknownWorld unknownWorldPrefab;

    [Tooltip("던전이 아무리 커져도 절대 안 겹치도록 멀리 띄워서 생성할 좌표 (기본: 지하 2000유닛). " +
             "프리팹 자체에 저장된 위치는 무시하고 항상 이 좌표에 생성된다.")]
    [SerializeField] private Vector3 dimensionWorldPosition = new Vector3(0f, -2000f, 0f);

    Vector3 realWorldPosition;
    Quaternion realWorldRotation;
    UnknownWorld preloadedWorld;
    PhotonView pv;
    string realWorldTag;

    public bool IsInDimension { get; private set; }

    void Awake()
    {
        pv = GetComponent<PhotonView>();

        // 내 캐릭터가 아니면(다른 플레이어의 복제본) 미리 생성할 필요가 없다.
        // CharacterManager와 동일한 패턴: pv.IsMine이 아니면 이 컴포넌트를 통째로 비활성화.
        if (pv != null && !pv.IsMine)
            enabled = false;
    }

    void Start()
    {
        // 미지 차원은 규모가 커서 공포 100 도달 시점에 그때 생성하면 순간적으로 끊겨 보일 수 있다.
        // 그래서 플레이어가 생성될 때 미리 만들어서 NavMesh까지 구워두고 비활성화해둔다.
        // 실제 진입 시점에는 활성화 + 배치만 하면 되므로 훨씬 가볍다.
        preloadedWorld = Instantiate(unknownWorldPrefab, dimensionWorldPosition, Quaternion.identity);
        preloadedWorld.Preload();
        preloadedWorld.gameObject.SetActive(false);
    }

    // 공포 스탯 시스템에서 호출하는 진입점.
    public void EnterDimension()
    {
        if (IsInDimension) return;
        IsInDimension = true;

        realWorldPosition = transform.position;
        realWorldRotation = transform.rotation;

        SetNetworkSyncEnabled(false);

        // MonsterAI가 "Player" 태그로 씬을 스캔해서 탐지하므로, 태그를 잠깐 빼서
        // 미지 차원에 있는 동안 던전의 몬스터가 이 플레이어를 아예 인식하지 못하게 한다.
        realWorldTag = gameObject.tag;
        gameObject.tag = "Untagged";

        preloadedWorld.gameObject.SetActive(true);
        preloadedWorld.Enter(this);
    }

    // HiddenDoor -> UnknownWorld를 거쳐 호출됨 (탈출 완료)
    public void ExitDimension()
    {
        if (!IsInDimension) return;

        preloadedWorld.Leave();
        preloadedWorld.gameObject.SetActive(false);

        transform.position = realWorldPosition;
        transform.rotation = realWorldRotation;

        gameObject.tag = realWorldTag;
        SetNetworkSyncEnabled(true);
        IsInDimension = false;
    }

    // 미지 차원에 있는 동안 PhotonView 자체를 꺼서, 위치를 포함한 어떤 것도 다른 클라이언트에 전파되지 않게 한다.
    void SetNetworkSyncEnabled(bool value)
    {
        if (pv != null)
            pv.enabled = value;
    }
}
