using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public AudioClip clickSound;

    public void PlayGame()
    {
        LoadingManager.Instance.LoadNextLevel();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void PlayClickSound()
    {
        if (clickSound != null)
        {
            WorldAudioManager.Instance.PlaySFX(clickSound);
        }
    }
}