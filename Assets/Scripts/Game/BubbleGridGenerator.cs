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

        // 오리진 기준 설정 (기즈모와 동일)
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

        // 대략적인 행 계산
        int y = Mathf.RoundToInt(-localPos.y / yOffset);

        // 짝수/홀수 줄 구분해 x 계산
        float xPosOffset = (y % 2 == 1) ? bubbleRadius : 0f;
        int x = Mathf.RoundToInt((localPos.x - xPosOffset) / xOffset);

        x = Mathf.Clamp(x, 0, columns - 1);
        y = Mathf.Clamp(y, 0, rows - 1);

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


    public List<(int, int)> GetNeighbors(int x, int y)
    {
        List<(int, int)> neighbors = new List<(int, int)>();

        int[][] evenRowOffsets = new int[][]
        {
            new int[]{-1, 0}, new int[]{0, -1}, new int[]{1, -1},
            new int[]{1, 0}, new int[]{1, 1}, new int[]{0, 1}
        };
        int[][] oddRowOffsets = new int[][]
        {
            new int[]{-1, 0}, new int[]{-1, -1}, new int[]{0, -1},
            new int[]{1, 0}, new int[]{0, 1}, new int[]{-1, 1}
        };

        int[][] offsets = (y % 2 == 0) ? evenRowOffsets : oddRowOffsets;

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


    public (int, int) FindClosestFreeNeighborGrid(int bx, int by, Vector2 shootDir)
    {
        var neighbors = GetNeighbors(bx, by);

        float maxDot = -1f;
        (int, int) bestCell = (bx, by);

        Vector2 baseWorldPos = GridToWorld(bx, by);
        Vector2 dirNorm = shootDir.normalized;

        foreach (var cell in neighbors)
        {
            int nx = cell.Item1;
            int ny = cell.Item2;

            if (IsCellOccupied(nx, ny)) continue;

            Vector2 neighborWorldPos = GridToWorld(nx, ny);
            Vector2 toNeighbor = (neighborWorldPos - baseWorldPos).normalized;

            float dot = Vector2.Dot(dirNorm, toNeighbor);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestCell = (nx, ny);
            }
        }

        return bestCell;
    }

    public (int, int) SnapBubbleToGrid(GameObject bubble, Vector2 contactPoint)
    {
        // 가장 가까운 그리드 인덱스 계산
        (int x, int y) = FindNearestGridIndex(contactPoint);

        // 그리드 범위 초과 방지
        if (x < 0 || x >= columns || y < 0 || y >= rows)
        {
            Debug.LogWarning($"❌ SnapBubbleToGrid 실패: 범위 밖 인덱스 ({x},{y})");
            return (-1, -1);
        }

        // 이미 점유된 셀이면 방향 기반 인접한 빈 셀 찾기
        if (IsCellOccupied(x, y))
        {
            Rigidbody2D rb = bubble.GetComponent<Rigidbody2D>();
            Vector2 shootDir = rb != null ? rb.linearVelocity.normalized : Vector2.down;

            (x, y) = FindClosestFreeNeighborGrid(x, y, shootDir);

            if (IsCellOccupied(x, y))
            {
                Debug.LogWarning($"⚠️ SnapBubbleToGrid 실패: 대체 가능한 인접 셀도 없음 ({x},{y})");
                return (-1, -1);
            }
        }

        // 실제 위치 이동 및 그리드 등록
        Vector2 snappedWorldPos = GridToWorld(x, y);
        bubble.transform.position = snappedWorldPos;
        SetCellOccupied(x, y, bubble);

        // Bubble 컴포넌트가 있다면 gridX, gridY 설정
        Bubble bubbleComp = bubble.GetComponent<Bubble>();
        if (bubbleComp != null)
        {
            bubbleComp.gridX = x;
            bubbleComp.gridY = y;
            //bubbleComp.isAttached = true; // 이 프로퍼티는 선택 사항
        }

        Debug.Log($"📌 SnapBubbleToGrid 완료: Grid=({x},{y}), WorldPos={snappedWorldPos}");
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
