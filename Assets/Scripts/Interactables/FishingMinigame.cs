using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

public class FishingMinigame : MonoBehaviour
{
    public enum FishingDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    [Header("Catch Bar")]
    [Min(0f)] public float catchBarRiseSpeed = 300f;
    [Min(0f)] public float catchBarAcceleration = 900f;
    [Min(0f)] public float catchBarMaxFallSpeed = 620f;

    [Header("Fish")]
    [Min(0f)] public float fishMoveSpeed = 150f;
    [Min(0.01f)] public float fishDirectionChangeInterval = 0.4f;

    [Header("Progress")]
    [Min(0f)] public float progressGainPerSecond = 0.42f;
    [Min(0f)] public float progressLossPerSecond = 0.28f;
    [Min(0f)] public float minigameDuration = 30f;

    [Header("Difficulty")]
    public FishingDifficulty difficulty = FishingDifficulty.Easy;
    public Toggle easyToggle;
    public Toggle normalToggle;
    public Toggle hardToggle;

    [Header("Mouse Indicator")]
    public Image mouseIndicator;
    public Color mousePressedColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Temperature Readout")]
    public TextMeshProUGUI temperatureText;
    public float minTemperature = 600f;
    public float maxTemperature = 1600f;

    [Header("UI References")]
    public RectTransform gameRoot;
    public RectTransform track;
    public RectTransform catchBar;
    public RectTransform fish;
    public Image progressFill;
    public RectTransform progressMin;
    public RectTransform progressMax;
    public GameObject startButton;
    public GameObject replayButton;
    public TextMeshProUGUI resultText;
    public string winMessage = "铸造完成，可以去找铁匠拿螺丝了";
    public string loseMessage = "失败了，再试一次";
    public GameObject[] hideWhileFishing;

    [Header("Events")]
    public UnityEvent onWon;
    public UnityEvent onLost;
    public UnityEvent onFinished;

    private bool isFishing;
    private float progress;
    private float elapsed;
    private float catchVelocity;
    private float fishTargetY;
    private float fishDirectionTimer;
    private float activeCatchBarRiseSpeed;
    private float activeCatchBarAcceleration;
    private float activeCatchBarMaxFallSpeed;
    private float activeFishMoveSpeed;
    private float activeFishDirectionChangeInterval;
    private float activeProgressGainPerSecond;
    private float activeProgressLossPerSecond;
    private Color mouseNormalColor = Color.white;

    public bool IsFishing => isFishing;

    public void Show()
    {
        if (isFishing || !HasReferences())
            return;

        isFishing = false;
        progress = 0.5f;
        elapsed = 0f;
        catchVelocity = 0f;
        fishDirectionTimer = 0f;
        fishTargetY = 0f;

        catchBar.anchoredPosition = Vector2.zero;
        fish.anchoredPosition = Vector2.zero;
        SetProgressFill(progress);
        UpdateTemperatureText();

        SetResultText(string.Empty);
        SyncDifficultyToggles();
        CacheMouseNormalColor();
        ResetMouseIndicator();

        if (startButton != null)
            startButton.SetActive(true);

        if (replayButton != null)
            replayButton.SetActive(false);

        EnsureEventSystem();
        HideAuxiliaryPanels();
        gameRoot.gameObject.SetActive(true);
    }

    public void Begin()
    {
        if (isFishing || !HasReferences())
            return;

        isFishing = true;
        progress = 0.5f;
        elapsed = 0f;
        catchVelocity = 0f;
        fishDirectionTimer = 0f;
        fishTargetY = 0f;

        catchBar.anchoredPosition = Vector2.zero;
        fish.anchoredPosition = Vector2.zero;
        SetProgressFill(progress);
        UpdateTemperatureText();

        SetResultText(string.Empty);
        SyncDifficultyToggles();
        ApplyDifficulty();
        CacheMouseNormalColor();
        ResetMouseIndicator();

        if (startButton != null)
            startButton.SetActive(false);

        if (replayButton != null)
            replayButton.SetActive(false);

        HideAuxiliaryPanels();
        gameRoot.gameObject.SetActive(true);
    }

    public void Cancel()
    {
        isFishing = false;

        if (gameRoot != null)
            gameRoot.gameObject.SetActive(false);

        if (startButton != null)
            startButton.SetActive(true);

        if (replayButton != null)
            replayButton.SetActive(false);

        RestoreAuxiliaryPanels();
        ResetMouseIndicator();
        onFinished.Invoke();
    }

    private void Update()
    {
        if (!isFishing)
            return;

        float deltaTime = Time.unscaledDeltaTime;
        elapsed += deltaTime;

        UpdateMouseIndicator();
        UpdateCatchBar(deltaTime);
        UpdateFish(deltaTime);
        UpdateProgress(deltaTime);
        UpdateTemperatureText();

        if (progress >= 1f)
        {
            Finish(true);
        }
        else if (progress <= 0f || (minigameDuration > 0f && elapsed >= minigameDuration))
        {
            Finish(false);
        }
    }

    private void Finish(bool won)
    {
        if (!isFishing)
            return;

        isFishing = false;

        if (won)
        {
            onWon.Invoke();
            SetResultText(winMessage);
        }
        else
        {
            onLost.Invoke();
            SetResultText(loseMessage);
        }

        if (replayButton != null)
            replayButton.SetActive(true);

        RestoreAuxiliaryPanels();
        ResetMouseIndicator();
    }

    private bool HasReferences()
    {
        if (gameRoot != null && track != null && catchBar != null && fish != null
            && progressFill != null && progressMin != null && progressMax != null
            && startButton != null && replayButton != null && resultText != null
            && easyToggle != null && normalToggle != null && hardToggle != null
            && temperatureText != null)
            return true;

        Debug.LogError($"FishingMinigame '{name}' is missing UI references.", this);
        return false;
    }

    public void SetDifficultyEasy()
    {
        SetDifficulty(FishingDifficulty.Easy);
    }

    public void SetDifficultyNormal()
    {
        SetDifficulty(FishingDifficulty.Normal);
    }

    public void SetDifficultyHard()
    {
        SetDifficulty(FishingDifficulty.Hard);
    }

    private void SetDifficulty(FishingDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
        SyncDifficultyToggles();
    }

    private void SyncDifficultyToggles()
    {
        if (easyToggle != null)
            easyToggle.SetIsOnWithoutNotify(difficulty == FishingDifficulty.Easy);

        if (normalToggle != null)
            normalToggle.SetIsOnWithoutNotify(difficulty == FishingDifficulty.Normal);

        if (hardToggle != null)
            hardToggle.SetIsOnWithoutNotify(difficulty == FishingDifficulty.Hard);
    }

    private void ApplyDifficulty()
    {
        switch (difficulty)
        {
            case FishingDifficulty.Easy:
                activeCatchBarRiseSpeed = catchBarRiseSpeed * 1.15f;
                activeCatchBarAcceleration = catchBarAcceleration * 0.9f;
                activeCatchBarMaxFallSpeed = catchBarMaxFallSpeed * 0.85f;
                activeFishMoveSpeed = fishMoveSpeed * 0.7f;
                activeFishDirectionChangeInterval = fishDirectionChangeInterval * 1.2f;
                activeProgressGainPerSecond = progressGainPerSecond * 1.2f;
                activeProgressLossPerSecond = progressLossPerSecond * 0.7f;
                break;
            case FishingDifficulty.Hard:
                activeCatchBarRiseSpeed = catchBarRiseSpeed * 0.85f;
                activeCatchBarAcceleration = catchBarAcceleration * 1.1f;
                activeCatchBarMaxFallSpeed = catchBarMaxFallSpeed * 1.15f;
                activeFishMoveSpeed = fishMoveSpeed * 1.4f;
                activeFishDirectionChangeInterval = fishDirectionChangeInterval * 0.8f;
                activeProgressGainPerSecond = progressGainPerSecond * 0.85f;
                activeProgressLossPerSecond = progressLossPerSecond * 1.3f;
                break;
            default:
                activeCatchBarRiseSpeed = catchBarRiseSpeed;
                activeCatchBarAcceleration = catchBarAcceleration;
                activeCatchBarMaxFallSpeed = catchBarMaxFallSpeed;
                activeFishMoveSpeed = fishMoveSpeed;
                activeFishDirectionChangeInterval = fishDirectionChangeInterval;
                activeProgressGainPerSecond = progressGainPerSecond;
                activeProgressLossPerSecond = progressLossPerSecond;
                break;
        }
    }

    private void SetResultText(string message)
    {
        if (resultText != null)
            resultText.text = message;
    }

    private void CacheMouseNormalColor()
    {
        if (mouseIndicator != null)
            mouseNormalColor = mouseIndicator.color;
    }

    private void ResetMouseIndicator()
    {
        if (mouseIndicator != null)
            mouseIndicator.color = mouseNormalColor;
    }

    private void UpdateMouseIndicator()
    {
        if (mouseIndicator == null)
            return;

        Mouse mouse = Mouse.current;
        bool pressed = mouse != null && mouse.leftButton.isPressed;
        mouseIndicator.color = pressed ? mousePressedColor : mouseNormalColor;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private void HideAuxiliaryPanels()
    {
        if (hideWhileFishing == null)
            return;

        for (int i = 0; i < hideWhileFishing.Length; i++)
        {
            if (hideWhileFishing[i] != null)
                hideWhileFishing[i].SetActive(false);
        }
    }

    private void RestoreAuxiliaryPanels()
    {
        if (hideWhileFishing == null)
            return;

        for (int i = 0; i < hideWhileFishing.Length; i++)
        {
            if (hideWhileFishing[i] != null)
                hideWhileFishing[i].SetActive(true);
        }
    }

    private void UpdateCatchBar(float deltaTime)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.leftButton.isPressed)
        {
            catchVelocity = activeCatchBarRiseSpeed;
        }
        else
        {
            catchVelocity = Mathf.Max(-activeCatchBarMaxFallSpeed,
                catchVelocity - activeCatchBarAcceleration * deltaTime);
        }

        float halfTrack = track.rect.height * 0.5f;
        float halfBar = catchBar.rect.height * 0.5f;
        float minY = -halfTrack + halfBar;
        float maxY = halfTrack - halfBar;

        float nextY = Mathf.Clamp(catchBar.anchoredPosition.y + catchVelocity * deltaTime, minY, maxY);
        catchBar.anchoredPosition = new Vector2(0f, nextY);
    }

    private void UpdateFish(float deltaTime)
    {
        float halfTrack = track.rect.height * 0.5f;
        float halfFish = fish.rect.height * 0.5f;
        float minY = -halfTrack + halfFish;
        float maxY = halfTrack - halfFish;

        fishDirectionTimer -= deltaTime;
        if (fishDirectionTimer <= 0f)
        {
            fishDirectionTimer = activeFishDirectionChangeInterval;
            fishTargetY = Random.Range(minY, maxY);
        }

        float nextY = Mathf.MoveTowards(fish.anchoredPosition.y, fishTargetY, activeFishMoveSpeed * deltaTime);
        nextY = Mathf.Clamp(nextY, minY, maxY);
        fish.anchoredPosition = new Vector2(0f, nextY);
    }

    private void UpdateProgress(float deltaTime)
    {
        float distance = Mathf.Abs(fish.anchoredPosition.y - catchBar.anchoredPosition.y);
        float overlapHeight = (catchBar.rect.height + fish.rect.height) * 0.5f;
        bool overlaps = distance <= overlapHeight;

        float change = (overlaps ? activeProgressGainPerSecond : -activeProgressLossPerSecond) * deltaTime;
        progress = Mathf.Clamp01(progress + change);
        SetProgressFill(progress);
    }

    private void SetProgressFill(float value)
    {
        if (progressFill == null || progressMin == null || progressMax == null)
            return;

        RectTransform fillRect = progressFill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 0f);
        fillRect.pivot = new Vector2(0.5f, 0f);

        float minY = progressMin.anchoredPosition.y;
        float maxY = progressMax.anchoredPosition.y;
        float currentY = Mathf.Lerp(minY, maxY, Mathf.Clamp01(value));

        fillRect.offsetMin = new Vector2(0f, minY);
        fillRect.offsetMax = new Vector2(0f, currentY);
    }

    private void UpdateTemperatureText()
    {
        if (temperatureText == null)
            return;

        float halfTrack = track.rect.height * 0.5f;
        float halfBar = catchBar.rect.height * 0.5f;
        float minY = -halfTrack + halfBar;
        float maxY = halfTrack - halfBar;

        float t = Mathf.InverseLerp(minY, maxY, catchBar.anchoredPosition.y);
        float temperature = Mathf.Lerp(minTemperature, maxTemperature, t);
        temperatureText.text = temperature.ToString("0") + "\u00B0C";
    }
}
