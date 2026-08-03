using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 미지 차원 전용 추적자. MonsterAI의 탐지(시야+청각)/이동/공격 로직을 같은 방식으로 재사용하되,
// 라운드/구역/문 같은 개념 없이 "돌아다니다가 -> 발견하면 쫓기"만 반복하는 단순한 상태머신.
// 순수 로컬 인스턴스(미지 차원)에서만 존재하므로 Photon 동기화가 필요 없다.
[RequireComponent(typeof(NavMeshAgent))]
public class ClickerAI : MonoBehaviour
{
    enum State { Wander, Chase }

    [Header("이동 범위")]
    public Transform[] wanderPoints;

    [Tooltip("wanderPoints가 비어있을 때, 현재 위치 주변 이 반경 안에서 NavMesh 위 무작위 지점으로 배회한다")]
    public float wanderRadius = 8f;

    [Header("Speed")]
    public float wanderSpeed = 2f;
    public float chaseSpeed = 5f;

    [Header("Vision (눈)")]
    public Transform eyes;
    public float viewDistance = 8f;
    [Range(0f, 360f)] public float viewAngle = 100f;
    public LayerMask obstacleMask;

    [Header("Hearing (귀)")]
    public float hearingRadius = 10f;

    [Header("Attack")]
    public int attackDamage = 10;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Tooltip("추적 중 이 시간(초) 동안 시야에 안 잡히면 다시 배회 상태로")]
    public float loseSightTimeout = 4f;

    NavMeshAgent agent;
    Transform player;
    PlayerNoiseSource playerNoise;
    State state;
    int wanderIndex;
    Coroutine stateRoutine;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (eyes == null) eyes = transform;
    }

    // UnknownWorld가 스폰 직후 호출
    public void Begin(Transform targetPlayer)
    {
        player = targetPlayer;
        playerNoise = player.GetComponent<PlayerNoiseSource>();

        if (stateRoutine != null) StopCoroutine(stateRoutine);
        stateRoutine = StartCoroutine(Wander());
    }

    // UnknownWorld.Leave()가 Destroy() 하기 직전에 호출한다.
    // Destroy() 시점에 NavMeshAgent가 NavMesh에서 즉시 분리되는데, 코루틴이 그 다음 틱에
    // agent.remainingDistance 등을 계속 읽으려다 "NavMesh에 없는 에이전트" 예외가 나는 걸 막는다.
    public void StopChasing()
    {
        if (stateRoutine != null)
        {
            StopCoroutine(stateRoutine);
            stateRoutine = null;
        }
    }

    IEnumerator Wander()
    {
        state = State.Wander;
        agent.speed = wanderSpeed;
        GoToNextWanderPoint();

        while (state == State.Wander)
        {
            if (!agent.isOnNavMesh) yield break;

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
                GoToNextWanderPoint();

            if (IsVisible() || IsHeard())
            {
                stateRoutine = StartCoroutine(Chase());
                yield break;
            }

            yield return null;
        }
    }

    IEnumerator Chase()
    {
        state = State.Chase;
        agent.speed = chaseSpeed;

        float attackTimer = 0f;
        float timeSinceLastSeen = 0f;

        while (state == State.Chase)
        {
            if (!agent.isOnNavMesh) yield break;

            TrySetDestination(player.position);

            attackTimer -= Time.deltaTime;
            if (Vector3.Distance(transform.position, player.position) <= attackRange && attackTimer <= 0f)
            {
                TryAttack();
                attackTimer = attackCooldown;
            }

            if (IsVisible())
                timeSinceLastSeen = 0f;
            else
                timeSinceLastSeen += Time.deltaTime;

            if (timeSinceLastSeen >= loseSightTimeout)
            {
                stateRoutine = StartCoroutine(Wander());
                yield break;
            }

            yield return null;
        }
    }

    void GoToNextWanderPoint()
    {
        if (wanderPoints != null && wanderPoints.Length > 0)
        {
            agent.SetDestination(wanderPoints[wanderIndex].position);
            wanderIndex = (wanderIndex + 1) % wanderPoints.Length;
            return;
        }

        // 지정된 배회 지점이 없으면 현재 위치 주변 NavMesh에서 무작위 지점을 샘플링한다
        // (wanderPoints를 안 채워도 최소한 가만히 서있지는 않게 하는 폴백).
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * wanderRadius;
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    bool TrySetDestination(Vector3 worldPosition) => AiPerception.TrySetDestination(agent, worldPosition);

    bool IsVisible() => AiPerception.CanSeeTarget(eyes, player, viewDistance, viewAngle, obstacleMask);

    bool IsHeard() => AiPerception.CanHearTarget(transform.position, player, playerNoise, hearingRadius);

    void TryAttack() => AiPerception.TryAttack(player, attackDamage, gameObject);

    void OnDrawGizmosSelected()
    {
        AiPerception.DrawHearingGizmo(transform.position, hearingRadius);
        AiPerception.DrawVisionGizmo(eyes != null ? eyes : transform, viewDistance, viewAngle);
    }
}
