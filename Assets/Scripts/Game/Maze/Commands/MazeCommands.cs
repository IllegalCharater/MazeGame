public sealed class StartMazeRunCommand : ICommand
{
    public string playerId;
    public string ruleId;
    public string startNodeId;

    public StartMazeRunCommand(string playerId, string ruleId = "default", string startNodeId = "node_01")
    {
        this.playerId = playerId;
        this.ruleId = ruleId;
        this.startNodeId = startNodeId;
    }
}

public sealed class MoveToMazeNodeCommand : ICommand
{
    public string nodeId;

    public MoveToMazeNodeCommand(string nodeId)
    {
        this.nodeId = nodeId;
    }
}

public sealed class CollectMazeNodeRewardCommand : ICommand
{
    public string nodeId;

    public CollectMazeNodeRewardCommand(string nodeId = null)
    {
        this.nodeId = nodeId;
    }
}

public sealed class ActivateMazeSwitchCommand : ICommand
{
    public string nodeId;

    public ActivateMazeSwitchCommand(string nodeId = null)
    {
        this.nodeId = nodeId;
    }
}

public sealed class SubmitMazePuzzleCommand : ICommand
{
    public string puzzleId;

    public SubmitMazePuzzleCommand(string puzzleId)
    {
        this.puzzleId = puzzleId;
    }
}

public sealed class ResolveMazeTrapCommand : ICommand
{
    public string trapId;
    public bool succeeded;

    public ResolveMazeTrapCommand(string trapId, bool succeeded)
    {
        this.trapId = trapId;
        this.succeeded = succeeded;
    }
}

public sealed class EvacuateMazeRunCommand : ICommand
{
}

public sealed class FinishMazeRunCommand : ICommand
{
}

public abstract class MazeCommandHandlerBase
{
    protected readonly MazeService maze;

    protected MazeCommandHandlerBase(MazeService maze)
    {
        this.maze = maze;
    }

    protected bool IsReady(out CommandResult result)
    {
        if (maze == null || !maze.IsInitialized)
        {
            result = CommandResult.Failed("Maze service is not ready.");
            return false;
        }

        result = null;
        return true;
    }
}

public sealed class StartMazeRunCommandHandler : MazeCommandHandlerBase, ICommandHandler<StartMazeRunCommand>
{
    public StartMazeRunCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(StartMazeRunCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.StartRun(command.playerId, command.ruleId, command.startNodeId)
            : result;
    }
}

public sealed class MoveToMazeNodeCommandHandler : MazeCommandHandlerBase, ICommandHandler<MoveToMazeNodeCommand>
{
    public MoveToMazeNodeCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(MoveToMazeNodeCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.MoveToNode(command.nodeId)
            : result;
    }
}

public sealed class CollectMazeNodeRewardCommandHandler : MazeCommandHandlerBase, ICommandHandler<CollectMazeNodeRewardCommand>
{
    public CollectMazeNodeRewardCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(CollectMazeNodeRewardCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.CollectNodeReward(command.nodeId)
            : result;
    }
}

public sealed class ActivateMazeSwitchCommandHandler : MazeCommandHandlerBase, ICommandHandler<ActivateMazeSwitchCommand>
{
    public ActivateMazeSwitchCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(ActivateMazeSwitchCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.ActivateSwitch(command.nodeId)
            : result;
    }
}

public sealed class SubmitMazePuzzleCommandHandler : MazeCommandHandlerBase, ICommandHandler<SubmitMazePuzzleCommand>
{
    public SubmitMazePuzzleCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(SubmitMazePuzzleCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.SubmitPuzzle(command.puzzleId)
            : result;
    }
}

public sealed class ResolveMazeTrapCommandHandler : MazeCommandHandlerBase, ICommandHandler<ResolveMazeTrapCommand>
{
    public ResolveMazeTrapCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(ResolveMazeTrapCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.ResolveTrap(command.trapId, command.succeeded)
            : result;
    }
}

public sealed class EvacuateMazeRunCommandHandler : MazeCommandHandlerBase, ICommandHandler<EvacuateMazeRunCommand>
{
    public EvacuateMazeRunCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(EvacuateMazeRunCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.EvacuateRun()
            : result;
    }
}

public sealed class FinishMazeRunCommandHandler : MazeCommandHandlerBase, ICommandHandler<FinishMazeRunCommand>
{
    public FinishMazeRunCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(FinishMazeRunCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.FinishRun()
            : result;
    }
}


public sealed class UseMazeFoodCommand : ICommand
{
    public string foodId;

    public UseMazeFoodCommand(string foodId)
    {
        this.foodId = foodId;
    }
}

public sealed class OpenMazeExitPuzzleCommand : ICommand
{
}

public sealed class AssembleMazePuzzleCommand : ICommand
{
}

public sealed class LeaveMazeWithoutPerfectCommand : ICommand
{
}

public sealed class StartMazeTrapCommand : ICommand
{
    public string trapId;

    public StartMazeTrapCommand(string trapId = null)
    {
        this.trapId = trapId;
    }
}

public sealed class UseMazeFoodCommandHandler : MazeCommandHandlerBase, ICommandHandler<UseMazeFoodCommand>
{
    public UseMazeFoodCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(UseMazeFoodCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.UseFood(command.foodId)
            : result;
    }
}

public sealed class OpenMazeExitPuzzleCommandHandler : MazeCommandHandlerBase, ICommandHandler<OpenMazeExitPuzzleCommand>
{
    public OpenMazeExitPuzzleCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(OpenMazeExitPuzzleCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.OpenExitPuzzle()
            : result;
    }
}

public sealed class AssembleMazePuzzleCommandHandler : MazeCommandHandlerBase, ICommandHandler<AssembleMazePuzzleCommand>
{
    public AssembleMazePuzzleCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(AssembleMazePuzzleCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.AssembleExitPuzzle()
            : result;
    }
}

public sealed class LeaveMazeWithoutPerfectCommandHandler : MazeCommandHandlerBase, ICommandHandler<LeaveMazeWithoutPerfectCommand>
{
    public LeaveMazeWithoutPerfectCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(LeaveMazeWithoutPerfectCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.LeaveWithoutPerfect()
            : result;
    }
}

public sealed class StartMazeTrapCommandHandler : MazeCommandHandlerBase, ICommandHandler<StartMazeTrapCommand>
{
    public StartMazeTrapCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(StartMazeTrapCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.StartTrap(command.trapId)
            : result;
    }
}
