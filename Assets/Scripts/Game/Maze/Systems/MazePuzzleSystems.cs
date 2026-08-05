using System;
using System.Collections.Generic;

public interface IMazePuzzleSystem : ISystem {
    string PuzzleType { get; }
    MazePuzzleRoomViewModel GetViewModel(string puzzleId, MazeRunComponent run);
    MazePuzzleEvaluation Evaluate(string puzzleId, MazeRunComponent run);
    void ApplyResolution(string puzzleId, MazePuzzleEvaluation evaluation, MazeRunComponent run);
}

public sealed class ItemSocketPuzzleStateComponent : IComponent {
    public string puzzleId;
    // 北斗七槽，下标即天枢→瑶光的摆放顺序，空槽存 string.Empty。
    // 判定要按顺序拼接，所以必须是定长有序表，长度由 ItemSocketPuzzleSystem.SlotCount 维护。
    public readonly List<string> slotItemKeys = new List<string>();
    public string feedback;
    public bool solved;
}

public sealed class CandleNumberPuzzleStateComponent : IComponent {
    public string puzzleId;
    public List<bool> candleStates = new List<bool>();
    public bool poolOpened;
    public string selectedNumber;
    public string feedback;
    public bool solved;
}

public sealed class FloorChoicePuzzleStateComponent : IComponent {
    public string puzzleId;
    public string selectedTileKey;
    public HashSet<string> failedTileKeys = new HashSet<string>();
    public string feedback;
    public bool solved;
}

public sealed class RockWordPuzzleStateComponent : IComponent {
    public string puzzleId;
    public bool lightActivated;
    public HashSet<string> blockedWordKeys = new HashSet<string>();
    public string feedback;
    public bool solved;
}

public abstract class MazePuzzleSystemBase : IMazePuzzleSystem {
    private readonly GameDatabase database;
    private readonly PlayerDatabase player;
    private readonly EventBus eventBus;

    protected EcsWorld world;
    protected EntityId stateEntity;

    public abstract string PuzzleType { get; }

    protected MazePuzzleSystemBase(GameDatabase database, PlayerDatabase player, EventBus eventBus) {
        this.database = database;
        this.player = player;
        this.eventBus = eventBus;
    }

    public void Initialize(EcsWorld world) {
        this.world = world;
        stateEntity = world.CreateEntity("MazePuzzleSystem:" + PuzzleType);
        CreateStateComponent();
        eventBus?.Subscribe<MazeRunStartedEvent>(OnMazeRunStarted);
        ResetState();
    }

    public void Tick(float deltaTime) {
    }

    public void Dispose() {
        eventBus?.Unsubscribe<MazeRunStartedEvent>(OnMazeRunStarted);
        if (world != null && stateEntity.IsValid && world.IsAlive(stateEntity))
            world.DestroyEntity(stateEntity);
        stateEntity = default;
        world = null;
    }

    public abstract MazePuzzleRoomViewModel GetViewModel(string puzzleId, MazeRunComponent run);
    public abstract MazePuzzleEvaluation Evaluate(string puzzleId, MazeRunComponent run);
    public abstract void ApplyResolution(string puzzleId, MazePuzzleEvaluation evaluation, MazeRunComponent run);

    protected abstract void CreateStateComponent();
    protected abstract void ResetState();

    protected MazePuzzleData GetPuzzle(string puzzleId) {
        if (string.IsNullOrEmpty(puzzleId) || database == null)
            return null;
        return database.Get<MazePuzzleData>("maze_puzzles", puzzleId);
    }

    protected MazePuzzleRoomViewModel CreateBaseViewModel(MazePuzzleData data) {
        MazePuzzleRoomViewModel vm = new MazePuzzleRoomViewModel {
            puzzleId = data != null ? data.puzzleId : string.Empty,
            puzzleType = data != null ? data.puzzleType : PuzzleType,
            hintText = data != null ? data.hintText : string.Empty,
            fragmentText = data != null ? data.fragmentText : string.Empty,
            currentEnergy = player?.Profile != null ? player.Profile.energy : 0,
            maxEnergy = player?.Profile != null ? player.Profile.maxEnergy : 0
        };
        if (data?.successRewardItems != null) {
            foreach (KeyValuePair<string, int> reward in data.successRewardItems)
                vm.successRewards[reward.Key] = reward.Value;
        }
        return vm;
    }

    protected void FillOptions(MazePuzzleRoomViewModel vm, MazePuzzleData data, Func<string, bool> selected, Func<string, bool> available, Func<string, bool> failed) {
        if (vm == null || data?.optionKeys == null)
            return;

        for (int i = 0; i < data.optionKeys.Count; i++) {
            string key = data.optionKeys[i];
            vm.options.Add(new MazePuzzleOptionViewModel {
                key = key,
                label = ResolveOptionLabel(data, i, key),
                selected = selected != null && selected(key),
                available = available == null || available(key),
                failed = failed != null && failed(key)
            });
        }
    }

    protected void Publish(MazePuzzleRoomViewModel viewModel) {
        eventBus?.Publish(new MazePuzzleStateChangedEvent(viewModel));
    }

    protected static bool ContainsOption(MazePuzzleData data, string key) {
        return data?.optionKeys != null && !string.IsNullOrEmpty(key) && data.optionKeys.Contains(key);
    }

    protected static string ResolveOptionLabel(MazePuzzleData data, int index, string key) {
        if (data?.optionLabels != null && index >= 0 && index < data.optionLabels.Count && !string.IsNullOrEmpty(data.optionLabels[index]))
            return data.optionLabels[index];

        switch (key) {
            case "profit": return "利益";
            case "fame": return "名望";
            case "power": return "权力";
            case "trust": return "信义";
            case "fear": return "恐惧";
            case "gold": return "金钱";
            case "silence": return "沉默";
            case "escape": return "逃离";
            case "clear": return "清空";
            default: return key;
        }
    }

    private void OnMazeRunStarted(MazeRunStartedEvent evt) {
        ResetState();
    }
}

public sealed class ItemSocketPuzzleSystem : MazePuzzleSystemBase {
    public const string TypeId = "item_socket";
    // 北斗七星槽位数。改这里要同步改 ItemSocketPuzzleUIController.SlotCount 与 prefab 的 SlotButton_* 数量。
    public const int SlotCount = 7;
    private ItemSocketPuzzleStateComponent state;

    public override string PuzzleType => TypeId;

    public ItemSocketPuzzleSystem(GameDatabase database, PlayerDatabase player, EventBus eventBus)
        : base(database, player, eventBus) {
    }

    /// <summary>把道具放进指定槽位。slotIndex 为 -1 时放进第一个空槽（旧的"选中即摆放"语义）。</summary>
    public CommandResult SelectItem(string puzzleId, string itemKey, int slotIndex, MazeRunComponent run) {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        if (!ContainsOption(data, itemKey))
            return Fail("Item is not a puzzle option.", puzzleId, run);
        if (!HasItem(run, itemKey))
            return Fail("Item has not been collected.", puzzleId, run);

        int target = slotIndex >= 0 ? slotIndex : FindFirstEmptySlot();
        if (target < 0)
            return Fail("All sockets are filled.", puzzleId, run);
        if (target >= SlotCount)
            return Fail("Socket index is out of range.", puzzleId, run);

        // 同一件道具只能占一个槽：换位摆放时先把旧槽腾空，避免一件道具铺满七槽。
        int previous = state.slotItemKeys.IndexOf(itemKey);
        if (previous >= 0)
            state.slotItemKeys[previous] = string.Empty;

        state.slotItemKeys[target] = itemKey;
        state.feedback = "Item placed on the altar.";
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Succeeded(state.feedback, vm);
    }

    /// <summary>清空指定槽位。slotIndex 为 -1 时清空全部槽位。</summary>
    public CommandResult RemoveItem(string puzzleId, int slotIndex, MazeRunComponent run) {
        EnsurePuzzle(puzzleId);
        if (slotIndex >= SlotCount)
            return Fail("Socket index is out of range.", puzzleId, run);

        if (slotIndex < 0) {
            ClearSlots();
            state.feedback = "Altar cleared.";
        }
        else {
            state.slotItemKeys[slotIndex] = string.Empty;
            state.feedback = "Altar slot cleared.";
        }

        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Succeeded(state.feedback, vm);
    }

    public override MazePuzzleEvaluation Evaluate(string puzzleId, MazeRunComponent run) {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        if (data == null)
            return MazePuzzleEvaluation.Invalid("Puzzle data is not ready.");

        List<string> placed = PlacedKeys();
        if (placed.Count == 0)
            return MazePuzzleEvaluation.Invalid("Place an item on the altar first.");
        for (int i = 0; i < placed.Count; i++) {
            if (!HasItem(run, placed[i]))
                return MazePuzzleEvaluation.Invalid("The selected item is not available.");
        }

        // 答案支持两种写法：单道具（"correct_items"）沿用旧表；
        // 七槽有序摆放写成逗号分隔（"天枢,天璇,..."），按槽位顺序拼接后整体比对。
        List<string> expected = ParseAnswer(data.answer);
        if (expected.Count != placed.Count)
            return MazePuzzleEvaluation.Incorrect("The altar rejects the arrangement.");
        for (int i = 0; i < expected.Count; i++) {
            if (!string.Equals(expected[i], placed[i], StringComparison.Ordinal))
                return MazePuzzleEvaluation.Incorrect("The altar rejects the arrangement.");
        }
        return MazePuzzleEvaluation.Correct("The altar accepts the arrangement.");
    }

    public override MazePuzzleRoomViewModel GetViewModel(string puzzleId, MazeRunComponent run) {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        MazePuzzleRoomViewModel vm = CreateBaseViewModel(data);
        for (int i = 0; i < state.slotItemKeys.Count; i++)
            vm.slotItemKeys.Add(state.slotItemKeys[i]);
        // selectedInput 保留为"已摆放道具的顺序串"，供 SelectionText 与旧断言读取。
        vm.selectedInput = string.Join(",", PlacedKeys().ToArray());
        vm.feedback = state.feedback;
        vm.isSolved = state.solved;
        FillOptions(vm, data,
            key => state.slotItemKeys.Contains(key),
            key => HasItem(run, key),
            null);
        vm.canSubmit = !vm.isSolved && CanSubmit(run);
        return vm;
    }

    public override void ApplyResolution(string puzzleId, MazePuzzleEvaluation evaluation, MazeRunComponent run) {
        EnsurePuzzle(puzzleId);
        state.feedback = evaluation != null ? evaluation.message : string.Empty;
        state.solved = evaluation != null && evaluation.status == MazePuzzleEvaluationStatus.Correct;
        // 一次提交＝一次完整的献祭尝试，判错就把祭坛复位重摆（体力已在判定层扣过）。
        // Invalid 表示"还没摆够/道具不可用"，不算一次尝试，槽位保留。
        if (evaluation != null && evaluation.status == MazePuzzleEvaluationStatus.Incorrect)
            ClearSlots();
        Publish(GetViewModel(puzzleId, run));
    }

    protected override void CreateStateComponent() {
        state = new ItemSocketPuzzleStateComponent();
        world.AddComponent(stateEntity, state);
    }

    protected override void ResetState() {
        if (state == null)
            return;
        state.puzzleId = string.Empty;
        ClearSlots();
        state.feedback = string.Empty;
        state.solved = false;
    }

    private void ClearSlots() {
        // 槽位表始终保持 SlotCount 长度，下标即槽位号，别用 Clear() 缩短它。
        state.slotItemKeys.Clear();
        for (int i = 0; i < SlotCount; i++)
            state.slotItemKeys.Add(string.Empty);
    }

    private int FindFirstEmptySlot() {
        for (int i = 0; i < state.slotItemKeys.Count; i++) {
            if (string.IsNullOrEmpty(state.slotItemKeys[i]))
                return i;
        }
        return -1;
    }

    private List<string> PlacedKeys() {
        List<string> placed = new List<string>();
        for (int i = 0; i < state.slotItemKeys.Count; i++) {
            if (!string.IsNullOrEmpty(state.slotItemKeys[i]))
                placed.Add(state.slotItemKeys[i]);
        }
        return placed;
    }

    private bool CanSubmit(MazeRunComponent run) {
        List<string> placed = PlacedKeys();
        if (placed.Count == 0)
            return false;
        for (int i = 0; i < placed.Count; i++) {
            if (!HasItem(run, placed[i]))
                return false;
        }
        return true;
    }

    private static bool HasItem(MazeRunComponent run, string itemKey) {
        return run?.puzzleItems != null
            && !string.IsNullOrEmpty(itemKey)
            && run.puzzleItems.TryGetValue(itemKey, out int amount)
            && amount > 0;
    }

    private static List<string> ParseAnswer(string answer) {
        List<string> parsed = new List<string>();
        if (string.IsNullOrEmpty(answer))
            return parsed;
        string[] parts = answer.Split(',');
        for (int i = 0; i < parts.Length; i++) {
            string trimmed = parts[i].Trim();
            if (!string.IsNullOrEmpty(trimmed))
                parsed.Add(trimmed);
        }
        return parsed;
    }

    private void EnsurePuzzle(string puzzleId) {
        if (state.puzzleId == puzzleId && state.slotItemKeys.Count == SlotCount)
            return;
        ResetState();
        state.puzzleId = puzzleId;
    }

    private CommandResult Fail(string message, string puzzleId, MazeRunComponent run) {
        state.feedback = message;
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Failed(message, vm);
    }
}

public sealed class CandleNumberPuzzleSystem : MazePuzzleSystemBase {
    public const string TypeId = "numeric_input";
    private const int CandleCount = 9;
    private CandleNumberPuzzleStateComponent state;

    public override string PuzzleType => TypeId;

    public CandleNumberPuzzleSystem(GameDatabase database, PlayerDatabase player, EventBus eventBus)
        : base(database, player, eventBus) {
    }

    public CommandResult ToggleCandle(string puzzleId, int candleIndex, MazeRunComponent run) {
        EnsurePuzzle(puzzleId);
        if (candleIndex < 0 || candleIndex >= CandleCount)
            return Fail("Candle index is invalid.", puzzleId, run);
        state.candleStates[candleIndex] = !state.candleStates[candleIndex];
        state.feedback = state.candleStates[candleIndex] ? "Candle lit." : "Candle extinguished.";
        return Success(puzzleId, run);
    }

    public CommandResult OpenPool(string puzzleId, MazeRunComponent run) {
        EnsurePuzzle(puzzleId);
        state.poolOpened = true;
        state.feedback = "The pool reflects a numeric lock.";
        return Success(puzzleId, run);
    }

    public CommandResult SelectNumber(string puzzleId, string number, MazeRunComponent run) {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        if (!state.poolOpened)
            return Fail("Inspect the pool first.", puzzleId, run);
        if (!ContainsOption(data, number) || number == "clear")
            return Fail("Number is not a puzzle option.", puzzleId, run);
        state.selectedNumber = number;
        state.feedback = "Number selected: " + number;
        return Success(puzzleId, run);
    }

    public CommandResult ClearNumber(string puzzleId, MazeRunComponent run) {
        EnsurePuzzle(puzzleId);
        state.selectedNumber = string.Empty;
        state.feedback = "Number cleared.";
        return Success(puzzleId, run);
    }

    public override MazePuzzleEvaluation Evaluate(string puzzleId, MazeRunComponent run) {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        if (data == null)
            return MazePuzzleEvaluation.Invalid("Puzzle data is not ready.");
        if (!state.poolOpened)
            return MazePuzzleEvaluation.Invalid("Inspect the pool first.");
        if (string.IsNullOrEmpty(state.selectedNumber))
            return MazePuzzleEvaluation.Invalid("Choose a number first.");
        return string.Equals(data.answer, state.selectedNumber, StringComparison.Ordinal)
            ? MazePuzzleEvaluation.Correct("The pool surges and reveals a dragon shadow.")
            : MazePuzzleEvaluation.Incorrect("The pool becomes still. The answer is incorrect.");
    }

    public override MazePuzzleRoomViewModel GetViewModel(string puzzleId, MazeRunComponent run) {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        MazePuzzleRoomViewModel vm = CreateBaseViewModel(data);
        vm.selectedInput = state.selectedNumber;
        vm.feedback = state.feedback;
        vm.isSolved = state.solved;
        vm.poolOpened = state.poolOpened;
        vm.candleStates.AddRange(state.candleStates);
        FillOptions(vm, data, key => key == state.selectedNumber, null, null);
        vm.canSubmit = !vm.isSolved && state.poolOpened && !string.IsNullOrEmpty(state.selectedNumber);
        return vm;
    }

    public override void ApplyResolution(string puzzleId, MazePuzzleEvaluation evaluation, MazeRunComponent run) {
        EnsurePuzzle(puzzleId);
        state.feedback = evaluation != null ? evaluation.message : string.Empty;
        state.solved = evaluation != null && evaluation.status == MazePuzzleEvaluationStatus.Correct;
        Publish(GetViewModel(puzzleId, run));
    }

    protected override void CreateStateComponent() {
        state = new CandleNumberPuzzleStateComponent();
        world.AddComponent(stateEntity, state);
    }

    protected override void ResetState() {
        if (state == null)
            return;
        state.puzzleId = string.Empty;
        state.candleStates.Clear();
        for (int i = 0; i < CandleCount; i++)
            state.candleStates.Add(false);
        state.poolOpened = false;
        state.selectedNumber = string.Empty;
        state.feedback = string.Empty;
        state.solved = false;
    }

    private void EnsurePuzzle(string puzzleId) {
        if (state.puzzleId == puzzleId)
            return;
        ResetState();
        state.puzzleId = puzzleId;
    }

    private CommandResult Success(string puzzleId, MazeRunComponent run) {
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Succeeded(state.feedback, vm);
    }

    private CommandResult Fail(string message, string puzzleId, MazeRunComponent run) {
        state.feedback = message;
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Failed(message, vm);
    }
}

public sealed class FloorChoicePuzzleSystem : MazePuzzleSystemBase {
    public const string TypeId = "floor_choice";
    private FloorChoicePuzzleStateComponent state;

    public override string PuzzleType => TypeId;

    public FloorChoicePuzzleSystem(GameDatabase database, PlayerDatabase player, EventBus eventBus)
        : base(database, player, eventBus) {
    }

    public CommandResult SelectTile(string puzzleId, string tileKey, MazeRunComponent run) {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        if (!ContainsOption(data, tileKey))
            return Fail("Floor tile is not a puzzle option.", puzzleId, run);
        state.selectedTileKey = tileKey;
        state.feedback = "You step onto " + tileKey + ".";
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Succeeded(state.feedback, vm);
    }

    public override MazePuzzleEvaluation Evaluate(string puzzleId, MazeRunComponent run) {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        if (data == null)
            return MazePuzzleEvaluation.Invalid("Puzzle data is not ready.");
        if (string.IsNullOrEmpty(state.selectedTileKey))
            return MazePuzzleEvaluation.Invalid("Choose a floor tile first.");
        return string.Equals(data.answer, state.selectedTileKey, StringComparison.Ordinal)
            ? MazePuzzleEvaluation.Correct("The chosen tile shines white and the room brightens.")
            : MazePuzzleEvaluation.Incorrect("The chosen tile flashes red.");
    }

    public override MazePuzzleRoomViewModel GetViewModel(string puzzleId, MazeRunComponent run) {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        MazePuzzleRoomViewModel vm = CreateBaseViewModel(data);
        vm.selectedInput = state.selectedTileKey;
        vm.feedback = state.feedback;
        vm.isSolved = state.solved;
        FillOptions(vm, data,
            key => key == state.selectedTileKey,
            null,
            key => state.failedTileKeys.Contains(key));
        vm.canSubmit = false;
        return vm;
    }

    public override void ApplyResolution(string puzzleId, MazePuzzleEvaluation evaluation, MazeRunComponent run) {
        EnsurePuzzle(puzzleId);
        state.feedback = evaluation != null ? evaluation.message : string.Empty;
        if (evaluation != null && evaluation.status == MazePuzzleEvaluationStatus.Incorrect && !string.IsNullOrEmpty(state.selectedTileKey))
            state.failedTileKeys.Add(state.selectedTileKey);
        state.solved = evaluation != null && evaluation.status == MazePuzzleEvaluationStatus.Correct;
        Publish(GetViewModel(puzzleId, run));
    }

    protected override void CreateStateComponent() {
        state = new FloorChoicePuzzleStateComponent();
        world.AddComponent(stateEntity, state);
    }

    protected override void ResetState() {
        if (state == null)
            return;
        state.puzzleId = string.Empty;
        state.selectedTileKey = string.Empty;
        state.failedTileKeys.Clear();
        state.feedback = string.Empty;
        state.solved = false;
    }

    private void EnsurePuzzle(string puzzleId) {
        if (state.puzzleId == puzzleId)
            return;
        ResetState();
        state.puzzleId = puzzleId;
    }

    private CommandResult Fail(string message, string puzzleId, MazeRunComponent run) {
        state.feedback = message;
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Failed(message, vm);
    }
}

public sealed class RockWordPuzzleSystem : MazePuzzleSystemBase {
    public const string TypeId = "block_words";
    private const int RequiredBlockCount = 3;
    private RockWordPuzzleStateComponent state;

    public override string PuzzleType => TypeId;

    public RockWordPuzzleSystem(GameDatabase database, PlayerDatabase player, EventBus eventBus)
        : base(database, player, eventBus) {
    }

    public CommandResult ActivateLight(string puzzleId, MazeRunComponent run) {
        EnsurePuzzle(puzzleId);
        state.lightActivated = true;
        state.feedback = "The candle projects a beam across the seven words.";
        return Success(puzzleId, run);
    }

    public CommandResult ToggleWord(string puzzleId, string wordKey, MazeRunComponent run) {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        if (!state.lightActivated)
            return Fail("Activate the light first.", puzzleId, run);
        if (!ContainsOption(data, wordKey))
            return Fail("Word is not a puzzle option.", puzzleId, run);

        if (state.blockedWordKeys.Contains(wordKey))
            state.blockedWordKeys.Remove(wordKey);
        else {
            if (state.blockedWordKeys.Count >= RequiredBlockCount)
                return Fail("Only three words can be blocked.", puzzleId, run);
            state.blockedWordKeys.Add(wordKey);
        }

        state.feedback = state.blockedWordKeys.Count + "/" + RequiredBlockCount + " words blocked.";
        return Success(puzzleId, run);
    }

    public override MazePuzzleEvaluation Evaluate(string puzzleId, MazeRunComponent run) {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        if (data == null)
            return MazePuzzleEvaluation.Invalid("Puzzle data is not ready.");
        if (!state.lightActivated)
            return MazePuzzleEvaluation.Invalid("Activate the light first.");
        if (state.blockedWordKeys.Count != RequiredBlockCount)
            return MazePuzzleEvaluation.Invalid("Block exactly three words.");

        HashSet<string> expected = new HashSet<string>(
            (data.answer ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
            StringComparer.Ordinal);
        return expected.Count == RequiredBlockCount && expected.SetEquals(state.blockedWordKeys)
            ? MazePuzzleEvaluation.Correct("The remaining inscription reveals the hidden name.")
            : MazePuzzleEvaluation.Incorrect("The blocked words do not reveal the answer.");
    }

    public override MazePuzzleRoomViewModel GetViewModel(string puzzleId, MazeRunComponent run) {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        MazePuzzleRoomViewModel vm = CreateBaseViewModel(data);
        vm.feedback = state.feedback;
        vm.isSolved = state.solved;
        vm.lightActivated = state.lightActivated;
        List<string> selected = new List<string>();
        if (data?.optionKeys != null) {
            for (int i = 0; i < data.optionKeys.Count; i++) {
                if (state.blockedWordKeys.Contains(data.optionKeys[i]))
                    selected.Add(data.optionKeys[i]);
            }
        }
        vm.selectedInput = string.Join(",", selected);
        FillOptions(vm, data, key => state.blockedWordKeys.Contains(key), null, null);
        vm.canSubmit = !vm.isSolved && state.lightActivated && state.blockedWordKeys.Count == RequiredBlockCount;
        return vm;
    }

    public override void ApplyResolution(string puzzleId, MazePuzzleEvaluation evaluation, MazeRunComponent run) {
        EnsurePuzzle(puzzleId);
        state.feedback = evaluation != null ? evaluation.message : string.Empty;
        state.solved = evaluation != null && evaluation.status == MazePuzzleEvaluationStatus.Correct;
        Publish(GetViewModel(puzzleId, run));
    }

    protected override void CreateStateComponent() {
        state = new RockWordPuzzleStateComponent();
        world.AddComponent(stateEntity, state);
    }

    protected override void ResetState() {
        if (state == null)
            return;
        state.puzzleId = string.Empty;
        state.lightActivated = false;
        state.blockedWordKeys.Clear();
        state.feedback = string.Empty;
        state.solved = false;
    }

    private void EnsurePuzzle(string puzzleId) {
        if (state.puzzleId == puzzleId)
            return;
        ResetState();
        state.puzzleId = puzzleId;
    }

    private CommandResult Success(string puzzleId, MazeRunComponent run) {
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Succeeded(state.feedback, vm);
    }

    private CommandResult Fail(string message, string puzzleId, MazeRunComponent run) {
        state.feedback = message;
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Failed(message, vm);
    }
}
