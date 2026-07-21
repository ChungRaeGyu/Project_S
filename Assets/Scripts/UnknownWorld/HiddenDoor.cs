using UnityEngine;

// 미지 차원 안, 숨겨진 문에 부착. 플레이어가 바라보고 상호작용(CharacterLook의 레이캐스트 + CharacterStat.Interact)하면 열린다.
// UnknownWorld.Enter()가 Initialize()로 이 인스턴스의 주인을 알려주면,
// 상호작용 시 FearDimensionController.ExitDimension()을 호출해 탈출 처리한다.
public class HiddenDoor : MonoBehaviour, IInteractable
{
    FearDimensionController owner;

    public void Initialize(FearDimensionController controller)
    {
        owner = controller;
    }

    public void OnInteract(GameObject[] obj = null)
    {
        owner?.ExitDimension();
    }
}
