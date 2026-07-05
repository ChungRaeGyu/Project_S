using UnityEngine;

public class CharacterLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 0.2f;
    [SerializeField] private float minXRotation = -70f;
    [SerializeField] private float maxXRotation = 70f;
    private float yRotation;

    private float xRotation;
    public void Set()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
        SetCamera();
    }

    private void SetCamera()
    {
        Camera camera = Camera.main;
        camera.transform.SetParent(transform);
        camera.transform.position = transform.position + new Vector3(0, 1, 0);
        camera.transform.rotation = Quaternion.identity;
    }
    public void Look(Vector2 mouseDelta)
    {
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;
        // 좌우 회전: 캐릭터 몸통이 회전
        yRotation += mouseX;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // 상하 회전: 카메라만 회전
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minXRotation, maxXRotation);

        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

/*    private void CheckInteractable()
    {
        currentInteractable = null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                currentInteractable = interactable;
            }
        }
    }*/
}
