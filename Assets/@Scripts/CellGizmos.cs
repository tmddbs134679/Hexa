#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 헥사 보드 셀 좌표를 씬 뷰에 Gizmos로 표시하는 스크립트
/// </summary>
[ExecuteAlways]
public class CellGizmos : MonoBehaviour
{
    [Header("Refs")]
    public BoardState board;

    [Header("Gizmo Options")]
    public bool showLabels = true;           // ✔ Inspector에서 On/Off
    public Color labelColor = Color.white;   // 글자 색
    public int fontSize = 12;                // 글자 크기
    public Vector3 worldOffset = new(0, 0.2f, 0); // 타일 위로 살짝 띄우기

    void OnDrawGizmos()
    {
        if (!showLabels) return;     // 끔
        if (!board) return;          // 참조 없음

        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = labelColor },
            fontSize = fontSize,
            alignment = TextAnchor.MiddleCenter
        };

        foreach (var c in board.AllCells())
        {
            var wp = board.WorldCenter(c);
            Handles.Label(wp + worldOffset, $"{c.x},{c.y}", style);
        }
    }
}
#endif
