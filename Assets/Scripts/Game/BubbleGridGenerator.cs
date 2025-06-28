using System.Collections.Generic;
using Unity.VisualScripting;
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
        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f + 5.95f);

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

    public GameObject GetPrefabByColor(BubbleColor color)
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

        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f + 5.95f);

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

    public (int, int) FindNearestGridIndex(Vector2 worldPos)
    {
        float xOffset = bubbleRadius * 2f;
        float yOffset = Mathf.Sqrt(3f) * bubbleRadius;
        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f + 5.95f);

        Vector2 localPos = worldPos - origin;

        int y = Mathf.RoundToInt(-localPos.y / yOffset);
        float xPosOffset = (y % 2 == 1) ? bubbleRadius : 0f;
        int x = Mathf.RoundToInt((localPos.x - xPosOffset) / xOffset);

        x = Mathf.Clamp(x, 0, columns - 1);
        y = Mathf.Clamp(y, 0, rows - 1);

        Debug.Log($"FindNearestGridIndex 완료: Grid=({x},{y}), contactPoint={worldPos}");
        return (x, y);
    }

    public bool IsCellOccupied(int x, int y)
    {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return true;
        return grid[y, x] != null;
    }

    public void SetCellOccupied(int x, int y, GameObject bubble)
    {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return;
        grid[y, x] = bubble;
    }

    public List<(int, int)> GetConnectedSameColorBubbles(int startX, int startY, BubbleColor color)
    {
        List<(int, int)> connected = new List<(int, int)>();
        bool[,] visited = new bool[rows, columns];

        Queue<(int, int)> queue = new Queue<(int, int)>();
        queue.Enqueue((startX, startY));
        visited[startY, startX] = true;

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            connected.Add((x, y));

            foreach (var (nx, ny) in GetNeighbors(x, y))
            {
                if (visited[ny, nx]) continue;
                GameObject neighbor = grid[ny, nx];
                if (neighbor == null) continue;

                Bubble neighborBubble = neighbor.GetComponent<Bubble>();
                if (neighborBubble != null && neighborBubble.bubbleColor == color)
                {
                    visited[ny, nx] = true;
                    queue.Enqueue((nx, ny));
                }
            }
        }

        return connected;
    }

    // 유틸 함수: 짝수/홀수 줄에 따른 이웃 오프셋 반환
    public int[][] GetRowOffsets(int row)
    {
        int[][] evenRowOffsets = new int[][]
        {
            new int[]{-1, 0}, // 좌
            new int[]{1, 0},  // 우
            new int[]{-1, -1}, // 좌상
            new int[]{0, -1},  // 우상
            new int[]{-1, 1}, // 좌하
            new int[]{0, 1}   // 우하
        };

        int[][] oddRowOffsets = new int[][]
        {
            new int[]{-1, 0}, // 좌
            new int[]{1, 0},  // 우
            new int[]{0, -1}, // 좌상
            new int[]{1, -1}, // 우상
            new int[]{0, 1},  // 좌하
            new int[]{1, 1}   // 우하
        };

        return (row % 2 == 0) ? evenRowOffsets : oddRowOffsets;
    }

    public List<(int, int)> GetNeighbors(int x, int y)
    {
        List<(int, int)> neighbors = new List<(int, int)>();
        int[][] offsets = GetRowOffsets(y);

        foreach (var offset in offsets)
        {
            int nx = x + offset[0];
            int ny = y + offset[1];
            if (nx >= 0 && nx < columns && ny >= 0 && ny < rows)
            {
                neighbors.Add((nx, ny));
            }
        }

        return neighbors;
    }

    public GameObject GetBubbleAt(int x, int y)
    {
        if (x < 0 || x >= columns || y < 0 || y >= rows)
            return null;
        return grid[y, x];
    }

    public (int, int) FindClosestFreeNeighborGrid(int bx, int by, Vector2 shootDir, Vector2 contactPoint)
    {
        var neighbors = GetNeighbors(bx, by);

        Vector2 checkOrigin = contactPoint - shootDir.normalized * 0.1f;

        float bestScore = float.MaxValue;
        (int, int) bestCell = (-1, -1);

        foreach ((int nx, int ny) in neighbors)
        {
            if (!IsCellOccupied(nx, ny))
            {
                Vector2 neighborWorld = GridToWorld(nx, ny);
                Vector2 toNeighborDir = (neighborWorld - checkOrigin);

                float finalScore = toNeighborDir.magnitude;

                Debug.Log($"contactPoint = {contactPoint.x}, {contactPoint.y} | checkOrigin = {checkOrigin.x}, {checkOrigin.y} |  currentCell=({nx},{ny})");

                if (finalScore < bestScore)
                {
                    bestScore = finalScore;
                    bestCell = (nx, ny);

                    // 시각화 및 디버깅
                    Debug.DrawLine(checkOrigin, neighborWorld, new Color(1f, 0.5f, 0f), 3f); // orange
                    Debug.DrawRay(checkOrigin, shootDir.normalized * 1.2f, Color.black, 3f);   // black
                    Debug.DrawRay(checkOrigin, -shootDir.normalized * 0.1f, Color.blue, 3f);   // short backward
                }

            }
        }

        if (bestCell == (-1, -1))
            Debug.LogWarning("FindClosestFreeNeighborGrid 실패: 모든 인접 셀이 점유됨");

        Debug.Log($"최종 결정 셀: ({bestCell.Item1}, {bestCell.Item2}), 발사방향: {shootDir}, 기준점: {checkOrigin}");

        return bestCell;
    }

    public (int, int) SnapBubbleToGrid(GameObject bubble, Vector2 contactPoint)
    {
        (int x, int y) = FindNearestGridIndex(contactPoint);
        Debug.Log($"FindNearestGridIndex 완료: Grid=({x},{y}), contactPoint={contactPoint}");

        if (x < 0 || x >= columns || y < 0 || y >= rows)
        {
            Debug.LogWarning($"SnapBubbleToGrid 실패: 범위 밖 인덱스 ({x},{y})");
            return (-1, -1);
        }

        Debug.LogWarning($"SnapBubbleToGrid 성공: 범위 안 인덱스 : ({x},{y})");

        if (IsCellOccupied(x, y))
        {
            Debug.LogWarning($"IsCellOccupied 성공: 이미 점유된 셀 : ({x},{y})");

            Vector2 shootDir = Vector2.down;

            BubbleProjectile bp = bubble.GetComponent<BubbleProjectile>();
            if (bp != null)
            {
                shootDir = bp.GetCachedDirection();
                if (shootDir == Vector2.zero) shootDir = Vector2.down;
            }

            Debug.LogWarning($"FindClosestFreeNeighborGrid 시도 : 이미 점유된 셀, 발사방향 : ({x},{y}, {shootDir})");
            (x, y) = FindClosestFreeNeighborGrid(x, y, shootDir, contactPoint);

            if (IsCellOccupied(x, y))
            {
                Debug.LogWarning($"SnapBubbleToGrid 실패: 대체 가능한 인접 셀도 없음 ({x},{y})");
                return (-1, -1);
            }
        }

        Vector2 snappedWorldPos = GridToWorld(x, y);
        bubble.transform.position = snappedWorldPos;
        SetCellOccupied(x, y, bubble);

        Vector3 center = bubble.transform.position;
        float size = 0.2f;

        Debug.DrawLine(center + new Vector3(-size, -size), center + new Vector3(-size, size), Color.yellow, 3f);
        Debug.DrawLine(center + new Vector3(-size, size), center + new Vector3(size, size), Color.yellow, 3f);
        Debug.DrawLine(center + new Vector3(size, size), center + new Vector3(size, -size), Color.yellow, 3f);
        Debug.DrawLine(center + new Vector3(size, -size), center + new Vector3(-size, -size), Color.yellow, 3f);

        Bubble bubbleComp = bubble.GetComponent<Bubble>();
        if (bubbleComp != null)
        {
            bubbleComp.gridX = x;
            bubbleComp.gridY = y;
        }

        Debug.Log($"SnapBubbleToGrid 완료: Grid=({x},{y}), WorldPos={snappedWorldPos}");
        return (x, y);
    }

    public Vector2 GridToWorld(int x, int y)
    {
        float xOffset = bubbleRadius * 2f;
        float yOffset = Mathf.Sqrt(3f) * bubbleRadius;

        float xPos = x * xOffset + (y % 2 == 1 ? bubbleRadius : 0);
        float yPos = -y * yOffset;

        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f + 5.95f);
        return origin + new Vector2(xPos, yPos);
    }

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        Gizmos.color = Color.green;

        float xOffset = bubbleRadius * 2f;
        float yOffset = Mathf.Sqrt(3f) * bubbleRadius;
        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f + 5.95f);

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
