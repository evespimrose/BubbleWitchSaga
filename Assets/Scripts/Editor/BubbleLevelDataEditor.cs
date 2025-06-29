using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BubbleLevelData))]
public class BubbleLevelDataEditor : Editor
{
    private const float cellSize = 20f;
    private const float xSpacing = 22f;
    private const float ySpacing = 19f;

    public override void OnInspectorGUI()
    {
        BubbleLevelData data = (BubbleLevelData)target;

        // 행/열 설정
        data.rows = EditorGUILayout.IntField("Rows", data.rows);
        data.columns = EditorGUILayout.IntField("Columns", data.columns);

        int expectedLength = data.rows * data.columns;
        if (data.gridData == null || data.gridData.Length != expectedLength)
        {
            data.gridData = new BubbleCell[expectedLength];
            for (int i = 0; i < expectedLength; i++)
                data.gridData[i] = new BubbleCell();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bubble Grid Layout", EditorStyles.boldLabel);

        // 데이터 수정 UI
        for (int y = 0; y < data.rows; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < data.columns; x++)
            {
                int index = y * data.columns + x;
                BubbleCell cell = data.gridData[index];

                EditorGUILayout.BeginVertical(GUILayout.Width(70));
                cell.hasBubble = EditorGUILayout.Toggle(cell.hasBubble);
                cell.bubbleColor = (BubbleColor)EditorGUILayout.EnumPopup(cell.bubbleColor);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);
        DrawGridPreview(data);

        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }

    void DrawGridPreview(BubbleLevelData data)
    {
        Rect previewRect = GUILayoutUtility.GetRect(400, data.rows * ySpacing + 20);
        Handles.BeginGUI();

        for (int y = 0; y < data.rows; y++)
        {
            for (int x = 0; x < data.columns; x++)
            {
                int index = y * data.columns + x;
                BubbleCell cell = data.gridData[index];
                if (!cell.hasBubble) continue;

                float offsetX = (y % 2 == 1) ? xSpacing / 2f : 0f;

                Vector2 center = new Vector2(
                    previewRect.x + x * xSpacing + offsetX + 10,
                    previewRect.y + y * ySpacing + 10
                );

                Color color = cell.bubbleColor switch
                {
                    BubbleColor.Red => Color.red,
                    BubbleColor.Blue => Color.cyan,
                    BubbleColor.Green => Color.green,
                    _ => Color.gray
                };

                DrawHexagon(center, cellSize / 2f, color);
            }
        }

        Handles.EndGUI();
    }

    void DrawHexagon(Vector2 center, float radius, Color color)
    {
        Vector3[] points = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60 * i - 30;
            float angleRad = Mathf.Deg2Rad * angleDeg;
            points[i] = new Vector3(
                center.x + radius * Mathf.Cos(angleRad),
                center.y + radius * Mathf.Sin(angleRad),
                0
            );
        }

        Handles.color = color;
        Handles.DrawAAConvexPolygon(points);

        Handles.color = Color.black;
        Vector3[] outline = new Vector3[7];
        for (int i = 0; i < 6; i++)
            outline[i] = points[i];
        outline[6] = points[0];
        Handles.DrawPolyLine(outline);
    }

}
