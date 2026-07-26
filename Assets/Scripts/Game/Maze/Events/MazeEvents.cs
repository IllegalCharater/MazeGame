public struct MazeRunStartedEvent
{
    public MazeViewModel viewModel;

    public MazeRunStartedEvent(MazeViewModel viewModel)
    {
        this.viewModel = viewModel;
    }
}

public struct MazeRunUpdatedEvent
{
    public MazeViewModel viewModel;

    public MazeRunUpdatedEvent(MazeViewModel viewModel)
    {
        this.viewModel = viewModel;
    }
}

public struct MazePuzzleStateChangedEvent
{
    public string puzzleId;
    public string puzzleType;
    public MazePuzzleRoomViewModel viewModel;

    public MazePuzzleStateChangedEvent(MazePuzzleRoomViewModel viewModel)
    {
        this.viewModel = viewModel;
        puzzleId = viewModel != null ? viewModel.puzzleId : string.Empty;
        puzzleType = viewModel != null ? viewModel.puzzleType : string.Empty;
    }
}
