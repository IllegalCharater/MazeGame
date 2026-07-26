using System.Threading.Tasks;

public sealed class OpenViewCommand : ICommand
{
    public string viewName;
    public string layerName;

    public OpenViewCommand(string viewName, string layerName = null)
    {
        this.viewName = viewName;
        this.layerName = layerName;
    }
}

public sealed class ChangeSceneCommand : ICommand
{
    public GameState targetState;

    public ChangeSceneCommand(GameState targetState)
    {
        this.targetState = targetState;
    }
}

public sealed class OpenViewCommandHandler : IAsyncCommandHandler<OpenViewCommand>
{
    public async Task<CommandResult> HandleAsync(OpenViewCommand command)
    {
        if (string.IsNullOrEmpty(command.viewName))
            return CommandResult.Failed("View name is empty.");

        BaseUIController controller = await UIManager.GotoView(command.viewName, command.layerName);
        return controller != null
            ? CommandResult.Succeeded("View opened.", controller)
            : CommandResult.Failed("View open failed: " + command.viewName);
    }
}

public sealed class ChangeSceneCommandHandler : ICommandHandler<ChangeSceneCommand>
{
    public CommandResult Handle(ChangeSceneCommand command)
    {
        if (SceneFlowManager.Instance == null)
            return CommandResult.Failed("SceneFlowManager is not ready.");

        switch (command.targetState)
        {
            case GameState.MainMenu:
                SceneFlowManager.Instance.GoToMain();
                break;
            case GameState.InMaze:
                SceneFlowManager.Instance.GoToMaze();
                break;
            default:
                SceneFlowManager.Instance.LoadScene(command.targetState);
                break;
        }

        return CommandResult.Succeeded("Scene change requested.");
    }
}
