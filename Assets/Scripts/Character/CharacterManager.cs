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

        //if (!pv.IsMine) this.enabled = false;
        characterInput = GetComponent<CharacterInput>();
        characterBehavior = GetComponent<CharacterBehavior>();
        characterLook = GetComponent<CharacterLook>();
        stat = GetComponent<CharacterStat>();

        characterBehavior.Init(stat);
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
        Interact();
        ItemInteract();
        ItemDrop();
        characterInput.OnMouseButton += characterBehavior.OnMouseButton;
        
    }
    private void Interact()
    {
        // Look -> stat -> Input -> stat
        // Look에서 상호작용 가능한 오브젝트를 감지하면 stat에 전달
        // Input에서 상호작용 버튼을 누르면 stat에서 상호작용 수행
        characterLook.changeInteractable += stat.ChangeInteractable;
        characterInput.interact += stat.Interact;

    }
    private void ItemInteract()
    {
        // Input -> 
        characterLook.changeItem += stat.ChangeItemInteract;
        characterInput.itemInteract += characterBehavior.GetItem;

    }
    private void ItemDrop()
    {
        characterInput.itemDrop += characterBehavior.DropItem;

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
        characterLook.UpdateLook(characterInput.LookInput);
    }
}
