using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    public GameObject loadScreen;
    public Slider slider;
    public TextMeshProUGUI progressText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void LoadNextLevel()
    {
        StartCoroutine(LoadLevelAsync());
    }

    private IEnumerator LoadLevelAsync()
    {
        loadScreen.SetActive(true);
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);

        operation.allowSceneActivation = false;

        bool activationRequested = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            slider.value = progress;
            progressText.text = (progress * 100f).ToString("F0") + "%";

            if (!activationRequested && operation.progress >= 0.9f)
            {
                activationRequested = true;

                yield return new WaitForSeconds(0.2f);

                operation.allowSceneActivation = true;
            }

            yield return null;
        }
        loadScreen.SetActive(false);
    }

    public void LoadTargetScene(int idx)
    {
        StartCoroutine(LoadLevelAsync(idx));
    }

    private IEnumerator LoadLevelAsync(int idx)
    {
        loadScreen.SetActive(true);
        AsyncOperation operation = SceneManager.LoadSceneAsync(idx);

        operation.allowSceneActivation = false;

        bool activationRequested = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            slider.value = progress;
            progressText.text = (progress * 100f).ToString("F0") + "%";

            if (!activationRequested && operation.progress >= 0.9f)
            {
                activationRequested = true;

                yield return new WaitForSeconds(0.2f);

                operation.allowSceneActivation = true;
            }

            yield return null;
        }
        loadScreen.SetActive(false);
    }

    public void FakeLoad()
    {
        StartCoroutine(FakeLoadCoroutine());
    }

    private IEnumerator FakeLoadCoroutine()
    {
        loadScreen.SetActive(true);
        float fakeProgress = 0f;

        while (fakeProgress < 1f)
        {
            fakeProgress += Time.deltaTime * 1f; // Adjust the speed of the fake loading here
            slider.value = fakeProgress;
            progressText.text = (fakeProgress * 100f).ToString("F0") + "%";
            yield return null;
        }

        loadScreen.SetActive(false);
    }
}
