using Photon.Pun;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    private void Start()
    {
        PhotonNetwork.Instantiate("Cube", Vector3.zero, Quaternion.identity);
    }

    public void GameStart()
    {
        PhotonNetwork.LoadLevel("GameScene");
    }
}
