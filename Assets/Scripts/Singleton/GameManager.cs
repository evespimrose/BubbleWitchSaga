using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private BubbleGridGenerator bubbleGridGenerator;

    public BubbleGridGenerator BubbleGridGenerator() => bubbleGridGenerator;

    public void MarkConnectedGroup(int startX, int startY, BubbleColor color, int minMatchCount = 3)
    {
        var connected = bubbleGridGenerator.GetConnectedSameColorBubbles(startX, startY, color);

        if (connected.Count >= minMatchCount)
        {
            Debug.Log($"💥 연결된 {color} 버블 {connected.Count}개 제거");

            foreach (var (x, y) in connected)
            {
                GameObject bubble = bubbleGridGenerator.GetBubbleAt(x, y);
                if (bubble != null)
                {
                    Destroy(bubble);
                    bubbleGridGenerator.SetCellOccupied(x, y, null);
                }
            }
        }
    }
}

