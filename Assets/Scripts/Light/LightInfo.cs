using UnityEngine;

[CreateAssetMenu(fileName = "LightInfo", menuName = "Scriptable Objects/LightInfo")]
public class LightInfo : ScriptableObject
{
    public float lightIntensity = 1f;
    public float lightRange = 5f;
}
