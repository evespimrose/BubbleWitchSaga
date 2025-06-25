using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartButton : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClickStartButton);
        }
        
    }

    public void OnClickStartButton()
    {
        SceneManager.LoadScene("Game");
    }


}
