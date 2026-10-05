using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerUIManuManager : MonoBehaviour
{
    // 可以通过 PlayerUIManager.Instance 访问其他UIManager
    // 其他UIManager 都有显示的开关 Show Hide
    public GameObject menuWindow;

    // 给 ESC 键调用
    public void Show()
    {
        menuWindow.SetActive(true); // 显示暂停菜单
        Time.timeScale = 0f;        // 暂停游戏时间
    }

    public void Hide()
    {
        menuWindow.SetActive(false); // 隐藏暂停菜单
        Time.timeScale = 1f;         // 恢复游戏时间
    }


    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
