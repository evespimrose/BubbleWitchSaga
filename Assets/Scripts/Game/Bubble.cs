using UnityEngine;

public class Bubble : MonoBehaviour
{
    public int gridX;
    public int gridY;
    public BubbleColor bubbleColor;
}
public enum BubbleColor
{
    Red,
    Blue,
    Green
}
public static class BubbleColorUtil
{
    public static Color ToColor(this BubbleColor bubbleColor)
    {
        return bubbleColor switch
        {
            BubbleColor.Red => Color.red,
            BubbleColor.Blue => Color.blue,
            BubbleColor.Green => Color.green,
            _ => Color.white,
        };
    }
}

[System.Serializable]
public class BubbleCell
{
    public bool hasBubble;
    public BubbleColor bubbleColor;
}
