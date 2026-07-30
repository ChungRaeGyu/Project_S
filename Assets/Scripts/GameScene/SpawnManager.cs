using Photon.Pun;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField]ItemDatabase itemDatabase;
    [SerializeField] Transform[] spawnPos;

    public static SpawnManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject obj = PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
        obj.gameObject.name = PhotonNetwork.NickName;
        SpawnStart();
    }
    //아이템 스폰도 적어놓을까
    private void SpawnStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            //대충 반복문 아이템 있는거 다 스폰
            //ItemSpawn(itemDatabase.items[0].itemName, spawnPos[0]);
        }
    }

    //상점에서 살때도 ItemSpawn을 사용하던가 상점에다가 넣던가 하세여라~
    public void ItemSpawn(string name,Transform pos)
    {
        PhotonNetwork.Instantiate(name, pos.position, Quaternion.identity);
    }
}
