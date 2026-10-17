using UnityEngine;

/// <summary>
/// @trace MECH-205
/// 部首拼字管理器：监听当前玩家背包，当集齐某个字的全部部首后，
/// 自动把这些部首消耗掉，拼成一个完整的字牌（Level02GlyphItem）放进背包，
/// 玩家选中它按 Q 即可放到对应建筑（同「路/水/禁」玩法）。
///
/// 配方：醫 = 医+殳+酉；書 = 聿+曰；駅 = 馬+尺。
/// </summary>
public class Level02CombineManager : MonoBehaviour
{
    [System.Serializable]
    public struct Recipe
    {
        public string result;      // 拼合后的字
        public string[] pieces;    // 需要的部首
    }

    [SerializeField] private Recipe[] recipes = new Recipe[]
    {
        new Recipe { result = "醫", pieces = new[] { "医", "殳", "酉" } },
        new Recipe { result = "書", pieces = new[] { "聿", "曰" } },
        new Recipe { result = "駅", pieces = new[] { "馬", "尺" } },
    };

    private float nextCheck;

    private void Update()
    {
        if (Time.time < nextCheck)
            return;
        nextCheck = Time.time + 0.25f;

        PlayerManager player = CurrentMainPlayer();
        PlayerInventoryManager inventory = player != null ? player.playerInventory : null;
        if (inventory == null || inventory.inventory == null)
            return;

        foreach (Recipe recipe in recipes)
        {
            if (!HasAllFragments(inventory, recipe))
                continue;

            // 消耗部首
            foreach (string p in recipe.pieces)
                inventory.RemoveItem(FindFragment(inventory, p));

            // 拼成字牌
            Level02GlyphItem combined = ScriptableObject.CreateInstance<Level02GlyphItem>();
            combined.Configure(recipe.result, "「" + recipe.result + "」");
            inventory.TryAdd(combined);
            inventory.NotifyChanged();

            ShowMessage($"部首严丝合缝地拼在了一起——「{recipe.result}」！\n把它带到需要的地方，按 Q 使用吧。");
        }
    }

    private static bool HasAllFragments(PlayerInventoryManager inventory, Recipe recipe)
    {
        foreach (string p in recipe.pieces)
            if (FindFragment(inventory, p) == null)
                return false;
        return true;
    }

    private static Level02FragmentItem FindFragment(PlayerInventoryManager inventory, string glyph)
    {
        foreach (InventoryItemData item in inventory.inventory)
            if (item is Level02FragmentItem frag && frag.Glyph == glyph)
                return frag;
        return null;
    }

    private static PlayerManager CurrentMainPlayer()
    {
        if (PlayerSelector.Instance == null)
            return null;
        PlayerManager p = PlayerSelector.Instance.currentPlayer;
        return p != null && p.playerType == PlayerType.Main ? p : null;
    }

    private static void ShowMessage(string message)
    {
        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.popUpManager != null)
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message);
    }
}
