using UnityEngine;

public class BubbleGridGenerator : MonoBehaviour
{
    public GameObject bubblePrefab;

    [Header("Grid Settings")]
    public int rows = 12;
    public int columns = 11;
    public float bubbleRadius = 0.5f;  // 유니티 월드 유닛 기준

    private GameObject[,] grid;

    void Start()
    {
        grid = new GameObject[rows, columns];
        GenerateGrid();
    }

    void GenerateGrid()
    {
        float xOffset = bubbleRadius * 2f;                     // 버블 너비
        float yOffset = Mathf.Sqrt(3f) * bubbleRadius;         // 버블 수직 거리 ≈ 0.866 * 2r

        // 화면 중앙 기준 배치
        Vector2 origin = new Vector2(-(columns - 1) * xOffset / 2f, (rows - 1) * yOffset / 2f);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                float xPos = x * xOffset;
                if (y % 2 == 1) xPos += bubbleRadius; // 홀수줄 오프셋 적용

                float yPos = -y * yOffset;
                Vector2 spawnPos = origin + new Vector2(xPos, yPos);

                GameObject bubble = Instantiate(bubblePrefab, spawnPos, Quaternion.identity, transform);
                grid[y, x] = bubble;
            }
        }
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
