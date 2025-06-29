using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Linq;

public class BubbleShooter : MonoBehaviour
{
    [Header("Bubble Settings")]
    public List<GameObject> bubbleProjectilePrefabs; // Red, Blue, Green
    public List<GameObject> targetBubblePrefabs;     // Red, Blue, Green
    public BubbleColor currentColor = BubbleColor.Red;

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
    [SerializeField] private GameObject targetBubbleInstance;
    private BubbleGridGenerator gridGenerator;
    private InputSystem_Actions inputActions;

    [SerializeField] private List<GameObject> trajectoryDots = new List<GameObject>();

    public bool canAim = true;

    private Vector2? lastTargetGridWorldPos = null;

    void Awake()
    {
        cam = Camera.main;
        gridGenerator = GameManager.Instance.BubbleGridGenerator();

        inputActions = new InputSystem_Actions();
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
        UpdateTargetBubble(trajectoryPoints);
    }

    void UpdateShootDirection()
    {
        Vector2 mouseWorldPos = cam.ScreenToWorldPoint(inputActions.Gameplay.PointerPosition.ReadValue<Vector2>());
        Vector2 rawDirection = mouseWorldPos - (Vector2)firePoint.position;

        float angle = Vector2.SignedAngle(Vector2.right, rawDirection);

        if (rawDirection.y <= 0f)
        {
            angle = rawDirection.x < 0f ? 160f : 20f;
        }
        else
        {
            angle = Mathf.Clamp(angle, 20f, 160f);
        }

        float radians = angle * Mathf.Deg2Rad;
        shootDirection = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;

        Debug.DrawRay(firePoint.position, shootDirection * 5f, Color.yellow);
    }

    void UpdateTargetBubble(List<Vector2> points)
    {
        if (points.Count < 2)
        {
            lastTargetGridWorldPos = null;
            return;
        }

        Vector2 lastPoint = points[^1];
        Vector2 secondLastPoint = points[^2];

        RaycastHit2D hit = Physics2D.Raycast(secondLastPoint, (lastPoint - secondLastPoint).normalized,
            Vector2.Distance(lastPoint, secondLastPoint), bubbleMask);

        if (hit.collider != null && (hit.collider.CompareTag("Bubble")))
        {
            Vector2 shootDir = (lastPoint - secondLastPoint).normalized;
            Vector2 contactPoint = hit.point;

            HandleTargetBubblePlacement(contactPoint, shootDir);
        }
        else if(hit.collider != null && hit.collider.CompareTag("UpperWall"))
        {
            SetTargetBubbleActive(false);
        }
        else
        {
            lastTargetGridWorldPos = null;
        }
    }

    void HandleTargetBubblePlacement(Vector2 contactPoint, Vector2 shootDir)
    {
        (int gx, int gy) = gridGenerator.FindNearestGridIndex(contactPoint);

        Collider2D hitCol = Physics2D.OverlapPoint(contactPoint);

        if (gx < 0 || gx >= gridGenerator.columns || gy < 0 || gy >= gridGenerator.rows)
        {
            lastTargetGridWorldPos = null;
            return;
        }

        if (gridGenerator.IsCellOccupied(gx, gy))
        {
            (gx, gy) = gridGenerator.FindClosestFreeNeighborGrid(gx, gy, shootDir, contactPoint);
            if (gridGenerator.IsCellOccupied(gx, gy))
            {
                lastTargetGridWorldPos = null;
                return;
            }
        }

        Vector2 targetPos = gridGenerator.GridToWorld(gx, gy);

        if (lastTargetGridWorldPos.HasValue && Vector2.Distance(lastTargetGridWorldPos.Value, targetPos) < 0.01f)
            return;

        // 🎯 타겟 버블 생성
        if (targetBubbleInstance == null)
        {
            GameObject targetPrefab = GetTargetPrefabByColor(currentColor);
            targetBubbleInstance = Instantiate(targetPrefab);
        }

        targetBubbleInstance.SetActive(true);
        targetBubbleInstance.transform.position = targetPos;

        var bubbleComp = targetBubbleInstance.GetComponent<Bubble>();
        if (bubbleComp != null)
        {
            bubbleComp.gridX = gx;
            bubbleComp.gridY = gy;
            bubbleComp.IsTarget = true;
            bubbleComp.SetAlpha(0.5f);
        }

        lastTargetGridWorldPos = targetPos;
    }

    void SetTargetBubbleActive(bool active)
    {
        if (targetBubbleInstance != null)
            targetBubbleInstance.SetActive(active);
    }

    private void StartAiming()
    {
        if (!canAim) return;
        isAiming = true;

        currentColor = GetRandomColor();

        // 기존 타겟 버블 참조 해제 (파괴는 하지 않음)
        targetBubbleInstance = null;
        lastTargetGridWorldPos = null;
    }

    private void ReleaseShot()
    {
        if (!isAiming) return;

        FireBubble();
        isAiming = false;
        ClearTrajectoryDots();

        if (targetBubbleInstance != null && !targetBubbleInstance.gameObject.activeSelf)
        {
            Destroy(targetBubbleInstance);
        }

        targetBubbleInstance = null;
        lastTargetGridWorldPos = null;
        canAim = false;
    }

    void FireBubble()
    {
        if (targetBubbleInstance != null)
        {
            var bubbleComp = targetBubbleInstance.GetComponent<Bubble>();
            if (bubbleComp != null)
                bubbleComp.SetAlpha(0f); // 투명화
        }

        GameObject prefab = GetProjectilePrefabByColor(currentColor);
        GameObject bubble = Instantiate(prefab, firePoint.position, Quaternion.identity);
        BubbleProjectile projectile = bubble.GetComponent<BubbleProjectile>();

        if (projectile != null)
        {
            projectile.bubbleColor = currentColor;

            if (targetBubbleInstance != null)
            {
                projectile.Init(shootDirection, shootForce, targetBubbleInstance);
            }
        }
        else
        {
            Debug.LogError("발사된 버블에 BubbleProjectile 컴포넌트가 없습니다.");
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

            Vector2 normal = new Vector2(-currentDir.y, currentDir.x);
            Vector2 leftOrigin = currentPos - normal * widthOffset;
            Vector2 rightOrigin = currentPos + normal * widthOffset;

            RaycastHit2D centerHit = Physics2D.Raycast(currentPos, currentDir, remainingDist, wallMask | bubbleMask);
            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, currentDir, remainingDist, wallMask | bubbleMask);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, currentDir, remainingDist, wallMask | bubbleMask);

            Debug.DrawRay(currentPos, currentDir * 5f, Color.white, 0.5f);
            Debug.DrawRay(leftOrigin, currentDir * 5f, Color.red, 0.5f);
            Debug.DrawRay(rightOrigin, currentDir * 5f, Color.blue, 0.5f);

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
                else if(finalHit.collider.CompareTag("UpperWall"))
                {
                    SetTargetBubbleActive(false);
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

    GameObject GetProjectilePrefabByColor(BubbleColor color)
    {
        int index = (int)color;
        return (index >= 0 && index < bubbleProjectilePrefabs.Count)
            ? bubbleProjectilePrefabs[index]
            : bubbleProjectilePrefabs[0];
    }

    GameObject GetTargetPrefabByColor(BubbleColor color)
    {
        int index = (int)color;
        return (index >= 0 && index < targetBubblePrefabs.Count)
            ? targetBubblePrefabs[index]
            : targetBubblePrefabs[0];
    }

    private BubbleColor GetRandomColor()
    {
        // BubbleColor enum에서 유효한 색상만 필터링
        var validColors = System.Enum.GetValues(typeof(BubbleColor))
                            .Cast<BubbleColor>()
                            .ToList();

        // 랜덤 인덱스 선택
        int randomIndex = Random.Range(0, validColors.Count);
        return validColors[randomIndex];
    }

}
