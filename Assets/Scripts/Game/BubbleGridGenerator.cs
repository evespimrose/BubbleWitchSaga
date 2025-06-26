using System.Collections.Generic;
using UnityEngine;

public class BubbleGridGenerator : MonoBehaviour
{
    public List<GameObject> bubblePrefab;

    [Header("Grid Settings")]
    public int rows = 12;
    public int columns = 11;
    public float bubbleRadius = 0.5f;

    [Header("Grid Data")]
    public BubbleCell[] gridData; // 길이 = rows * columns

    private GameObject[,] grid;

    void OnValidate()
    {
        if (gridData == null || gridData.Length != rows * columns)
        {
            gridData = new BubbleCell[rows * columns];
            for (int i = 0; i < gridData.Length; i++)
            {
                gridData[i] = new BubbleCell();
            }
        }
    }

    void Start()
    {
        if (bubblePrefab == null)
        {
            Debug.LogError("BubbleGridGenerator: bubblePrefab이 할당되지 않았습니다.");
            return;
        }

        grid = new GameObject[rows, columns];
        GenerateGrid();
    }

    void GenerateGrid()
    {
        float xOffset = bubbleRadius * 2f;
        float yOffset = Mathf.Sqrt(3f) * bubbleRadius;
        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f + 0.95f);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int index = y * columns + x;
                if (index < 0 || index >= gridData.Length) continue;

                BubbleCell cell = gridData[index];
                if (!cell.hasBubble) continue;

                float xPos = x * xOffset;
                if (y % 2 == 1) xPos += bubbleRadius;
                float yPos = -y * yOffset;
                Vector2 spawnPos = origin + new Vector2(xPos, yPos);

                GameObject prefabToUse = GetPrefabByColor(cell.bubbleColor);
                if (prefabToUse == null)
                {
                    Debug.LogWarning($"Prefab이 존재하지 않음: {cell.bubbleColor} 색상.");
                    continue;
                }

                GameObject bubble = Instantiate(prefabToUse, spawnPos, Quaternion.identity, transform);

                Bubble bubbleComp = bubble.GetComponent<Bubble>();
                if (bubbleComp != null)
                {
                    bubbleComp.gridX = x;
                    bubbleComp.gridY = y;
                    bubbleComp.bubbleColor = cell.bubbleColor;
                }

                grid[y, x] = bubble;
            }
        }
    }

    GameObject GetPrefabByColor(BubbleColor color)
    {
        switch (color)
        {
            case BubbleColor.Blue:
                return bubblePrefab.Count > 0 ? bubblePrefab[0] : null;
            case BubbleColor.Green:
                return bubblePrefab.Count > 1 ? bubblePrefab[1] : null;
            case BubbleColor.Red:
                return bubblePrefab.Count > 2 ? bubblePrefab[2] : null;
            default:
                return null;
        }
    }

    public Vector2 FindNearestGridPosition(Vector2 contact)
    {
        float xOffset = bubbleRadius * 2f;
        float yOffset = Mathf.Sqrt(3f) * bubbleRadius;

        // 오리진 기준 설정 (기즈모와 동일)
        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f + 0.95f);

        float minDist = float.MaxValue;
        Vector2 nearestPos = Vector2.zero;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                float xPos = x * xOffset + (y % 2 == 1 ? bubbleRadius : 0);
                float yPos = -y * yOffset;
                Vector2 gridPos = origin + new Vector2(xPos, yPos);

                float dist = Vector2.Distance(contact, gridPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestPos = gridPos;
                }
            }
        }

        return nearestPos;
    }

    public (int, int) FindNearestGridIndex(Vector2 contact)
    {
        float xOffset = bubbleRadius * 2f;
        float yOffset = Mathf.Sqrt(3f) * bubbleRadius;
        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f + 0.95f);

        float minDist = float.MaxValue;
        int nearestX = -1, nearestY = -1;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                float xPos = x * xOffset + (y % 2 == 1 ? bubbleRadius : 0);
                float yPos = -y * yOffset;
                Vector2 gridPos = origin + new Vector2(xPos, yPos);

                float dist = Vector2.Distance(contact, gridPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestX = x;
                    nearestY = y;
                }
            }
        }

        return (nearestX, nearestY);
    }



    public Vector2 GridToWorld(int x, int y)
    {
        float xOffset = bubbleRadius * 2f;
        float yOffset = Mathf.Sqrt(3f) * bubbleRadius;

        float xPos = x * xOffset + (y % 2 == 1 ? bubbleRadius : 0);
        float yPos = -y * yOffset;

        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f);
        return origin + new Vector2(xPos, yPos);
    }

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        Gizmos.color = Color.green;

        float xOffset = bubbleRadius * 2f;
        float yOffset = Mathf.Sqrt(3f) * bubbleRadius;
        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f + 0.95f);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                float xPos = x * xOffset;
                if (y % 2 == 1) xPos += bubbleRadius;

                float yPos = -y * yOffset;
                Vector2 pos = origin + new Vector2(xPos, yPos);

                Gizmos.DrawWireSphere(pos, bubbleRadius * 0.95f); // 살짝 작게 그리기
            }
        }
#endif
    }

}
