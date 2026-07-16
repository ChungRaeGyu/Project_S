using UnityEngine;

public class CharacterStat : MonoBehaviour, IDamageable
{
    //여기서 이제 어떤 장비를 가지고 있는 지 확인하고 
    public GameObject[] equips = new GameObject[2]; //장비
    IInteractable currentInteractable;
    GameObject currentItem; //보고 있는 장비

    public bool IsDead => throw new System.NotImplementedException();
    public void TakeDamage(int amount, GameObject source)
    {
        throw new System.NotImplementedException();
    }
    public void ChangeItemInteract(GameObject obj)
    {
        currentItem = obj;
    }
    public GameObject GetcurrentItem()
    {
        return currentItem;
    }
    public void Interact()
    {
        if (currentInteractable == null) 
        {
            Debug.Log("없음");
            return;
        }
        currentInteractable.OnInteract(equips);
        Debug.Log("상호작용");
    }

    public void ChangeInteractable(IInteractable interactable)
    {
        currentInteractable = interactable;
    }


}
