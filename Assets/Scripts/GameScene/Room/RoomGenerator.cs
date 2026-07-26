using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Unity.AI.Navigation;

/// <summary>
/// 씬 로드 시 마스터 클라이언트만 건물을 생성합니다.
/// 클라이언트는 PhotonNetwork.Instantiate 동기화로 자동 수신합니다.
/// 순서: StartRoom -> [Piece -> ShopRoom -> ReadyRoom] x N -> Piece -> EndRoom
/// </summary>
public class RoomGenerator : MonoBehaviourPunCallbacks
{
    [Header("Resources 폴더 내 프리팹 이름")]
    [SerializeField] private string startRoomName = "StartRoom";
    [SerializeField] private string monsterRoomName = "MonsterRoom";
    [SerializeField] private string shopRoomName = "ShopRoom";
    [SerializeField] private string readyRoomName = "ReadyRoom";
    [SerializeField] private string endRoomName = "EndRoom";

    [Header("Piece 반복 횟수 (마지막 Piece 뒤에는 Shop/Ready 없음)")]
    [SerializeField] private int pieceCount = 3;

    [Header("Piece 프리팹 이름 목록 (Inspector에서 추가/삭제 가능)")]
    [SerializeField] private string[] pieceNames = { "Piece0", "Piece1", "Piece2" }; // [변경] private readonly -> SerializeField

    [Header("건물 간 간격")]
    [SerializeField] private float padding = 1f;

    [Header("런타임 NavMesh (방이 절차적으로 생성되므로 에디터에서 미리 구운 NavMesh는 쓸 수 없음)")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    // 문이 열리거나 닫힐 때 다시 구워야 하므로 다른 스크립트(PieceDoor, MonsterDoor)가 호출할 수 있게 싱글턴으로 노출
    public static RoomGenerator Instance { get; private set; }

    private float currentX = 0f;
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    // [변경] 방 생성 순서를 나타내는 전역 카운터 (0부터 시작, 모든 방 종류 포함)
    private int stepIndex = 0;

    // [변경] 어떤 Piece가 몇 라운드에 해당하는지 RoundManager에 알리기 위한 매핑
    // key: 라운드 번호(1부터), value: 해당 Piece의 stepIndex
    private readonly Dictionary<int, int> roundToPieceStep = new Dictionary<int, int>();

    // Piece 방에 붙어있는 MonsterZone을 생성 순서대로 모아둠 - 생성 완료 후 MonsterAI에 전달
    private readonly List<MonsterAI.Zone> monsterZones = new List<MonsterAI.Zone>();

    // stepIndex -> 생성된 방 오브젝트. RoundManager가 "라운드 N의 ReadyRoom" 같은 특정 방을 찾을 때 사용.
    // (MonsterRoom은 stepIndex가 없어서 여기 포함되지 않음)
    private readonly Dictionary<int, GameObject> roomsByStepIndex = new Dictionary<int, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // stepIndex로 생성된 방 오브젝트를 찾는다 (없으면 null).
    public GameObject GetRoomByStepIndex(int stepIndex)
    {
        return roomsByStepIndex.TryGetValue(stepIndex, out GameObject room) ? room : null;
    }

    // 문이 열리거나 닫혀서 통로 상태가 바뀔 때마다 호출 - 마스터에서만 실제로 다시 굽는다.
    // (MonsterAI가 마스터에서만 동작하므로 이 갱신도 마스터에서만 의미 있음)
    public void RebuildNavMesh()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (navMeshSurface == null) return;

        navMeshSurface.BuildNavMesh();
    }

    // -----------------------------------------------
    // 씬 로드 시 자동 실행 - 마스터만 생성
    // -----------------------------------------------
    private void Start()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        ClearSpawned();
        currentX = 0f;
        stepIndex = 0;            // [변경]
        roundToPieceStep.Clear(); // [변경]
        monsterZones.Clear();
        roomsByStepIndex.Clear();
        StartCoroutine(GenerateRooms());
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[RoomGenerator] 새 마스터: {newMasterClient.NickName}");
    }

    // -----------------------------------------------
    // 건물 생성 순서 코루틴
    // -----------------------------------------------
    private IEnumerator GenerateRooms()
    {
        yield return StartCoroutine(SpawnBuilding(startRoomName, isFirst: true, doorType: DoorType.Start));

        List<string> selectedPieces = GetRandomPieces(pieceCount);
        for (int i = 0; i < selectedPieces.Count; i++)
        {
            int roundNumber = i + 1; // [변경] 이 Piece가 몇 라운드인지

            GameObject spawnedPiece = null;
            yield return StartCoroutine(SpawnBuilding(selectedPieces[i], doorType: DoorType.Piece, onSpawned: obj => spawnedPiece = obj));

            // [변경] 방금 생성한 Piece의 stepIndex를 라운드 번호와 매핑
            roundToPieceStep[roundNumber] = stepIndex - 1;

            // 첫 Piece 방 안에 미리 찍어둔 MonsterSpawnPoint 위치에 MonsterRoom 생성 (메인 통로에서 벗어난 곁가지)
            if (i == 0)
                yield return StartCoroutine(SpawnMonsterRoom(spawnedPiece));

            bool isLastPiece = (i == selectedPieces.Count - 1);
            if (!isLastPiece)
            {
                yield return StartCoroutine(SpawnBuilding(shopRoomName, doorType: DoorType.Shop));
                yield return StartCoroutine(SpawnBuilding(readyRoomName, doorType: DoorType.Ready));
            }
            else
            {
                yield return StartCoroutine(SpawnBuilding(endRoomName));
            }
        }

        // 모든 방(몬스터룸 포함)이 생성된 뒤 NavMesh를 처음으로 굽는다.
        if (navMeshSurface == null)
            Debug.LogWarning("[RoomGenerator] navMeshSurface가 연결되지 않아 몬스터가 NavMesh 위에 있지 못할 수 있습니다.");
        RebuildNavMesh();

        // [변경] RoundManager에 매핑 정보 전달
        if (RoundManager.Instance != null)
            RoundManager.Instance.SetRoundPieceStepMap(roundToPieceStep);

        if (MonsterAI.Instance != null)
        {
            // Piece 방 순서대로 모아둔 MonsterZone 목록을 몬스터에게 전달
            MonsterAI.Instance.SetZones(monsterZones.ToArray());

            // NavMesh가 방금 새로 만들어졌으니 몬스터를 그 위에 다시 배치하고 FSM을 시작시킨다.
            MonsterAI.Instance.OnNavMeshReady();
        }

        Debug.Log("[RoomGenerator] 모든 건물 생성 완료.");
    }

    private enum DoorType { None, Start, Piece, Shop, Ready }

    // -----------------------------------------------
    // 건물 생성 후 DoorManager 등록은 마스터만 처리
    // [변경] DoorManager 등록을 마스터에서만 하도록 제한
    // -----------------------------------------------
    private IEnumerator SpawnBuilding(string prefabName, bool isFirst = false, DoorType doorType = DoorType.None, System.Action<GameObject> onSpawned = null)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabName);
        if (prefab == null)
        {
            Debug.LogError($"[RoomGenerator] '{prefabName}' 프리팹을 Resources 폴더에서 찾을 수 없습니다.");
            yield break;
        }

        float prefabWidth = GetPrefabWidth(prefab);

        float spawnX;
        if (isFirst)
        {
            spawnX = 0f;
            currentX = prefabWidth * 0.5f;
        }
        else
        {
            spawnX = currentX + padding + prefabWidth * 0.5f;
            currentX = spawnX + prefabWidth * 0.5f;
        }

        Vector3 spawnPos = new Vector3(spawnX, 0f, 0f);
        GameObject obj = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity);

        if (obj != null)
        {
            spawnedObjects.Add(obj);

            // 문 종류에 따라 DoorManager에 등록 (PieceDoor를 찾아서 전달)
            if (DoorManager.Instance != null)
            {
                PieceDoor door = obj.GetComponentInChildren<PieceDoor>();
                if (door != null)
                {
                    if (doorType == DoorType.Start) DoorManager.Instance.RegisterStartDoor(door);
                    if (doorType == DoorType.Piece) DoorManager.Instance.RegisterPieceDoor(door);
                    if (doorType == DoorType.Shop) DoorManager.Instance.RegisterShopDoor(door);
                    if (doorType == DoorType.Ready) DoorManager.Instance.RegisterReadyDoor(door);
                }
            }

            // [변경] RoomTrigger에 이 방의 순서 번호(stepIndex) 부여
            RoomTrigger trigger = obj.GetComponentInChildren<RoomTrigger>();
            if (trigger != null)
                trigger.SetStepIndex(stepIndex);

            roomsByStepIndex[stepIndex] = obj;

            // Piece 방이면 MonsterZone을 찾아 몬스터 순찰 구역 목록에 등록 (생성 순서 = 순찰 순서)
            if (doorType == DoorType.Piece)
            {
                MonsterZone zone = obj.GetComponentInChildren<MonsterZone>();
                if (zone != null)
                {
                    monsterZones.Add(new MonsterAI.Zone
                    {
                        zoneName = prefabName,
                        patrolPoints = zone.patrolPoints,
                        zoneCenter = zone.zoneCenter,
                        patrolRadius = zone.patrolRadius
                    });
                }
            }

            stepIndex++; // [변경] 다음 방을 위해 1 증가

            Debug.Log($"[RoomGenerator] '{prefabName}' 생성 완료 | 위치: {spawnPos} | stepIndex: {stepIndex - 1}");

            onSpawned?.Invoke(obj);
        }

        yield return null;
    }

    // 첫 Piece 방 안의 MonsterSpawnPoint 위치에 MonsterRoom을 생성한다.
    // 메인 통로 배치(currentX 누적)와는 별개로, 지정된 좌표에 그대로 생성된다.
    private IEnumerator SpawnMonsterRoom(GameObject pieceRoom)
    {
        if (pieceRoom == null)
        {
            Debug.LogWarning("[RoomGenerator] 첫 Piece 방을 찾지 못해 MonsterRoom을 생성하지 못했습니다.");
            yield break;
        }

        MonsterSpawnPoint spawnPoint = pieceRoom.GetComponentInChildren<MonsterSpawnPoint>();
        if (spawnPoint == null)
        {
            Debug.LogWarning($"[RoomGenerator] '{pieceRoom.name}'에 MonsterSpawnPoint가 없어 MonsterRoom을 생성하지 못했습니다.");
            yield break;
        }

        GameObject prefab = Resources.Load<GameObject>(monsterRoomName);
        if (prefab == null)
        {
            Debug.LogError($"[RoomGenerator] '{monsterRoomName}' 프리팹을 Resources 폴더에서 찾을 수 없습니다.");
            yield break;
        }

        // MonsterSpawnPoint 위치를 "연결 지점(문)"으로 보고, 방의 깊이(로컬 Z) 절반만큼 forward 방향으로 밀어서
        // 중심이 아니라 벽이 그 지점에 맞닿도록 한다. (Piece 방 벽과 안 겹치게)
        float halfDepth = GetPrefabBounds(prefab).size.z * 0.5f;
        Vector3 spawnPos = spawnPoint.transform.position + spawnPoint.transform.forward * halfDepth;

        GameObject obj = PhotonNetwork.Instantiate(monsterRoomName, spawnPos, spawnPoint.transform.rotation);
        if (obj != null)
        {
            spawnedObjects.Add(obj);
            Debug.Log($"[RoomGenerator] '{monsterRoomName}' 생성 완료 | 위치: {spawnPos}");

            // 이 Piece 방에만 실제로 MonsterRoom이 생겼으므로, 이 방의 MonsterDoor만 활성화한다.
            MonsterDoor door = pieceRoom.GetComponentInChildren<MonsterDoor>();
            if (door != null)
                door.Activate();
            else
                Debug.LogWarning($"[RoomGenerator] '{pieceRoom.name}'에 MonsterDoor가 없어 몬스터 문 연출을 활성화하지 못했습니다.");
        }

        yield return null;
    }

    private float GetPrefabWidth(GameObject prefab)
    {
        return GetPrefabBounds(prefab).size.x;
    }

    private Bounds GetPrefabBounds(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds;
        }

        Collider[] colliders = prefab.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            foreach (Collider c in colliders)
                bounds.Encapsulate(c.bounds);
            return bounds;
        }

        Debug.LogWarning($"[RoomGenerator] '{prefab.name}': 크기를 감지하지 못해 기본값 10f 사용.");
        return new Bounds(Vector3.zero, Vector3.one * 10f);
    }

    private List<string> GetRandomPieces(int count)
    {
        List<string> result = new List<string>();
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pieceNames.Length);
            result.Add(pieceNames[idx]);
        }
        return result;
    }

    private void ClearSpawned()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                PhotonNetwork.Destroy(obj);
        }
        spawnedObjects.Clear();
    }
}