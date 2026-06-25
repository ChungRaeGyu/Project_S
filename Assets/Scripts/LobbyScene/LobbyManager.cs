using Photon.Pun;
using TMPro;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private NickNamePanel nickNamePanelScript;
    [SerializeField] private GameObject randomJoinBtn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(PhotonNetwork.NickName == "")
        {
            nickNamePanelScript.gameObject.SetActive(true);
        }
        else
        {
            nickNamePanelScript.gameObject.SetActive(false);
            nickNamePanelScript.SetNickName();
        }
    }   
}
