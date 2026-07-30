using UnityEngine;
using Unity.AI.Navigation;

// "미지 차원" 프리팹의 루트에 부착. FearDimensionController가 로컬로 Instantiate해서 사용한다.
// 이 인스턴스는 그 플레이어만을 위한 개인 공간이므로 별도 동기화가 필요 없다.
public class UnknownWorld : MonoBehaviour
{
    [Header("랜덤 배치 지점 (플레이어가 랜덤으로 놓일 위치)")]
    [SerializeField] private Transform[] randomSpawnPoints;

    [Header("클리커")]
    [SerializeField] private ClickerAI clickerPrefab;
    [SerializeField] private Transform clickerSpawnPoint;

    [Header("숨겨진 문")]
    [SerializeField] private HiddenDoor hiddenDoor;

    [Header("NavMesh (이 프리팹 전용, Instantiate 직후 로컬로 빌드)")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    ClickerAI activeClicker;

    // FearDimensionController.Start()에서 Instantiate 직후 1회 호출 (아직 비활성화 전).
    // 무거운 NavMesh 빌드를 여기서 미리 끝내둬서, 실제 진입(Enter) 시점엔 배치/스폰만 하면 되게 한다.
    public void Preload()
    {
        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();
    }

    // 공포 100 도달 시 호출 - 활성화된 상태에서 플레이어 배치 + 클리커 스폰
    public void Enter(FearDimensionController controller)
    {
        if (randomSpawnPoints != null && randomSpawnPoints.Length > 0)
        {
            Transform spawn = randomSpawnPoints[Random.Range(0, randomSpawnPoints.Length)];
            controller.transform.position = spawn.position;
            controller.transform.rotation = spawn.rotation;
        }

        if (clickerPrefab != null && clickerSpawnPoint != null)
        {
            activeClicker = Instantiate(clickerPrefab, clickerSpawnPoint.position, clickerSpawnPoint.rotation);
            activeClicker.Begin(controller.transform);
        }

        if (hiddenDoor != null)
            hiddenDoor.Initialize(controller);
    }

    // 탈출 시 호출 - 다음 재진입을 위해 클리커를 정리한다 (차원 자체는 비활성화만 되고 재사용됨)
    public void Leave()
    {
        if (activeClicker != null)
        {
            // Destroy() 시점에 NavMeshAgent가 즉시 NavMesh에서 분리되므로, 코루틴이 그 다음 틱에
            // remainingDistance 등을 읽으려다 예외가 나지 않도록 파괴 전에 먼저 멈춘다.
            activeClicker.StopChasing();
            Destroy(activeClicker.gameObject);
        }
        activeClicker = null;
    }
}
