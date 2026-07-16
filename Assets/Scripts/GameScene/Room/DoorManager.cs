using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬에 미리 배치된 2D UI 버튼들을 런타임에 생성되는 문과 연결합니다.
/// </summary>
public class DoorManager : MonoBehaviour
{
    [Header("StartRoom 문 버튼")]
    [SerializeField] private Button[] startDoorButtons;

    [Header("Piece 문 버튼 (순서대로)")]
    [SerializeField] private Button[] pieceDoorButtons;

    [Header("ShopRoom 문 버튼 (순서대로)")]
    [SerializeField] private Button[] shopDoorButtons;

    [Header("ReadyRoom 문 버튼 (순서대로)")]
    [SerializeField] private Button[] readyDoorButtons;

    private int startIndex = 0;
    private int pieceIndex = 0;
    private int shopIndex = 0;
    private int readyIndex = 0;

    private PieceDoor startDoor = null;

    public static DoorManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // -----------------------------------------------
    // PieceDoor를 직접 받아 등록
    // -----------------------------------------------
    public void RegisterStartDoor(PieceDoor door)
    {
        startDoor = door;
        startDoor.OnOpened += HandleRoundStartDoorOpened;
        RegisterDoor(door, startDoorButtons, ref startIndex, "StartRoom");
    }

    // StartRoom 또는 ReadyRoom 문이 열리면(플레이어 상호작용으로) 다음 라운드를 시작한다.
    // RoundManager.RoundStart()가 알아서 "다음" 라운드를 계산하고, 마스터에서만 실제로 동작하므로
    // 여기서 별도 IsMasterClient 체크나 라운드 번호 지정은 불필요.
    private void HandleRoundStartDoorOpened()
    {
        Debug.Log("[DoorManager] 문이 열려서 다음 라운드를 시작합니다.");
        RoundManager.Instance?.RoundStart();
    }

    public void RegisterPieceDoor(PieceDoor door)
    {
        RegisterDoor(door, pieceDoorButtons, ref pieceIndex, "Piece");
    }

    public void RegisterShopDoor(PieceDoor door)
    {
        RegisterDoor(door, shopDoorButtons, ref shopIndex, "ShopRoom");
    }

    public void RegisterReadyDoor(PieceDoor door)
    {
        // ReadyRoom 문은 안전지대를 나와 다음 Piece(사냥 구역)로 들어가는 문이므로, 열리면 다음 라운드를 시작한다.
        door.OnOpened += HandleRoundStartDoorOpened;
        RegisterDoor(door, readyDoorButtons, ref readyIndex, "ReadyRoom");
    }

    // -----------------------------------------------
    // 공통 등록 로직
    // -----------------------------------------------
    private void RegisterDoor(PieceDoor door, Button[] buttons, ref int index, string label)
    {
        if (index >= buttons.Length)
        {
            Debug.LogWarning($"[DoorManager] {label} 버튼이 부족합니다. '{door.gameObject.name}'에 연결할 버튼이 없습니다.");
            door.Init(null);
            return;
        }

        door.Init(buttons[index]);
        Debug.Log($"[DoorManager] {label} '{door.gameObject.name}' -> 버튼[{index}] 연결 완료.");
        index++;
    }

    // -----------------------------------------------
    // 씬 재시작 시 초기화
    // -----------------------------------------------
    public void ResetDoors()
    {
        startIndex = 0;
        pieceIndex = 0;
        shopIndex = 0;
        readyIndex = 0;
        startDoor = null; // [변경]

        ResetButtons(startDoorButtons);
        ResetButtons(pieceDoorButtons);
        ResetButtons(shopDoorButtons);
        ResetButtons(readyDoorButtons);
    }

    private void ResetButtons(Button[] buttons)
    {
        foreach (Button btn in buttons)
        {
            if (btn != null)
            {
                btn.interactable = true;
                btn.gameObject.SetActive(true);
            }
        }
    }
}