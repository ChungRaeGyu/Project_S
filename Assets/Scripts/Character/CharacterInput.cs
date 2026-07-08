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
        inputActions.Character.Test.performed += OnTest;
    }

    private void OnTest(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            if(Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
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
    public void InputOnEnable()
    {
        inputActions.Enable();
    }
    public void InputOnDisable()
    {
        inputActions.Disable();
    }
    public void InputOnDestroy()
    {
        inputActions.Character.Move.performed -= OnMove;
    }


}
