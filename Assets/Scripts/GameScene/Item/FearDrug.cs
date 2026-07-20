using UnityEngine;

public class FearDrug : ItemBasic
{
    public int recovery = 20;

    public override void Use()
    {
        player.GetComponent<CharacterStat>().FearRecovery(recovery);
        Debug.Log("공포심 회복");
        base.Use(); //삭제가 담겨있다.
    }
}
