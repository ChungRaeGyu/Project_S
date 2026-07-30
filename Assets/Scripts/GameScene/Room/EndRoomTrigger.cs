using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// EndRoom의 트리거 콜라이더에 RoomTrigger와 함께 부착.
/// 탈락(사망)하지 않은 모든 플레이어가 EndRoom 안에 들어오면 RoundManager를 통해 클리어 UI를 띄운다.
/// 판정은 마스터에서만 하고(각 클라이언트가 독립 판정하면 중복/타이밍 어긋남 발생), 결과는 RPC로 전파된다.
/// </summary>
public class EndRoomTrigger : MonoBehaviour
{
    private readonly HashSet<GameObject> playersInside = new HashSet<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInside.Add(other.gameObject);
        CheckAllGathered();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInside.Remove(other.gameObject);
    }

    private void CheckAllGathered()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (RoundManager.Instance == null) return;

        List<GameObject> activePlayers = RoundManager.Instance.GetPlayers()
            .Where(p => !RoundManager.Instance.IsPlayerEliminated(p))
            .ToList();

        if (activePlayers.Count == 0) return;
        if (!activePlayers.All(p => playersInside.Contains(p))) return;

        RoundManager.Instance.NotifyAllPlayersReachedEndRoom();
    }
}
