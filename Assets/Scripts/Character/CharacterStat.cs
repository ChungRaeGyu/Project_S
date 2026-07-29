using System;
using System.Collections;
using UnityEngine;

public class CharacterStat : MonoBehaviour, IDamageable
{
    //여기서 이제 어떤 장비를 가지고 있는 지 확인하고 
    public GameObject[] equips = new GameObject[2]; //장비
    IInteractable currentInteractable;
    GameObject currentItem; //보고 있는 장비

    [SerializeField]private int hp = 100;
    public int fear = 3;
    [SerializeField] float fearturm = 1f;
    [SerializeField] int fearmount = 1;
    public event Action Onfear;

    [SerializeField] private int deadCount = 0;
    
    public float speed = 10f;

    public bool IsDead => throw new System.NotImplementedException();

    private void Update()
    {
        if (hp <= 0)
        {
            Dead();
            hp = 100;
        }
    }
    public void TakeDamage(int amount, GameObject source)
    {
        //뭐 어쩌다가 
        if (hp <= 0)
        {
            Dead();
        }
    }
    public void Dead()
    {
        StartCoroutine(CDead());//FindObjects를 안쓰는 방법이 있긴 한데 귀찮..
        //안쓸 수 있는 방법 각 EchoObject에서 생성됐을때 각자 어딘가에 보관을 한다. 그리고 죽었을때 invoke로 호출해버리면 되긴함
    }

    IEnumerator CDead()
    {
        int batchSize = 20;
        if (deadCount == 0)
        {
            //여기선 눈이 안보이도록 해야하는데 음;; 근데 음..
            //건물과 모든 플레이어를 안보이도록 해야한단 말이지
            //제일 간단한법
            EchoObject[] objects = FindObjectsByType<EchoObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i].SetReveal(0);
                if (i % batchSize == 0)
                {
                    yield return null;
                }
            }
            hp = 100;
        }
        else
        {
            EchoObject[] objects = FindObjectsByType<EchoObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i].SetReveal(1);
                if (i % batchSize == 0)
                {
                    yield return null;
                }
            }
        }
        deadCount++;
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
