using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Scriptable Objects/PlayerSettings")]
public class PlayerSettings : ScriptableObject
{
    [Header("MOVEMENT")]
    [Tooltip("移动速度")]
    public float MoveSpeed = 2f;
    [Tooltip("加速：有输入时逼近目标速度的快慢")]
    public float Acceleration = 20f;
    [Tooltip("减速：松开输入后停下，值越大越急停")]
    public float Deceleration = 50f;
}
