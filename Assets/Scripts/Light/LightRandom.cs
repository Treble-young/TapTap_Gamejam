using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 缓慢呼吸式的明暗变化（偏暗的柔和灯光）。
/// 亮度在 minIntensity 和 maxIntensity 之间做正弦缓动。
/// </summary>
public class LightRandom : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Light2D light2D;

    [Header("亮度范围")]
    [SerializeField] private float minIntensity = 0.25f;
    [SerializeField] private float maxIntensity = 0.6f;

    [Header("呼吸节奏")]
    [Tooltip("一个完整呼吸循环的秒数")]
    [SerializeField] private float breathPeriod = 4f;

    private float phaseOffset;

    private void Awake()
    {
        if (light2D == null)
            light2D = GetComponentInChildren<Light2D>();

        // 随机相位，避免场景里多盏灯同步呼吸
        phaseOffset = Random.Range(0f, breathPeriod);
    }

    private void Update()
    {
        if (light2D == null)
            return;

        // sin 输出 -1~1，映射到 [minIntensity, maxIntensity]
        float angle = (Time.time + phaseOffset) * Mathf.PI * 2f / breathPeriod;
        float t = (Mathf.Sin(angle) + 1f) * 0.5f;

        light2D.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}
