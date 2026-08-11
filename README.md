# MazeGame

一款以「迷宫探索 + 食材经营」为核心的 2D Unity 游戏。玩家在迷宫中消耗体力探索节点、破解机关、躲避陷阱、收集食材与图纸碎片，再回到经营侧进行制作、售卖、图鉴收集与装扮养成。

> 当前分支 `new-structure` 处于架构重构期：迷宫玩法与经营侧领域系统已从仓库中移除（旧的 ECS 迷宫系统、Shop/Crafting 等服务均已删除），`GameServices` 为骨架，UI 层已全面切换到 MVC。玩法数据表仍完整保留。

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
3. 直接进入 Play Mode。`GameManager` 会完成数据库加载、框架初始化、ECS 世界创建、UI 引导（先打开 `MainMenu` 视图，再打开常驻 `HUD`）。
4. 若配置表有变动，先执行菜单 `MazeGame/Config/Export Excel To Json` 重新导出（见下文「数据流水线」）。

## 目录结构

```
Assets/
  Scenes/                Main（常驻引导）/ MainMenu / Maze
  Scripts/
    Core/                GameManager（唯一入口）、GameDatabase、UIManager、SceneFlowManager
    Config/              DataConfig（表名→类型）、UIConfig（UI 层级与预制体地址）、SceneConfig（状态→场景）
    Framework/
      Context/           Context（总线封装）、GameContext（全局门面）、CommandBus / EventBus、
                         CommandType / EventType（partial 类型化键）、DataBag、Injector
      ECS/               EcsWorld / EntityId / IComponent / ISystem 轻量 ECS 基建
      MVC/UI/            非 MonoBehaviour 三件套：Controller / Model / View + UIHelper
      Services/          GameServices（领域服务构建入口，当前为骨架）
      Timer/             GameTimer
      Extensions/        GameObjectExtend
    Game/
      Commands/          全局命令与实现（ChangeCurrency）
      Events/            EventType 扩展（CurrencyChanged、EnergyChanged）
    Player/              PlayerDatabase（聚合）、PlayerProfile、PlayerInventory、PlayerCollections
    Data/                BaseData / BaseDatabase / DatabaseHelper + 各领域数据模型
                         （Item / Food / Recipe / Ingredient / Blueprint / Outfit / Furniture / Buff / Maze / Player）
    UI/
      Controller/        HUDController / MainMenuController / MazeController
      Model/             HUDModel / MainMenuModel / MazeModel
      View/              HUDView / MainMenuView / MazeView
    Editor/              Excel→JSON 导出器、ExportConfig（导出模块登记）、各领域导出模块（Base/）
  Configs/Generated/Resources/   导出产物 JSON，base.json 为清单
  UI/Prefabs/            Addressable UI 预制体（HUD / MainMenu / Maze，含遗留的 Shop / ProfileDetail / MazePuzzle*）
  Arts/                  切图与效果图
  Packages/              本地 NuGet 包（ExcelDataReader）
Excel/                   设计表（唯一数据源）
策划案/                   策划文档（.docx）
```

## 架构概览

### 启动链路

[GameManager.cs](Assets/Scripts/Core/GameManager.cs) 是唯一入口（`DontDestroyOnLoad` 单例），`InitializeGame()` 的顺序即整个依赖顺序：

```
GameDatabase.Init()      读取 base.json 清单 → 按 DataConfig.DataTypes 构建各表字典 + 默认玩家数据
GameContext.Init()       创建 Context（EventBus + CommandBus），注册自身进 Injector
GameTimer.Init()         计时器
EcsWorld.Init()          ECS 世界（基建保留，暂无注册系统）
GameServices.Init()      领域服务构建入口（当前为骨架）
UIManager.Init()         建立 UILayers 层级 + EventSystem，注册进 Injector
SceneFlowManager.Init()  确保 Main 场景常驻
AddCommand(ChangeCurrency) 注册全局货币指令
SceneFlowManager.GoToMain() 加载 MainMenu 场景并打开 MainMenu 视图
UIManager.GotoView("HUD")   打开常驻 HUD
```

`Update()` 每帧驱动 `gameTimer.Tick(deltaTime)`（当前未驱动 ECS 世界）；`OnDestroy()` 逆序释放全部模块。

### 分层职责

- **Core（入口层）**：`GameManager` 负责组装与编排，`GameDatabase` 提供配置表与玩家数据访问，`UIManager` 与 `SceneFlowManager` 分别管理 UI 与场景。
- **Framework/Context（与游戏无关的基建）**：[Context.cs](Assets/Scripts/Framework/Context/Context.cs) 封装 `CommandBus`（用户操作 → 状态变更）与 `EventBus`（状态变更 → 界面刷新）；[GameContext.cs](Assets/Scripts/Framework/Context/GameContext.cs) 是全局门面单例，提供 `Execute` / `DispatchEvent` / `AddEvent` / `AddCommand` 等入口；[Injector.cs](Assets/Scripts/Framework/Context/IOC/Injector.cs) 提供构造注入（singleton / transient）；[DataBag.cs](Assets/Scripts/Framework/Context/Databag/DataBag.cs) 是通用数据包，支持集合初始化器、`FromObject` 反射展开与 `DataBindAttribute` 字段自动绑定；[GameTimer.cs](Assets/Scripts/Framework/Timer/GameTimer.cs) 提供单次 / 循环定时。
- **Framework/MVC**：UI 采用 `Controller / Model / View` 三件套，三者均为普通 C# 类（View 挂到预制体上成为 MonoBehaviour），见下文「UI（MVC）」。
- **Framework/ECS**：轻量 ECS 基建（`EcsWorld` / `EntityId` / `IComponent` / `ISystem`）保留，但重构后暂无系统注册、暂未驱动 Tick。
- **Game/Commands 与 Game/Events**：全局指令与事件的「类型化键」扩展文件。`CommandType` 与 `EventType` 是 partial readonly struct，各自自动自增（指令从 1000 起、事件从 2000 起）。新增时新建 `CommandType.<模块>.cs` / `EventType.<模块>.cs` 用 `Next()` 取下一个值。
- **Player**：[PlayerDatabase.cs](Assets/Scripts/Player/PlayerDatabase.cs) 聚合玩家的 `Profile` / `Inventory` / `Collections` / `Blueprints`，初值由 `player_start` 表注入，运行时状态由 DataBag 更新。
- **Data**：运行时数据模型 + JSON 加载。`BaseDatabase` 依据 `base.json` 清单 `Resources.Load` 对应哈希 JSON，再按 `DataConfig.DataTypes` 反序列化进表字典。

### 指令与事件

- **指令（CommandBus）**：跨 UI/领域边界的状态变更走指令。`GameContext.Instance.Execute(CommandType.X, dataBag)` 返回 `Task<CommandResult>`。当前全局指令见 [CurrencyCommands.cs](Assets/Scripts/Game/Commands/CurrencyCommands.cs)（`ChangeCurrency`，payload 支持 `type=add/sub/set`），注册发生在 [GameManager.cs](Assets/Scripts/Core/GameManager.cs)。
- **事件（EventBus）**：状态广播走事件，界面据此刷新。当前事件见 [PlayerEventType.cs](Assets/Scripts/Game/Events/PlayerEventType.cs)（`CurrencyChanged`、`EnergyChanged`）。单值事件 `DispatchEvent(EventType.X, new object[]{ 值 })`；多字段事件 `DispatchEvent(EventType.X, new DataBag{...})`。
- `EventBus` 支持「父事件 → 子事件」绑定（带循环依赖检测）。
- `Model` 会记录自己注册的事件 / 指令 / 按钮事件，`Dispose()` 时按身份解绑，只清理本界面自己的条目，不触碰共享总线上的其他注册。

### UI（MVC）

[UIManager.cs](Assets/Scripts/Core/UIManager.cs) 按 viewName 推导类型：`{viewName}Controller` / `{viewName}Model` / `{viewName}View`（先 `Type.GetType`，失败后遍历已加载程序集按全名/简单名匹配），并通过 [Injector](Assets/Scripts/Framework/Context/IOC/Injector.cs) 构造注入创建。预制体按 Addressables 地址 `Assets/UI/Prefabs/{viewName}.prefab` 加载，句柄缓存避免重复加载，并用 `loadingViewTasks` 防止并发打开同一界面。

- **层级**：`UILayers` 根下分 `BackgroundLayer / NormalLayer / PopupLayer / TopLayer / ToastLayer` 五层，由 `Model.Layer` 指定。
- **生命周期**：`Initialize` → `OnOpen`（进入）→ `OnRefresh` / `OnHide`（刷新 / 隐藏）→ `OnClose`（销毁）。`Controller.EnterViewWithData(DataBag)` 是控制器的唯一数据入口，`shouldRefresh` / `isActive` 决定走刷新还是隐藏分支。
- **Controller**：只做「绑定控件、把输入翻译成指令/服务调用、订阅事件、渲染状态」；业务规则不写在 UI。参见 [HUDController.cs](Assets/Scripts/UI/Controller/HUDController.cs)。
- **Model**：持有 `Context`，负责数据同步（`SyncModel`）与自身事件/指令/点击事件的注册与解绑。
- **View**：在 `BindViewUI` 中按路径缓存控件引用，`SyncView` 根据 DataBag 刷新界面；`GetChildByPath` / `SetText` 由 [UIHelper](Assets/Scripts/Framework/MVC/UI/UIHelper.cs) 提供。

### 场景流

[SceneFlowManager.cs](Assets/Scripts/Core/SceneFlowManager.cs) 以 `SceneState`（`MainMenu` / `InMaze`）为状态，映射定义在 [SceneConfig.cs](Assets/Scripts/Config/SceneConfig.cs)。`Main` 场景始终常驻，动态场景以 additive 方式加载：

| 入口 | 场景 | UI |
| --- | --- | --- |
| `GoToMain()` | `MainMenu` | `MainMenu` |
| `GoToMaze()` | `Maze` | `Maze` |
| `GoToShop()` | `MainMenu` | `Shop`（视图待重建） |

## 迷宫玩法（重构中）

> 旧的 ECS 迷宫玩法（`MazeService`、四个解密房间系统）已在本重构分支删除。

- 迷宫配置数据仍完整保留并正常加载：`maze_nodes`（节点图，含 `entrance` / `corridor_reward` / `room_reward` / `switch` / `puzzle_room` / `trap_corridor` / `trap_room` / `evacuate` / `exit` 等类型）、`maze_puzzles`、`maze_traps`、`maze_fragments`、`maze_rules`（体力消耗与撤离/失败/通关/完美通关倍率）。
- 当前 `Maze` 仅为一个 MVC 占位视图（[MazeController.cs](Assets/Scripts/UI/Controller/MazeController.cs) / [MazeView.cs](Assets/Scripts/UI/View/MazeView.cs)），View 从 `MazeMapRoot/MazeNodeRoot` 读取节点物体列表，Controller 内置一段测试按钮逻辑。
- **遗留资源**：`Assets/UI/Prefabs` 下的 `MazePuzzle*UI.prefab`、`ShopUI.prefab`、`ProfileDetailUI.prefab` 尚无对应 Controller/Model/View 代码，待随玩法重建。

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
| `MazeGame/Config/Refresh Excel Json Imports` | 刷新生成的 JSON 资源导入 |
| `MazeGame/Config/delete all Jsons` | 清空生成的 JSON |

不要手改 `Assets/Configs/Generated/Resources/*.json`，改表后走导出流程，并同时 review JSON 与 `.meta` 的变更。表结构变化时，数据模型、导出模块、运行时消费方需一并更新。

## 测试

本重构分支已移除旧的 EditMode 测试（`Assets/Tests/EditMode/Editor` 已删除）。恢复测试时建议优先覆盖：总线（指令/事件/IOC）、数据加载与导出器、命令与事件、UI 生命周期。

## 开发约定

- **命名空间**：当前全部位于全局命名空间，除刻意的整体重构外请沿用。
- **代码风格**：四空格缩进，大括号独占一行，显式访问修饰符，类型/方法 `PascalCase`，局部变量与字段 `camelCase`。不做与任务无关的格式改动。
- **指令与事件**：跨 UI/领域边界的状态变更走 `CommandBus`（已有对应命令时），状态广播走 `EventBus`。新增键用 `CommandType.Next()` / `EventType.Next()` 在对应 partial 文件中追加。
- **生命周期对称**：注册的监听要注销（`Model.Dispose` 自动解绑本视图条目），计时器要清理，ECS 系统要 Dispose，Addressables 句柄要释放。
- **状态安全**：修改玩家状态前先校验 ID 与消耗，避免部分修改，返回明确的 `CommandResult` 或失败值。
- **一致性**：viewName、Addressables 预制体地址、`{viewName}Controller/Model/View` 类型名、`SceneConfig` 场景名必须保持对齐（`UIManager` 依据命名约定推导类型）。
- **资源操作**：新增、移动、重命名、删除资源请通过 Unity 编辑器（或 Unity MCP），让 Unity 维护 `.meta` 与 GUID。切勿手改 `.meta` 中的 GUID。
- **编码**：源码使用 UTF-8。部分历史注释存在乱码，不要扩散；仅在任务范围内修复。
- **不要提交/编辑**的生成内容：`Library/`、`Temp/`、`Logs/`、`obj/`、`UserSettings/`、`*.csproj`、`*.sln` 及构建产物（已在 [.gitignore](.gitignore) 中忽略）。

更完整的协作规范见 [AGENTS.md](AGENTS.md)。

## 其他

- `.mcp.json` 配置了 Unity MCP（`http://127.0.0.1:8080/mcp`），供编辑器自动化使用。
- `策划案/` 存放策划文档：`迷宫探索系统.docx`、`迷宫食材探险经营类游戏 - 系统开发总案.docx`。
- `Assets/Screenshots/MazePuzzles/` 存放解密房间的界面截图（历史遗留）。
