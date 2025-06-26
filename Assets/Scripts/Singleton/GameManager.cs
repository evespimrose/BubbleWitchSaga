using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private BubbleGridGenerator bubbleGridGenerator;

    public BubbleGridGenerator BubbleGridGenerator() => bubbleGridGenerator;

}
