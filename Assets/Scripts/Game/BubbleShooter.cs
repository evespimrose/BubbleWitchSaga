using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class BubbleShooter : MonoBehaviour
{
    [Header("Bubble Settings")]
    public GameObject bubblePrefab;
    public GameObject ghostBubblePrefab;
    public float shootForce = 10f;
    public Transform firePoint;
    public LayerMask wallMask;
    public LayerMask bubbleMask;
    public float maxPredictionDistance = 30f;
    public int maxBounces = 3;

    [Header("Trajectory Dot Settings")]
    public GameObject trajectoryDotPrefab; // 작고 투명한 원형 스프라이트 프리팹
    public float dotSpacing = 0.3f;         // 점 간격(유니티 월드 단위)

    private Camera cam;
    private bool isAiming;
    private Vector2 shootDirection;
    private GameObject ghostBubbleInstance;
    private BubbleGridGenerator gridGenerator;
    private @InputSystem_Actions inputActions;

    // 궤적 점 오브젝트 리스트 (재활용)
    [SerializeField] private List<GameObject> trajectoryDots = new List<GameObject>();

    void Awake()
    {
        cam = Camera.main;
        gridGenerator = GameManager.Instance.BubbleGridGenerator();

        inputActions = new @InputSystem_Actions();
        inputActions.Gameplay.Enable();
        inputActions.Gameplay.Fire.started += _ => StartAiming();
        inputActions.Gameplay.Fire.canceled += _ => ReleaseShot();
    }

    void OnDestroy()
    {
        inputActions.Gameplay.Fire.started -= _ => StartAiming();
        inputActions.Gameplay.Fire.canceled -= _ => ReleaseShot();
    }

    void Update()
    {
        if (!isAiming) return;

        Vector2 mouseWorldPos = cam.ScreenToWorldPoint(inputActions.Gameplay.PointerPosition.ReadValue<Vector2>());
        shootDirection = (mouseWorldPos - (Vector2)firePoint.position).normalized;

        List<Vector2> points = CalculateTrajectory(firePoint.position, shootDirection);
        UpdateTrajectoryDots(points);

        if (points.Count >= 2)
        {
            Vector2 lastPoint = points[points.Count - 1];
            Vector2 secondLastPoint = points[points.Count - 2];

            // 마지막 구간에 대해 충돌한 Bubble 체크
            RaycastHit2D hit = Physics2D.Raycast(secondLastPoint, (lastPoint - secondLastPoint).normalized,
                                                 Vector2.Distance(lastPoint, secondLastPoint), bubbleMask);

            if (hit.collider != null && hit.collider.CompareTag("Bubble"))
            {
                Vector2 collidedBubblePos = hit.collider.transform.position;
                (int bx, int by) = gridGenerator.FindNearestGridIndex(collidedBubblePos);

                Vector2 shootDir = (lastPoint - secondLastPoint).normalized;

                // 경로 끝점 기준 그리드 좌표
                (int gx, int gy) = gridGenerator.FindNearestGridIndex(lastPoint);

                bool isAdjacentAndFree = false;

                // 만약 predictedGrid가 만난 Bubble 주변이고 비었으면 바로 사용
                var neighbors = gridGenerator.GetNeighbors(bx, by);
                foreach (var n in neighbors)
                {
                    if (n.Item1 == gx && n.Item2 == gy && !gridGenerator.IsCellOccupied(gx, gy))
                    {
                        isAdjacentAndFree = true;
                        break;
                    }
                }

                if (!isAdjacentAndFree)
                {
                    // 빈 칸이 아니거나 인접하지 않으면 방향 기반 가장 적합 빈 칸 찾기
                    (gx, gy) = gridGenerator.FindClosestFreeNeighborGrid(bx, by, shootDir);
                }

                Vector2 ghostPos = gridGenerator.GridToWorld(gx, gy);

                CreateGhostBubbleIfNeeded();
                ghostBubbleInstance.SetActive(true);
                ghostBubbleInstance.transform.position = ghostPos;

                Debug.Log($"👻 GhostBubble 위치 변경: Grid=({gx}, {gy}), WorldPos={ghostPos}");
            }
            else
            {
                if (ghostBubbleInstance != null)
                    ghostBubbleInstance.SetActive(false);
            }
        }
        else
        {
            if (ghostBubbleInstance != null)
                ghostBubbleInstance.SetActive(false);
        }
    }



    private void StartAiming()
    {
        isAiming = true;
        CreateGhostBubbleIfNeeded();
        ghostBubbleInstance.SetActive(false);
    }

    private void ReleaseShot()
    {
        if (!isAiming) return;

        FireBubble();

        isAiming = false;
        ClearTrajectoryDots();

        if (ghostBubbleInstance != null)
        {
            Destroy(ghostBubbleInstance);
            ghostBubbleInstance = null;
        }
    }

    List<Vector2> CalculateTrajectory(Vector2 start, Vector2 dir)
    {
        List<Vector2> points = new List<Vector2>();
        points.Add(start);

        Vector2 currentPos = start;
        Vector2 currentDir = dir;
        float remainingDist = maxPredictionDistance;

        for (int i = 0; i < maxBounces; i++)
        {
            if (remainingDist <= 0f)
            {
                points.Add(currentPos);
                break;
            }

            RaycastHit2D hit = Physics2D.Raycast(currentPos, currentDir, remainingDist, wallMask | bubbleMask);
            if (hit.collider != null)
            {
                points.Add(hit.point);

                float traveled = Vector2.Distance(currentPos, hit.point);
                remainingDist -= traveled;

                if (hit.collider.CompareTag("Wall"))
                {
                    currentDir = Vector2.Reflect(currentDir, hit.normal);
                    currentPos = hit.point + currentDir * 0.01f; // 중복 충돌 방지 약간 이동
                    continue;
                }
                else if (hit.collider.CompareTag("Bubble"))
                {
                    // Bubble 충돌 시 경로 계산 종료
                    break;
                }
            }
            else
            {
                points.Add(currentPos + currentDir * remainingDist);
                break;
            }
        }

        return points;
    }

    void UpdateTrajectoryDots(List<Vector2> points)
    {
        ClearTrajectoryDots();

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 start = points[i];
            Vector2 end = points[i + 1];
            float segmentLength = Vector2.Distance(start, end);
            int dotCount = Mathf.CeilToInt(segmentLength / dotSpacing);

            for (int j = 0; j < dotCount; j++)
            {
                Vector2 dotPos = Vector2.Lerp(start, end, j / (float)dotCount);
                GameObject dot = Instantiate(trajectoryDotPrefab, dotPos, Quaternion.identity, transform);
                trajectoryDots.Add(dot);
            }
        }
    }

    void ClearTrajectoryDots()
    {
        foreach (var dot in trajectoryDots)
        {
            if (dot != null)
                Destroy(dot);
        }
        trajectoryDots.Clear();
    }

    void CreateGhostBubbleIfNeeded()
    {
        if (ghostBubbleInstance == null && ghostBubblePrefab != null)
            ghostBubbleInstance = Instantiate(ghostBubblePrefab);
    }

    void FireBubble()
    {
        GameObject bubble = Instantiate(bubblePrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D rb = bubble.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = shootDirection * shootForce;
        }

        float angle = Vector2.SignedAngle(Vector2.right, shootDirection);
        angle = Mathf.Abs(angle);

        List<Vector2> trajectory = CalculateTrajectory(firePoint.position, shootDirection);
        Vector2 finalContact = trajectory[trajectory.Count - 1];
        Vector2 gridPos = gridGenerator.FindNearestGridPosition(finalContact);
        (int gridX, int gridY) = gridGenerator.FindNearestGridIndex(gridPos);

        Debug.Log($"🟢 Bubble 발사! 방향 각도: {angle:F1}°, 그리드 좌표: ({gridX}, {gridY})");
    }
}