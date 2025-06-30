using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody2D))]
public class BubbleProjectile : MonoBehaviour
{
    private Rigidbody2D rb;
    public BubbleColor bubbleColor;

    [SerializeField] private GameObject TargetBubble;

    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask bubbleLayer;

    public float maxLifeTime = 5f;

    private Vector2 cachedVelocity;

    private Vector2 initialDirection;

    private bool hasCollided = false;

    //public Vector2 GetCachedDirection() => cachedVelocity.normalized;
    //public Vector2 GetInitialDirection() => initialDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cachedVelocity = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (hasCollided) return;
        if (TargetBubble == null) return;

        if (transform.position.y >= TargetBubble.transform.position.y)
        {
            Bubble bubble = TargetBubble.GetComponent<Bubble>();
            if (bubble != null && bubble.IsTarget)
            {
                hasCollided = true;

                BubbleGridGenerator grid = GameManager.Instance.BubbleGridGenerator();
                grid.SnapTargetBubbleToGrid(TargetBubble);

                Destroy(gameObject);
            }
            GameManager.Instance.SetAiming(true);
        }
    }

    public void Init(Vector2 direction, float force, GameObject targetBubble)
    {
        TargetBubble = targetBubble;
        initialDirection = direction.normalized;
        rb.gravityScale = 0f;
        cachedVelocity = direction.normalized * force;
        rb.linearVelocity = cachedVelocity;

        Destroy(gameObject, maxLifeTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasCollided) return;

        Debug.Log($"콜리젼 만남 : {collision.gameObject.name}, 태그 : {collision.gameObject.tag}");

        if (collision.CompareTag("LeftWall") || collision.CompareTag("RightWall"))
        {
            ReflectDirection(collision);
        }
        else if(collision.CompareTag("UpperWall"))
        {
            Destroy(gameObject);
        }
        else if (collision.CompareTag("Bubble"))
        {
            Bubble bubble = collision.GetComponent<Bubble>();
            if (bubble != null && bubble.IsTarget)
            {
                hasCollided = true;

                BubbleGridGenerator grid = GameManager.Instance.BubbleGridGenerator();
                grid.SnapTargetBubbleToGrid(bubble.gameObject);

                Destroy(gameObject);
            }
        }
    }

    void ReflectDirection(Collider2D collision)
    {
        Vector2 incoming = cachedVelocity;

        Vector2 normal = collision.gameObject.CompareTag("LeftWall") ? Vector2.right :
                         collision.gameObject.CompareTag("RightWall") ? Vector2.left :
                         Vector2.zero;

        Vector2 reflected = Vector2.Reflect(incoming, normal);

        cachedVelocity = reflected.normalized * incoming.magnitude;
        rb.linearVelocity = cachedVelocity;

        Debug.Log($"반사 처리됨: 입사={incoming}, 법선={normal}, 반사={cachedVelocity}");
    }
}
