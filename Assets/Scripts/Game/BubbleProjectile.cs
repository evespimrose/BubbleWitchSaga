using Microsoft.Unity.VisualStudio.Editor;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody2D))]
public class BubbleProjectile : MonoBehaviour
{
    private Rigidbody2D rb;
    public BubbleColor bubbleColor;

    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask bubbleLayer;

    public float maxLifeTime = 5f;

    private Vector2 cachedVelocity;

    private Vector2 initialDirection;

    private bool hasCollided = false;

    public Vector2 GetCachedDirection() => cachedVelocity.normalized;
    public Vector2 GetInitialDirection() => initialDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cachedVelocity = Vector2.zero;
    }

    public void Init(Vector2 direction, float force)
    {
        initialDirection = direction.normalized;
        rb.gravityScale = 0f;
        cachedVelocity = direction.normalized * force;
        rb.linearVelocity = cachedVelocity;

        Destroy(gameObject, maxLifeTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasCollided) return;

        Debug.Log($"콜리젼 만남 : {collision.gameObject.name}, 태그 : {collision.gameObject.tag}");

        if (collision.collider.CompareTag("LeftWall") || collision.collider.CompareTag("RightWall"))
        {
            ReflectDirection(collision);
        }
        else if (collision.gameObject.CompareTag("Bubble"))
        {
            hasCollided = true;

            Debug.Log($"버블 만남");
            StartCoroutine(HandleBubbleCollision(collision));
        }
    }


    void ReflectDirection(Collision2D collision)
    {
        ContactPoint2D contact = collision.contacts[0];
        Vector2 incoming = cachedVelocity;

        Vector2 normal = collision.collider.CompareTag("LeftWall") ? Vector2.right :
                         collision.collider.CompareTag("RightWall") ? Vector2.left :
                         Vector2.zero;

        Vector2 reflected = Vector2.Reflect(incoming, normal);

        cachedVelocity = reflected.normalized * incoming.magnitude;
        rb.linearVelocity = cachedVelocity;

        Debug.Log($"반사 처리됨: 입사={incoming}, 법선={normal}, 반사={cachedVelocity}");
    }


    IEnumerator HandleBubbleCollision(Collision2D collision)
    {
        if (!hasCollided)
        {
            Debug.LogWarning("HandleBubbleCollision 진입 조건 이상. hasCollided is not true");
            yield break;
        }

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 충돌 지점 계산
        Vector2 contactPoint = collision.contacts[0].point;

        Vector2 hitPoint = contactPoint;  // 충돌 지점
        Vector2 shootDir = cachedVelocity.normalized; // 발사 방향

        // 피격 벡터를 빨간색으로 표시 (1초간)
        Debug.DrawRay(hitPoint, shootDir * 1f, Color.black, 3f);

        // Snap 처리
        BubbleGridGenerator grid = GameManager.Instance.BubbleGridGenerator();
        (int gx, int gy) = grid.SnapBubbleToGrid(gameObject, contactPoint);
        if (gx == -1 || gy == -1)
        {
            Debug.LogWarning("Snap 실패: Bubble 제거");
            Destroy(gameObject);
            yield break;
        }

        // 새로운 Bubble 생성 및 속성 계승
        Vector2 snappedPos = grid.GridToWorld(gx, gy);
        GameObject newBubble = Instantiate(
            grid.GetPrefabByColor(bubbleColor),
            snappedPos,
            Quaternion.identity,
            grid.transform
        );

        Bubble bubbleComp = newBubble.GetComponent<Bubble>();
        if (bubbleComp != null)
        {
            bubbleComp.gridX = gx;
            bubbleComp.gridY = gy;
            bubbleComp.bubbleColor = bubbleColor;
            //bubbleComp.isAttached = true;
        }

        grid.SetCellOccupied(gx, gy, newBubble);

        GameManager.Instance.MarkConnectedGroup(gx, gy, bubbleColor);

        yield return null;
        Destroy(gameObject);
    }

}
