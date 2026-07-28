using System;
using System.Collections;
using UnityEngine;

public class CharacterStat : MonoBehaviour, IDamageable
{
    //여기서 이제 어떤 장비를 가지고 있는 지 확인하고 
    public GameObject[] equips = new GameObject[2]; //장비
    IInteractable currentInteractable;
    IItemUse currentItem; //보고 있는 장비

    [SerializeField]private int hp = 100;
    public int fear = 3;
    [SerializeField] float fearturm = 1f;
    [SerializeField] int fearmount = 1;
    public event Action Onfear;
    
    public float speed = 10f;

    public bool IsDead => throw new System.NotImplementedException();
    public void TakeDamage(int amount, GameObject source)
    {
        throw new System.NotImplementedException();
    }
    public void ChangeItemInteract(IItemUse obj)
    {
        currentItem = obj;
    }
    public IItemUse GetcurrentItem()
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

    internal void Recovery(int recovery)
    {
        hp += recovery;
        hp = Math.Min(hp, 100);
    }
    internal void FearRecovery(int recovery)
    {
        hp += recovery;
        hp = Math.Min(hp, 100);
    }
    public void StatUpdate()
    {
        StartCoroutine(CStatUpdate());
    }
    IEnumerator CStatUpdate()
    {
        while (hp>0)
        {
            yield return new WaitForSecondsRealtime(fearturm);
            fear = Math.Max(fear - fearmount, 0);
            if (fear == 0)
            {
                Onfear?.Invoke();
            }
            yield return new WaitUntil(()=> fear > 0);
        }
    }
}
