using UnityEngine;

/// <summary>
/// 【已停用】第二关开场杂草走廊。
/// 杂草与「限水平移动」的设计暂时取消：本脚本不再做任何限制，
/// 并且在 Awake 时把自己连同杂草子物体一起销毁，避免残留影响操作。
/// 保留文件仅为兼容已保存的场景引用，可随时删除。
/// </summary>
public class Level02GrassCorridor : MonoBehaviour
{
    private void Awake()
    {
        // 自我清理：移除所有杂草子物体与自身
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
        Destroy(this);
    }
}
