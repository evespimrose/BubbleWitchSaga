using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Level/BubbleLevelData")]
public class BubbleLevelData : ScriptableObject
{
    public int rows = 12;
    public int columns = 11;
    public BubbleCell[] gridData;
}