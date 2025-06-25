using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class StageButton : MonoBehaviour
{
    [SerializeField] private int StageNum;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClickStageButton);
        }
    }

    private void OnClickStageButton()
    {
        LoadStage(StageNum);
    }

    void LoadStage(int stageNumber)
    {
        string sceneName = $"Stage_{stageNumber}";
        SceneManager.LoadScene(sceneName);
    }
}
