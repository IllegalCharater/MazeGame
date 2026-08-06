using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
public interface ICommand {
    string Name { get; }
    Task<CommandResult> Handle(DataBag payload = null);
}
public sealed class CommandBus {
    private readonly Dictionary<string, ICommand> handlers = new();

    public void Register<TCommand>(TCommand handler, string commandName = null) where TCommand : ICommand {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        if (commandName == null) {
            commandName = handler.Name;
        }
        if (HasHandler(commandName)) {
            Debug.LogWarning(commandName + "commad has registered");
            return;
        }
        handlers[commandName] = handler;
    }
    public void UnRegister(string commandName) {
        if (!string.IsNullOrEmpty(commandName))
            handlers.Remove(commandName); // Remove 在键不存在时不会抛出异常
    }

    public Task<CommandResult> Execute(string commandName, DataBag payload = null) {
        if (string.IsNullOrEmpty(commandName) || !handlers.TryGetValue(commandName, out ICommand command) || command == null)
            return Task.FromResult(CommandResult.Failed("No command handler registered for " + commandName + "."));

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

    public bool HasHandler(string commandName) {
        return handlers.ContainsKey(commandName);
    }

    public bool TryGetHandler(string commandName, out ICommand handler) {
        return handlers.TryGetValue(commandName, out handler);
    }

    public void Clear() {
        handlers.Clear();
    }
}
