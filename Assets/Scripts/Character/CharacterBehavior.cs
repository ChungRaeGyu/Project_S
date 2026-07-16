using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterBehavior : MonoBehaviour
{
    [SerializeField] private float speed=10f;
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
    private void Move()
    {
       //Vector3 movement = new Vector3(input.x, 0, input.y) * speed;
       Vector3 movement = transform.forward*input.y + transform.right * input.x;
        rd.linearVelocity = movement*speed;
    }
    private void FixedUpdate()
    {
        Move();
    }
    internal void OnMouseButton(InputAction.CallbackContext context)
    {
/*        if (context.control == Mouse.current.leftButton)
        {
            if (stat.equips[0] == null) return;
            if (stat.equips[0].TryGetComponent<IItemUse>(out var itemUse))
            {
                itemUse.Use();
            }
        }
        else if (context.control == Mouse.current.rightButton)
        {
            if (stat.equips[1] == null) return;
            if (stat.equips[1].TryGetComponent<IItemUse>(out var itemUse))
            {
                itemUse.Use();
            }
        }*/
    }

    public void GetItem()
    {
        //아이템키는 E, 상점에서 소환후 땅에 떨어뜨리기
        if (stat.equips[1] != null)
        {
            //Items[1]을 버린다.
        }
        int num = stat.equips[0] == null ? 0 : 1;
        stat.equips[num] = stat.GetcurrentItem();
    }

    public void RemoveItem(GameObject item)
    {
        //키를 1번, 2번으로 구분
        //아이템 버리기 만들기
    }


}
