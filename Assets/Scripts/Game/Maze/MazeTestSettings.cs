/// <summary>
/// 测试期开关。打开后四个解密房间（nodeType = puzzle_room）无视 nextNodeIds 线性链，
/// 在迷宫地图上随处可直接点进去，省掉每次都要从 node_01 一路走到 node_11/node_12 的成本。
/// 正式版本前把 directPuzzleRoomEntry 置回 false —— 只影响 MazeSystem 的两处判定
/// （CanMoveToNode 的连通性检查、GetViewModel 的可达列表），关掉即恢复原有线性流程。
/// 编辑器里可通过菜单 MazeGame/Debug/直接进入解密房间 勾选切换。
/// </summary>
/// <summary>
/// maze_nodes 表里 nodeType 的取值。系统层与 UI 层都从这里取，避免各处散落字符串字面量。
/// </summary>
public static class MazeNodeTypes
{
    public const string Entrance = "entrance";
    public const string Exit = "exit";
    public const string Evacuate = "evacuate";
    public const string Switch = "switch";
    public const string PuzzleRoom = "puzzle_room";
    public const string RoomReward = "room_reward";
    public const string CorridorReward = "corridor_reward";
    public const string CorridorEnergy = "corridor_energy";
    public const string TrapRoom = "trap_room";
    public const string TrapCorridor = "trap_corridor";
}

public static class MazeTestSettings
{
    public const string PuzzleRoomNodeType = MazeNodeTypes.PuzzleRoom;
    public const string DirectPuzzleRoomEntryPrefKey = "MazeGame.Debug.DirectPuzzleRoomEntry";

    /// <summary>四个解密房间是否可直达。测试阶段默认开启。</summary>
    public static bool directPuzzleRoomEntry = true;
}
