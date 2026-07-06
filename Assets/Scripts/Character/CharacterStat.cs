using UnityEngine;

public class CharacterStat : MonoBehaviour
{
    //여기서 이제 어떤 장비를 가지고 있는 지 확인하고 
    GameObject[] objects; //장비
    IInteractable currentInteractable;
    public void Interact()
    {
        if(currentInteractable == null) return;
        currentInteractable.OnInteract(objects);
        Debug.Log("상호작용");
    }

    public void ChangeInteractable(IInteractable interactable)
    {
        currentInteractable = interactable;
    }
}
