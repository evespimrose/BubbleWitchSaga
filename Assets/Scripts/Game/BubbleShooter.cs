// BubbleShooter.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Linq;

public class BubbleShooter : MonoBehaviour
{
    [Header("Bubble Settings")]
    public List<GameObject> bubbleProjectilePrefabs; // Red, Blue, Green
    public List<GameObject> targetBubblePrefabs;     // Red, Blue, Green

    public float shootForce = 10f;
    public Transform firePoint;
    public LayerMask wallMask;
    public LayerMask bubbleMask;
    public float maxPredictionDistance = 30f;
    public int maxBounces = 3;

    [Header("Trajectory Dot Settings")]
    public GameObject trajectoryDotPrefab;
    public float dotSpacing = 0.3f;

    private float aimingStartTime;
    private Camera cam;
    private bool isAiming;
    private Vector2 shootDirection;
    private Vector2? lastTargetGridWorldPos = null;

    [SerializeField] private GameObject targetBubbleInstance;
    [SerializeField] private BubbleGridGenerator gridGenerator;
    [SerializeField] private List<GameObject> trajectoryDots = new List<GameObject>();

    [SerializeField] private SpriteRenderer firstBullet;
    [SerializeField] private SpriteRenderer secondBullet;

    private InputSystem_Actions inputActions;
    public bool canAim = true;

    // ➜ 탄창 2개
    private BubbleColor[] bubbleMagazine = new BubbleColor[2];

    void OnEnable()
    {
        if (cam == null) cam = Camera.main;
        if (inputActions == null) inputActions = new InputSystem_Actions();

        inputActions.Gameplay.Enable();
        inputActions.Gameplay.Fire.started += _ => StartAiming();
        inputActions.Gameplay.Fire.canceled += _ => ReleaseShot();

        InitMagazine();
    }

    void OnDisable()
    {
        inputActions.Gameplay.Fire.canceled -= _ => ReleaseShot();
        inputActions.Gameplay.Fire.started -= _ => StartAiming();
        inputActions.Gameplay.Disable();
    }

    void InitMagazine()
    {
        bubbleMagazine[0] = GetRandomColor();
        bubbleMagazine[1] = GetRandomColor();
        UpdateBulletSprites();
    }

    private void OnCheatPerformed()
    {
        if (canAim)
        {
            GameManager.Instance.BubbleGridGenerator().ClearAllBubbles();
        }
    }

    void Update()
    {
        if (gridGenerator == null) gridGenerator = GameManager.Instance.BubbleGridGenerator();
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
        angle = (rawDirection.y <= 0f) ? (rawDirection.x < 0f ? 160f : 20f) : Mathf.Clamp(angle, 20f, 160f);

        float radians = angle * Mathf.Deg2Rad;
        shootDirection = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;

        Debug.DrawRay(firePoint.position, shootDirection * 5f, Color.yellow);
    }

    void UpdateTargetBubble(List<Vector2> points)
    {
        if (points.Count < 2) { lastTargetGridWorldPos = null; return; }

        Vector2 lastPoint = points[^1];
        Vector2 secondLastPoint = points[^2];
        Vector2 dir = (lastPoint - secondLastPoint).normalized;

        RaycastHit2D hit = Physics2D.Raycast(secondLastPoint, dir, Vector2.Distance(lastPoint, secondLastPoint), bubbleMask);

        if (hit.collider != null && hit.collider.CompareTag("Bubble"))
        {
            HandleTargetBubblePlacement(hit.point, dir);
        }
        else if (hit.collider != null && hit.collider.CompareTag("UpperWall"))
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
        if (gx < 0 || gx >= gridGenerator.columns || gy < 0 || gy >= gridGenerator.rows) return;

        if (gridGenerator.IsCellOccupied(gx, gy))
        {
            (gx, gy) = gridGenerator.FindClosestFreeNeighborGrid(gx, gy, shootDir, contactPoint);
            if (gridGenerator.IsCellOccupied(gx, gy)) return;
        }

        Vector2 targetPos = gridGenerator.GridToWorld(gx, gy);
        if (lastTargetGridWorldPos.HasValue && Vector2.Distance(lastTargetGridWorldPos.Value, targetPos) < 0.01f) return;

        if (targetBubbleInstance == null)
        {
            GameObject targetPrefab = GetTargetPrefabByColor(bubbleMagazine[0]);
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
        aimingStartTime = Time.time;

        targetBubbleInstance = null;
        lastTargetGridWorldPos = null;
    }

    private void ReleaseShot()
    {
        if (!isAiming) return;
        float heldTime = Time.time - aimingStartTime;

        if (heldTime <= 0.2f || targetBubbleInstance == null || !targetBubbleInstance.activeSelf)
        {
            isAiming = false;
            ClearTrajectoryDots();
            if (targetBubbleInstance != null) Destroy(targetBubbleInstance);
            targetBubbleInstance = null;
            lastTargetGridWorldPos = null;
            return;
        }

        FireBubble();
        isAiming = false;
        ClearTrajectoryDots();
        targetBubbleInstance = null;
        lastTargetGridWorldPos = null;
        canAim = false;
    }

    void FireBubble()
    {
        BubbleColor fireColor = bubbleMagazine[0];

        GameObject prefab = GetProjectilePrefabByColor(fireColor);
        GameObject bubble = Instantiate(prefab, firePoint.position, Quaternion.identity);
        BubbleProjectile projectile = bubble.GetComponent<BubbleProjectile>();

        if (projectile != null)
        {
            projectile.bubbleColor = fireColor;
            projectile.Init(shootDirection, shootForce, targetBubbleInstance);
        }

        bubbleMagazine[0] = bubbleMagazine[1];
        bubbleMagazine[1] = GetRandomColor();
        UpdateBulletSprites();
    }

    List<Vector2> CalculateTrajectory(Vector2 start, Vector2 dir)
    {
        List<Vector2> points = new List<Vector2> { start };
        Vector2 currentPos = start;
        Vector2 currentDir = dir.normalized;
        float remainingDist = maxPredictionDistance;
        float widthOffset = 0.15f;

        for (int i = 0; i < maxBounces && remainingDist > 0f; i++)
        {
            Vector2 normal = new Vector2(-currentDir.y, currentDir.x);
            Vector2 leftOrigin = currentPos - normal * widthOffset;
            Vector2 rightOrigin = currentPos + normal * widthOffset;

            RaycastHit2D centerHit = Physics2D.Raycast(currentPos, currentDir, remainingDist, wallMask | bubbleMask);
            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, currentDir, remainingDist, wallMask | bubbleMask);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, currentDir, remainingDist, wallMask | bubbleMask);

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
                else break;
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
            float segmentLength = Vector2.Distance(points[i], points[i + 1]);
            int dotCount = Mathf.CeilToInt(segmentLength / dotSpacing);
            for (int j = 0; j < dotCount; j++)
            {
                Vector2 dotPos = Vector2.Lerp(points[i], points[i + 1], j / (float)dotCount);
                GameObject dot = Instantiate(trajectoryDotPrefab, dotPos, Quaternion.identity, transform);
                trajectoryDots.Add(dot);
            }
        }
    }

    void ClearTrajectoryDots()
    {
        foreach (var dot in trajectoryDots)
        {
            if (dot != null) Destroy(dot);
        }
        trajectoryDots.Clear();
    }

    GameObject GetProjectilePrefabByColor(BubbleColor color)
    {
        int index = (int)color;
        return (index >= 0 && index < bubbleProjectilePrefabs.Count) ? bubbleProjectilePrefabs[index] : bubbleProjectilePrefabs[0];
    }

    GameObject GetTargetPrefabByColor(BubbleColor color)
    {
        int index = (int)color;
        return (index >= 0 && index < targetBubblePrefabs.Count) ? targetBubblePrefabs[index] : targetBubblePrefabs[0];
    }

    private BubbleColor GetRandomColor()
    {
        var validColors = System.Enum.GetValues(typeof(BubbleColor)).Cast<BubbleColor>().ToList();
        return validColors[Random.Range(0, validColors.Count)];
    }

    void UpdateBulletSprites()
    {
        if (firstBullet != null)
            firstBullet.color = bubbleMagazine[0].ToColor();
        if (secondBullet != null)
            secondBullet.color = bubbleMagazine[1].ToColor();
    }
}
