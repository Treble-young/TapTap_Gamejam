using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevel : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Level_04";

    public bool needCarDriven = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (needCarDriven)
        {
            Car car = other.GetComponentInParent<Car>();
            if (car != null && car == Car.ActiveDrivingCar && car.isDriving)
            {
                LoadNextScene();
                return;
            }

            PlayerManager player = other.GetComponentInParent<PlayerManager>();
            if (player != null || car != null)
            {
                PlayerUIManager.Instance?.popUpManager?.ShowPopUpWindow("我得开车才能离开这里。");
            }
            return;
        }

        PlayerManager walkingPlayer = other.GetComponentInParent<PlayerManager>();
        if (walkingPlayer != null)
            LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            LoadingManager.Instance.LoadNextLevel();
    }
}
