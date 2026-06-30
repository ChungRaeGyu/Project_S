using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 씬 로드 시 마스터 클라이언트만 건물을 생성합니다.
/// 클라이언트는 PhotonNetwork.Instantiate 동기화로 자동 수신합니다.
/// 순서: StartRoom -> [Piece -> ShopRoom -> ReadyRoom] x N -> Piece -> EndRoom
/// </summary>
public class RoomGenerator : MonoBehaviourPunCallbacks
{
    [Header("Resources 폴더 내 프리팹 이름")]
    [SerializeField] private string startRoomName = "StartRoom";
    [SerializeField] private string shopRoomName = "ShopRoom";
    [SerializeField] private string readyRoomName = "ReadyRoom";
    [SerializeField] private string endRoomName = "EndRoom";

    [Header("Piece 반복 횟수 (마지막 Piece 뒤에는 Shop/Ready 없음)")]
    [SerializeField] private int pieceCount = 3;

    [Header("Piece 프리팹 이름 목록 (Inspector에서 추가/삭제 가능)")]
    [SerializeField] private string[] pieceNames = { "Piece0", "Piece1", "Piece2" }; // [변경] private readonly -> SerializeField

    [Header("건물 간 간격")]
    [SerializeField] private float padding = 1f;

    private float currentX = 0f;
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    // [변경] 방 생성 순서를 나타내는 전역 카운터 (0부터 시작, 모든 방 종류 포함)
    private int stepIndex = 0;

    // [변경] 어떤 Piece가 몇 라운드에 해당하는지 RoundManager에 알리기 위한 매핑
    // key: 라운드 번호(1부터), value: 해당 Piece의 stepIndex
    private readonly Dictionary<int, int> roundToPieceStep = new Dictionary<int, int>();

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

            yield return StartCoroutine(SpawnBuilding(selectedPieces[i], doorType: DoorType.Piece));

            // [변경] 방금 생성한 Piece의 stepIndex를 라운드 번호와 매핑
            roundToPieceStep[roundNumber] = stepIndex - 1;

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

        // [변경] RoundManager에 매핑 정보 전달
        if (RoundManager.Instance != null)
            RoundManager.Instance.SetRoundPieceStepMap(roundToPieceStep);

        Debug.Log("[RoomGenerator] 모든 건물 생성 완료.");
    }

    private enum DoorType { None, Start, Piece, Shop, Ready }

    // -----------------------------------------------
    // 건물 생성 후 DoorManager 등록은 마스터만 처리
    // [변경] DoorManager 등록을 마스터에서만 하도록 제한
    // -----------------------------------------------
    private IEnumerator SpawnBuilding(string prefabName, bool isFirst = false, DoorType doorType = DoorType.None)
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

            stepIndex++; // [변경] 다음 방을 위해 1 증가

            Debug.Log($"[RoomGenerator] '{prefabName}' 생성 완료 | 위치: {spawnPos} | stepIndex: {stepIndex - 1}");
        }

        yield return null;
    }

    private float GetPrefabWidth(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds.size.x;
        }

        Collider[] colliders = prefab.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            foreach (Collider c in colliders)
                bounds.Encapsulate(c.bounds);
            return bounds.size.x;
        }

        Debug.LogWarning($"[RoomGenerator] '{prefab.name}': 크기를 감지하지 못해 기본값 10f 사용.");
        return 10f;
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