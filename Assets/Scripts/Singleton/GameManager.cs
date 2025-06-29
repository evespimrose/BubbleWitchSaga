using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private BubbleGridGenerator bubbleGridGenerator;
    [SerializeField] private BubbleShooter bubbleShooter;

    [SerializeField] private bool isLose = false;
    [SerializeField] private bool isWin = false;

    public BubbleGridGenerator BubbleGridGenerator() => bubbleGridGenerator;
    public BubbleShooter BubbleShooter() => bubbleShooter;

    [SerializeField] private int currentLevel = 0;
    public BubbleLevelData currentLevelData;
    [SerializeField] private List<BubbleLevelData> currentLevelDatas;

    public event Action OnWin;
    public event Action OnLose;

    protected override void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game")
        {
            bubbleGridGenerator = FindFirstObjectByType<BubbleGridGenerator>();
            bubbleShooter = FindFirstObjectByType<BubbleShooter>();

            if (bubbleGridGenerator == null || bubbleShooter == null)
            {
                Debug.LogError("Game 씬에서 필요한 컴포넌트를 찾지 못했습니다.");
            }

            if (currentLevel >= 0 && currentLevel < currentLevelDatas.Count)
            {
                currentLevelData = currentLevelDatas[currentLevel];
                bubbleGridGenerator.LoadFrom(currentLevelData);
            }
            else
            {
                Debug.LogError("currentLevel이 범위를 벗어났습니다.");
            }
        }
        else
        {
            bubbleGridGenerator = null;
            bubbleShooter = null;
            currentLevelData = null;
            isLose = false;
            isWin = false;
        }
    }

    public void MarkConnectedGroup(int startX, int startY, BubbleColor color, int minMatchCount = 3)
    {
        var connected = bubbleGridGenerator.GetConnectedSameColorBubbles(startX, startY, color);
        Debug.Log($"GetConnectedSameColorBubbles 완료, 연결된 버블 {connected.Count}개 제거 시도");

        if (connected.Count >= minMatchCount)
        {
            foreach (var (x, y) in connected)
            {
                GameObject bubble = bubbleGridGenerator.GetBubbleAt(x, y);
                if (bubble != null)
                {
                    Bubble bubbleComp = bubble.GetComponent<Bubble>();
                    if (bubbleComp != null)
                    {
                        Vector3 targetPos = bubbleComp.transform.position + Vector3.down * 10f;
                        bubbleComp.StartDropAnimation(targetPos, 1.0f);
                        bubbleGridGenerator.ClearCell(x, y);
                    }
                    else
                    {
                        Destroy(bubble);
                    }
                }
            }
        }

        Debug.Log($"{connected.Count}개 제거 완료. CheckAndDropFloatingBubbles 호출 시도");
        bubbleGridGenerator.CheckAndDropFloatingBubbles();
        bubbleShooter.canAim = true;
    }
}
