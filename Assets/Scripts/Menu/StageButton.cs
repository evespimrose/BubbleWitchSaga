using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class StageButton : MonoBehaviour
{
    [SerializeField] private int StageNum;
    [SerializeField] private GameObject stars;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClickStageButton);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.TopLevel() + 1 < StageNum)
            button.interactable = false;
        else
            button.interactable = true;
        if (GameManager.Instance.TopLevel() < StageNum)
            stars.SetActive(false);
        else
            stars.SetActive(true);
    }

    private void OnClickStageButton()
    {
        LoadStage(StageNum);
    }

    void LoadStage(int stageNumber)
    {
        GameManager.Instance.SetLevel(StageNum);
        SceneManager.LoadScene("Game");
    }
}
