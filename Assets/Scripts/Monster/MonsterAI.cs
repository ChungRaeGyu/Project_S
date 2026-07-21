using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

// 몬스터.drawio 플로우차트를 그대로 옮긴 상태 기반 AI.
// 이동/판단 로직은 마스터 클라이언트만 실행하고(RoomGenerator, RoundManager와 동일한 권한 모델),
// 상태 변경은 RPC로 다른 클라이언트에 전파해 연출/애니메이션에 활용할 수 있게 한다.
// 실제 트랜스폼 동기화는 프리팹에 PhotonTransformView(또는 PhotonRigidbodyView)를 별도로 추가해야 한다.
[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : MonoBehaviourPun
{
    public enum MonsterState
    {
        Dormant,        // StartRoom 진입 대기 (문 열리기 전)
        HuntWindup,     // 헌팅시간 시작 (10초)
        Berserk,        // 광폭모드로 타겟 서칭
        Patrol,         // 천천히 방 돌아다니며 플레이어 서칭
        Chase,          // 플레이어 따라가서 공격
        ZoneSearch,     // 30초 동안 현재 구역 서칭 유지
        MoveToNextZone, // 다음 구역으로 이동
        GameEnded
    }

    public enum TargetingMode
    {
        SingleSnapshot, // 타겟 서칭: 현재 플레이어 위치를 한번 지정해서 간다
        FixedTarget     // 고정 타겟: 정해진 플레이어를 지정해서 계속 따라간다
    }

    [System.Serializable]
    public class Zone
    {
        public string zoneName;
        public Transform[] patrolPoints;

        // patrolPoints가 비어 있을 때 자동 샘플링에 쓰이는 기준점/반경 (MonsterZone 컴포넌트에서 채워짐)
        public Transform zoneCenter;
        public float patrolRadius = 5f;
    }

    [Header("Multiplayer")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float playerScanInterval = 0.5f;

    [Header("References")]
    public Transform eyes;

    [Header("Zones (구역별 순찰 지점)")]
    public Zone[] zones;

    [Header("Timing - 플로우차트에 명시된 값")]
    public float huntWindupDuration = 10f;          // Monster Door가 열린 뒤 광폭모드 진입까지 대기 시간
    public float postChaseZoneSearchDuration = 30f; // 30초 동안 현재 구역 서칭 유지

    [Header("Timing - 플로우차트에 값이 없어 임의 지정 (기획값 확정 필요)")]
    public float berserkDuration = 15f;
    public float berserkRechargeInterval = 45f;

    [Tooltip("RoundManager.Instance가 없을 때만 쓰이는 폴백 라운드 길이 (정상 플레이에서는 RoundManager.OnRoundEnded를 사용)")]
    public float fallbackRoundDuration = 60f;

    [Header("Speed")]
    public float patrolSpeed = 2f;
    public float berserkSpeed = 6f;
    public float chaseSpeed = 5f;

    [Header("Vision (눈)")]
    public float viewDistance = 10f;
    [Range(0f, 360f)] public float viewAngle = 90f;
    public LayerMask obstacleMask;

    [Header("Hearing (귀)")]
    public float hearingRadius = 15f;

    [Header("Berserk Targeting")]
    public TargetingMode berserkTargetingMode = TargetingMode.SingleSnapshot;

    [Header("Attack")]
    public int attackDamage = 10;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Tooltip("공격 사거리 밖에서 이 시간(초) 동안 시야에 안 잡히면 '놓침'으로 처리")]
    public float loseSightTimeout = 3f;

    [Header("문 강제 개방")]
    [Tooltip("이 반경 안의 닫힌 PieceDoor는 플레이어 상호작용 없이 몬스터가 강제로 연다")]
    public float doorBreakRadius = 3f;
    public float doorCheckInterval = 0.5f;

    // 씬에 몬스터가 하나뿐이라고 가정한 싱글턴 (RoomGenerator가 구역 정보를 넘겨줄 때 사용)
    public static MonsterAI Instance { get; private set; }

    public MonsterState CurrentState { get; private set; } = MonsterState.Dormant;

    NavMeshAgent agent;
    Coroutine stateRoutine;
    int currentZoneIndex;
    int currentPatrolIndex;
    Vector3? lastHeardPosition;
    bool chaseStartedFromBerserk;
    Transform target; // Chase 상태에서 쫓는 특정 플레이어
    bool roundEndedSignal;
    bool doorOpenedSignal;

    Transform[] cachedPlayers = System.Array.Empty<Transform>();
    float playerCacheTimer;
    float doorCheckTimer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        agent = GetComponent<NavMeshAgent>();
        if (eyes == null) eyes = transform;
    }

    // RoomGenerator가 맵 생성을 마친 뒤 Piece 방들의 MonsterZone을 순서대로 모아서 전달한다 (마스터에서만 호출됨)
    public void SetZones(Zone[] newZones)
    {
        zones = newZones;
        currentZoneIndex = 0;
        currentPatrolIndex = 0;
    }

    void Start()
    {
        // AI 판단/이동은 마스터만 수행. 다른 클라이언트는 RPC로 전달받은 상태만 참고한다.
        // (마스터 클라이언트가 도중에 바뀌면 진행 중이던 상태 머신은 새 마스터로 인계되지 않는다 - 추후 보완 필요)
        if (!PhotonNetwork.IsMasterClient)
        {
            enabled = false;
            return;
        }

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnRoundEnded += HandleRoundEnded;

        // FSM은 여기서 바로 시작하지 않는다. 이 시점엔 아직 RoomGenerator가 NavMesh를 굽기 전이라
        // NavMeshAgent가 NavMesh 위에 있지 않은 상태다. RoomGenerator가 BuildNavMesh() 이후
        // OnNavMeshReady()를 호출해줘야 실제로 움직이기 시작한다.
    }

    // RoomGenerator가 모든 방을 생성하고 NavMesh를 다시 구운 직후 호출한다.
    public void OnNavMeshReady()
    {
        agent.Warp(transform.position);

        if (!agent.isOnNavMesh)
        {
            Debug.LogError($"[MonsterAI] NavMesh 위에 배치하지 못했습니다. NavMeshSurface가 이 위치({transform.position})를 덮고 있는지 확인하세요.");
            return;
        }

        ChangeState(MonsterState.Dormant);
    }

    void OnDestroy()
    {
        if (RoundManager.Instance != null)
            RoundManager.Instance.OnRoundEnded -= HandleRoundEnded;
    }

    void Update()
    {
        // 사냥을 시작하기 전(Dormant/HuntWindup)이나 게임이 끝난 뒤에는 문을 부수지 않는다
        if (CurrentState == MonsterState.Dormant || CurrentState == MonsterState.HuntWindup || CurrentState == MonsterState.GameEnded)
            return;

        doorCheckTimer -= Time.deltaTime;
        if (doorCheckTimer > 0f) return;
        doorCheckTimer = doorCheckInterval;

        ForceOpenNearbyDoors();
    }

    // 근처의 닫힌 PieceDoor를 플레이어 상호작용 없이 강제로 연다 (몬스터는 문에 막히지 않는다)
    void ForceOpenNearbyDoors()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, doorBreakRadius);
        foreach (Collider hit in hits)
        {
            PieceDoor door = hit.GetComponentInParent<PieceDoor>();
            if (door != null)
                door.OpenDoor();
        }
    }

    void HandleRoundEnded()
    {
        roundEndedSignal = true;
    }

    // MonsterDoor가 실제로 문을 다 연 직후 호출한다. Dormant는 이 신호를 기다린 뒤에야 헌팅 시퀀스를 시작한다.
    public void NotifyDoorOpened()
    {
        doorOpenedSignal = true;
    }

    void ChangeState(MonsterState newState)
    {
        if (stateRoutine != null) StopCoroutine(stateRoutine);
        CurrentState = newState;
        photonView.RPC(nameof(RPC_SyncState), RpcTarget.Others, (int)newState);

        switch (newState)
        {
            case MonsterState.Dormant: stateRoutine = StartCoroutine(Dormant()); break;
            case MonsterState.HuntWindup: stateRoutine = StartCoroutine(HuntWindup()); break;
            case MonsterState.Berserk: stateRoutine = StartCoroutine(Berserk()); break;
            case MonsterState.Patrol: stateRoutine = StartCoroutine(Patrol()); break;
            case MonsterState.Chase: stateRoutine = StartCoroutine(Chase()); break;
            case MonsterState.ZoneSearch: stateRoutine = StartCoroutine(ZoneSearch()); break;
            case MonsterState.MoveToNextZone: stateRoutine = StartCoroutine(MoveToNextZone()); break;
            case MonsterState.GameEnded: stateRoutine = StartCoroutine(GameEnded()); break;
        }
    }

    [PunRPC]
    void RPC_SyncState(int state)
    {
        CurrentState = (MonsterState)state;
    }

    // Monster Door가 실제로 다 열릴 때까지 대기 (MonsterDoor.NotifyDoorOpened가 신호를 보냄)
    IEnumerator Dormant()
    {
        agent.isStopped = true;

        // Dormant는 게임당 한 번만 실행되므로 여기서 굳이 false로 리셋하지 않는다.
        // (NavMesh 빌드가 늦어져 OnNavMeshReady()보다 문 오픈 신호가 먼저 도착하는 경우를 대비)
        while (!doorOpenedSignal)
            yield return null;

        ChangeState(MonsterState.HuntWindup);
    }

    // 문이 열린 뒤 광폭모드 진입까지 대기
    IEnumerator HuntWindup()
    {
        agent.isStopped = true;
        yield return new WaitForSeconds(huntWindupDuration);
        ChangeState(MonsterState.Berserk);
    }

    // 광폭모드로 *타겟 서칭
    IEnumerator Berserk()
    {
        agent.isStopped = false;
        agent.speed = berserkSpeed;

        Transform berserkTarget = TryFindVisiblePlayer(out Transform seen) ? seen : GetNearestPlayer();
        if (berserkTarget == null)
        {
            ChangeState(MonsterState.Patrol);
            yield break;
        }

        bool snapshotTaken = false;
        if (berserkTargetingMode == TargetingMode.SingleSnapshot)
            snapshotTaken = TrySetDestination(berserkTarget.position);

        float timer = 0f;
        while (timer < berserkDuration)
        {
            timer += Time.deltaTime;

            if (berserkTarget == null)
            {
                // 타겟이 접속 종료 등으로 사라졌으면 광폭모드를 계속할 이유가 없다
                ChangeState(MonsterState.Patrol);
                yield break;
            }

            if (berserkTargetingMode == TargetingMode.FixedTarget)
            {
                TrySetDestination(berserkTarget.position);
            }
            else if (!snapshotTaken)
            {
                // 첫 시도가 실패했을 수 있으니(플레이어 위치가 NavMesh에서 살짝 벗어난 경우 등) 성공할 때까지 재시도
                snapshotTaken = TrySetDestination(berserkTarget.position);
            }

            // 플레이어 찾았거나 좇는 중인가요? -> Yes
            if (TryFindVisiblePlayer(out Transform seenPlayer))
            {
                target = seenPlayer;
                chaseStartedFromBerserk = true;
                ChangeState(MonsterState.Chase);
                yield break;
            }

            yield return null;
        }

        // 광폭모드가 끝났나요? -> Yes
        ChangeState(MonsterState.Patrol);
    }

    // 천천히 방 돌아다니며 플레이어 서칭
    IEnumerator Patrol()
    {
        agent.isStopped = false;
        agent.speed = patrolSpeed;

        roundEndedSignal = false;
        float fallbackRoundTimer = 0f;
        float berserkRechargeTimer = 0f;
        GoToNextPatrolPoint();

        while (true)
        {
            fallbackRoundTimer += Time.deltaTime;
            berserkRechargeTimer += Time.deltaTime;

            // 광폭모드 시간이 되었나요? -> Yes
            if (berserkRechargeTimer >= berserkRechargeInterval)
            {
                ChangeState(MonsterState.HuntWindup);
                yield break;
            }

            // 플레이어 찾았나요? (시야) -> Yes
            if (TryFindVisiblePlayer(out Transform seenPlayer))
            {
                target = seenPlayer;
                chaseStartedFromBerserk = false;
                ChangeState(MonsterState.Chase);
                yield break;
            }

            if (TryHearPlayer(out Vector3 heardPos))
                lastHeardPosition = heardPos;

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                if (lastHeardPosition.HasValue)
                {
                    TrySetDestination(lastHeardPosition.Value);
                    lastHeardPosition = null; // 도착하면 소리 단서 소모, 이후 일반 순찰 재개
                }
                else
                {
                    GoToNextPatrolPoint();
                }
            }

            // 라운드가 끝났나요? -> Yes (RoundManager가 있으면 실제 라운드 타이머를 따르고, 없으면 폴백 타이머 사용)
            bool roundOver = RoundManager.Instance != null
                ? roundEndedSignal
                : fallbackRoundTimer >= fallbackRoundDuration;

            if (roundOver)
            {
                ChangeState(MonsterState.ZoneSearch);
                yield break;
            }

            yield return null;
        }
    }

    // 플레이어 따라가서 공격 함
    IEnumerator Chase()
    {
        agent.isStopped = false;
        agent.speed = chaseSpeed;

        float attackTimer = 0f;
        float timeSinceLastSeen = 0f;

        while (true)
        {
            // 플레이어를 죽였거나 놓쳤나요? -> Yes
            if (IsPlayerDead(target))
            {
                ChangeState(chaseStartedFromBerserk ? MonsterState.Berserk : MonsterState.Patrol);
                yield break;
            }

            TrySetDestination(target.position);

            attackTimer -= Time.deltaTime;
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance <= attackRange && attackTimer <= 0f)
            {
                TryAttack(target);
                attackTimer = attackCooldown;
            }

            if (IsVisible(target))
                timeSinceLastSeen = 0f;
            else
                timeSinceLastSeen += Time.deltaTime;

            if (timeSinceLastSeen >= loseSightTimeout)
            {
                // 광폭모드에서 이어진 추적이면 광폭모드 종료 체크로, 아니면 일반 서칭으로 복귀
                ChangeState(chaseStartedFromBerserk ? MonsterState.Berserk : MonsterState.Patrol);
                yield break;
            }

            yield return null;
        }
    }

    // 30초 동안 현재 구역을 돌아다니며 서칭 유지
    IEnumerator ZoneSearch()
    {
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        GoToNextPatrolPoint();

        float timer = 0f;
        while (timer < postChaseZoneSearchDuration)
        {
            timer += Time.deltaTime;

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
                GoToNextPatrolPoint();

            if (TryFindVisiblePlayer(out Transform seenPlayer))
            {
                target = seenPlayer;
                chaseStartedFromBerserk = false;
                ChangeState(MonsterState.Chase);
                yield break;
            }

            yield return null;
        }

        ChangeState(MonsterState.MoveToNextZone);
    }

    // 다음 구역으로 이동 -> 다음 구역이 있나요?
    IEnumerator MoveToNextZone()
    {
        currentZoneIndex++;

        if (zones == null || currentZoneIndex >= zones.Length)
        {
            ChangeState(MonsterState.GameEnded);
            yield break;
        }

        currentPatrolIndex = 0;
        ChangeState(MonsterState.Patrol);
    }

    IEnumerator GameEnded()
    {
        agent.isStopped = true;
        yield return null;
    }

    bool TrySetDestination(Vector3 worldPosition) => AiPerception.TrySetDestination(agent, worldPosition);

    void GoToNextPatrolPoint()
    {
        if (zones == null || zones.Length == 0) return;
        Zone zone = zones[currentZoneIndex];

        Transform[] points = zone.patrolPoints;
        if (points != null && points.Length > 0)
        {
            agent.SetDestination(points[currentPatrolIndex].position);
            currentPatrolIndex = (currentPatrolIndex + 1) % points.Length;
            return;
        }

        // 지정된 순찰 지점이 없으면 zoneCenter 주변 NavMesh에서 무작위 지점을 샘플링한다
        if (zone.zoneCenter == null) return;

        Vector3 randomPoint = zone.zoneCenter.position + Random.insideUnitSphere * zone.patrolRadius;
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, zone.patrolRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    // -----------------------------------------------
    // 플레이어 탐지 (멀티플레이어: "Player" 태그를 가진 모든 오브젝트 대상)
    // -----------------------------------------------

    Transform[] GetActivePlayers()
    {
        playerCacheTimer -= Time.deltaTime;
        if (playerCacheTimer <= 0f)
        {
            GameObject[] objs = GameObject.FindGameObjectsWithTag(playerTag);
            cachedPlayers = new Transform[objs.Length];
            for (int i = 0; i < objs.Length; i++)
                cachedPlayers[i] = objs[i].transform;

            playerCacheTimer = playerScanInterval;
        }
        return cachedPlayers;
    }

    Transform GetNearestPlayer()
    {
        Transform nearest = null;
        float best = float.MaxValue;
        foreach (Transform p in GetActivePlayers())
        {
            float d = Vector3.Distance(transform.position, p.position);
            if (d < best)
            {
                best = d;
                nearest = p;
            }
        }
        return nearest;
    }

    bool TryFindVisiblePlayer(out Transform found)
    {
        found = null;
        float bestDist = float.MaxValue;

        foreach (Transform p in GetActivePlayers())
        {
            if (!IsVisible(p)) continue;

            float d = Vector3.Distance(eyes.position, p.position);
            if (d < bestDist)
            {
                bestDist = d;
                found = p;
            }
        }
        return found != null;
    }

    bool IsVisible(Transform p) => AiPerception.CanSeeTarget(eyes, p, viewDistance, viewAngle, obstacleMask);

    bool TryHearPlayer(out Vector3 heardPos)
    {
        heardPos = default;
        bool found = false;
        float bestDist = float.MaxValue;

        foreach (Transform p in GetActivePlayers())
        {
            PlayerNoiseSource noise = p.GetComponent<PlayerNoiseSource>();
            if (!AiPerception.CanHearTarget(transform.position, p, noise, hearingRadius)) continue;

            float d = Vector3.Distance(transform.position, p.position);
            if (d < bestDist)
            {
                bestDist = d;
                heardPos = p.position;
                found = true;
            }
        }
        return found;
    }

    // 대상이 IDamageable을 구현하고 있으면 그 값을 그대로 신뢰한다.
    // 플레이어 체력 스크립트가 아직 없으면 항상 false (죽지 않음)로 취급된다.
    bool IsPlayerDead(Transform t)
    {
        if (t == null) return true;
        IDamageable damageable = t.GetComponent<IDamageable>();
        return damageable != null && damageable.IsDead;
    }

    void TryAttack(Transform t) => AiPerception.TryAttack(t, attackDamage, gameObject);

    void OnDrawGizmosSelected()
    {
        Transform e = eyes != null ? eyes : transform;
        AiPerception.DrawHearingGizmo(transform.position, hearingRadius);
        AiPerception.DrawVisionGizmo(e, viewDistance, viewAngle);
    }
}
