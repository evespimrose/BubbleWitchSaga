using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ClearUI : MonoBehaviour
{
    [SerializeField] private Button menuButton;
    [SerializeField] private Button titleButton;

    [SerializeField] private Image clearImage;
    [SerializeField] private Image failImage;

    private void OnEnable()
    {
        menuButton.onClick.AddListener(OpenMenuScene);
        titleButton.onClick.AddListener(OpenTitleScene);
    }

    private void OnDisable()
    {
        menuButton.onClick.RemoveListener(OpenMenuScene);
        titleButton.onClick.RemoveListener(OpenTitleScene);
    }

    private void OpenMenuScene()
    {
        SceneManager.LoadScene("Menu");
    }

    private void OpenTitleScene()
    {
        SceneManager.LoadScene("Title");
    }

    public void ShowResult(bool isClear)
    {
        if (clearImage != null) clearImage.gameObject.SetActive(isClear);
        if (failImage != null) failImage.gameObject.SetActive(!isClear);
    }
}
