using UnityEngine;

public class ItemBasic : MonoBehaviour,IItemUse
{
    public ItemData itemData;
    int num;
    public GameObject player;
    public virtual void Use()
    {
        Debug.Log("아이템 사용");
    }
    private void SetPosition()
    {
        GetComponent<Rigidbody>().useGravity = false;
        transform.SetParent(Camera.main.transform); //이거 왜 안되냐
        transform.position = itemData.position;
        transform.rotation = Quaternion.Euler(itemData.rotation);
    }
    public void Setting(int num,GameObject player)
    {
        this.num = num;
        this.player = player;
        SetPosition();
    }
}
