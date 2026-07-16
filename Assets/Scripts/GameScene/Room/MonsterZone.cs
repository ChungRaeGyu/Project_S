using UnityEngine;

// Piece 룸 프리팹에 부착하면 RoomGenerator가 생성 시 자동으로 몬스터 순찰 구역(MonsterAI.Zone)으로 등록한다.
// patrolPoints를 직접 배치하면 그 지점들을 순서대로 순찰하고,
// 비워두면 MonsterAI가 zoneCenter 주변 NavMesh 위에서 순찰 지점을 매번 무작위로 샘플링한다.
public class MonsterZone : MonoBehaviour
{
    public Transform[] patrolPoints;
    public Transform zoneCenter;
    public float patrolRadius = 5f;

    void Reset()
    {
        zoneCenter = transform;
    }
}
