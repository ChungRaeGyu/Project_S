using Photon.Pun;
using System;
using UnityEngine;

public class CharacterBehavior : MonoBehaviour
{
    [SerializeField] private float speed=10f;
    private Vector2 input;
    Rigidbody rd;
    private void Awake()
    {
        rd = GetComponent<Rigidbody>();
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
}
