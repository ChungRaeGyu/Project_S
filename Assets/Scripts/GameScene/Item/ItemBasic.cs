using Photon.Pun;
using UnityEngine;

public class ItemBasic : MonoBehaviour,IItemUse
{
    public ItemData itemData;
    int num;
    protected GameObject player;
    public virtual void Use()
    {
        if (itemData.canUse)
        {
            PhotonNetwork.Destroy(this.gameObject);
        }
        else
        {
            Debug.Log("사용할 수 있는 아이템이 아닙니다.");
        }
    }
    private void SetPosition()
    {
        var rigidbody = GetComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;

        transform.SetParent(Camera.main.transform); 
        transform.position = itemData.position[num];
        transform.rotation = Quaternion.Euler(itemData.rotation[num]);
    }
    public void Setting(int num,GameObject player)
    {
        this.num = num;
        this.player = player;
        SetPosition();
    }
    public void Used()
    {
        // 아이템 사용 후 제거
        PhotonNetwork.Destroy(this.gameObject);
    }
}
