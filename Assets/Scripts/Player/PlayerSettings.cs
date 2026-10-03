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

    [Header("AUTO FOLLOW")]
    [Tooltip("跟随（寻路）移动速度")]
    public float FollowSpeed = 2.5f;
    [Tooltip("离目标多近算到达，到达后停下，不再往里挤")]
    public float FollowStopDistance = 0.6f;
    [Tooltip("离目标多近开始缓缓减速（应大于 FollowStopDistance）")]
    public float FollowSlowDistance = 2f;
    [Tooltip("与其他跟随玩家保持的最小间距，小于该距离会相互避开")]
    public float SeparationRadius = 0.9f;
    [Tooltip("避让其他玩家的强度")]
    public float SeparationWeight = 1.5f;
    [Tooltip("每隔多久重新寻路一次")]
    public float FollowRepathInterval = 0.4f;
    [Tooltip("目标移动超过这个距离就立即重新寻路")]
    public float FollowRepathThreshold = 0.4f;
}
