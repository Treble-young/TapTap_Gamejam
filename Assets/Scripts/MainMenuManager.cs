using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Level_01");
    }

    public void QuitGame()
    {
        Debug.Log("玩家点击了退出");
        Application.Quit();
    }
}