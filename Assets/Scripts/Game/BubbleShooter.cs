using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
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

    [Header("Line Settings")]
    public LineRenderer lineRenderer;

    private Camera cam;
    private bool isAiming;
    private Vector2 shootDirection;
    private GameObject ghostBubbleInstance;
    private BubbleGridGenerator gridGenerator;
    private @InputSystem_Actions inputActions;

    void Awake()
    {
        cam = Camera.main;
        gridGenerator = GameManager.Instance.BubbleGridGenerator();
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;

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
        if (isAiming)
        {
            Vector2 mouseWorldPos = cam.ScreenToWorldPoint(inputActions.Gameplay.PointerPosition.ReadValue<Vector2>());
            shootDirection = (mouseWorldPos - (Vector2)firePoint.position).normalized;
            List<Vector2> points = CalculateTrajectory(firePoint.position, shootDirection);
            lineRenderer.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
                lineRenderer.SetPosition(i, points[i]);
        }
    }

    private void StartAiming()
    {
        isAiming = true;
        lineRenderer.enabled = true;
    }

    private void ReleaseShot()
    {
        if (!isAiming) return;

        FireBubble();
        isAiming = false;
        lineRenderer.enabled = false;

        if (ghostBubbleInstance != null)
            Destroy(ghostBubbleInstance);
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
            RaycastHit2D hit = Physics2D.Raycast(currentPos, currentDir, remainingDist, wallMask | bubbleMask);
            if (hit.collider != null)
            {
                points.Add(hit.point);

                if (hit.collider.CompareTag("Wall"))
                {
                    remainingDist -= Vector2.Distance(currentPos, hit.point);
                    currentDir = Vector2.Reflect(currentDir, hit.normal);
                    currentPos = hit.point;
                    continue;
                }
                else if (hit.collider.CompareTag("Bubble"))
                {
                    PredictSnapToGrid(hit.point);
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

    void PredictSnapToGrid(Vector2 contactPoint)
    {
        Vector2 gridPos = gridGenerator.FindNearestGridPosition(contactPoint);
        if (ghostBubbleInstance == null)
        {
            ghostBubbleInstance = Instantiate(ghostBubblePrefab);
        }
        ghostBubbleInstance.transform.position = gridPos;
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

        Vector2 gridPos = gridGenerator.FindNearestGridPosition(bubble.transform.position);

        (int gridX, int gridY) = gridGenerator.FindNearestGridIndex(gridPos);

        Debug.Log($"🟢 Bubble 발사! 방향 각도: {angle:F1}°, 그리드 좌표: ({gridX}, {gridY})");
    }

}
