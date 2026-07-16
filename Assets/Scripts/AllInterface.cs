using UnityEngine;

public interface IInteractable
{
    public void OnInteract(GameObject[] obj=null);
}

public interface IItemUse
{
    public void Use();
}
public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(int amount, GameObject source);
}
public class AllInterface : MonoBehaviour
{

}
