using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// Piece, ShopRoom, ReadyRoom, StartRoom ¹®¿¡ ¸ğµÎ »ç¿ë °¡´ÉÇÑ ¹ü¿ë ¹® ½ºÅ©¸³Æ®.
/// ºÎ¸ğ ¿ÀºêÁ§Æ®¿¡ ºÎÂø. ÀÚ½Ä ¿ÀºêÁ§Æ®°¡ ½ÇÁ¦·Î ½½¶óÀÌµåµÇ´Â ¹®.
/// OnInteract() È£Ãâ ½Ã:
///   - ºÎ¸ğÀÇ BoxCollider ·ÎÄÃ·Î ºñÈ°¼ºÈ­ (´Ù½Ã ÄÑÁöÁö ¾ÊÀ½)
///   - ÀÚ½Ä ¹® ¿ÀºêÁ§Æ® À§·Î ½½¶óÀÌµå (RPC·Î ÀüÃ¼ µ¿±âÈ­)
/// </summary>
public class PieceDoor : MonoBehaviourPun, IInteractable
{
    [Header("¹® ¼³Á¤")]
    [SerializeField] private float openHeight = 4f;
    [SerializeField] private float openDuration = 1f;
    [SerializeField] private float closeDelay = 10f;

    // [º¯°æ] ½½¶óÀÌµåÇÒ ÀÚ½Ä ¿ÀºêÁ§Æ®
    [Header("½½¶óÀÌµåÇÒ ÀÚ½Ä ¹® ¿ÀºêÁ§Æ®")]
    [SerializeField] private Transform doorChild;

    private Button linkedButton;
    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;
    private bool isMoving = false;

    // [º¯°æ] ºÎ¸ğÀÇ BoxCollider ÂüÁ¶
    private BoxCollider doorCollider;

    // ì´ ë¬¸ì´ ì—´ë¦¬ê¸° ì‹œì‘í•  ë•Œ ë°œìƒ (ì˜ˆ: DoorManagerê°€ StartRoom ë¬¸ì— êµ¬ë…í•´ì„œ ë¼ìš´ë“œ ì‹œì‘ íŠ¸ë¦¬ê±°ë¡œ ì‚¬ìš©)
    public event System.Action OnOpened;

    // -----------------------------------------------
    // DoorManager¿¡¼­ È£Ãâ - ¹öÆ° ¿¬°á ¹× À§Ä¡ ÃÊ±âÈ­
    // -----------------------------------------------
    public void Init(Button button)
    {
        // [º¯°æ] ÀÚ½Ä ±âÁØÀ¸·Î À§Ä¡ ÃÊ±âÈ­
        if (doorChild != null)
        {
            closedPos = doorChild.position;
            openPos = closedPos + new Vector3(0f, openHeight, 0f);
        }
        else
        {
            Debug.LogWarning($"[PieceDoor] '{gameObject.name}' doorChild°¡ ¿¬°áµÇÁö ¾Ê¾Ò½À´Ï´Ù.");
        }

        // [º¯°æ] ºÎ¸ğÀÇ BoxCollider Ä³½Ì
        doorCollider = GetComponent<BoxCollider>();

        linkedButton = button;

        if (linkedButton != null)
        {
            linkedButton.onClick.AddListener(OnButtonClick);
            Debug.Log($"[PieceDoor] '{gameObject.name}' ¹öÆ° ¿¬°á ¼º°ø.");
        }
        else
        {
            Debug.LogWarning($"[PieceDoor] '{gameObject.name}'¿¡ ¿¬°áµÈ ¹öÆ°ÀÌ ¾ø½À´Ï´Ù.");
        }
    }

    // -----------------------------------------------
    // ¹öÆ° Å¬¸¯ ½Ã - RPC·Î ¸ğµç Å¬¶óÀÌ¾ğÆ®¿¡ Àü´Ş
    // -----------------------------------------------
    private void OnButtonClick()
    {
        Debug.Log($"[PieceDoor] ¹öÆ° Å¬¸¯µÊ. isOpen={isOpen} isMoving={isMoving}");
        if (isOpen || isMoving) return;

        photonView.RPC("RPC_OpenDoor", RpcTarget.AllViaServer);
    }

    // -----------------------------------------------
    // ¸ğµç Å¬¶óÀÌ¾ğÆ®¿¡¼­ ½ÇÇàµÇ´Â RPC
    // -----------------------------------------------
    [PunRPC]
    private void RPC_OpenDoor()
    {
        Debug.Log($"[PieceDoor] RPC_OpenDoor ¼ö½Å. '{gameObject.name}'");
        if (isOpen || isMoving) return;
        OnOpened?.Invoke();
        StartCoroutine(OpenThenClose());
    }

    // -----------------------------------------------
    // ¿­±â -> ´ë±â -> ´İ±â
    // -----------------------------------------------
    private IEnumerator OpenThenClose()
    {
        yield return StartCoroutine(SlideDoor(closedPos, openPos));
        yield return new WaitForSeconds(closeDelay);
        yield return StartCoroutine(SlideDoor(openPos, closedPos));

        if (linkedButton != null)
            linkedButton.interactable = true;
    }

    // -----------------------------------------------
    // ÀÚ½Ä ¹® ½½¶óÀÌµå
    // [º¯°æ] transform ´ë½Å doorChild ±âÁØÀ¸·Î ÀÌµ¿
    // -----------------------------------------------
    private IEnumerator SlideDoor(Vector3 from, Vector3 to)
    {
        isMoving = true;
        isOpen = (to == openPos);

        if (linkedButton != null)
            linkedButton.interactable = false;

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            if (doorChild != null)
                doorChild.position = Vector3.Lerp(from, to, t); // [º¯°æ]
            yield return null;
        }

        if (doorChild != null)
            doorChild.position = to;
        isMoving = false;
    }

    // -----------------------------------------------
    // »óÈ£ÀÛ¿ë ½Ã È£Ãâ
    // [º¯°æ] ºÎ¸ğ BoxCollider ·ÎÄÃ·Î ²ô±â + RPC·Î ÀÚ½Ä ¹® ¿­±â
    // -----------------------------------------------
    public void OnInteract(GameObject[] obj = null)
    {
        Debug.Log($"[PieceDoor] OnInteract È£Ãâ. isOpen={isOpen} isMoving={isMoving}");
        if (isOpen || isMoving) return;

        // [º¯°æ] ºÎ¸ğ BoxCollider ·ÎÄÃ·Î ºñÈ°¼ºÈ­ (´Ù½Ã ÄÑÁöÁö ¾ÊÀ½)
        if (doorCollider != null)
            doorCollider.enabled = false;

        photonView.RPC("RPC_OpenDoor", RpcTarget.AllViaServer);
    }

    // -----------------------------------------------
    // ÄÚµå·Î Á÷Á¢ ¿­ ¶§ »ç¿ë (Ä³¸¯ÅÍ ½Ã½ºÅÛ ¿¬µ¿¿ë)
    // -----------------------------------------------
    public void OpenDoor()
    {
        if (isOpen || isMoving) return;
        photonView.RPC("RPC_OpenDoor", RpcTarget.AllViaServer);
    }

}