using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface ICommand {
    CommandType Type { get; }
    Task<CommandResult> Handle(DataBag payload = null);
}

public sealed class CommandBus {
    private readonly Dictionary<CommandType, ICommand> handlers = new();

    public void Register<TCommand>(TCommand handler) where TCommand : ICommand {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        if (HasHandler(handler.Type)) {
            Debug.LogWarning(handler.Type + " command has registered");
            return;
        }
        handlers[handler.Type] = handler;
    }
    public void UnRegister(CommandType commandType) {
        handlers.Remove(commandType); // Remove 在键不存在时不会抛出异常
    }

    public Task<CommandResult> Execute(CommandType commandType, DataBag payload = null) {
        if (!handlers.TryGetValue(commandType, out ICommand command) || command == null)
            return Task.FromResult(CommandResult.Failed("No command handler registered for " + commandType + "."));

        return execute(command, payload);
    }
    private Task<CommandResult> execute(ICommand command, DataBag payload) {
        try {
            return command.Handle(payload) ?? Task.FromResult(CommandResult.Failed("Command handler returned null."));
        }
        catch (Exception ex) {
            return Task.FromResult(CommandResult.Failed(ex.Message));
        }
    }

    public bool HasHandler(CommandType commandType) {
        return handlers.ContainsKey(commandType);
    }

    public bool TryGetHandler(CommandType commandType, out ICommand handler) {
        return handlers.TryGetValue(commandType, out handler);
    }

    public void Clear() {
        handlers.Clear();
    }
}
