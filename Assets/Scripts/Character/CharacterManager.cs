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
        characterInput.InputSetting();

    }
    private void OnEnable()
    {
        characterInput.InputOnEnable();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterLook.Set();
        characterInput.interact += stat.Interact;
    }
    private void OnDisable()
    {
        if(characterInput != null)
        characterInput.InputOnDisable();
    }
    private void OnDestroy()
    {
        characterInput.InputOnDestroy();
    }

    // Update is called once per frame
    void Update()
    {
        characterBehavior.UpdateCharacter(characterInput.MoveInput);
        characterLook.Look(characterInput.LookInput);
    }
}
