using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public AudioClip clickSound;
    public GameObject settingsPanel;

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

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void SetMasterVolume(float value)
    {
        if (WorldAudioManager.Instance != null)
            WorldAudioManager.Instance.SetMasterVolume(value);
    }

    public void SetBGMVolume(float value)
    {
        if (WorldAudioManager.Instance != null)
            WorldAudioManager.Instance.SetBGMVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        if (WorldAudioManager.Instance != null)
            WorldAudioManager.Instance.SetSFXVolume(value);
    }
}
