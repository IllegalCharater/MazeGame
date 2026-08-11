# MazeGame Codex Guide

## Project snapshot

- Unity `2021.3.45f1c2`, 2D URP (`12.1.15`).
- C# project using uGUI/TextMeshPro, Input System, Addressables, Cinemachine,
  Newtonsoft JSON, and the Unity Test Framework.
- The game vision combines maze exploration with crafting, shop, collection,
  outfit, blueprint, buff, inventory, and player-profile systems. On the
  `new-structure` branch those domain systems are being rebuilt: the old ECS
  maze gameplay and the Shop/Crafting/Collection/etc. services have been
  removed, `GameServices` is a skeleton, and the UI layer now uses MVC
  (Controller / Model / View). The design tables still cover all systems.
- Keep compatibility with the Unity version and the packages pinned in
  `Packages/manifest.json`. Do not upgrade Unity or packages unless requested.

## Important paths

- `Assets/Scripts/Core`: application lifetime and orchestration — `GameManager`
  (entry), `GameDatabase`, `UIManager`, `SceneFlowManager`.
- `Assets/Scripts/Framework/Context`: `Context` (wraps the buses), `GameContext`
  (global singleton facade), `CommandBus`, `EventBus`, `CommandType`/`EventType`
  (partial typed keys), `DataBag`, and `Injector` (DI).
- `Assets/Scripts/Framework/ECS`: lightweight entity/component/system
  infrastructure (`EcsWorld`, `EntityId`, `IComponent`, `ISystem`). Retained;
  no systems registered yet.
- `Assets/Scripts/Framework/MVC/UI`: UI MVC trio — `Controller`, `Model`,
  `View` — plus `UIHelper`.
- `Assets/Scripts/Framework/Services`: `GameServices`, the domain-service
  construction entry point (currently a skeleton).
- `Assets/Scripts/Framework/Timer`: `GameTimer`.
- `Assets/Scripts/Game/Commands`: global commands and handlers (e.g.
  `ChangeCurrency`).
- `Assets/Scripts/Game/Events`: `EventType` extension files for game events
  (e.g. `CurrencyChanged`, `EnergyChanged`).
- `Assets/Scripts/Player`: runtime player data — `PlayerDatabase` (aggregate),
  `PlayerProfile`, `PlayerInventory`, `PlayerCollections`.
- `Assets/Scripts/Data`: runtime data models and database loading
  (`BaseData`/`BaseDatabase`/`DatabaseHelper` plus per-domain models).
- `Assets/Scripts/UI`: MVC per view — `Controller/`, `Model/`, `View/`
  subfolders. `UIManager` derives `{viewName}Controller/Model/View` from the
  view name and creates them through the `Injector`.
- `Assets/Scripts/Editor`: Excel-to-JSON exporter, `ExportConfig` (module
  registry), and per-domain export modules.
- `Excel`: source-of-truth design tables.
- `Assets/Configs/Generated/Resources`: generated JSON; `base.json` maps logical
  table names to hashed resource stems.
- `Assets/UI/Prefabs`: Addressable UI prefabs (`HUD`, `MainMenu`, `Maze`, plus
  legacy `Shop` / `ProfileDetail` / `MazePuzzle*` without matching code).
- `Assets/Scenes`: `Main` is the persistent bootstrap scene; `MainMenu` and
  `Maze` are loaded additively.
- There are currently no automated tests on this branch; `Assets/Tests` was
  removed during the restructure.

## Working rules

1. Start by checking `git status --short`. The worktree may contain ongoing
   user changes; preserve them and do not revert, reformat, or overwrite
   unrelated files.
2. Before a Unity Editor operation, read the Unity MCP editor state and project
   info. If more than one Editor is connected, select the MazeGame instance.
3. Use Unity MCP for scenes, GameObjects, components, prefabs, Addressables, and
   other serialized Unity assets. Discover targets before mutation and batch
   independent operations when practical.
4. Existing C# and text files may be patched directly. Use Unity MCP when
   creating, moving, renaming, or deleting assets so Unity maintains `.meta`
   files and GUID references. Never hand-edit a `.meta` GUID.
5. Do not edit generated or transient content under `Library`, `Temp`, `Logs`,
   `obj`, `UserSettings`, generated solution/project files, or build output.
6. Treat `Excel/*.xlsx` as the design-data source. Do not manually edit
   `Assets/Configs/Generated/Resources/*.json` unless the task explicitly targets
   generated output. Regenerate through `MazeGame/Config/Export Excel To Json`
   and review both the JSON and `.meta` changes.
7. Avoid broad YAML edits to `.unity`, `.prefab`, or Addressables assets. Use the
   Editor/MCP so object references and file IDs stay valid.
8. Keep the current global-namespace convention unless a requested refactor
   deliberately introduces namespaces across all affected files.
9. Use UTF-8 for source text. Some legacy comments are mojibake; do not propagate
   corrupted text. Repair only comments or strings inside the requested scope.

## Architecture conventions

- Keep UI controllers thin (MVC): bind widgets, translate input into
  commands/service calls, subscribe to events, and render state. Business rules
  belong in domain services or command handlers, not in `Controller`/`Model`.
- User-triggered state changes that cross UI/domain boundaries should use
  `CommandBus` where an appropriate command exists. Publish state changes through
  `EventBus` rather than coupling screens together. Add new keys with
  `CommandType.Next()` / `EventType.Next()` in the matching partial file
  (commands start at 1000, events at 2000).
- A view's `Model` records the events/commands/click listeners it registers and
  unbinds them on `Dispose` by identity — never clear the shared bus from a view.
- When the domain services are re-established, they must be initialized,
  registered in the IOC container, and exposed from `GameServices`. Add cleanup
  when a service owns subscriptions, timers, handles, or other resources.
- Preserve lifecycle symmetry: unsubscribe listeners, clear timers, dispose ECS
  systems, and release Addressables handles that were acquired.
- Validate IDs and costs before mutating player state. Avoid partial mutations;
  return a clear `CommandResult` or failure value.
- Keep data lookups keyed by `DataConfig` root keys and normalized IDs. When a
  schema changes, update the data model, exporter module, and runtime consumer
  together.
- View names, Addressables prefab addresses
  (`Assets/UI/Prefabs/{viewName}.prefab`), `{viewName}Controller/Model/View`
  type names, and scene names in `SceneConfig` must remain aligned — `UIManager`
  derives types from the view name by convention.
- Follow the local C# style in touched code: four-space indentation, braces on
  their own lines, explicit access modifiers, `PascalCase` types/methods and
  `camelCase` locals/fields. Avoid unrelated style-only churn.

## Verification

Use the narrowest relevant checks, then expand for cross-cutting changes:

1. After C# changes, wait until Unity reports `is_compiling == false`, then read
   Console errors with stack traces. Do not call a redundant refresh after an
   MCP script edit.
2. There are currently no EditMode tests on this branch. If/when tests are
   restored, run targeted EditMode tests, then the full EditMode suite for
   framework, command, service, database, or exporter changes.
3. For scene, prefab, UI, or rendering changes, inspect the modified hierarchy
   and capture a Game/Scene view screenshot. Exercise the affected flow in Play
   Mode when feasible.
4. For Addressables changes, verify the address/group entry and that the asset
   loads and releases successfully.
5. For Excel/schema changes, run the exporter, check its Console output, inspect
   the generated manifest/data diff, and load the affected database entry.
6. If Unity MCP is unavailable, report that limitation. A batch-mode Unity test
   run is an acceptable fallback only when the matching Unity Editor is installed
   and no Editor process has the project locked.

Do not claim completion while there are new Unity compilation errors, failing
relevant tests, missing references, or unreviewed generated-data changes.
