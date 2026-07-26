using Photon.Pun;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
<<<<<<< Updated upstream
=======
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

>>>>>>> Stashed changes
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject obj = PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
        RoundManager.Instance.AddPlayer(obj);
    }
}
