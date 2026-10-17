using UnityEditor;

/// <summary>
/// 【已停用自动重建】之前的 build-level02.flag 会在打开工程时自动整场景重建，
/// 曾覆盖了用户手动调好的布局，造成不可逆损失。
/// 现在改为：不再做任何打开工程即自动改场景的行为。
/// 需要重建时，请手动点击菜单 Tools → Level02 → 相关项。
/// </summary>
[InitializeOnLoad]
public static class Level02AutoRunOnOpen
{
    static Level02AutoRunOnOpen()
    {
        // 有意留空：禁止任何打开工程即自动改场景的行为。
        // 若确需临时自动执行，请显式加回，完成后立即移除。
    }
}
