using System;

public interface IMazeView
{
    void Render(MazeViewModel viewModel);
    void ShowNodeActions(MazeViewModel viewModel);
    void HideNodeActions();
    void ShowExitPuzzle(MazeViewModel viewModel);
    void HideExitPuzzle();
    void ShowMessage(string message);
    void ShowResult(MazeRunResult result);
}

public sealed class MazeMediator : IDisposable
{
    private readonly IMazeView view;
    private readonly MazeService maze;
    private readonly CommandBus commands;
    private readonly EventBus events;
    private MazeViewModel currentViewModel;
    private bool disposed;

    public MazeViewModel CurrentViewModel => currentViewModel;

    public MazeMediator(IMazeView view, MazeService maze, CommandBus commands, EventBus events)
    {
        this.view = view;
        this.maze = maze;
        this.commands = commands;
        this.events = events;
    }

    public void Initialize()
    {
        events?.Subscribe<MazeRunStartedEvent>(OnMazeRunStarted);
        events?.Subscribe<MazeRunUpdatedEvent>(OnMazeRunUpdated);
        GameEvents.OnMazeRunEndedWithResult += OnMazeRunEnded;

        Refresh();
        EnsureRunStarted();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        events?.Unsubscribe<MazeRunStartedEvent>(OnMazeRunStarted);
        events?.Unsubscribe<MazeRunUpdatedEvent>(OnMazeRunUpdated);
        GameEvents.OnMazeRunEndedWithResult -= OnMazeRunEnded;
    }

    public void Refresh()
    {
        currentViewModel = maze != null ? maze.GetViewModel() : CreateMissingServiceViewModel();
        view?.Render(currentViewModel);
        if (currentViewModel.result != null)
            view?.ShowResult(currentViewModel.result);
    }

    public void EnsureRunStarted()
    {
        if (commands == null)
        {
            view?.ShowMessage("Command bus is not ready.");
            return;
        }

        MazeViewModel vm = maze != null ? maze.GetViewModel() : null;
        if (vm != null && vm.hasRun && !vm.isEnded)
        {
            ApplyViewModel(vm);
            return;
        }

        ApplyResult(commands.Execute(new StartMazeRunCommand(null)));
    }

    public void OnNodeClicked(int index)
    {
        if (maze == null)
        {
            view?.ShowMessage("Maze service is not ready.");
            return;
        }

        if (!maze.TryGetNodeIdByIndex(index, out string nodeId))
        {
            view?.ShowMessage("Maze node does not exist.");
            return;
        }

        MazeViewModel vm = currentViewModel ?? maze.GetViewModel();
        if (vm == null || !vm.hasRun)
        {
            ApplyResult(commands?.Execute(new StartMazeRunCommand(null)));
            return;
        }

        if (vm.currentNodeId == nodeId)
        {
            // 已经站在出口上（MazeNode_16 / node_16）：再点一次就是"点击出口"，
            // 够四张拼图弹完整拼图窗口，不够则只回提示。
            if (IsExitNode(vm, nodeId))
                OpenExitPuzzle();
            else
                view?.ShowNodeActions(vm);
            return;
        }

        if (!vm.reachableNodeIds.Contains(nodeId))
        {
            view?.ShowMessage("Node is locked or not connected.");
            return;
        }

        CommandResult move = commands?.Execute(new MoveToMazeNodeCommand(nodeId));
        ApplyResult(move);
        // 首次走进出口也算"点击出口"，直接把出口流程带出来。
        if (move != null && move.success && IsExitNode(currentViewModel, nodeId))
            OpenExitPuzzle();
    }

    // 出口由配置里的 nodeType 决定，不依赖按钮序号，改表换出口时 UI 无需跟着改。
    private static bool IsExitNode(MazeViewModel vm, string nodeId)
    {
        return vm != null
            && !string.IsNullOrEmpty(nodeId)
            && vm.nodeTypes.TryGetValue(nodeId, out string nodeType)
            && nodeType == MazeNodeTypes.Exit;
    }

    public void CollectReward()
    {
        ApplyResult(commands?.Execute(new CollectMazeNodeRewardCommand(CurrentNodeId())));
    }

    public void ActivateSwitch()
    {
        ApplyResult(commands?.Execute(new ActivateMazeSwitchCommand(CurrentNodeId())));
    }

    public void OpenCurrentPuzzle()
    {
        MazeViewModel vm = currentViewModel ?? maze?.GetViewModel();
        if (vm == null || string.IsNullOrEmpty(vm.puzzleId))
        {
            view?.ShowMessage("Current node has no puzzle.");
            return;
        }

        string viewName = ResolvePuzzleViewName(vm.puzzleType);
        if (string.IsNullOrEmpty(viewName))
        {
            view?.ShowMessage("Puzzle type is not supported: " + vm.puzzleType);
            return;
        }
        if (commands == null)
        {
            view?.ShowMessage("Command bus is not ready.");
            return;
        }

        _ = commands.ExecuteAsync(new OpenViewCommand(viewName, UIConfig.PopupLayerName));
    }

    public void UseRecommendedFood()
    {
        MazeViewModel vm = currentViewModel ?? maze?.GetViewModel();
        if (vm == null || string.IsNullOrEmpty(vm.recommendedFoodId))
        {
            view?.ShowMessage("No usable food in backpack.");
            return;
        }

        ApplyResult(commands?.Execute(new UseMazeFoodCommand(vm.recommendedFoodId)));
    }

    public void StartTrap()
    {
        MazeViewModel vm = currentViewModel ?? maze?.GetViewModel();
        if (vm == null || string.IsNullOrEmpty(vm.trapId))
        {
            view?.ShowMessage("Current node has no trap.");
            return;
        }

        ApplyResult(commands?.Execute(new StartMazeTrapCommand(vm.trapId)));
    }

    public void ResolveTrapAsSuccess()
    {
        MazeViewModel vm = currentViewModel ?? maze?.GetViewModel();
        if (vm == null || string.IsNullOrEmpty(vm.trapId))
        {
            view?.ShowMessage("Current node has no trap.");
            return;
        }

        ApplyResult(commands?.Execute(new ResolveMazeTrapCommand(vm.trapId, true)));
    }

    public void ResolveTrapAsFailure()
    {
        MazeViewModel vm = currentViewModel ?? maze?.GetViewModel();
        if (vm == null || string.IsNullOrEmpty(vm.trapId))
        {
            view?.ShowMessage("Current node has no trap.");
            return;
        }

        ApplyResult(commands?.Execute(new ResolveMazeTrapCommand(vm.trapId, false)));
    }

    public void Evacuate()
    {
        ApplyResult(commands?.Execute(new EvacuateMazeRunCommand()));
    }

    // 出口条件（集满四张拼图）由 MazeSystem 判定：成功才弹出完整拼图窗口，
    // 失败时 ApplyResult 已经把"还差几张"的提示推给了视图。
    public void OpenExitPuzzle()
    {
        CommandResult result = commands?.Execute(new OpenMazeExitPuzzleCommand());
        ApplyResult(result);
        if (result != null && result.success)
            view?.ShowExitPuzzle(currentViewModel);
    }

    public void AssembleExitPuzzle()
    {
        ApplyResult(commands?.Execute(new AssembleMazePuzzleCommand()));
    }

    public void LeaveWithoutPerfect()
    {
        ApplyResult(commands?.Execute(new LeaveMazeWithoutPerfectCommand()));
    }

    public void Finish()
    {
        LeaveWithoutPerfect();
    }

    public void ReturnToShop()
    {
        ApplyResult(commands?.Execute(new ChangeSceneCommand(GameState.MainMenu)));
        if (commands != null)
            _ = commands.ExecuteAsync(new OpenViewCommand("ShopUI"));
    }

    private string CurrentNodeId()
    {
        MazeViewModel vm = currentViewModel ?? maze?.GetViewModel();
        return vm != null ? vm.currentNodeId : null;
    }

    private void OnMazeRunStarted(MazeRunStartedEvent evt)
    {
        ApplyViewModel(evt.viewModel);
    }

    private void OnMazeRunUpdated(MazeRunUpdatedEvent evt)
    {
        ApplyViewModel(evt.viewModel);
    }

    private void OnMazeRunEnded(MazeRunResult result)
    {
        view?.HideExitPuzzle();
        Refresh();
        if (result != null)
            view?.ShowResult(result);
    }

    private void ApplyResult(CommandResult result)
    {
        if (result == null)
        {
            view?.ShowMessage("Command bus is not ready.");
            return;
        }

        if (result.payload is MazeViewModel vm)
            ApplyViewModel(vm);
        else
            Refresh();

        if (result.payload is MazeRunResult runResult)
            view?.ShowResult(runResult);

        if (!string.IsNullOrEmpty(result.message))
            view?.ShowMessage(result.message);
    }

    private void ApplyViewModel(MazeViewModel viewModel)
    {
        currentViewModel = viewModel ?? CreateMissingServiceViewModel();
        view?.Render(currentViewModel);
        if (currentViewModel.result != null)
            view?.ShowResult(currentViewModel.result);
    }

    private static MazeViewModel CreateMissingServiceViewModel()
    {
        return new MazeViewModel
        {
            hasRun = false,
            state = MazeRunState.NotStarted,
            message = "Maze service is not ready."
        };
    }

    private static string ResolvePuzzleViewName(string puzzleType)
    {
        switch (puzzleType)
        {
            case ItemSocketPuzzleSystem.TypeId:
                return "MazePuzzleItemSocketUI";
            case CandleNumberPuzzleSystem.TypeId:
                return "MazePuzzleCandleNumberUI";
            case FloorChoicePuzzleSystem.TypeId:
                return "MazePuzzleFloorChoiceUI";
            case RockWordPuzzleSystem.TypeId:
                return "MazePuzzleRockWordUI";
            default:
                return string.Empty;
        }
    }
}
