using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum LightFilterMode
{
    Whitelist,
    Blacklist
}

public class LightFollow : MonoBehaviour
{
    public static LightFollow Instance { get; private set; }

    private Light2D light2D;

    public int LightStage = 0;

    public Transform target;
    public Vector3 offset;
    public float smoothTime = 0.3f;

    public float lightIntensity = 1f;
    public float lightRange = 5f;

    private Vector3 velocity = Vector3.zero;

    public List<LightInfo> lightInfos = new List<LightInfo>();

    [Header("世界灯光")]
    [Tooltip("白名单模式：开局时关闭场景里除跟随灯外的所有 Light2D，等手电筒打开时再开启")]
    [SerializeField] private bool worldLightsStartOff = true;

    [SerializeField] private LightFilterMode lightFilterMode = LightFilterMode.Whitelist;

    [Tooltip("黑名单模式下，场景里除跟随灯外的所有 Light2D 默认关闭，等手电筒打开时再开启")]
    [SerializeField] private List<Light2D> blacklistLights = new List<Light2D>();

    [Tooltip("白名单里的 Light2D 不受全局开关控制，始终保持发光")]
    [SerializeField] private List<Light2D> whitelistLights = new List<Light2D>();

    [Header("Level03")]
    public bool isLevel03 = false;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        light2D = GetComponentInChildren<Light2D>();
    }

    void Start()
    {
        if (worldLightsStartOff && lightFilterMode == LightFilterMode.Whitelist)
            SetWorldLights(false);
    }

    void Update()
    {
        if (lightInfos.Count > 0 && LightStage < lightInfos.Count)
        {
            lightIntensity = lightInfos[LightStage].lightIntensity;
            lightRange = lightInfos[LightStage].lightRange;
        }

        if (light2D != null)
        {
            if (light2D.intensity != lightIntensity || light2D.pointLightOuterRadius != lightRange)
            {
                light2D.intensity = Mathf.Lerp(light2D.intensity, lightIntensity, Time.deltaTime * 5f);
                light2D.pointLightOuterRadius = Mathf.Lerp(light2D.pointLightOuterRadius, lightRange, Time.deltaTime * 5f);
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;

        desiredPosition.z = transform.position.z;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }

    public void SetStage(int stage)
    {
        if (stage >= 0 && stage < lightInfos.Count)
        {
            LightStage = stage;
        }
        else
        {
            Debug.LogWarning($"SetStage: Invalid stage {stage}. It should be between 0 and {lightInfos.Count - 1}.");
        }
    }

    /// <summary>白名单模式下，开启/关闭场景里除跟随灯和白名单灯外的所有 Light2D。</summary>
    public void SetWorldLights(bool enabled)
    {
        if (lightFilterMode != LightFilterMode.Whitelist)
            return;

        Light2D[] allLights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (Light2D light in allLights)
        {
            if (light == null || light == light2D)
                continue;

            if (whitelistLights.Contains(light))
                continue;

            light.enabled = enabled;
        }
    }

    public void SetBlackListLights(bool enabled)
    {
        if (!isLevel03)
            return;

        if (lightFilterMode != LightFilterMode.Blacklist)
            return;

        if (LightStage > 0)
            enabled = true;

        foreach (Light2D light in blacklistLights)
        {
            // 跟随灯由 LightStage 控制，不要在这里开关
            if (light == null || light == light2D)
                continue;

            // 白名单里的灯不受全局开关控制
            light.enabled = enabled;
        }
    }
}
