/// <summary>
/// 全局游戏流程状态，供场景流、迷宫与商店逻辑分支使用。
/// </summary>
public enum GameState
{
    /// <summary>主菜单 / 标题界面</summary>
    MainMenu,
    /// <summary>玩家处于迷宫探索中</summary>
    InMaze,
    /// <summary>商店界面（或城镇中的商店区域）</summary>
    InShop
}