using System.Collections.Generic;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    public int gridX;
    public int gridY;
    public BubbleColor bubbleColor;

    [SerializeField]
    private bool isTarget = false;
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

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("Bubble: SpriteRenderer가 없음!");
        }
        UpdateAlpha();
    }

    public void SetAlpha(float alpha)
    {
        if (spriteRenderer == null) return;

        Color c = spriteRenderer.color;
        c.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = c;
    }

    private void UpdateAlpha()
    {
        if (spriteRenderer == null) return;

        SetAlpha(isTarget ? 0f : 1f);
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
