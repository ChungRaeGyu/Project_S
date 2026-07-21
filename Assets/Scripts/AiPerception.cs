using UnityEngine;
using UnityEngine.AI;

// MonsterAI와 ClickerAI가 공통으로 쓰는 탐지/이동/공격 로직 모음.
// 두 스크립트의 베이스 클래스가 달라(MonoBehaviourPun vs MonoBehaviour) 상속 대신 정적 헬퍼로 공유한다.
public static class AiPerception
{
    // 시야각 + 레이캐스트 기반 탐지 (눈)
    public static bool CanSeeTarget(Transform eyes, Transform target, float viewDistance, float viewAngle, LayerMask obstacleMask)
    {
        Vector3 toTarget = target.position - eyes.position;
        float distance = toTarget.magnitude;
        if (distance > viewDistance) return false;

        float angle = Vector3.Angle(eyes.forward, toTarget);
        if (angle > viewAngle * 0.5f) return false;

        if (Physics.Raycast(eyes.position, toTarget.normalized, out _, distance, obstacleMask))
            return false;

        return true;
    }

    // 거리 + 대상이 소리를 내고 있는지 기반 탐지 (귀)
    public static bool CanHearTarget(Vector3 listenerPosition, Transform target, PlayerNoiseSource targetNoise, float hearingRadius)
    {
        if (targetNoise == null || !targetNoise.IsMakingNoise) return false;
        return Vector3.Distance(listenerPosition, target.position) <= hearingRadius;
    }

    // NavMesh 위 지점으로 스냅한 뒤 목적지 설정. 대상 좌표가 NavMesh에서 살짝 벗어나 있어도
    // (플레이어 위치 등) 안전하게 시도하고, 실패 여부를 반환해 호출부가 재시도할 수 있게 한다.
    public static bool TrySetDestination(NavMeshAgent agent, Vector3 worldPosition, float sampleRadius = 3f)
    {
        if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            return agent.SetDestination(hit.position);

        return false;
    }

    // IDamageable을 통한 공격. 대상이 구현하지 않았으면 조용히 무시한다.
    public static void TryAttack(Transform target, int damage, GameObject source)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null) return;
        damageable.TakeDamage(damage, source);
    }

    public static void DrawHearingGizmo(Vector3 position, float hearingRadius)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, hearingRadius);
    }

    public static void DrawVisionGizmo(Transform eyes, float viewDistance, float viewAngle)
    {
        Gizmos.color = Color.red;
        Vector3 left = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * eyes.forward * viewDistance;
        Vector3 right = Quaternion.Euler(0, viewAngle * 0.5f, 0) * eyes.forward * viewDistance;
        Gizmos.DrawLine(eyes.position, eyes.position + left);
        Gizmos.DrawLine(eyes.position, eyes.position + right);
        Gizmos.DrawLine(eyes.position, eyes.position + eyes.forward * viewDistance);
    }
}
