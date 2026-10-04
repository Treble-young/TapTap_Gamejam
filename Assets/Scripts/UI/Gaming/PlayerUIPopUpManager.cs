using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIPopUpManager : MonoBehaviour
{
    public GameObject popUpWindow;

    public TextMeshProUGUI popUpText;

    public bool IsShowing => popUpWindow != null && popUpWindow.activeSelf;

    public void ShowPopUpWindow(string message)
    {
        if (popUpWindow != null && popUpText != null)
        {
            popUpText.text = message;
            popUpWindow.SetActive(true);
        }
    }

    public void HidePopUpWindow()
    {
        if (popUpWindow != null)
        {
            popUpWindow.SetActive(false);
        }
    }
}
