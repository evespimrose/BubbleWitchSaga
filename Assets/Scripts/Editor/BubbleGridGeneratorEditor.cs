using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BubbleGridGenerator))]
public class BubbleGridGeneratorEditor : Editor
{
    private const float cellWidth = 70f;
    private const float cellHeight = 25f;
    private const float colorBoxSize = 16f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BubbleGridGenerator generator = (BubbleGridGenerator)target;

        if (generator.gridData == null || generator.gridData.Length != generator.rows * generator.columns)
        {
            EditorGUILayout.HelpBox("Grid Data가 초기화되지 않았습니다.\nGrid 설정을 바꾼 후 저장하거나 Play 해보세요.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bubble Grid Editor", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        for (int y = 0; y < generator.rows; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < generator.columns; x++)
            {
                int index = y * generator.columns + x;
                BubbleCell cell = generator.gridData[index];

                EditorGUILayout.BeginVertical(GUILayout.Width(cellWidth));

                // Toggle (hasBubble)
                cell.hasBubble = EditorGUILayout.Toggle(cell.hasBubble, GUILayout.Width(cellWidth));

                // Enum Popup (Color)
                cell.bubbleColor = (BubbleColor)EditorGUILayout.EnumPopup(cell.bubbleColor, GUILayout.Width(cellWidth));

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(generator);
        }
    }
}
