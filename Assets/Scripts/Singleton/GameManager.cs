using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private BubbleGridGenerator bubbleGridGenerator;
    [SerializeField] private BubbleShooter bubbleShooter;

    public BubbleGridGenerator BubbleGridGenerator() => bubbleGridGenerator;
    public BubbleShooter BubbleShooter() => bubbleShooter;
    public int TopLevel() => topLevel;

    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int topLevel = 0;
    public BubbleLevelData currentLevelData;
    [SerializeField] private List<BubbleLevelData> currentLevelDatas;

    public event Action OnWin;
    public event Action OnLose;

    private ClearUI clearUI;

    protected override void Awake()
    {
        base.Awake();
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
            clearUI = FindFirstObjectByType<ClearUI>();

            if (clearUI != null) clearUI.gameObject.SetActive(false);

            if (bubbleGridGenerator == null || bubbleShooter == null)
            {
                Debug.LogError("Game 씬에서 필요한 컴포넌트를 찾지 못했습니다.");
            }

            if (currentLevel >= 0 && currentLevel < currentLevelDatas.Count)
            {
                currentLevelData = currentLevelDatas[currentLevel];
                bubbleGridGenerator.currentLevel = currentLevel;

                bubbleGridGenerator.LoadFrom(currentLevelData);
            }
            else
            {
                Debug.LogError("currentLevel이 범위를 벗어났습니다.");
            }

            OnWin += () =>
            {
                if (bubbleGridGenerator != null && topLevel < bubbleGridGenerator.currentLevel)
                    topLevel++;

                if (clearUI != null) clearUI.gameObject.SetActive(true);

                clearUI.ShowResult(true);

                SetAiming(false);
            };

            OnLose += () =>
            {
                if (clearUI != null) clearUI.gameObject.SetActive(true);

                clearUI.ShowResult(false);

                SetAiming(false);
            };
        }
        else
        {
            OnWin = null;
            OnLose = null;
            
            bubbleGridGenerator = null;
            bubbleShooter = null;
            currentLevelData = null;
            clearUI = null;
        }
    }

    public void MarkConnectedGroup(int startX, int startY, BubbleColor color, int minMatchCount = 3)
    {
        var connected = bubbleGridGenerator.GetConnectedSameColorBubbles(startX, startY, color);

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

        bubbleGridGenerator.CheckAndDropFloatingBubbles();
        SetAiming(true);
    }

    public void SetLevel(int level)
    {
        currentLevel = level;
    }

    public void SetAiming(bool enable)
    {
        if (bubbleShooter != null)
            bubbleShooter.canAim = enable;
    }

    public void StageClear()
    {
        OnWin?.Invoke();
    }

    public void StageFail()
    {
        OnLose?.Invoke();
    }
}
