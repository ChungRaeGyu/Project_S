using System;
using UnityEngine;

public class CharacterLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 0.2f;
    [SerializeField] private float minXRotation = -70f;
    [SerializeField] private float maxXRotation = 70f;
    private float yRotation;
    private float xRotation;
    private Camera camera;

    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayer; // Interactable layer

    private Collider currentCollider;
    public event Action<IInteractable> changeInteractable;
    public event Action<GameObject> changeItem;
    public void Set()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
        SetCamera();
    }

    private void SetCamera()
    {
        camera = Camera.main;
        camera.transform.SetParent(transform);
        camera.transform.position = transform.position + new Vector3(0, 1, 0);
        camera.transform.rotation = Quaternion.identity;
    }
    public void UpdateLook(Vector2 mouseDelta)
    {
        Look(mouseDelta);
        CheckInteractable();
    }
    private void Look(Vector2 mouseDelta)
    {
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;
        // 좌우 회전: 캐릭터 몸통이 회전
        yRotation += mouseX;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // 상하 회전: 카메라만 회전
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minXRotation, maxXRotation);

        camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    public void CheckInteractable()
    {
        Ray ray = new Ray(camera.transform.position, camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
        {
            if (currentCollider == hit.collider) return;

            currentCollider = hit.collider;
            
            if(currentCollider.TryGetComponent(out IInteractable co))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                changeInteractable?.Invoke(interactable);
            }
            else
            {
                changeItem?.Invoke(currentCollider.gameObject);
            }

        }
        else
        {
            if(currentCollider ==null) return;

            currentCollider = null;
            changeInteractable?.Invoke(null);
        }
    }
}
