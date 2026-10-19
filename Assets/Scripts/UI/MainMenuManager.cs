using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        LoadingManager.Instance.LoadNextLevel();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}