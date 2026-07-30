using System.Collections.Generic;
using UnityEngine;
using TMPro;          // [변경] TMP 사용
using Photon.Pun;

/// <summary>
/// 라운드 진행을 관리하는 스크립트.
/// 마스터가 RoundStart()를 호출하면 타이머가 시작되고 RPC로 모든 클라이언트에 동기화.
/// 플레이어가 방(RoomTrigger) 진입/퇴장 시 RoundManager에 직접 등록/해제.
/// 타이머 종료 시 "현재 라운드 Piece방의 stepIndex 이하"에 있는 플레이어 전부 OnRoundTimeUp() 호출.
/// (StartRoom, 이전 ShopRoom/ReadyRoom 등 포함)
/// </summary>
public class RoundManager : MonoBehaviourPun
{
    [Header("라운드 설정")]
    [SerializeField] private int totalRounds = 3;
    [SerializeField] private float roundTime = 60f;

    // [변경] 타이머를 표시할 TMP 텍스트 (Inspector에서 연결)
    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    [Tooltip("같은 플레이어가 라운드 시간 초과를 두 번째 이상 겪으면 켜지는 검은 화면 패널")]
    [SerializeField] private GameObject blackScreenPanel;

    [Tooltip("탈락(사망)하지 않은 모든 플레이어가 EndRoom에 모이면 띄울 UI 이미지")]
    [SerializeField] private GameObject endRoomClearImage;

    public int CurrentRound { get; private set; } = 0;
    public float RemainingTime { get; private set; } = 0f;

    // 라운드 타이머가 아직 카운트다운 중인지. PieceDoor가 "시간 끝나기 전엔 못 열게" 체크할 때 사용.
    public bool IsRoundRunning => isRunning;

    private bool isRunning = false;

    // 플레이어별 현재 위치한 방의 stepIndex (map에 없으면 기본값 0으로 간주 = StartRoom)
    private readonly Dictionary<GameObject, int> playerStepMap = new Dictionary<GameObject, int>();

    // 씬에 있는 모든 플레이어 목록
    private readonly List<GameObject> allPlayers = new List<GameObject>();

    // 라운드 시간 초과로 처리된 적 있는 플레이어 집합 - 최초 1회는 텔레포트, 그다음부터는 검은 화면
    private readonly HashSet<GameObject> hasBeenLateOnce = new HashSet<GameObject>();

    // EndRoom 클리어 UI를 이미 띄웠는지 - 중복 RPC 방지
    private bool endRoomCleared = false;

    // [변경] 라운드 번호 -> 해당 라운드 Piece방의 stepIndex
    // RoomGenerator가 생성 완료 후 채워줌
    private readonly Dictionary<int, int> roundToPieceStep = new Dictionary<int, int>();

    public static RoundManager Instance { get; private set; }

    // 라운드 타이머가 종료될 때 로컬에서 발생 (네트워크 이벤트 아님 - 각 클라이언트가 자신의 로컬 타이머로 개별 발생)
    public event System.Action OnRoundEnded;

    // 라운드가 시작될 때 로컬에서 발생 (RPC_RoundStart는 모든 클라이언트에서 실행되므로 사실상 전체에 전파됨)
    public event System.Action OnRoundStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (blackScreenPanel != null)
            blackScreenPanel.SetActive(false);

        if (endRoomClearImage != null)
            endRoomClearImage.SetActive(false);
    }

    private void Update()
    {
        if (!isRunning) return;

        RemainingTime -= Time.deltaTime;

        if (RemainingTime <= 0f)
        {
            RemainingTime = 0f;
            isRunning = false;
            UpdateTimerText(); // [변경] 0:00 표시 후 종료
            HandleTimeUp();
            return;
        }

        UpdateTimerText(); // [변경] 매 프레임 텍스트 갱신
    }

    // -----------------------------------------------
    // [변경] 남은 시간을 분:초 형식으로 텍스트에 표시
    // -----------------------------------------------
    private void UpdateTimerText()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(RemainingTime / 60f);
        int seconds = Mathf.FloorToInt(RemainingTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // -----------------------------------------------
    // RoomGenerator가 맵 생성 완료 후 호출 (마스터에서만 실행됨).
    // RoomGenerator의 방 생성 코루틴 자체가 마스터에서만 도는데, 여기서 로컬로만 채우면
    // 클라이언트의 roundToPieceStep은 영원히 비어있게 되어 HandleTimeUp()이 매번 조용히 실패한다
    // (라운드가 끝나도 마스터만 이동하고 클라이언트는 그대로 남는 버그의 원인이었다).
    // 그래서 RPC로 모든 클라이언트에 매핑을 전파한다.
    // -----------------------------------------------
    public void SetRoundPieceStepMap(Dictionary<int, int> map)
    {
        int[] rounds = new int[map.Count];
        int[] steps = new int[map.Count];
        int i = 0;
        foreach (var kv in map)
        {
            rounds[i] = kv.Key;
            steps[i] = kv.Value;
            i++;
        }

        photonView.RPC("RPC_SetRoundPieceStepMap", RpcTarget.AllViaServer, rounds, steps);
    }

    [PunRPC]
    private void RPC_SetRoundPieceStepMap(int[] rounds, int[] steps)
    {
        roundToPieceStep.Clear();
        for (int i = 0; i < rounds.Length; i++)
            roundToPieceStep[rounds[i]] = steps[i];

        Debug.Log($"[RoundManager] 라운드-Piece stepIndex 매핑 수신 완료. (총 {roundToPieceStep.Count}개)");
    }

    // -----------------------------------------------
    // 외부에서 라운드 시작 시 호출 (마스터만 호출)
    // -----------------------------------------------
    public void RoundStart()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[RoundManager] RoundStart()는 마스터만 호출할 수 있습니다.");
            return;
        }

        int nextRound = CurrentRound + 1;

        if (nextRound > totalRounds)
        {
            Debug.Log("[RoundManager] 모든 라운드가 종료됐습니다.");
            return;
        }

        photonView.RPC("RPC_RoundStart", RpcTarget.All, nextRound, roundTime);
    }

    [PunRPC]
    private void RPC_RoundStart(int round, float time)
    {
        CurrentRound = round;
        RemainingTime = time;
        isRunning = true;

        Debug.Log($"[RoundManager] {CurrentRound}라운드 시작! 제한시간: {time}초");
        OnRoundStarted?.Invoke();
    }

    // -----------------------------------------------
    // 플레이어가 씬에 스폰될 때 전체 목록에 추가
    // -----------------------------------------------
    public void AddPlayer(GameObject player)
    {
        if (!allPlayers.Contains(player))
        {
            allPlayers.Add(player);
            Debug.Log($"[RoundManager] '{player.name}' 플레이어 추가.");
        }
    }
    public List<GameObject> GetPlayers()
    {
        return allPlayers;
    }

    public void RemovePlayer(GameObject player)
    {
        allPlayers.Remove(player);
        playerStepMap.Remove(player);
        Debug.Log($"[RoundManager] '{player.name}' 플레이어 제거.");
    }

    // -----------------------------------------------
    // [변경] RoomTrigger에서 플레이어 진입 시 호출 - stepIndex 기록
    // -----------------------------------------------
    public void OnPlayerEnterRoom(GameObject player, int stepIndex)
    {
        playerStepMap[player] = stepIndex;
        Debug.Log($"[RoundManager] '{player.name}' -> stepIndex {stepIndex} 진입.");
    }

    // -----------------------------------------------
    // [변경] RoomTrigger에서 플레이어 퇴장 시 호출
    // -----------------------------------------------
    public void OnPlayerExitRoom(GameObject player, int stepIndex)
    {
        Debug.Log($"[RoundManager] '{player.name}' -> stepIndex {stepIndex} 퇴장.");
    }

    // -----------------------------------------------
    // [변경] 타이머 종료 시 - "현재 라운드 Piece방의 stepIndex 이하"에 있는 플레이어 전부 처리
    //        (StartRoom, 이전 ShopRoom/ReadyRoom 포함)
    // -----------------------------------------------
    private void HandleTimeUp()
    {
        Debug.Log($"[RoundManager] {CurrentRound}라운드 시간 종료.");
        OnRoundEnded?.Invoke();

        if (!roundToPieceStep.TryGetValue(CurrentRound, out int targetStep))
        {
            Debug.LogWarning($"[RoundManager] {CurrentRound}라운드에 해당하는 Piece stepIndex를 찾을 수 없습니다.");
            return;
        }

        foreach (GameObject player in allPlayers)
        {
            // 미지 차원에 있는 동안은 이 체크를 무시한다 - 어차피 ExitDimension()이 위치를 되돌려버려서
            // 지금 패널티를 적용해도 무효화된다. 대신 CheckLateAfterDimensionReturn()이 복귀 시점에 한 번만 체크한다.
            if (IsInDimension(player)) continue;

            // map에 없으면 기본값 0 (StartRoom)으로 간주
            int currentStep = playerStepMap.ContainsKey(player) ? playerStepMap[player] : 0;

            // 현재 라운드 Piece방의 stepIndex 이하에 머물러 있으면 처리
            if (currentStep <= targetStep)
                OnRoundTimeUp(player);
        }
    }

    private bool IsInDimension(GameObject player)
    {
        FearDimensionController fdc = player.GetComponent<FearDimensionController>();
        return fdc != null && fdc.IsInDimension;
    }

    // -----------------------------------------------
    // 완전 탈락(사망)한 플레이어를 제외한 모든 활성 플레이어가 특정 방(stepIndex)에 있는지 확인.
    // PieceDoor가 "라운드 시간 안 끝나도 다 모였으면 조기 개방"에 사용.
    // -----------------------------------------------
    public bool AreAllActivePlayersInRoom(int stepIndex)
    {
        foreach (GameObject player in allPlayers)
        {
            if (IsPlayerEliminated(player)) continue;

            int currentStep = playerStepMap.ContainsKey(player) ? playerStepMap[player] : 0;
            if (currentStep != stepIndex) return false;
        }
        return true;
    }

    // 체력/사망 시스템이 아직 구현 전(IsDead가 NotImplementedException)이면 "탈락 아님"으로 안전하게 처리.
    public bool IsPlayerEliminated(GameObject player)
    {
        IDamageable damageable = player.GetComponent<IDamageable>();
        if (damageable == null) return false;

        try
        {
            return damageable.IsDead;
        }
        catch (System.NotImplementedException)
        {
            return false;
        }
    }

    // -----------------------------------------------
    // EndRoomTrigger가 마스터에서만 호출 - 탈락(사망)하지 않은 모든 플레이어가 EndRoom에 모였을 때 알림.
    // 각 클라이언트가 독립적으로 판정하면 중복 호출/타이밍이 어긋날 수 있어서 마스터가 한 번 판정하고
    // RPC로 전체 클라이언트에 UI 표시를 전파한다.
    // -----------------------------------------------
    public void NotifyAllPlayersReachedEndRoom()
    {
        if (endRoomCleared) return;
        endRoomCleared = true;

        photonView.RPC("RPC_ShowEndRoomClearUI", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_ShowEndRoomClearUI()
    {
        if (endRoomClearImage != null)
            endRoomClearImage.SetActive(true);
    }

    // -----------------------------------------------
    // FearDimensionController.ExitDimension()이 미지 차원에서 돌아온 직후 호출한다.
    // 미지 차원에 있는 동안 라운드가 끝나서 HandleTimeUp()이 이 플레이어에게 페널티(텔레포트)를
    // 적용했더라도, ExitDimension()이 그 직후 realWorldPosition(미지 차원 들어가기 직전 위치)으로
    // 강제로 되돌리기 때문에 그 페널티 이동이 조용히 무효화될 수 있다. 그래서 돌아온 시점에
    // "라운드가 이미 끝났는데 아직도 뒤처져 있는지"를 다시 한번 확인해서 필요하면 페널티를 재적용한다.
    // -----------------------------------------------
    public void CheckLateAfterDimensionReturn(GameObject player)
    {
        if (isRunning) return;   // 라운드가 아직 진행 중이면(안 끝났으면) 해당 없음
        if (CurrentRound <= 0) return; // 라운드가 아직 한 번도 시작 안 했으면 해당 없음

        if (!roundToPieceStep.TryGetValue(CurrentRound, out int targetStep)) return;

        int currentStep = playerStepMap.ContainsKey(player) ? playerStepMap[player] : 0;
        if (currentStep <= targetStep)
            OnRoundTimeUp(player);
    }

    // -----------------------------------------------
    // 해당 라운드 Piece방 또는 그 이전 방에 남아있는 플레이어에게만 실행.
    // 최초 1회는 다음 ShopRoom으로 강제 이동, 두 번째부터는 검은 화면 표시.
    // allPlayers는 모든 클라이언트에서 동일하게 순회되므로, 실제 처리는 본인 소유 오브젝트에서만 한다.
    // -----------------------------------------------
    public void OnRoundTimeUp(GameObject player)
    {
        Debug.Log($"[RoundManager] OnRoundTimeUp() - '{player.name}'이 {CurrentRound}라운드 진행에 뒤처짐.");

        PhotonView playerPv = player.GetComponent<PhotonView>();
        if (playerPv == null || !playerPv.IsMine) return;

        if (hasBeenLateOnce.Add(player))
        {
            TeleportToNextShopRoom(player);
        }
        else
        {
            if (blackScreenPanel != null)
                blackScreenPanel.SetActive(true);
        }
    }

    // -----------------------------------------------
    // 현재 라운드 Piece방 기준 다음 ShopRoom(Piece -> ShopRoom)으로 순간이동
    // -----------------------------------------------
    private void TeleportToNextShopRoom(GameObject player)
    {
        if (!roundToPieceStep.TryGetValue(CurrentRound, out int pieceStep))
        {
            Debug.LogWarning($"[RoundManager] {CurrentRound}라운드의 Piece stepIndex를 찾지 못해 텔레포트하지 못했습니다.");
            return;
        }

        if (RoomGenerator.Instance == null)
        {
            Debug.LogWarning("[RoundManager] RoomGenerator.Instance가 없어 텔레포트하지 못했습니다.");
            return;
        }

        int shopStep = pieceStep + 1; // Piece -> ShopRoom
        GameObject shopRoom = RoomGenerator.Instance.GetRoomByStepIndex(shopStep);

        if (shopRoom == null)
        {
            Debug.LogWarning($"[RoundManager] stepIndex {shopStep}에 해당하는 ShopRoom을 찾지 못했습니다 (마지막 라운드는 ShopRoom이 없음).");
            return;
        }

        player.transform.position = shopRoom.transform.position;
        Debug.Log($"[RoundManager] '{player.name}'을 다음 ShopRoom(stepIndex {shopStep})으로 이동시켰습니다.");
    }
}
