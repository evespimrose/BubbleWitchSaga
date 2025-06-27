using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Linq;

public class BubbleShooter : MonoBehaviour
{
    [Header("Bubble Settings")]
    public GameObject bubbleProjectilePrefab;
    public GameObject ghostBubblePrefab;
    public float shootForce = 10f;
    public Transform firePoint;
    public LayerMask wallMask;
    public LayerMask bubbleMask;
    public float maxPredictionDistance = 30f;
    public int maxBounces = 3;

    [Header("Trajectory Dot Settings")]
    public GameObject trajectoryDotPrefab; // 작고 투명한 원형 스프라이트 프리팹
    public float dotSpacing = 0.3f;        // 점 간격(유니티 월드 단위)

    private Camera cam;
    private bool isAiming;
    private Vector2 shootDirection;
    private GameObject ghostBubbleInstance;
    private BubbleGridGenerator gridGenerator;
    private @InputSystem_Actions inputActions;

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

        UpdateShootDirection();
        List<Vector2> trajectoryPoints = CalculateTrajectory(firePoint.position, shootDirection);
        UpdateTrajectoryDots(trajectoryPoints);
        UpdateGhostBubble(trajectoryPoints);
    }
    void UpdateShootDirection()
    {
        Vector2 mouseWorldPos = cam.ScreenToWorldPoint(inputActions.Gameplay.PointerPosition.ReadValue<Vector2>());
        Vector2 rawDirection = mouseWorldPos - (Vector2)firePoint.position;

        float angle = Vector2.SignedAngle(Vector2.right, rawDirection);

        // 🎯 아래 방향으로 향할 경우, 좌/우 구분해서 강제 고정 (보정 처리 X)
        if (rawDirection.y <= 0f)
        {
            if (rawDirection.x < 0f)
            {
                angle = 160f; // 왼쪽 아래 → 좌측 상단 방향
            }
            else
            {
                angle = 20f;  // 오른쪽 아래 → 우측 상단 방향
            }
        }
        else
        {
            // 🔒 위쪽을 향하지만 너무 수평에 가까우면 보정
            angle = Mathf.Clamp(angle, 20f, 160f);
        }

        // 📐 최종 벡터 계산
        float radians = angle * Mathf.Deg2Rad;
        shootDirection = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;

        Debug.DrawRay(firePoint.position, shootDirection * 5f, Color.yellow);
        Debug.Log($"📐 최종 발사 각도: {angle}°, shootDirection: {shootDirection}");
    }




    void UpdateGhostBubble(List<Vector2> points)
    {
        if (points.Count < 2)
        {
            SetGhostBubbleActive(false);
            return;
        }

        Vector2 lastPoint = points[^1];
        Vector2 secondLastPoint = points[^2];

        RaycastHit2D hit = Physics2D.Raycast(secondLastPoint, (lastPoint - secondLastPoint).normalized, Vector2.Distance(lastPoint, secondLastPoint), bubbleMask);

        if (hit.collider != null && hit.collider.CompareTag("Bubble"))
        {
            HandleGhostBubblePlacement(hit.collider.transform.position, lastPoint, secondLastPoint);
        }
        else
        {
            SetGhostBubbleActive(false);
        }
    }

    void HandleGhostBubblePlacement(Vector2 collidedBubblePos, Vector2 lastPoint, Vector2 secondLastPoint)
    {
        (int bx, int by) = gridGenerator.FindNearestGridIndex(collidedBubblePos);
        Vector2 shootDir = (lastPoint - secondLastPoint).normalized;
        (int gx, int gy) = gridGenerator.FindNearestGridIndex(lastPoint);

        if (!IsAdjacentFreeCell(bx, by, gx, gy))
        {
            (gx, gy) = gridGenerator.FindClosestFreeNeighborGrid(bx, by, shootDir);
        }

        Vector2 ghostPos = gridGenerator.GridToWorld(gx, gy);
        CreateGhostBubbleIfNeeded();
        ghostBubbleInstance.SetActive(true);
        ghostBubbleInstance.transform.position = ghostPos;

        //Debug.Log($"👻 GhostBubble 위치 변경: Grid=({gx}, {gy}), WorldPos={ghostPos}");
    }

    bool IsAdjacentFreeCell(int baseX, int baseY, int checkX, int checkY)
    {
        var neighbors = gridGenerator.GetNeighbors(baseX, baseY);
        foreach (var n in neighbors)
        {
            if (n.Item1 == checkX && n.Item2 == checkY && !gridGenerator.IsCellOccupied(checkX, checkY))
                return true;
        }
        return false;
    }

    void SetGhostBubbleActive(bool active)
    {
        if (ghostBubbleInstance != null)
            ghostBubbleInstance.SetActive(active);
    }

    private void StartAiming()
    {
        isAiming = true;
        CreateGhostBubbleIfNeeded();
        ghostBubbleInstance.SetActive(false);
    }

    private void ReleaseShot()
    {
        //Debug.Log("🎯 ReleaseShot called");
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

                if (hit.collider.CompareTag("LeftWall") || hit.collider.CompareTag("RightWall"))
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
        //Debug.Log("🎯 FireBubble called");
        GameObject bubble = Instantiate(bubbleProjectilePrefab, firePoint.position, Quaternion.identity);

        var projectile = bubble.GetComponent<BubbleProjectile>();
        if (projectile != null)
        {
            Debug.Log($"🚀 Init 전달 직전 shootDirection = {shootDirection}, Magnitude = {shootDirection.magnitude}, Force = {shootForce}");
            projectile.Init(shootDirection, shootForce);
        }
        else
        {
            Debug.LogError("💥 발사된 버블에 BubbleProjectile 컴포넌트가 없습니다.");
        }

        // 로그 예시
        float angle = Vector2.SignedAngle(Vector2.right, shootDirection);
        Vector2 finalContact = CalculateTrajectory(firePoint.position, shootDirection).Last();
        Vector2 gridPos = gridGenerator.FindNearestGridPosition(finalContact);
        (int gridX, int gridY) = gridGenerator.FindNearestGridIndex(gridPos);
        Debug.Log($"🟢 Bubble 발사! 각도: {angle:F1}°, 예상 격자 위치: ({gridX}, {gridY})");
    }
}