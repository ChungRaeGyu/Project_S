using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInput : MonoBehaviour
{
    CharacterInputAction inputActions;
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    public event Action interact;

    public event Action itemInteract;

    public event Action<int> itemDrop;

    public event Action ecoLocation;

    public event Action<InputAction.CallbackContext> OnMouseButton;
    public void InputSetting()
    {
        inputActions = new CharacterInputAction();
        inputActions.Character.Move.performed += OnMove;
        inputActions.Character.Move.canceled += OnMove;
        inputActions.Character.MouseInput.performed += OnLook;
        inputActions.Character.MouseInput.canceled += OnLook;
        inputActions.Character.Interaction.performed += OnInteraction;
        inputActions.Character.ItemInteraction.performed += OnItemInteraction;
        inputActions.Character.Test.performed += OnTest;
        inputActions.Character.ItemUse.performed += OnItemUse;
        inputActions.Character.ItemDrop.performed += OnItemDrop;
        inputActions.Character.EcoLocation.performed += OnEcoLocation;
    }

    private void OnEcoLocation(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed) 
        {
            ecoLocation?.Invoke();
        }
    }

    private void OnItemDrop(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            if(context.control == Keyboard.current.digit1Key)
                itemDrop?.Invoke(0);
            else if(context.control == Keyboard.current.digit2Key)
                itemDrop?.Invoke(1);

        }
    }

    private void OnItemUse(InputAction.CallbackContext context)
    {
        if(context.phase == InputActionPhase.Performed)
            OnMouseButton?.Invoke(context);
    }

    private void OnTest(InputAction.CallbackContext context)
    {
        //Tab키를 눌러 마우스의 자유를 얻는다.
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
            Debug.Log("상호작용");
            interact?.Invoke();
        }
    }
    private void OnItemInteraction(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            itemInteract?.Invoke();
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
