using UnityEngine;
using System.Threading.Tasks;

public readonly partial struct CommandType {
    public static readonly CommandType AfterViewLoaded = Next();
}

public sealed class AfterViewCommand : ICommand {
    public CommandType Type => CommandType.AfterViewLoaded;

    public Task<CommandResult> Handle(DataBag payload = null) {
        if (payload == null) return Task.FromResult(CommandResult.Failed("payload is null"));
        string viewName = payload.Get<string>("ViewName", null);

        Debug.Log(viewName + "is loaded");

        return Task.FromResult(CommandResult.Succeeded("view loaded success"));
    }
}
