using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    public int gridX;
    public int gridY;
    public BubbleColor bubbleColor;

    [SerializeField] private bool isTarget = false;
    public bool IsTarget
    {
        get => isTarget;
        set
        {
            isTarget = value;
            UpdateAlpha();
        }
    }

    private SpriteRenderer spriteRenderer;
    private Collider2D circlecollider;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        circlecollider = GetComponent<CircleCollider2D>();
        if (spriteRenderer == null)
            Debug.LogError("Bubble: SpriteRenderer가 없음!");

        UpdateAlpha();
    }

    public void SetAlpha(float alpha)
    {
        if (spriteRenderer == null) return;

        Color c = spriteRenderer.color;
        c.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = c;
    }

    public float GetAlpha()
    {
        return spriteRenderer.color.a;
    }

    private void UpdateAlpha()
    {
        SetAlpha(isTarget ? 0.5f : 1f);
    }

    public void StartDropAnimation(Vector3 targetPos, float duration = 1f)
    {
        StartCoroutine(DropAnimationRoutine(targetPos, duration));
    }

    private IEnumerator DropAnimationRoutine(Vector3 targetPos, float duration)
    {
        if (circlecollider != null) { circlecollider.enabled = false; }
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        float ySpeedMultiplier = Random.Range(0.9f, 1.1f);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float tSquared = t * t;

            float newY = Mathf.Lerp(startPos.y, targetPos.y, tSquared * ySpeedMultiplier);
            float newX = Mathf.Lerp(startPos.x, targetPos.x, t);
            float newZ = Mathf.Lerp(startPos.z, targetPos.z, t);

            transform.position = new Vector3(newX, newY, newZ);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        Destroy(gameObject);
    }

}

public enum BubbleColor
{

    Red,
    Blue,
    Green,
}

public static class BubbleColorUtil
{
    private static readonly Dictionary<BubbleColor, Color> colorMap = new()
    {
        { BubbleColor.Red, Color.red },
        { BubbleColor.Blue, Color.blue },
        { BubbleColor.Green, Color.green },
        // { BubbleColor.Yellow, new Color(1f, 1f, 0f) },
    };

    public static Color ToColor(this BubbleColor bubbleColor)
    {
        return colorMap.TryGetValue(bubbleColor, out var color) ? color : Color.white;
    }
}

[System.Serializable]
public class BubbleCell
{
    public bool hasBubble;
    public BubbleColor bubbleColor;
}
