using UnityEngine;

public interface IInteractable
{
    public void OnInteract(GameObject[] obj=null);
}

public interface IItemUse
{
    public void Use();
}
public class AllInterface : MonoBehaviour
{

}
