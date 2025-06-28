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
    public GameObject trajectoryDotPrefab;
    public float dotSpacing = 0.3f;

    private Camera cam;
    private bool isAiming;
    private Vector2 shootDirection;
    private GameObject ghostBubbleInstance;
    private BubbleGridGenerator gridGenerator;
    private @InputSystem_Actions inputActions;

    [SerializeField] private List<GameObject> trajectoryDots = new List<GameObject>();

    private Vector2? lastGhostGridWorldPos = null; // 직전 위치 기억용 필드 추가

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

        if (rawDirection.y <= 0f)
        {
            if (rawDirection.x < 0f)
                angle = 160f;
            else
                angle = 20f;
        }
        else
        {
            angle = Mathf.Clamp(angle, 20f, 160f);
        }

        float radians = angle * Mathf.Deg2Rad;
        shootDirection = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;

        Debug.DrawRay(firePoint.position, shootDirection * 5f, Color.yellow);
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
            Vector2 shootDir = (lastPoint - secondLastPoint).normalized;
            Vector2 contactPoint = hit.point;
            HandleGhostBubblePlacement(contactPoint, shootDir);
        }
        else
        {
            if (lastGhostGridWorldPos.HasValue)
            {
                // Do nothing → 유지
                return;
            }

            SetGhostBubbleActive(false);
        }
    }

    void HandleGhostBubblePlacement(Vector2 contactPoint, Vector2 shootDir)
    {
        (int gx, int gy) = gridGenerator.FindNearestGridIndex(contactPoint);

        if (gx < 0 || gx >= gridGenerator.columns || gy < 0 || gy >= gridGenerator.rows)
        {
            // 경계 밖인 경우에도 잔류 유지
            return;
        }

        if (gridGenerator.IsCellOccupied(gx, gy))
        {
            (gx, gy) = gridGenerator.FindClosestFreeNeighborGrid(gx, gy, shootDir, contactPoint);
            if (gridGenerator.IsCellOccupied(gx, gy))
            {
                // 여전히 자리 없을 경우, 이전 위치 유지
                return;
            }
        }

        Vector2 ghostPos = gridGenerator.GridToWorld(gx, gy);

        // 이전과 같은 위치면 다시 비활성화하지 않음
        if (lastGhostGridWorldPos.HasValue && Vector2.Distance(lastGhostGridWorldPos.Value, ghostPos) < 0.01f)
        {
            // 위치 같으면 그대로 유지 (SetActive 하지 않음)
            return;
        }

        CreateGhostBubbleIfNeeded();
        ghostBubbleInstance.SetActive(true);
        ghostBubbleInstance.transform.position = ghostPos;
        lastGhostGridWorldPos = ghostPos;

        Debug.Log($"GhostBubble 위치 갱신: Grid=({gx},{gy}), WorldPos={ghostPos}");
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
        Vector2 currentDir = dir.normalized;
        float remainingDist = maxPredictionDistance;
        float widthOffset = 0.15f;

        for (int i = 0; i < maxBounces; i++)
        {
            if (remainingDist <= 0f) break;

            // 직각 방향 벡터 (왼쪽/오른쪽 offset용)
            Vector2 normal = new Vector2(-currentDir.y, currentDir.x);
            Vector2 leftOrigin = currentPos - normal * widthOffset;
            Vector2 rightOrigin = currentPos + normal * widthOffset;

            RaycastHit2D centerHit = Physics2D.Raycast(currentPos, currentDir, remainingDist, wallMask | bubbleMask);
            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, currentDir, remainingDist, wallMask | bubbleMask);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, currentDir, remainingDist, wallMask | bubbleMask);

            // 디버그 선 시각화
            Debug.DrawRay(currentPos, currentDir * 5f, Color.white, 0.5f);       // 중심
            Debug.DrawRay(leftOrigin, currentDir * 5f, Color.red, 0.5f);         // 왼쪽
            Debug.DrawRay(rightOrigin, currentDir * 5f, Color.blue, 0.5f);       // 오른쪽

            // 가장 가까운 Raycast 결과 선택
            RaycastHit2D finalHit = GetClosestHit(centerHit, leftHit, rightHit);

            if (finalHit.collider != null)
            {
                points.Add(finalHit.point);
                float traveled = Vector2.Distance(currentPos, finalHit.point);
                remainingDist -= traveled;

                if (finalHit.collider.CompareTag("LeftWall") || finalHit.collider.CompareTag("RightWall"))
                {
                    currentDir = Vector2.Reflect(currentDir, finalHit.normal);
                    currentPos = finalHit.point + currentDir * 0.01f;
                    continue;
                }
                else if (finalHit.collider.CompareTag("Bubble"))
                {
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


    RaycastHit2D GetClosestHit(params RaycastHit2D[] hits)
    {
        RaycastHit2D closest = new RaycastHit2D();
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.collider != null)
            {
                float dist = Vector2.Distance(hit.point, hit.centroid);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = hit;
                }
            }
        }

        return closest;
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
        GameObject bubble = Instantiate(bubbleProjectilePrefab, firePoint.position, Quaternion.identity);

        var projectile = bubble.GetComponent<BubbleProjectile>();
        if (projectile != null)
        {
            projectile.Init(shootDirection, shootForce);
        }
        else
        {
            Debug.LogError("발사된 버블에 BubbleProjectile 컴포넌트가 없습니다.");
        }

        float angle = Vector2.SignedAngle(Vector2.right, shootDirection);
        Vector2 finalContact = CalculateTrajectory(firePoint.position, shootDirection).Last();
        Vector2 gridPos = gridGenerator.FindNearestGridPosition(finalContact);
        (int gridX, int gridY) = gridGenerator.FindNearestGridIndex(gridPos);
        Debug.Log($"Bubble 발사! 각도: {angle:F1}°, 예상 격자 위치: ({gridX}, {gridY})");
    }
}
