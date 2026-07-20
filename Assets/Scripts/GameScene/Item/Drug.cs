using UnityEngine;

public class Drug : ItemBasic
{
    public int recovery = 20;

    public override void Use()
    {
        player.GetComponent<CharacterStat>().Recovery(recovery);
        Debug.Log("체력 회복");
        base.Use(); //삭제가 담겨있다.
    }
}
