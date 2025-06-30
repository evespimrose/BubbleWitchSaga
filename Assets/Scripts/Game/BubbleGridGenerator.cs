using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public class BubbleGridGenerator : MonoBehaviour
{
    public List<GameObject> bubblePrefab;

    [Header("Grid Settings")]
    public int rows = 12;
    public int columns = 11;
    public float bubbleRadius = 0.5f;

    public int currentLevel = 0;

    [Header("Grid Data")]
    public BubbleCell[] gridData;

    private GameObject[,] grid;          // 일반 버블 저장용
    private GameObject[,] targetGrid;    // 타겟 버블 저장용

    private void OnValidate()
    {
        if (gridData == null || gridData.Length != rows * columns)
        {
            gridData = new BubbleCell[rows * columns];
            for (int i = 0; i < gridData.Length; i++)
            {
                gridData[i] = new BubbleCell();
            }
        }

        grid = new GameObject[rows, columns];
        targetGrid = new GameObject[rows, columns];

        foreach (Transform child in transform)
#if UNITY_EDITOR
            DestroyImmediate(child.gameObject);
#else
    Destroy(child.gameObject);
#endif

        ClearAllBubbles();
    }
    void Update()
    {
        CheckStageClear();
    }

    public void GenerateGrid()
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
                    Debug.Log($"생성: {x},{y}");
                }

                grid[y, x] = bubble;
            }
        }
    }

    public void SwapGrid()
    {
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int idx = y * columns + x;
                if (idx < 0 || idx >= gridData.Length) continue;

                BubbleCell cell = gridData[idx];
                GameObject current = grid[y, x];

                if (cell.hasBubble)
                {
                    GameObject prefab = GetPrefabByColor(cell.bubbleColor);
                    if (prefab == null) continue;

                    if (current == null)
                    {
                        // 버블이 없으면 새로 생성
                        Vector2 pos = GridToWorld(x, y);
                        GameObject newBubble = Instantiate(prefab, pos, Quaternion.identity, transform);
                        Bubble bubbleComp = newBubble.GetComponent<Bubble>();
                        if (bubbleComp != null)
                        {
                            bubbleComp.gridX = x;
                            bubbleComp.gridY = y;
                            bubbleComp.bubbleColor = cell.bubbleColor;
                            bubbleComp.IsTarget = false;
                        }

                        grid[y, x] = newBubble;
                    }
                    else
                    {
                        // 이미 버블이 있으면 색깔만 바꾸기 (프리팹은 그대로)
                        Bubble bubbleComp = current.GetComponent<Bubble>();
                        if (bubbleComp != null && bubbleComp.bubbleColor != cell.bubbleColor)
                        {
                            bubbleComp.bubbleColor = cell.bubbleColor;

                            // 색 시각적 적용 (스프라이트 색이 BubbleColor에 따라 달라지면 필요)
                            SpriteRenderer sr = current.GetComponent<SpriteRenderer>();
                            if (sr != null)
                                sr.color = cell.bubbleColor.ToColor();
                        }
                    }
                }
                else
                {
                    // gridData가 false인데 버블이 남아있다면 제거
                    if (current != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(current);
#else
                    Destroy(current);
#endif
                        grid[y, x] = null;
                    }
                }
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

        if (bubble == null)
        {
            Debug.LogError("SetCellOccupied: bubble이 null입니다. 셀을 비우려면 ClearCell을 사용하세요.");
            return;
        }

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

    public void ClearCell(int x, int y)
    {
        if (!IsInGridRange(x, y)) return;

        if (grid[y, x] != null)
        {
            grid[y, x] = null;
        }

        if (targetGrid[y, x] != null)
        {
            targetGrid[y, x] = null;
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
            return;
        }

        int gx = bubble.gridX;
        int gy = bubble.gridY;

        if (gx < 0 || gx >= columns || gy < 0 || gy >= rows)
        {
            return;
        }

        if (IsCellOccupied(gx, gy))
        {
            return;
        }

        bubble.SetAlpha(1f);

        bubble.IsTarget = false;

        SetCellOccupied(gx, gy, bubbleObj);

        bubbleObj.transform.SetParent(transform);

        if (gy == rows - 1)
        {
            GameManager.Instance.StageFail();
        }

        GameManager.Instance.MarkConnectedGroup(gx, gy, bubble.bubbleColor);
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
                if (!IsInGridRange(nx, ny))
                {
                    continue;
                }

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
    public void CheckAndDropFloatingBubbles()
    {
        bool[,] visited = new bool[rows, columns];
        Queue<(int x, int y)> queue = new Queue<(int, int)>();

        // 1. 윗줄에 붙은 버블부터 BFS 시작 (붙어있는 그룹 판별용)
        for (int x = 0; x < columns; x++)
        {
            GameObject bubble = GetBubbleAt(x, 0);
            if (bubble != null)
            {
                visited[0, x] = true;
                queue.Enqueue((x, 0));
            }
        }

        // 2. BFS로 붙어있는 버블 모두 방문 처리
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();

            foreach (var (nx, ny) in GetNeighbors(x, y))
            {
                if (!IsInGridRange(nx, ny)) continue;
                if (visited[ny, nx]) continue;

                GameObject neighborBubble = GetBubbleAt(nx, ny);
                if (neighborBubble != null)
                {
                    visited[ny, nx] = true;
                    queue.Enqueue((nx, ny));
                }
            }
        }

        // 제거된 버블 좌표 저장용 리스트
        List<(int x, int y)> removedBubbles = new List<(int x, int y)>();

        // 3. 방문되지 않은 버블(떠있는 버블) 처리: 그리드에서 제거 및 낙하 애니메이션 시작
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (!visited[y, x])
                {
                    GameObject floatingBubble = GetBubbleAt(x, y);

                    if (floatingBubble != null)
                    {
                        // 제거 대상 좌표 저장
                        Debug.Log($"떠있는 버블발견 : {x},{y}");

                        removedBubbles.Add((x, y));

                        // 3-1. 그리드에서 버블 제거 (null 처리)
                        grid[y, x] = null;

                        Bubble bubbleComp = floatingBubble.GetComponent<Bubble>();
                        if (bubbleComp != null)
                        {
                            bubbleComp.IsTarget = false;

                            // 낙하할 목표 위치 (현재 위치에서 아래로 5 유닛 떨어진 지점)
                            Vector3 targetPos = floatingBubble.transform.position + Vector3.down * 10f;

                            // 코루틴 실행
                            bubbleComp.StartDropAnimation(targetPos, 1.0f);
                        }
                        else
                        {
                            Destroy(floatingBubble);
                        }

                        floatingBubble.transform.SetParent(null);
                    }
                }
            }
        }
    }
    public GameObject GetBubbleAt(int x, int y)
    {
        if (x < 0 || x >= columns || y < 0 || y >= rows)
            return null;
        return grid[y, x];
    }

    public void LoadFrom(BubbleLevelData level)
    {
        if (level.gridData.Length != rows * columns)
        {
            Debug.LogError("잘못된 gridData 크기");
            return;
        }

        gridData = level.gridData;
        ClearAllBubbles(); // 기존 제거
        GenerateGrid();    // 새로 생성
    }

    public void ClearAllBubbles()
    {
        // 일반 버블 제거
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (grid[y, x] != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(grid[y, x]);
#else
                    Destroy(grid[y, x]);
#endif
                    grid[y, x] = null;
                }

                if (targetGrid[y, x] != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(targetGrid[y, x]);
#else
                    Destroy(targetGrid[y, x]);
#endif
                    targetGrid[y, x] = null;
                }
            }
        }
    }

    public void CheckStageClear()
    {
        if (transform.childCount <= 0)
        {
            GameManager.Instance.StageClear();
        }

    }

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        Gizmos.color = UnityEngine.Color.green;
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
