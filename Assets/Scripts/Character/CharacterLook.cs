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
    [SerializeField] private Transform cameraPivot;
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

    public void SetCamera()
    {
        camera = Camera.main;
        camera.transform.SetParent(cameraPivot);
        camera.transform.localPosition = new Vector3(0, 0, 0);
        camera.transform.localRotation = Quaternion.identity;
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

        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    public void CheckInteractable()
    {
        Ray ray = new Ray(camera.transform.position, camera.transform.forward);
        Debug.DrawRay(
camera.transform.position,
camera.transform.forward * interactDistance,
Color.red
);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
        {
            Debug.Log("바라보는 중");
            if (currentCollider == hit.collider) return;
            Debug.Log("새로운 것");
            currentCollider = hit.collider;
            
            if(currentCollider.TryGetComponent(out IInteractable co))
            {
                Debug.Log("Interactable이 있음!");
                changeInteractable?.Invoke(co);
            }
            else if(currentCollider.TryGetComponent(out IItemUse item))
            {
                Debug.Log("Interactable이 없음!" + hit.collider.name);
                
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
