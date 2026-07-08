using UnityEngine;

public class CharacterStat : MonoBehaviour
{
    //여기서 이제 어떤 장비를 가지고 있는 지 확인하고 
    
    public void Interact()
    {
        //GameObject.GetComponent<IInteractable>().OnInteract();
        Debug.Log("상호작용");
        //들고있는거 상호작용;
    }
}
