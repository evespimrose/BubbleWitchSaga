using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BubbleProjectile : MonoBehaviour
{
    private Rigidbody2D rb;
    public BubbleColor bubbleColor;

    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask bubbleLayer;

    public float maxLifeTime = 5f;

    private Vector2 cachedVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cachedVelocity = Vector2.zero;
    }

    public void Init(Vector2 direction, float force)
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        cachedVelocity = direction.normalized * force;
        rb.linearVelocity = cachedVelocity;

        Destroy(gameObject, maxLifeTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("LeftWall") || collision.collider.CompareTag("RightWall"))
        {
            ReflectDirection(collision);
        }
        else if (collision.gameObject.CompareTag("Bubble"))
        {
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

        Debug.Log($"🔁 반사 처리됨: 입사={incoming}, 법선={normal}, 반사={cachedVelocity}");
    }


    IEnumerator HandleBubbleCollision(Collision2D collision)
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;  // Unity 6 스타일

        // 충돌 지점 계산
        Vector2 contactPoint = collision.contacts[0].point;

        // Snap 처리
        BubbleGridGenerator grid = GameManager.Instance.BubbleGridGenerator();
        (int gx, int gy) = grid.SnapBubbleToGrid(gameObject, contactPoint);
        if (gx == -1 || gy == -1)
        {
            Debug.LogWarning("❌ Snap 실패: Bubble 제거");
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
