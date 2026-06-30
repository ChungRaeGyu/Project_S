using System.Collections.Generic;
using UnityEngine;
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

    public int CurrentRound { get; private set; } = 0;
    public float RemainingTime { get; private set; } = 0f;

    private bool isRunning = false;

    // 플레이어별 현재 위치한 방의 stepIndex (map에 없으면 기본값 0으로 간주 = StartRoom)
    private readonly Dictionary<GameObject, int> playerStepMap = new Dictionary<GameObject, int>();

    // 씬에 있는 모든 플레이어 목록
    private readonly List<GameObject> allPlayers = new List<GameObject>();

    // [변경] 라운드 번호 -> 해당 라운드 Piece방의 stepIndex
    // RoomGenerator가 생성 완료 후 채워줌
    private readonly Dictionary<int, int> roundToPieceStep = new Dictionary<int, int>();

    public static RoundManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!isRunning) return;

        RemainingTime -= Time.deltaTime;

        if (RemainingTime <= 0f)
        {
            RemainingTime = 0f;
            isRunning = false;
            HandleTimeUp();
        }
    }

    // -----------------------------------------------
    // [변경] RoomGenerator가 맵 생성 완료 후 호출 - 라운드별 Piece stepIndex 전달
    // -----------------------------------------------
    public void SetRoundPieceStepMap(Dictionary<int, int> map)
    {
        roundToPieceStep.Clear();
        foreach (var kv in map)
            roundToPieceStep[kv.Key] = kv.Value;

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

        if (!roundToPieceStep.TryGetValue(CurrentRound, out int targetStep))
        {
            Debug.LogWarning($"[RoundManager] {CurrentRound}라운드에 해당하는 Piece stepIndex를 찾을 수 없습니다.");
            return;
        }

        foreach (GameObject player in allPlayers)
        {
            // map에 없으면 기본값 0 (StartRoom)으로 간주
            int currentStep = playerStepMap.ContainsKey(player) ? playerStepMap[player] : 0;

            // 현재 라운드 Piece방의 stepIndex 이하에 머물러 있으면 처리
            if (currentStep <= targetStep)
                OnRoundTimeUp(player);
        }
    }

    // -----------------------------------------------
    // 해당 라운드 Piece방 또는 그 이전 방에 남아있는 플레이어에게만 실행
    // 나중에 이 안에 처리 로직 추가
    // -----------------------------------------------
    public void OnRoundTimeUp(GameObject player)
    {
        Debug.Log($"[RoundManager] OnRoundTimeUp() - '{player.name}'이 {CurrentRound}라운드 진행에 뒤처짐.");
        // TODO: 처리 로직 추가
    }
}