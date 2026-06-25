using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬에 미리 배치된 2D UI 버튼들을 런타임에 생성되는 문과 연결합니다.
/// StartRoom, Piece, ShopRoom, ReadyRoom 각각 버튼 배열을 따로 관리합니다.
/// </summary>
public class DoorManager : MonoBehaviour
{
    // [변경] StartRoom 버튼 배열 추가
    [Header("StartRoom 문 버튼")]
    [SerializeField] private Button[] startDoorButtons;

    [Header("Piece 문 버튼 (순서대로)")]
    [SerializeField] private Button[] pieceDoorButtons;

    [Header("ShopRoom 문 버튼 (순서대로)")]
    [SerializeField] private Button[] shopDoorButtons;

    [Header("ReadyRoom 문 버튼 (순서대로)")]
    [SerializeField] private Button[] readyDoorButtons;

    // 각 문 종류별 인덱스 카운터
    private int startIndex = 0;  // [변경]
    private int pieceIndex = 0;
    private int shopIndex = 0;
    private int readyIndex = 0;

    // 싱글톤 - RoomGenerator에서 쉽게 접근하기 위해 사용
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
    // [변경] RoomGenerator에서 StartRoom 생성 시 호출
    // -----------------------------------------------
    public void RegisterStartDoor(GameObject startObject)
    {
        RegisterDoor(startObject, startDoorButtons, ref startIndex, "StartRoom");
    }

    // -----------------------------------------------
    // RoomGenerator에서 Piece 생성 시 호출
    // -----------------------------------------------
    public void RegisterPieceDoor(GameObject pieceObject)
    {
        RegisterDoor(pieceObject, pieceDoorButtons, ref pieceIndex, "Piece");
    }

    // -----------------------------------------------
    // RoomGenerator에서 ShopRoom 생성 시 호출
    // -----------------------------------------------
    public void RegisterShopDoor(GameObject shopObject)
    {
        RegisterDoor(shopObject, shopDoorButtons, ref shopIndex, "ShopRoom");
    }

    // -----------------------------------------------
    // RoomGenerator에서 ReadyRoom 생성 시 호출
    // -----------------------------------------------
    public void RegisterReadyDoor(GameObject readyObject)
    {
        RegisterDoor(readyObject, readyDoorButtons, ref readyIndex, "ReadyRoom");
    }

    // -----------------------------------------------
    // 공통 등록 로직 - 오브젝트에서 PieceDoor를 찾아 버튼 연결
    // -----------------------------------------------
    private void RegisterDoor(GameObject obj, Button[] buttons, ref int index, string label)
    {
        PieceDoor door = obj.GetComponentInChildren<PieceDoor>();
        if (door == null)
        {
            Debug.LogWarning($"[DoorManager] '{obj.name}'에서 PieceDoor 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        if (index >= buttons.Length)
        {
            Debug.LogWarning($"[DoorManager] {label} 버튼이 부족합니다. '{obj.name}'에 연결할 버튼이 없습니다.");
            door.Init(null);
            return;
        }

        door.Init(buttons[index]);
        Debug.Log($"[DoorManager] {label} '{obj.name}' -> 버튼[{index}] 연결 완료.");
        index++;
    }

    // -----------------------------------------------
    // 씬 재시작 시 인덱스와 버튼 상태 초기화
    // -----------------------------------------------
    public void ResetDoors()
    {
        startIndex = 0;  // [변경]
        pieceIndex = 0;
        shopIndex = 0;
        readyIndex = 0;

        ResetButtons(startDoorButtons);  // [변경]
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