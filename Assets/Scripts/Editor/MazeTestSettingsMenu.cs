using UnityEditor;

/// <summary>
/// 菜单开关：MazeGame/Debug/直接进入解密房间。
/// 勾选状态存 EditorPrefs，domain reload 后由 InitializeOnLoad 重新写回
/// MazeTestSettings.directPuzzleRoomEntry（静态字段每次重载都会回到默认值）。
/// </summary>
[InitializeOnLoad]
public static class MazeTestSettingsMenu
{
    private const string MenuPath = "MazeGame/Debug/直接进入解密房间";

    static MazeTestSettingsMenu()
    {
        MazeTestSettings.directPuzzleRoomEntry =
            EditorPrefs.GetBool(MazeTestSettings.DirectPuzzleRoomEntryPrefKey, true);
        EditorApplication.delayCall += () =>
            Menu.SetChecked(MenuPath, MazeTestSettings.directPuzzleRoomEntry);
    }

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        bool enabled = !MazeTestSettings.directPuzzleRoomEntry;
        MazeTestSettings.directPuzzleRoomEntry = enabled;
        EditorPrefs.SetBool(MazeTestSettings.DirectPuzzleRoomEntryPrefKey, enabled);
        Menu.SetChecked(MenuPath, enabled);
        UnityEngine.Debug.Log("[MazeTestSettings] 解密房间直达 = " + (enabled ? "开启（测试期）" : "关闭（正式流程）"));
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, MazeTestSettings.directPuzzleRoomEntry);
        return true;
    }
}
