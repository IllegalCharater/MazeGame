public sealed class MazeService
{
    private GameDatabase database;
    private PlayerDatabase player;
    private FrameworkContext framework;
    private EcsWorld world;
    private MazeSystem mazeSystem;
    private ItemSocketPuzzleSystem itemSocketPuzzleSystem;
    private CandleNumberPuzzleSystem candleNumberPuzzleSystem;
    private FloorChoicePuzzleSystem floorChoicePuzzleSystem;
    private RockWordPuzzleSystem rockWordPuzzleSystem;
    private readonly System.Collections.Generic.Dictionary<string, IMazePuzzleSystem> puzzleSystems =
        new System.Collections.Generic.Dictionary<string, IMazePuzzleSystem>();

    public bool IsInitialized => mazeSystem != null && world != null;
    public MazeSystem GameplaySystem => mazeSystem;
    public System.Collections.Generic.IReadOnlyDictionary<string, IMazePuzzleSystem> PuzzleSystems => puzzleSystems;
    public EcsWorld World => world;
    public CommandBus Commands => framework != null ? framework.Commands : null;
    public EventBus Events => framework != null ? framework.Events : null;

    public void Initialize(GameDatabase database, PlayerDatabase player, FrameworkContext framework, EcsWorld world)
    {
        this.database = database;
        this.player = player;
        this.framework = framework;
        this.world = world;

        mazeSystem = new MazeSystem(database, player, framework != null ? framework.Events : null);
        itemSocketPuzzleSystem = new ItemSocketPuzzleSystem(database, player, Events);
        candleNumberPuzzleSystem = new CandleNumberPuzzleSystem(database, player, Events);
        floorChoicePuzzleSystem = new FloorChoicePuzzleSystem(database, player, Events);
        rockWordPuzzleSystem = new RockWordPuzzleSystem(database, player, Events);

        puzzleSystems.Clear();
        puzzleSystems[itemSocketPuzzleSystem.PuzzleType] = itemSocketPuzzleSystem;
        puzzleSystems[candleNumberPuzzleSystem.PuzzleType] = candleNumberPuzzleSystem;
        puzzleSystems[floorChoicePuzzleSystem.PuzzleType] = floorChoicePuzzleSystem;
        puzzleSystems[rockWordPuzzleSystem.PuzzleType] = rockWordPuzzleSystem;

        world?.RegisterSystem(mazeSystem);
        world?.RegisterSystem(itemSocketPuzzleSystem);
        world?.RegisterSystem(candleNumberPuzzleSystem);
        world?.RegisterSystem(floorChoicePuzzleSystem);
        world?.RegisterSystem(rockWordPuzzleSystem);
        RegisterCommands();
    }

    public MazeViewModel GetViewModel()
    {
        if (mazeSystem != null)
            return mazeSystem.GetViewModel();

        return new MazeViewModel
        {
            hasRun = false,
            state = MazeRunState.NotStarted,
            message = "Maze service is not ready."
        };
    }

    public CommandResult EnsureRunStarted(string ruleId = "default", string startNodeId = "node_01")
    {
        MazeViewModel vm = GetViewModel();
        if (vm.hasRun && !vm.isEnded)
            return CommandResult.Succeeded("Maze run already active.", vm);

        return StartRun(null, ruleId, startNodeId);
    }

    public CommandResult StartRun(string playerId = null, string ruleId = "default", string startNodeId = "node_01")
    {
        if (!IsInitialized)
            return CommandResult.Failed("Maze service is not ready.", GetViewModel());

        if (string.IsNullOrEmpty(playerId))
            playerId = player != null ? player.playerId : null;

        return mazeSystem.StartRun(playerId, ruleId, startNodeId);
    }

    public CommandResult MoveToNode(string nodeId)
    {
        return IsInitialized ? mazeSystem.MoveToNode(nodeId) : NotReady();
    }

    public CommandResult MoveToNodeIndex(int index)
    {
        if (!TryGetNodeIdByIndex(index, out string nodeId))
            return CommandResult.Failed("Maze node index not found: " + index, GetViewModel());

        return MoveToNode(nodeId);
    }

    public CommandResult CollectNodeReward(string nodeId = null)
    {
        return IsInitialized ? mazeSystem.CollectNodeReward(nodeId) : NotReady();
    }

    public CommandResult CollectCurrentNodeReward()
    {
        return CollectNodeReward(null);
    }

    public CommandResult ActivateSwitch(string nodeId = null)
    {
        return IsInitialized ? mazeSystem.ActivateSwitch(nodeId) : NotReady();
    }

    public CommandResult ActivateCurrentSwitch()
    {
        return ActivateSwitch(null);
    }

    public MazePuzzleRoomViewModel GetCurrentPuzzleViewModel()
    {
        if (!TryGetPuzzleContext(null, null, out MazePuzzleData puzzle, out MazeRunComponent run, out IMazePuzzleSystem system, out _))
            return null;

        return system.GetViewModel(puzzle.puzzleId, run);
    }

    public CommandResult SubmitPuzzle(string puzzleId)
    {
        if (!TryGetPuzzleContext(puzzleId, null, out MazePuzzleData puzzle, out MazeRunComponent run, out IMazePuzzleSystem system, out string reason))
            return PuzzleFailure(reason);

        MazePuzzleEvaluation evaluation = system.Evaluate(puzzle.puzzleId, run);
        if (evaluation == null || evaluation.status == MazePuzzleEvaluationStatus.Invalid)
        {
            MazePuzzleEvaluation invalid = evaluation ?? MazePuzzleEvaluation.Invalid("Puzzle evaluation is invalid.");
            system.ApplyResolution(puzzle.puzzleId, invalid, run);
            return CommandResult.Failed(invalid.message, system.GetViewModel(puzzle.puzzleId, run));
        }

        CommandResult resolution = mazeSystem.ResolvePuzzleEvaluation(puzzle.puzzleId, evaluation);
        system.ApplyResolution(puzzle.puzzleId, evaluation, run);
        MazePuzzleRoomViewModel viewModel = system.GetViewModel(puzzle.puzzleId, run);
        return resolution.success
            ? CommandResult.Succeeded(resolution.message, viewModel)
            : CommandResult.Failed(resolution.message, viewModel);
    }

    public CommandResult SelectItemSocketItem(string puzzleId, string itemKey)
    {
        if (!TryGetPuzzleContext(puzzleId, ItemSocketPuzzleSystem.TypeId, out MazePuzzleData puzzle, out MazeRunComponent run, out _, out string reason))
            return PuzzleFailure(reason);

        return itemSocketPuzzleSystem.SelectItem(puzzle.puzzleId, itemKey, run);
    }

    public CommandResult RemoveItemSocketItem(string puzzleId)
    {
        if (!TryGetPuzzleContext(puzzleId, ItemSocketPuzzleSystem.TypeId, out MazePuzzleData puzzle, out MazeRunComponent run, out _, out string reason))
            return PuzzleFailure(reason);

        return itemSocketPuzzleSystem.RemoveItem(puzzle.puzzleId, run);
    }

    public CommandResult TogglePuzzleCandle(string puzzleId, int candleIndex)
    {
        if (!TryGetPuzzleContext(puzzleId, CandleNumberPuzzleSystem.TypeId, out MazePuzzleData puzzle, out MazeRunComponent run, out _, out string reason))
            return PuzzleFailure(reason);

        return candleNumberPuzzleSystem.ToggleCandle(puzzle.puzzleId, candleIndex, run);
    }

    public CommandResult OpenPuzzlePool(string puzzleId)
    {
        if (!TryGetPuzzleContext(puzzleId, CandleNumberPuzzleSystem.TypeId, out MazePuzzleData puzzle, out MazeRunComponent run, out _, out string reason))
            return PuzzleFailure(reason);

        return candleNumberPuzzleSystem.OpenPool(puzzle.puzzleId, run);
    }

    public CommandResult SelectPuzzleNumber(string puzzleId, string number)
    {
        if (!TryGetPuzzleContext(puzzleId, CandleNumberPuzzleSystem.TypeId, out MazePuzzleData puzzle, out MazeRunComponent run, out _, out string reason))
            return PuzzleFailure(reason);

        return candleNumberPuzzleSystem.SelectNumber(puzzle.puzzleId, number, run);
    }

    public CommandResult ClearPuzzleNumber(string puzzleId)
    {
        if (!TryGetPuzzleContext(puzzleId, CandleNumberPuzzleSystem.TypeId, out MazePuzzleData puzzle, out MazeRunComponent run, out _, out string reason))
            return PuzzleFailure(reason);

        return candleNumberPuzzleSystem.ClearNumber(puzzle.puzzleId, run);
    }

    public CommandResult ChoosePuzzleFloor(string puzzleId, string tileKey)
    {
        if (!TryGetPuzzleContext(puzzleId, FloorChoicePuzzleSystem.TypeId, out MazePuzzleData puzzle, out MazeRunComponent run, out _, out string reason))
            return PuzzleFailure(reason);

        CommandResult selection = floorChoicePuzzleSystem.SelectTile(puzzle.puzzleId, tileKey, run);
        return selection.success ? SubmitPuzzle(puzzle.puzzleId) : selection;
    }

    public CommandResult ActivateRockWordLight(string puzzleId)
    {
        if (!TryGetPuzzleContext(puzzleId, RockWordPuzzleSystem.TypeId, out MazePuzzleData puzzle, out MazeRunComponent run, out _, out string reason))
            return PuzzleFailure(reason);

        return rockWordPuzzleSystem.ActivateLight(puzzle.puzzleId, run);
    }

    public CommandResult ToggleRockWordBlock(string puzzleId, string wordKey)
    {
        if (!TryGetPuzzleContext(puzzleId, RockWordPuzzleSystem.TypeId, out MazePuzzleData puzzle, out MazeRunComponent run, out _, out string reason))
            return PuzzleFailure(reason);

        return rockWordPuzzleSystem.ToggleWord(puzzle.puzzleId, wordKey, run);
    }

    public CommandResult UseFood(string foodId)
    {
        return IsInitialized ? mazeSystem.UseFood(foodId) : NotReady();
    }

    public CommandResult StartTrap(string trapId = null)
    {
        return IsInitialized ? mazeSystem.StartTrap(trapId) : NotReady();
    }

    public CommandResult ResolveTrap(string trapId, bool succeeded)
    {
        return IsInitialized ? mazeSystem.ResolveTrap(trapId, succeeded) : NotReady();
    }

    public CommandResult OpenExitPuzzle()
    {
        return IsInitialized ? mazeSystem.OpenExitPuzzle() : NotReady();
    }

    public CommandResult AssembleExitPuzzle()
    {
        return IsInitialized ? mazeSystem.AssembleExitPuzzle() : NotReady();
    }

    public CommandResult LeaveWithoutPerfect()
    {
        return IsInitialized ? mazeSystem.LeaveWithoutPerfect() : NotReady();
    }

    public CommandResult EvacuateRun()
    {
        return IsInitialized ? mazeSystem.EvacuateRun() : NotReady();
    }

    public CommandResult FinishRun()
    {
        return IsInitialized ? mazeSystem.FinishRun() : NotReady();
    }

    public bool CanMoveToNode(string nodeId, out string reason)
    {
        if (!IsInitialized)
        {
            reason = "Maze service is not ready.";
            return false;
        }

        return mazeSystem.CanMoveToNode(nodeId, out reason);
    }

    public bool TryGetNodeIdByIndex(int index, out string nodeId)
    {
        if (IsInitialized)
            return mazeSystem.TryGetNodeIdByIndex(index, out nodeId);

        nodeId = string.Empty;
        return false;
    }

    private CommandResult NotReady()
    {
        return CommandResult.Failed("Maze service is not ready.", GetViewModel());
    }

    private void RegisterCommands()
    {
        CommandBus commands = Commands;
        if (commands == null)
            return;

        commands.Register(new StartMazeRunCommandHandler(this));
        commands.Register(new MoveToMazeNodeCommandHandler(this));
        commands.Register(new CollectMazeNodeRewardCommandHandler(this));
        commands.Register(new ActivateMazeSwitchCommandHandler(this));
        commands.Register(new SubmitMazePuzzleCommandHandler(this));
        commands.Register(new SelectItemSocketPuzzleItemCommandHandler(this));
        commands.Register(new RemoveItemSocketPuzzleItemCommandHandler(this));
        commands.Register(new ToggleCandlePuzzleCommandHandler(this));
        commands.Register(new OpenCandlePuzzlePoolCommandHandler(this));
        commands.Register(new SelectCandlePuzzleNumberCommandHandler(this));
        commands.Register(new ClearCandlePuzzleNumberCommandHandler(this));
        commands.Register(new ChooseFloorPuzzleTileCommandHandler(this));
        commands.Register(new ActivateRockWordPuzzleLightCommandHandler(this));
        commands.Register(new ToggleRockWordPuzzleBlockCommandHandler(this));
        commands.Register(new ResolveMazeTrapCommandHandler(this));
        commands.Register(new EvacuateMazeRunCommandHandler(this));
        commands.Register(new FinishMazeRunCommandHandler(this));
        commands.Register(new UseMazeFoodCommandHandler(this));
        commands.Register(new OpenMazeExitPuzzleCommandHandler(this));
        commands.Register(new AssembleMazePuzzleCommandHandler(this));
        commands.Register(new LeaveMazeWithoutPerfectCommandHandler(this));
        commands.Register(new StartMazeTrapCommandHandler(this));
    }

    private bool TryGetPuzzleContext(
        string puzzleId,
        string requiredType,
        out MazePuzzleData puzzle,
        out MazeRunComponent run,
        out IMazePuzzleSystem system,
        out string reason)
    {
        puzzle = null;
        run = null;
        system = null;
        reason = string.Empty;
        if (!IsInitialized)
        {
            reason = "Maze service is not ready.";
            return false;
        }
        if (!mazeSystem.TryGetActivePuzzle(puzzleId, out puzzle, out run, out reason))
            return false;
        if (!string.IsNullOrEmpty(requiredType) && puzzle.puzzleType != requiredType)
        {
            reason = "Puzzle action does not match the current puzzle type.";
            return false;
        }
        if (!puzzleSystems.TryGetValue(puzzle.puzzleType, out system) || system == null)
        {
            reason = "Puzzle system is not registered: " + puzzle.puzzleType;
            return false;
        }
        return true;
    }

    private CommandResult PuzzleFailure(string message)
    {
        return CommandResult.Failed(message, GetCurrentPuzzleViewModel());
    }
}
