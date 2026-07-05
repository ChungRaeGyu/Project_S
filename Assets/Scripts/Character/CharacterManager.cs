using Photon.Pun;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    CharacterInput characterInput;
    CharacterBehavior characterBehavior;
    CharacterLook characterLook;
    CharacterStat stat;
    PhotonView pv;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (!pv.IsMine) this.enabled = false;
        characterInput = GetComponent<CharacterInput>();
        characterBehavior = GetComponent<CharacterBehavior>();
        characterLook = GetComponent<CharacterLook>();
        stat = GetComponent<CharacterStat>();

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterLook.Set();
        characterInput.InputSetting();
        characterInput.interact += stat.Interact;
    }

    // Update is called once per frame
    void Update()
    {
        characterBehavior.UpdateCharacter(characterInput.MoveInput);
        characterLook.Look(characterInput.LookInput);
    }
}
