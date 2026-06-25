using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private Button randomJoinBtn;

    private void Start()
    {
        randomJoinBtn.onClick.AddListener(NetworkManager.Instance.RandomRoom);
        Debug.Log("버튼 장착");
    }
}
