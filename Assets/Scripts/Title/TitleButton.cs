using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleButton : MonoBehaviour
{
    [SerializeField] private Button button;

    private void OnTitleButtonClick()
    {
        SceneManager.LoadScene("Title");
    }

    private void OnEnable()
    {
        button.onClick.AddListener(OnTitleButtonClick);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(OnTitleButtonClick);
    }
}
