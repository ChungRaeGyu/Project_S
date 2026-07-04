using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInput : MonoBehaviour
{
    CharacterInputAction inputActions;
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    public event Action interact;

    public void InputSetting()
    {
        inputActions = new CharacterInputAction();
        inputActions.Character.Move.performed += OnMove;
        inputActions.Character.Move.canceled += OnMove;
        inputActions.Character.MouseInput.performed += OnLook;
        inputActions.Character.MouseInput.canceled += OnLook;
        inputActions.Character.Interaction.performed += OnInteraction;

    }
    private void OnInteraction(InputAction.CallbackContext context)
    {
        if(context.phase == InputActionPhase.Performed)
        {
            interact?.Invoke();
        }
    }
    public void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            MoveInput = context.ReadValue<Vector2>();
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            MoveInput = Vector2.zero;
        }
    }
    private void OnEnable()
    {
        inputActions.Enable();
    }
    private void OnDisable()
    {
        inputActions.Disable();
    }
    private void OnDestroy()
    {
        inputActions.Character.Move.performed -= OnMove;
    }


}
