using System;
using System.Threading.Tasks;

public class Context : IDisposable {
    protected EventBus events { get; private set; }
    protected CommandBus commands { get; private set; }

    public Context(EventBus events, CommandBus commands) {
        this.events = events;
        this.commands = commands;
    }

    public void HandleEvent(string eventName, object[] args = null) {
        if (string.IsNullOrEmpty(eventName))
            return;
        events.Publish(eventName, args);
    }

    public Task<CommandResult> HandleCommand(string commandName, DataBag payload) {
        if (string.IsNullOrEmpty(commandName))
            return Task.FromResult(CommandResult.Failed("Command name is empty."));
        return commands.Execute(commandName, payload);
    }

    public void BindEvent(string eventName, Delegate action) {
        if (string.IsNullOrEmpty(eventName) || action == null)
            return;

        events.Subscribe(eventName, action);
    }
    public void UnbindEvent(string eventName, Delegate action) {
        if (string.IsNullOrEmpty(eventName) || action == null)
            return;

        events.Unsubscribe(eventName, action);
    }

    public void BindCommand(string commandName, ICommand command) {
        if (string.IsNullOrEmpty(commandName) || command == null)
            return;
        commands.Register(commandName, command);
    }

    public void UnbindCommand(string commandName) {
        if (string.IsNullOrEmpty(commandName))
            return;
        commands.UnRegister(commandName);
    }

    public void ClearAllEvents() {
        events.Clear();
    }

    public void ClearAllCommands() {
        commands.Clear();
    }

    public void Dispose() {
        ClearAllEvents();
        ClearAllCommands();
        events = null;
        commands = null;
    }
}