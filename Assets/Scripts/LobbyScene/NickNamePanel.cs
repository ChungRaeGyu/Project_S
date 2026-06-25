using Photon.Pun;
using TMPro;
using UnityEngine;

public class NickNamePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text nickNameText;
    [SerializeField] private TMP_InputField nickNameInput;

    public void NickNameInput()
    {
        PhotonNetwork.NickName = nickNameInput.text;
        SetNickName();
        gameObject.SetActive(false);
    }
    public void SetNickName()
    {
        nickNameText.text = PhotonNetwork.NickName;
    }
}
