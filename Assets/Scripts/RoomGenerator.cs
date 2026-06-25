using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 로드 시 건물들을 순서대로 생성하는 스크립트.
/// 순서: StartRoom -> [Piece -> ShopRoom -> ReadyRoom] x N -> Piece -> EndRoom
/// 모든 프리팹은 Resources 폴더 안에 있어야 합니다.
/// </summary>
public class RoomGenerator : MonoBehaviour
{
    [Header("Resources 폴더 내 프리팹 이름")]
    [SerializeField] private string startRoomName = "StartRoom";
    [SerializeField] private string shopRoomName = "ShopRoom";
    [SerializeField] private string readyRoomName = "ReadyRoom";
    [SerializeField] private string endRoomName = "EndRoom";

    [Header("Piece 반복 횟수 (마지막 Piece 뒤에는 Shop/Ready 없음)")]
    [SerializeField] private int pieceCount = 3;

    // Piece 프리팹 이름 목록
    private readonly string[] pieceNames = { "Piece001", "Piece002", "Piece003" };

    [Header("건물 간 간격")]
    [SerializeField] private float padding = 1f;

    // 현재까지 누적된 X 위치
    private float currentX = 0f;

    // 생성된 오브젝트 목록 (씬 재시작 시 정리용)
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    // -----------------------------------------------
    // 씬 로드 시 자동 실행
    // -----------------------------------------------
    private void Start()
    {
        ClearSpawned();
        currentX = 0f;
        StartCoroutine(GenerateRooms());
    }

    // -----------------------------------------------
    // 건물 생성 순서 코루틴
    // -----------------------------------------------
    private IEnumerator GenerateRooms()
    {
        // 1) StartRoom - 원점(0,0,0)에 생성
        yield return StartCoroutine(SpawnBuilding(startRoomName, isFirst: true, doorType: DoorType.Start)); // [변경]

        // 2) Piece -> ShopRoom -> ReadyRoom 반복
        //    마지막 Piece 뒤에는 ShopRoom/ReadyRoom 대신 EndRoom 생성
        List<string> selectedPieces = GetRandomPieces(pieceCount);
        for (int i = 0; i < selectedPieces.Count; i++)
        {
            // Piece 생성 후 DoorManager에 등록
            yield return StartCoroutine(SpawnBuilding(selectedPieces[i], doorType: DoorType.Piece));

            bool isLastPiece = (i == selectedPieces.Count - 1);
            if (!isLastPiece)
            {
                // [변경] ShopRoom, ReadyRoom도 DoorManager에 등록
                yield return StartCoroutine(SpawnBuilding(shopRoomName, doorType: DoorType.Shop));
                yield return StartCoroutine(SpawnBuilding(readyRoomName, doorType: DoorType.Ready));
            }
            else
            {
                // 마지막 Piece 뒤에는 EndRoom
                yield return StartCoroutine(SpawnBuilding(endRoomName));
            }
        }

        Debug.Log("[RoomGenerator] 모든 건물 생성 완료.");
    }

    // -----------------------------------------------
    // 문 종류 구분용 열거형
    // [변경] Piece 외에 Shop, Ready 타입 추가
    // -----------------------------------------------
    private enum DoorType { None, Start, Piece, Shop, Ready } // [변경] Start 추가

    // -----------------------------------------------
    // 건물 하나를 생성하고 배치하는 함수
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
            // 첫 번째 건물은 중심을 원점에 맞춤
            spawnX = 0f;
            currentX = prefabWidth * 0.5f;
        }
        else
        {
            // 이전 건물 오른쪽 끝 + 간격 + 현재 건물 절반 너비
            spawnX = currentX + padding + prefabWidth * 0.5f;
            currentX = spawnX + prefabWidth * 0.5f;
        }

        Vector3 spawnPos = new Vector3(spawnX, 0f, 0f);
        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);

        if (obj != null)
        {
            spawnedObjects.Add(obj);

            // [변경] 문 종류에 따라 DoorManager에 등록
            if (DoorManager.Instance != null)
            {
                if (doorType == DoorType.Start) DoorManager.Instance.RegisterStartDoor(obj); // [변경]
                if (doorType == DoorType.Piece) DoorManager.Instance.RegisterPieceDoor(obj);
                if (doorType == DoorType.Shop) DoorManager.Instance.RegisterShopDoor(obj);
                if (doorType == DoorType.Ready) DoorManager.Instance.RegisterReadyDoor(obj);
            }

            Debug.Log($"[RoomGenerator] '{prefabName}' 생성 완료 | 위치: {spawnPos} | 너비: {prefabWidth:F2}");
        }

        yield return null;
    }

    // -----------------------------------------------
    // 프리팹의 X축 너비를 Renderer 또는 Collider로 계산
    // -----------------------------------------------
    private float GetPrefabWidth(GameObject prefab)
    {
        // 1순위: Renderer bounds 합산
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds.size.x;
        }

        // 2순위: Collider bounds 합산
        Collider[] colliders = prefab.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            foreach (Collider c in colliders)
                bounds.Encapsulate(c.bounds);
            return bounds.size.x;
        }

        // 크기를 감지 못한 경우 기본값 사용
        Debug.LogWarning($"[RoomGenerator] '{prefab.name}': 크기를 감지하지 못해 기본값 10f 사용.");
        return 10f;
    }

    // -----------------------------------------------
    // Piece 랜덤 선택 (중복 허용)
    // -----------------------------------------------
    private List<string> GetRandomPieces(int count)
    {
        List<string> result = new List<string>();
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pieceNames.Length);
            result.Add(pieceNames[idx]);
        }

        /* -- 중복 없는 랜덤으로 바꾸려면 아래 코드로 교체 --
        List<string> pool = new List<string>(pieceNames);
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        */

        return result;
    }

    // -----------------------------------------------
    // 생성된 오브젝트 전부 제거 (씬 재시작 등)
    // -----------------------------------------------
    private void ClearSpawned()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }
}