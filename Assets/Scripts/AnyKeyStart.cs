using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnyKeyStart : MonoBehaviour
{
    [SerializeField] private TMP_Text startText;
    [SerializeField] private TMP_Text status;

    [SerializeField] float speed = 1.3f;
    bool check = true;
    AnyKey playerInput;
    private void Awake()
    {
        playerInput = new AnyKey();
        playerInput.KeyBoardMouse.AnykeyStart.started += OnAnyKey;
    }

    private void OnEnable()
    {
        playerInput.Enable();
    }
    public void OnAnyKey(InputAction.CallbackContext context)
    {
        if (check)
        {
            NetworkManager.Instance.ConnectButton();
            check = false;
        }
    }

    private void Update()
    {
        float alpha = Mathf.PingPong(Time.time * speed, 1f);
        startText.alpha = alpha;
        status.text = PhotonNetwork.NetworkClientState.ToString();
    }
    // Update is called once per frame
}
