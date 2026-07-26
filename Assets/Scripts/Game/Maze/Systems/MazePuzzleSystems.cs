using System;
using System.Collections.Generic;

public interface IMazePuzzleSystem : ISystem
{
    string PuzzleType { get; }
    MazePuzzleRoomViewModel GetViewModel(string puzzleId, MazeRunComponent run);
    MazePuzzleEvaluation Evaluate(string puzzleId, MazeRunComponent run);
    void ApplyResolution(string puzzleId, MazePuzzleEvaluation evaluation, MazeRunComponent run);
}

public sealed class ItemSocketPuzzleStateComponent : IComponent
{
    public string puzzleId;
    public string selectedItemKey;
    public string feedback;
    public bool solved;
}

public sealed class CandleNumberPuzzleStateComponent : IComponent
{
    public string puzzleId;
    public List<bool> candleStates = new List<bool>();
    public bool poolOpened;
    public string selectedNumber;
    public string feedback;
    public bool solved;
}

public sealed class FloorChoicePuzzleStateComponent : IComponent
{
    public string puzzleId;
    public string selectedTileKey;
    public HashSet<string> failedTileKeys = new HashSet<string>();
    public string feedback;
    public bool solved;
}

public sealed class RockWordPuzzleStateComponent : IComponent
{
    public string puzzleId;
    public bool lightActivated;
    public HashSet<string> blockedWordKeys = new HashSet<string>();
    public string feedback;
    public bool solved;
}

public abstract class MazePuzzleSystemBase : IMazePuzzleSystem
{
    private readonly GameDatabase database;
    private readonly PlayerDatabase player;
    private readonly EventBus eventBus;

    protected EcsWorld world;
    protected EntityId stateEntity;

    public abstract string PuzzleType { get; }

    protected MazePuzzleSystemBase(GameDatabase database, PlayerDatabase player, EventBus eventBus)
    {
        this.database = database;
        this.player = player;
        this.eventBus = eventBus;
    }

    public void Initialize(EcsWorld world)
    {
        this.world = world;
        stateEntity = world.CreateEntity("MazePuzzleSystem:" + PuzzleType);
        CreateStateComponent();
        eventBus?.Subscribe<MazeRunStartedEvent>(OnMazeRunStarted);
        ResetState();
    }

    public void Tick(float deltaTime)
    {
    }

    public void Dispose()
    {
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

    protected MazePuzzleData GetPuzzle(string puzzleId)
    {
        if (string.IsNullOrEmpty(puzzleId) || database == null)
            return null;
        return database.Get<MazePuzzleData>("maze_puzzles", puzzleId);
    }

    protected MazePuzzleRoomViewModel CreateBaseViewModel(MazePuzzleData data)
    {
        MazePuzzleRoomViewModel vm = new MazePuzzleRoomViewModel
        {
            puzzleId = data != null ? data.puzzleId : string.Empty,
            puzzleType = data != null ? data.puzzleType : PuzzleType,
            hintText = data != null ? data.hintText : string.Empty,
            fragmentText = data != null ? data.fragmentText : string.Empty,
            currentEnergy = player?.profile != null ? player.profile.energy : 0,
            maxEnergy = player?.profile != null ? player.profile.maxEnergy : 0
        };
        if (data?.successRewardItems != null)
        {
            foreach (KeyValuePair<string, int> reward in data.successRewardItems)
                vm.successRewards[reward.Key] = reward.Value;
        }
        return vm;
    }

    protected void FillOptions(MazePuzzleRoomViewModel vm, MazePuzzleData data, Func<string, bool> selected, Func<string, bool> available, Func<string, bool> failed)
    {
        if (vm == null || data?.optionKeys == null)
            return;

        for (int i = 0; i < data.optionKeys.Count; i++)
        {
            string key = data.optionKeys[i];
            vm.options.Add(new MazePuzzleOptionViewModel
            {
                key = key,
                label = ResolveOptionLabel(data, i, key),
                selected = selected != null && selected(key),
                available = available == null || available(key),
                failed = failed != null && failed(key)
            });
        }
    }

    protected void Publish(MazePuzzleRoomViewModel viewModel)
    {
        eventBus?.Publish(new MazePuzzleStateChangedEvent(viewModel));
    }

    protected static bool ContainsOption(MazePuzzleData data, string key)
    {
        return data?.optionKeys != null && !string.IsNullOrEmpty(key) && data.optionKeys.Contains(key);
    }

    protected static string ResolveOptionLabel(MazePuzzleData data, int index, string key)
    {
        if (data?.optionLabels != null && index >= 0 && index < data.optionLabels.Count && !string.IsNullOrEmpty(data.optionLabels[index]))
            return data.optionLabels[index];

        switch (key)
        {
            case "correct_items": return "正确道具";
            case "wrong_items": return "干扰道具";
            case "old_coin": return "旧钱币";
            case "empty_bowl": return "空碗";
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

    private void OnMazeRunStarted(MazeRunStartedEvent evt)
    {
        ResetState();
    }
}

public sealed class ItemSocketPuzzleSystem : MazePuzzleSystemBase
{
    public const string TypeId = "item_socket";
    private ItemSocketPuzzleStateComponent state;

    public override string PuzzleType => TypeId;

    public ItemSocketPuzzleSystem(GameDatabase database, PlayerDatabase player, EventBus eventBus)
        : base(database, player, eventBus)
    {
    }

    public CommandResult SelectItem(string puzzleId, string itemKey, MazeRunComponent run)
    {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        if (!ContainsOption(data, itemKey))
            return Fail("Item is not a puzzle option.", puzzleId, run);
        if (run?.puzzleItems == null || !run.puzzleItems.TryGetValue(itemKey, out int amount) || amount <= 0)
            return Fail("Item has not been collected.", puzzleId, run);

        state.selectedItemKey = itemKey;
        state.feedback = "Item placed on the altar.";
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Succeeded(state.feedback, vm);
    }

    public CommandResult RemoveItem(string puzzleId, MazeRunComponent run)
    {
        EnsurePuzzle(puzzleId);
        state.selectedItemKey = string.Empty;
        state.feedback = "Altar slot cleared.";
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Succeeded(state.feedback, vm);
    }

    public override MazePuzzleEvaluation Evaluate(string puzzleId, MazeRunComponent run)
    {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        if (data == null)
            return MazePuzzleEvaluation.Invalid("Puzzle data is not ready.");
        if (string.IsNullOrEmpty(state.selectedItemKey))
            return MazePuzzleEvaluation.Invalid("Place an item on the altar first.");
        if (run?.puzzleItems == null || !run.puzzleItems.TryGetValue(state.selectedItemKey, out int amount) || amount <= 0)
            return MazePuzzleEvaluation.Invalid("The selected item is not available.");
        return string.Equals(data.answer, state.selectedItemKey, StringComparison.Ordinal)
            ? MazePuzzleEvaluation.Correct("The altar accepts the item.")
            : MazePuzzleEvaluation.Incorrect("The altar rejects the item.");
    }

    public override MazePuzzleRoomViewModel GetViewModel(string puzzleId, MazeRunComponent run)
    {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        MazePuzzleRoomViewModel vm = CreateBaseViewModel(data);
        vm.selectedInput = state.selectedItemKey;
        vm.feedback = state.feedback;
        vm.isSolved = state.solved;
        FillOptions(vm, data,
            key => key == state.selectedItemKey,
            key => run?.puzzleItems != null && run.puzzleItems.TryGetValue(key, out int amount) && amount > 0,
            null);
        vm.canSubmit = !vm.isSolved && !string.IsNullOrEmpty(state.selectedItemKey)
            && run?.puzzleItems != null && run.puzzleItems.TryGetValue(state.selectedItemKey, out int amount) && amount > 0;
        return vm;
    }

    public override void ApplyResolution(string puzzleId, MazePuzzleEvaluation evaluation, MazeRunComponent run)
    {
        EnsurePuzzle(puzzleId);
        state.feedback = evaluation != null ? evaluation.message : string.Empty;
        state.solved = evaluation != null && evaluation.status == MazePuzzleEvaluationStatus.Correct;
        Publish(GetViewModel(puzzleId, run));
    }

    protected override void CreateStateComponent()
    {
        state = new ItemSocketPuzzleStateComponent();
        world.AddComponent(stateEntity, state);
    }

    protected override void ResetState()
    {
        if (state == null)
            return;
        state.puzzleId = string.Empty;
        state.selectedItemKey = string.Empty;
        state.feedback = string.Empty;
        state.solved = false;
    }

    private void EnsurePuzzle(string puzzleId)
    {
        if (state.puzzleId == puzzleId)
            return;
        ResetState();
        state.puzzleId = puzzleId;
    }

    private CommandResult Fail(string message, string puzzleId, MazeRunComponent run)
    {
        state.feedback = message;
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Failed(message, vm);
    }
}

public sealed class CandleNumberPuzzleSystem : MazePuzzleSystemBase
{
    public const string TypeId = "numeric_input";
    private const int CandleCount = 9;
    private CandleNumberPuzzleStateComponent state;

    public override string PuzzleType => TypeId;

    public CandleNumberPuzzleSystem(GameDatabase database, PlayerDatabase player, EventBus eventBus)
        : base(database, player, eventBus)
    {
    }

    public CommandResult ToggleCandle(string puzzleId, int candleIndex, MazeRunComponent run)
    {
        EnsurePuzzle(puzzleId);
        if (candleIndex < 0 || candleIndex >= CandleCount)
            return Fail("Candle index is invalid.", puzzleId, run);
        state.candleStates[candleIndex] = !state.candleStates[candleIndex];
        state.feedback = state.candleStates[candleIndex] ? "Candle lit." : "Candle extinguished.";
        return Success(puzzleId, run);
    }

    public CommandResult OpenPool(string puzzleId, MazeRunComponent run)
    {
        EnsurePuzzle(puzzleId);
        state.poolOpened = true;
        state.feedback = "The pool reflects a numeric lock.";
        return Success(puzzleId, run);
    }

    public CommandResult SelectNumber(string puzzleId, string number, MazeRunComponent run)
    {
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

    public CommandResult ClearNumber(string puzzleId, MazeRunComponent run)
    {
        EnsurePuzzle(puzzleId);
        state.selectedNumber = string.Empty;
        state.feedback = "Number cleared.";
        return Success(puzzleId, run);
    }

    public override MazePuzzleEvaluation Evaluate(string puzzleId, MazeRunComponent run)
    {
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

    public override MazePuzzleRoomViewModel GetViewModel(string puzzleId, MazeRunComponent run)
    {
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

    public override void ApplyResolution(string puzzleId, MazePuzzleEvaluation evaluation, MazeRunComponent run)
    {
        EnsurePuzzle(puzzleId);
        state.feedback = evaluation != null ? evaluation.message : string.Empty;
        state.solved = evaluation != null && evaluation.status == MazePuzzleEvaluationStatus.Correct;
        Publish(GetViewModel(puzzleId, run));
    }

    protected override void CreateStateComponent()
    {
        state = new CandleNumberPuzzleStateComponent();
        world.AddComponent(stateEntity, state);
    }

    protected override void ResetState()
    {
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

    private void EnsurePuzzle(string puzzleId)
    {
        if (state.puzzleId == puzzleId)
            return;
        ResetState();
        state.puzzleId = puzzleId;
    }

    private CommandResult Success(string puzzleId, MazeRunComponent run)
    {
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Succeeded(state.feedback, vm);
    }

    private CommandResult Fail(string message, string puzzleId, MazeRunComponent run)
    {
        state.feedback = message;
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Failed(message, vm);
    }
}

public sealed class FloorChoicePuzzleSystem : MazePuzzleSystemBase
{
    public const string TypeId = "floor_choice";
    private FloorChoicePuzzleStateComponent state;

    public override string PuzzleType => TypeId;

    public FloorChoicePuzzleSystem(GameDatabase database, PlayerDatabase player, EventBus eventBus)
        : base(database, player, eventBus)
    {
    }

    public CommandResult SelectTile(string puzzleId, string tileKey, MazeRunComponent run)
    {
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

    public override MazePuzzleEvaluation Evaluate(string puzzleId, MazeRunComponent run)
    {
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

    public override MazePuzzleRoomViewModel GetViewModel(string puzzleId, MazeRunComponent run)
    {
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

    public override void ApplyResolution(string puzzleId, MazePuzzleEvaluation evaluation, MazeRunComponent run)
    {
        EnsurePuzzle(puzzleId);
        state.feedback = evaluation != null ? evaluation.message : string.Empty;
        if (evaluation != null && evaluation.status == MazePuzzleEvaluationStatus.Incorrect && !string.IsNullOrEmpty(state.selectedTileKey))
            state.failedTileKeys.Add(state.selectedTileKey);
        state.solved = evaluation != null && evaluation.status == MazePuzzleEvaluationStatus.Correct;
        Publish(GetViewModel(puzzleId, run));
    }

    protected override void CreateStateComponent()
    {
        state = new FloorChoicePuzzleStateComponent();
        world.AddComponent(stateEntity, state);
    }

    protected override void ResetState()
    {
        if (state == null)
            return;
        state.puzzleId = string.Empty;
        state.selectedTileKey = string.Empty;
        state.failedTileKeys.Clear();
        state.feedback = string.Empty;
        state.solved = false;
    }

    private void EnsurePuzzle(string puzzleId)
    {
        if (state.puzzleId == puzzleId)
            return;
        ResetState();
        state.puzzleId = puzzleId;
    }

    private CommandResult Fail(string message, string puzzleId, MazeRunComponent run)
    {
        state.feedback = message;
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Failed(message, vm);
    }
}

public sealed class RockWordPuzzleSystem : MazePuzzleSystemBase
{
    public const string TypeId = "block_words";
    private const int RequiredBlockCount = 3;
    private RockWordPuzzleStateComponent state;

    public override string PuzzleType => TypeId;

    public RockWordPuzzleSystem(GameDatabase database, PlayerDatabase player, EventBus eventBus)
        : base(database, player, eventBus)
    {
    }

    public CommandResult ActivateLight(string puzzleId, MazeRunComponent run)
    {
        EnsurePuzzle(puzzleId);
        state.lightActivated = true;
        state.feedback = "The candle projects a beam across the seven words.";
        return Success(puzzleId, run);
    }

    public CommandResult ToggleWord(string puzzleId, string wordKey, MazeRunComponent run)
    {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        if (!state.lightActivated)
            return Fail("Activate the light first.", puzzleId, run);
        if (!ContainsOption(data, wordKey))
            return Fail("Word is not a puzzle option.", puzzleId, run);

        if (state.blockedWordKeys.Contains(wordKey))
            state.blockedWordKeys.Remove(wordKey);
        else
        {
            if (state.blockedWordKeys.Count >= RequiredBlockCount)
                return Fail("Only three words can be blocked.", puzzleId, run);
            state.blockedWordKeys.Add(wordKey);
        }

        state.feedback = state.blockedWordKeys.Count + "/" + RequiredBlockCount + " words blocked.";
        return Success(puzzleId, run);
    }

    public override MazePuzzleEvaluation Evaluate(string puzzleId, MazeRunComponent run)
    {
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

    public override MazePuzzleRoomViewModel GetViewModel(string puzzleId, MazeRunComponent run)
    {
        MazePuzzleData data = GetPuzzle(puzzleId);
        EnsurePuzzle(puzzleId);
        MazePuzzleRoomViewModel vm = CreateBaseViewModel(data);
        vm.feedback = state.feedback;
        vm.isSolved = state.solved;
        vm.lightActivated = state.lightActivated;
        List<string> selected = new List<string>();
        if (data?.optionKeys != null)
        {
            for (int i = 0; i < data.optionKeys.Count; i++)
            {
                if (state.blockedWordKeys.Contains(data.optionKeys[i]))
                    selected.Add(data.optionKeys[i]);
            }
        }
        vm.selectedInput = string.Join(",", selected);
        FillOptions(vm, data, key => state.blockedWordKeys.Contains(key), null, null);
        vm.canSubmit = !vm.isSolved && state.lightActivated && state.blockedWordKeys.Count == RequiredBlockCount;
        return vm;
    }

    public override void ApplyResolution(string puzzleId, MazePuzzleEvaluation evaluation, MazeRunComponent run)
    {
        EnsurePuzzle(puzzleId);
        state.feedback = evaluation != null ? evaluation.message : string.Empty;
        state.solved = evaluation != null && evaluation.status == MazePuzzleEvaluationStatus.Correct;
        Publish(GetViewModel(puzzleId, run));
    }

    protected override void CreateStateComponent()
    {
        state = new RockWordPuzzleStateComponent();
        world.AddComponent(stateEntity, state);
    }

    protected override void ResetState()
    {
        if (state == null)
            return;
        state.puzzleId = string.Empty;
        state.lightActivated = false;
        state.blockedWordKeys.Clear();
        state.feedback = string.Empty;
        state.solved = false;
    }

    private void EnsurePuzzle(string puzzleId)
    {
        if (state.puzzleId == puzzleId)
            return;
        ResetState();
        state.puzzleId = puzzleId;
    }

    private CommandResult Success(string puzzleId, MazeRunComponent run)
    {
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Succeeded(state.feedback, vm);
    }

    private CommandResult Fail(string message, string puzzleId, MazeRunComponent run)
    {
        state.feedback = message;
        MazePuzzleRoomViewModel vm = GetViewModel(puzzleId, run);
        Publish(vm);
        return CommandResult.Failed(message, vm);
    }
}
