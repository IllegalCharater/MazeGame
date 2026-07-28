public sealed class SelectItemSocketPuzzleItemCommand : ICommand
{
    public string puzzleId;
    public string itemKey;
    // 目标槽位；-1 表示放进第一个空槽。
    public int slotIndex;

    public SelectItemSocketPuzzleItemCommand(string puzzleId, string itemKey)
        : this(puzzleId, itemKey, -1)
    {
    }

    public SelectItemSocketPuzzleItemCommand(string puzzleId, string itemKey, int slotIndex)
    {
        this.puzzleId = puzzleId;
        this.itemKey = itemKey;
        this.slotIndex = slotIndex;
    }
}

public sealed class RemoveItemSocketPuzzleItemCommand : ICommand
{
    public string puzzleId;
    // 要清空的槽位；-1 表示清空全部。
    public int slotIndex;

    public RemoveItemSocketPuzzleItemCommand(string puzzleId)
        : this(puzzleId, -1)
    {
    }

    public RemoveItemSocketPuzzleItemCommand(string puzzleId, int slotIndex)
    {
        this.puzzleId = puzzleId;
        this.slotIndex = slotIndex;
    }
}

public sealed class ToggleCandlePuzzleCommand : ICommand
{
    public string puzzleId;
    public int candleIndex;

    public ToggleCandlePuzzleCommand(string puzzleId, int candleIndex)
    {
        this.puzzleId = puzzleId;
        this.candleIndex = candleIndex;
    }
}

public sealed class OpenCandlePuzzlePoolCommand : ICommand
{
    public string puzzleId;

    public OpenCandlePuzzlePoolCommand(string puzzleId)
    {
        this.puzzleId = puzzleId;
    }
}

public sealed class SelectCandlePuzzleNumberCommand : ICommand
{
    public string puzzleId;
    public string number;

    public SelectCandlePuzzleNumberCommand(string puzzleId, string number)
    {
        this.puzzleId = puzzleId;
        this.number = number;
    }
}

public sealed class ClearCandlePuzzleNumberCommand : ICommand
{
    public string puzzleId;

    public ClearCandlePuzzleNumberCommand(string puzzleId)
    {
        this.puzzleId = puzzleId;
    }
}

public sealed class ChooseFloorPuzzleTileCommand : ICommand
{
    public string puzzleId;
    public string tileKey;

    public ChooseFloorPuzzleTileCommand(string puzzleId, string tileKey)
    {
        this.puzzleId = puzzleId;
        this.tileKey = tileKey;
    }
}

public sealed class ActivateRockWordPuzzleLightCommand : ICommand
{
    public string puzzleId;

    public ActivateRockWordPuzzleLightCommand(string puzzleId)
    {
        this.puzzleId = puzzleId;
    }
}

public sealed class ToggleRockWordPuzzleBlockCommand : ICommand
{
    public string puzzleId;
    public string wordKey;

    public ToggleRockWordPuzzleBlockCommand(string puzzleId, string wordKey)
    {
        this.puzzleId = puzzleId;
        this.wordKey = wordKey;
    }
}

public sealed class SelectItemSocketPuzzleItemCommandHandler : MazeCommandHandlerBase, ICommandHandler<SelectItemSocketPuzzleItemCommand>
{
    public SelectItemSocketPuzzleItemCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(SelectItemSocketPuzzleItemCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.SelectItemSocketItem(command.puzzleId, command.itemKey, command.slotIndex)
            : result;
    }
}

public sealed class RemoveItemSocketPuzzleItemCommandHandler : MazeCommandHandlerBase, ICommandHandler<RemoveItemSocketPuzzleItemCommand>
{
    public RemoveItemSocketPuzzleItemCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(RemoveItemSocketPuzzleItemCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.RemoveItemSocketItem(command.puzzleId, command.slotIndex)
            : result;
    }
}

public sealed class ToggleCandlePuzzleCommandHandler : MazeCommandHandlerBase, ICommandHandler<ToggleCandlePuzzleCommand>
{
    public ToggleCandlePuzzleCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(ToggleCandlePuzzleCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.TogglePuzzleCandle(command.puzzleId, command.candleIndex)
            : result;
    }
}

public sealed class OpenCandlePuzzlePoolCommandHandler : MazeCommandHandlerBase, ICommandHandler<OpenCandlePuzzlePoolCommand>
{
    public OpenCandlePuzzlePoolCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(OpenCandlePuzzlePoolCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.OpenPuzzlePool(command.puzzleId)
            : result;
    }
}

public sealed class SelectCandlePuzzleNumberCommandHandler : MazeCommandHandlerBase, ICommandHandler<SelectCandlePuzzleNumberCommand>
{
    public SelectCandlePuzzleNumberCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(SelectCandlePuzzleNumberCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.SelectPuzzleNumber(command.puzzleId, command.number)
            : result;
    }
}

public sealed class ClearCandlePuzzleNumberCommandHandler : MazeCommandHandlerBase, ICommandHandler<ClearCandlePuzzleNumberCommand>
{
    public ClearCandlePuzzleNumberCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(ClearCandlePuzzleNumberCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.ClearPuzzleNumber(command.puzzleId)
            : result;
    }
}

public sealed class ChooseFloorPuzzleTileCommandHandler : MazeCommandHandlerBase, ICommandHandler<ChooseFloorPuzzleTileCommand>
{
    public ChooseFloorPuzzleTileCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(ChooseFloorPuzzleTileCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.ChoosePuzzleFloor(command.puzzleId, command.tileKey)
            : result;
    }
}

public sealed class ActivateRockWordPuzzleLightCommandHandler : MazeCommandHandlerBase, ICommandHandler<ActivateRockWordPuzzleLightCommand>
{
    public ActivateRockWordPuzzleLightCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(ActivateRockWordPuzzleLightCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.ActivateRockWordLight(command.puzzleId)
            : result;
    }
}

public sealed class ToggleRockWordPuzzleBlockCommandHandler : MazeCommandHandlerBase, ICommandHandler<ToggleRockWordPuzzleBlockCommand>
{
    public ToggleRockWordPuzzleBlockCommandHandler(MazeService maze) : base(maze)
    {
    }

    public CommandResult Handle(ToggleRockWordPuzzleBlockCommand command)
    {
        return IsReady(out CommandResult result)
            ? maze.ToggleRockWordBlock(command.puzzleId, command.wordKey)
            : result;
    }
}
