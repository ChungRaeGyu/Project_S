using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterBehavior : MonoBehaviour
{
    
    private Vector2 input;
    Rigidbody rd;
    CharacterStat stat;
    private void Awake()
    {
        rd = GetComponent<Rigidbody>();
    }
    public void Init(CharacterStat stat)
    {
        this.stat = stat;
    }

    internal void UpdateCharacter(Vector2 moveInput)
    {
        input = moveInput;
    }
    public void Move()
    {
       //Vector3 movement = new Vector3(input.x, 0, input.y) * speed;
       Vector3 movement = transform.forward*input.y + transform.right * input.x;
        rd.linearVelocity = movement*stat.speed;
    }

    internal void OnMouseButton(InputAction.CallbackContext context)
    {
        int num = context.control == Mouse.current.leftButton ? 0 : 1;

        if (stat.equips[num] == null) return;
        if(stat.equips[num].TryGetComponent<IItemUse>(out var itemUse))
        {
            //뭔가 광클 방지가 필요할 꺼 같 긴해
            itemUse.Use();
            RemoveItem(num);
        }
    }

    public void GetItem()
    {
        if(stat.GetcurrentItem()==null) return;
        //아이템키는 E, 상점에서 소환후 땅에 떨어뜨리기
        if (stat.equips[1] != null)
        {
            DropItem(1);
        }
        int num = stat.equips[0] == null ? 0 : 1;
        stat.equips[num] = stat.GetcurrentItem();
        stat.equips[num].GetComponent<ItemBasic>().Setting(num,transform.gameObject);
    }

    public void DropItem(int num)
    {
        if (stat.equips[num] != null)
        {
            //무기 프리펩을 드랍시켜야한다.   
            stat.equips[num].transform.SetParent(null);
            var rigid = stat.equips[num].GetComponent<Rigidbody>();
            rigid.isKinematic = false;
            rigid.useGravity = true;
            stat.equips[num] = null;
            Debug.Log("아이템 드랍");

        }
        //아이템 버리기 만들기
    }
    public void RemoveItem(int num)
    {
        //아이템 사용시 삭제 시키기
        if (stat.equips[num] != null)
        {
            PhotonNetwork.Destroy(stat.equips[num]);
            stat.equips[num] = null;
        }
    }

    internal void OnChangeCamera()
    {
        //살아있는 애들중에 하나를 골라야한단 말이지
        if (stat.IsAlive) return;
        var temp = RoundManager.Instance.GetPlayers();
        Debug.Log("변경!");
        foreach (GameObject playerObj in RoundManager.Instance.GetPlayers())
        {
            CharacterManager player = playerObj.GetComponent<CharacterManager>();
            if (!player.stat.IsAlive)
                continue;

            player.characterLook.SetCamera();
            break;
        }
    }
}
