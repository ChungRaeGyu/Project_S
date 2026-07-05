using Photon.Pun;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject obj = PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
        RoundManager.Instance.AddPlayer(obj);
    }
}
