using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class BubbleShooter : MonoBehaviour
{
    [Header("Bubble Settings")]
    public GameObject bubblePrefab;
    public float shootForce = 2f;
    public Transform firePoint;

    [Header("Trajectory Settings")]
    public int trajectoryPointCount = 30;
    public float trajectoryPointSpacing = 0.1f;

    private LineRenderer lineRenderer;
    private Vector2 shootDirection;
    private bool isAiming;

    private @InputSystem_Actions inputActions;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
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
    }

    void Update()
    {
        if (isAiming) UpdateTrajectory();
    }

    private void UpdateTrajectory()
    {
        Vector2 startPoint = firePoint.position;
        Vector2 direction = (Camera.main.ScreenToWorldPoint(inputActions.Gameplay.PointerPosition.ReadValue<Vector2>()) - (Vector3)startPoint).normalized;

        List<Vector3> points = new List<Vector3>();
        points.Add(startPoint);

        int maxReflections = 3;      // 반사 최대 횟수
        float maxDistance = 20f;     // 레이 최대 길이
        Vector2 currentPos = startPoint;
        Vector2 currentDir = direction;

        for (int i = 0; i < maxReflections; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(currentPos, currentDir, maxDistance);
            if (hit.collider != null && hit.collider.gameObject.CompareTag("Wall"))  // 벽 태그 확인 필수
            {
                Vector2 hitPoint = hit.point;
                points.Add(hitPoint);

                // 반사 벡터 계산
                Vector2 inDirection = currentDir;
                Vector2 normal = hit.normal;
                Vector2 reflectDir = Vector2.Reflect(inDirection, normal).normalized;

                currentPos = hitPoint;
                currentDir = reflectDir;
            }
            else
            {
                // 벽 충돌 없으면 직선으로 끝까지 그리기
                points.Add(currentPos + currentDir * maxDistance);
                break;
            }
        }

        // LineRenderer 점 설정
        lineRenderer.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            lineRenderer.SetPosition(i, points[i]);
        }

        shootDirection = direction;  // 실제 발사 방향은 최초 방향 그대로 유지
    }


    private void FireBubble()
    {
        GameObject bubble = Instantiate(bubblePrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D rb = bubble.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = shootDirection * shootForce;
        }
    }
}