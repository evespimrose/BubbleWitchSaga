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
    public BubbleCell[] gridData;

    private GameObject[,] grid;          // 일반 버블 저장용
    private GameObject[,] targetGrid;    // 타겟 버블 저장용

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
        grid = new GameObject[rows, columns];
        targetGrid = new GameObject[rows, columns];
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
                int idx = y * columns + x;
                if (idx < 0 || idx >= gridData.Length) continue;

                BubbleCell cell = gridData[idx];
                if (!cell.hasBubble) continue;

                float xPos = x * xOffset + (y % 2 == 1 ? bubbleRadius : 0);
                float yPos = -y * yOffset;
                Vector2 spawnPos = origin + new Vector2(xPos, yPos);

                GameObject prefab = GetPrefabByColor(cell.bubbleColor);
                if (prefab == null)
                {
                    Debug.LogWarning($"Prefab not found for color {cell.bubbleColor}");
                    continue;
                }

                GameObject bubble = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
                Bubble bubbleComp = bubble.GetComponent<Bubble>();
                if (bubbleComp != null)
                {
                    bubbleComp.gridX = x;
                    bubbleComp.gridY = y;
                    bubbleComp.bubbleColor = cell.bubbleColor;
                    bubbleComp.IsTarget = false; // 일반 버블
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

    public Vector2 GridToWorld(int x, int y)
    {
        float xOffset = bubbleRadius * 2f;
        float yOffset = Mathf.Sqrt(3f) * bubbleRadius;

        float xPos = x * xOffset + (y % 2 == 1 ? bubbleRadius : 0);
        float yPos = -y * yOffset;

        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f + 5.95f);
        return origin + new Vector2(xPos, yPos);
    }

    public (int, int) FindNearestGridIndex(Vector2 worldPos)
    {
        float xOffset = bubbleRadius * 2f;
        float yOffset = Mathf.Sqrt(3f) * bubbleRadius;
        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f + 5.95f);

        Vector2 localPos = worldPos - origin;
        int y = Mathf.RoundToInt(-localPos.y / yOffset);
        float xOffsetRow = (y % 2 == 1) ? bubbleRadius : 0f;
        int x = Mathf.RoundToInt((localPos.x - xOffsetRow) / xOffset);

        x = Mathf.Clamp(x, 0, columns - 1);
        y = Mathf.Clamp(y, 0, rows - 1);

        return (x, y);
    }

    public bool IsCellOccupied(int x, int y)
    {
        if (!IsInGridRange(x, y)) return true;
        return (grid[y, x] != null) || (targetGrid[y, x] != null);
    }

    public bool IsInGridRange(int x, int y)
    {
        return x >= 0 && x < columns && y >= 0 && y < rows;
    }

    public void SetCellOccupied(int x, int y, GameObject bubble)
    {
        if (!IsInGridRange(x, y)) return;

        Bubble bubbleComp = bubble.GetComponent<Bubble>();
        if (bubbleComp == null)
        {
            Debug.LogError("SetCellOccupied: Bubble 컴포넌트가 없음");
            return;
        }

        if (bubbleComp.IsTarget)
        {
            if (targetGrid[y, x] != null)
            {
                Destroy(targetGrid[y, x]);
            }
            targetGrid[y, x] = bubble;
        }
        else
        {
            if (grid[y, x] != null)
            {
                Destroy(grid[y, x]);
            }
            grid[y, x] = bubble;
        }
    }

    public void RemoveTargetBubble(int x, int y)
    {
        if (!IsInGridRange(x, y)) return;

        if (targetGrid[y, x] != null)
        {
            Destroy(targetGrid[y, x]);
            targetGrid[y, x] = null;
        }
    }

    public void CreateTargetBubble(int x, int y, BubbleColor color)
    {

        if (!IsInGridRange(x, y)) return;

        if (targetGrid[y, x] != null)
        {
            Destroy(targetGrid[y, x]);
            targetGrid[y, x] = null;
        }

        Vector2 pos = GridToWorld(x, y);
        GameObject prefab = GetPrefabByColor(color);
        if (prefab == null)
        {
            Debug.LogWarning($"CreateTargetBubble: prefab 없음 - {color}");
            return;
        }

        GameObject targetBubble = Instantiate(prefab, pos, Quaternion.identity, transform);
        Bubble bubbleComp = targetBubble.GetComponent<Bubble>();
        if (bubbleComp != null)
        {
            bubbleComp.gridX = x;
            bubbleComp.gridY = y;
            bubbleComp.bubbleColor = color;
            bubbleComp.IsTarget = true;      // 타겟 버블 플래그
            bubbleComp.SetAlpha(0.5f);
        }

        targetGrid[y, x] = targetBubble;
    }

    public (int, int) FindClosestFreeNeighborGrid(int x, int y, Vector2 shootDir, Vector2 contactPoint)
    {
        List<(int, int)> neighbors = GetNeighbors(x, y);
        List<(int, int, float)> freeNeighbors = new List<(int, int, float)>();

        foreach (var (nx, ny) in neighbors)
        {
            if (!IsInGridRange(nx, ny)) continue;
            if (IsCellOccupied(nx, ny)) continue;

            Vector2 neighborWorldPos = GridToWorld(nx, ny);
            float dist = Vector2.Distance(neighborWorldPos, contactPoint);
            freeNeighbors.Add((nx, ny, dist));
        }

        if (freeNeighbors.Count == 0)
        {
            return (x, y);
        }

        freeNeighbors.Sort((a, b) => a.Item3.CompareTo(b.Item3));
        return (freeNeighbors[0].Item1, freeNeighbors[0].Item2);
    }

    public List<(int, int)> GetNeighbors(int x, int y)
    {
        List<(int, int)> neighbors = new List<(int, int)>();

        bool isOddRow = (y % 2 == 1);

        neighbors.Add((x + 1, y));
        neighbors.Add((x - 1, y));
        neighbors.Add((x, y + 1));
        neighbors.Add((x, y - 1));

        if (isOddRow)
        {
            neighbors.Add((x + 1, y + 1));
            neighbors.Add((x + 1, y - 1));
        }
        else
        {
            neighbors.Add((x - 1, y + 1));
            neighbors.Add((x - 1, y - 1));
        }

        return neighbors;
    }

    public void SnapTargetBubbleToGrid(GameObject bubbleObj)
    {
        Bubble bubble = bubbleObj.GetComponent<Bubble>();
        if (bubble == null || !bubble.IsTarget)
        {
            Debug.LogWarning("Snap 실패: 대상이 타겟 버블이 아님");
            return;
        }

        int gx = bubble.gridX;
        int gy = bubble.gridY;

        if (gx < 0 || gx >= columns || gy < 0 || gy >= rows)
        {
            Debug.LogWarning($"Snap 실패: 그리드 범위를 벗어남 (gx:{gx}, gy:{gy})");
            return;
        }

        if (IsCellOccupied(gx, gy))
        {
            Debug.LogWarning($"Snap 실패: 이미 점유된 셀 (gx:{gx}, gy:{gy})");
            return;
        }

        bubble.SetAlpha(1f);

        bubble.IsTarget = false;

        // 그리드에 정식 편입
        SetCellOccupied(gx, gy, bubbleObj);

        bubbleObj.transform.SetParent(transform);

        Debug.Log($"✅ 타겟 버블 그리드 편입 완료: ({gx}, {gy})");

        GameManager.Instance.MarkConnectedGroup(gx, gy, bubble.bubbleColor);
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
