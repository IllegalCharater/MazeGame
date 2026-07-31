# MazeGame

一款以「迷宫探索 + 食材经营」为核心的 2D Unity 游戏。玩家在迷宫中消耗体力探索节点、破解机关、躲避陷阱、收集食材与图纸碎片，再回到经营侧进行制作、售卖、图鉴收集与装扮养成。

## 环境要求

| 项 | 版本 |
| --- | --- |
| Unity | `2021.3.45f1c2`（`ProjectSettings/ProjectVersion.txt`） |
| 渲染管线 | Universal RP `12.1.15`（2D） |
| 关键包 | Addressables `1.19.19`、Input System `1.7.0`、Cinemachine `2.10.3`、TextMeshPro `3.0.6`、Newtonsoft JSON `3.2.2`、Test Framework `1.1.33` |

依赖固定在 [Packages/manifest.json](Packages/manifest.json)。除明确需求外，不要升级 Unity 或包版本。

## 快速开始

1. 用 Unity Hub 以 `2021.3.45f1c2` 打开本仓库根目录。
2. 打开 [Assets/Scenes/Main.unity](Assets/Scenes/Main.unity) —— 这是常驻的引导场景，`MainMenu` 与 `Maze` 会以 additive 方式加载。
3. 直接进入 Play Mode。`GameManager` 会完成数据库加载、框架初始化、ECS 世界创建、服务注册与 UI 引导。
4. 若配置表有变动，先执行菜单 `MazeGame/Config/Export Excel To Json` 重新导出（见下文「数据流水线」）。

## 目录结构

```
Assets/
  Scenes/            Main（常驻引导）/ MainMenu / Maze
  Scripts/
    Core/            GameManager、GameDatabase、UIManager、SceneFlowManager、GameEvents、GameState、GameTypes
    Framework/       CommandBus、EventBus、IOCContainer、GameTimer（FrameworkContext 组装）
    ECS/             IComponent / EntityId / ISystem / EcsWorld 轻量 ECS 基建
    Config/          DataConfig（表名→数据类型）、UIConfig（视图名→控制器）、SceneConfig（状态→场景）
    Data/            运行时数据模型与 JSON 加载（BaseData / BaseDatabase / DatabaseHelper）
    System/          领域服务，由 GameServices 统一构建与注册
    Game/Commands/   全局命令与处理器（货币、导航）
    Game/Maze/       迷宫玩法：Models / Components / Systems / Commands / Events
    Player/          PlayerDatabase、PlayerProfile、PlayerInventory、PlayerCollections
    UI/              非 MonoBehaviour 的 UI 控制器（Base / Mediator）
    Editor/          Excel→JSON 导出器、解密房间 Prefab 构建、调试菜单
  Configs/Generated/Resources/   导出产物 JSON，base.json 为清单
  UI/Prefabs/        Addressable UI 预制体
  Arts/              切图与效果图
  Tests/EditMode/    EditMode 测试
Excel/               设计表（唯一数据源）
策划案/               策划文档（.docx）
```

## 架构概览

### 启动链路

[GameManager.cs](Assets/Scripts/Core/GameManager.cs) 是唯一入口（`DontDestroyOnLoad` 单例），`InitializeGame()` 的顺序即整个依赖顺序：

```
GameDatabase.Init()      读取 Configs/Generated/Resources 下 JSON，构建各表字典 + 默认玩家数据
FrameworkContext.Init()  CommandBus / EventBus / IOCContainer / GameTimer
EcsWorld.Init()          ECS 世界
GameServices.Initialize() 构建全部领域服务 → 注册进 IOC → 注册命令处理器
UIManager.Init()         建立 UILayers 层级，加载 HUD
SceneFlowManager.Init()  确保 Main 场景常驻，进入 MainMenu
```

`Update()` 每帧驱动 `framework.Tick()` 与 `world.Tick()`；`OnDestroy()` 反向释放 ECS 与框架。

### 分层职责

- **Framework**：与游戏无关的基建。`CommandBus` 承载「用户操作 → 状态变更」，`EventBus` 承载「状态变更 → 界面刷新」，`IOCContainer` 提供服务定位，`GameTimer` 提供计时。见 [FrameworkContext.cs](Assets/Scripts/Framework/FrameworkContext.cs)。
- **Services（领域层）**：[GameServices.cs](Assets/Scripts/System/Services/GameServices.cs) 集中构建并暴露 `Shop`、`Crafting`、`Blueprints`、`Buffs`、`Collections`、`Outfits`、`Sell`、`Maze`。新增服务必须在此初始化、注册进 IOC 并对外暴露；服务若持有订阅、计时器或句柄，需补齐清理。
- **ECS**：迷宫玩法用轻量 ECS 承载。`MazeSystem` 与四个解密系统注册到 `EcsWorld`，状态放在 Component 中。
- **UI**：控制器是普通 C# 类（非 MonoBehaviour），职责只有绑定控件、把输入翻译成命令/服务调用、订阅事件、渲染状态；业务规则不写在 UI 里。`UIManager` 用 `Activator` 按 [UIConfig.viewMap](Assets/Scripts/Config/UIConfig.cs) 创建控制器，并通过 Addressables 按 `Assets/UI/Prefabs/{viewName}.prefab` 地址加载预制体，同时缓存句柄避免重复加载。
- **事件**：[GameEvents.cs](Assets/Scripts/Core/GameEvents.cs) 定义全局事件（货币、体力、背包、迷宫、制作、商店、图鉴、Buff 等），界面据此刷新，避免屏幕之间互相耦合。

### 场景流

[SceneFlowManager.cs](Assets/Scripts/Core/SceneFlowManager.cs) 按 `GameState` 加载动态场景，映射定义在 [SceneConfig.cs](Assets/Scripts/Config/SceneConfig.cs)：

| GameState | 场景 |
| --- | --- |
| `MainMenu` | `MainMenu` |
| `InMaze` | `Maze` |

`Main` 场景始终常驻，动态场景以 additive 方式替换。

## 迷宫玩法

迷宫由节点图构成，节点类型来自 `maze_nodes` 表：`entrance`、`corridor_reward`、`corridor_energy`、`room_reward`、`switch`、`puzzle_room`、`trap_corridor`、`trap_room`、`evacuate`、`exit`。

行动消耗体力，结算按 `maze_rules` 的倍率区分撤离 / 失败 / 通关 / 完美通关（默认 `0.5 / 0.3 / 1.0 / 2.0`，完美通关额外产出图纸）。

四个解密房间由 [MazePuzzleSystems.cs](Assets/Scripts/Game/Maze/Systems/MazePuzzleSystems.cs) 中的独立系统实现，`MazeService` 以 `puzzleType` 为键分发：

| puzzleType | 系统 | 视图 |
| --- | --- | --- |
| `item_socket` | `ItemSocketPuzzleSystem`（北斗七星 7 槽位） | `MazePuzzleItemSocketUI` |
| `numeric_input` | `CandleNumberPuzzleSystem` | `MazePuzzleCandleNumberUI` |
| `floor_choice` | `FloorChoicePuzzleSystem` | `MazePuzzleFloorChoiceUI` |
| `block_words` | `RockWordPuzzleSystem` | `MazePuzzleRockWordUI` |

调试开关：[MazeTestSettings.cs](Assets/Scripts/Game/Maze/MazeTestSettings.cs) 的 `directPuzzleRoomEntry` 在测试期默认为 `true`，允许跳过线性链直接点进解密房间；可通过菜单 `MazeGame/Debug/直接进入解密房间` 切换。**正式版本前需置回 `false`。**

## 数据流水线

`Excel/*.xlsx` 是设计数据的唯一来源，经编辑器导出为带哈希后缀的 JSON，运行时通过 `Resources.Load` 读取。

```
Excel/*.xlsx
  → 菜单 MazeGame/Config/Export Excel To Json（ExcelToJsonExporter + Editor/Base 下各领域导出模块）
  → Assets/Configs/Generated/Resources/<table>_<hash>.json
  → base.json（逻辑表名 → 实际文件 stem 的清单）
  → GameDatabase.Init() 按 DataConfig.DataTypes 逐表构建 Dictionary<id, BaseData>
```

当前表：`items`、`player_start`、`recipes`、`foods`、`ingredients`、`blueprints`、`outfits`、`furniture`、`buffs`、`maze_nodes`、`maze_puzzles`、`maze_traps`、`maze_fragments`、`maze_rules`。

编辑器菜单：

| 菜单 | 用途 |
| --- | --- |
| `MazeGame/Config/Export Excel To Json` | 全量导出 Excel 为 JSON 并刷新 `base.json` |
| `MazeGame/Config/Refresh Excel Json Imports` | 重新导入生成的 JSON 资源 |
| `MazeGame/Config/delete all Jsons` | 清空生成的 JSON |
| `MazeGame/UI/Rebuild Puzzle Room Prefabs` | 重建解密房间 UI 预制体 |
| `MazeGame/Debug/直接进入解密房间` | 切换解密房间直达调试开关 |

不要手改 `Assets/Configs/Generated/Resources/*.json`，改表后走导出流程，并同时 review JSON 与 `.meta` 的变更。表结构变化时，数据模型、导出模块、运行时消费方与测试需一并更新。

## 测试

EditMode 测试位于 [Assets/Tests/EditMode/Editor/](Assets/Tests/EditMode/Editor/)：

- `ArchitectureInfrastructureTests.cs` —— 命令总线、事件总线、IOC、计时器等基建
- `MazePuzzleSystemsTests.cs` —— 四类解密系统的判定逻辑
- `MazeVerticalSliceTests.cs` —— 迷宫从开局到结算的纵向切片

通过 `Window > General > Test Runner > EditMode` 运行。改动框架、命令、服务、数据库或导出器后，先跑相关用例，再跑完整 EditMode 套件。

## 开发约定

- **命名空间**：当前全部位于全局命名空间，除刻意的整体重构外请沿用。
- **代码风格**：四空格缩进，大括号独占一行，显式访问修饰符，类型/方法 `PascalCase`，局部变量与字段 `camelCase`。不做与任务无关的格式改动。
- **命令与事件**：跨 UI/领域边界的状态变更走 `CommandBus`（已有对应命令时），状态广播走 `EventBus` / `GameEvents`。
- **生命周期对称**：注册的监听要注销，计时器要清理，ECS 系统要 Dispose，Addressables 句柄要释放。
- **状态安全**：修改玩家状态前先校验 ID 与消耗，避免部分修改，返回明确的 `CommandResult` 或失败值。
- **一致性**：Addressable 视图名、`UIConfig.viewMap`、控制器类型、预制体地址、`SceneConfig` 场景名必须保持对齐。
- **资源操作**：新增、移动、重命名、删除资源请通过 Unity 编辑器（或 Unity MCP），让 Unity 维护 `.meta` 与 GUID。切勿手改 `.meta` 中的 GUID。
- **编码**：源码使用 UTF-8。部分历史注释存在乱码，不要扩散；仅在任务范围内修复。
- **不要提交/编辑**的生成内容：`Library/`、`Temp/`、`Logs/`、`obj/`、`UserSettings/`、`*.csproj`、`*.sln` 及构建产物（已在 [.gitignore](.gitignore) 中忽略）。

更完整的协作规范见 [AGENTS.md](AGENTS.md)。

## 其他

- `.mcp.json` 配置了 Unity MCP（`http://127.0.0.1:8080/mcp`），供编辑器自动化使用。
- `策划案/` 存放策划文档：`迷宫探索系统.docx`、`迷宫食材探险经营类游戏 - 系统开发总案.docx`。
- `Assets/Screenshots/MazePuzzles/` 存放解密房间的界面截图。
