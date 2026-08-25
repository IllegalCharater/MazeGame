using System;
using System.Threading.Tasks;

public class Context : IDisposable {
    protected EventBus events { get; private set; }
    protected CommandBus commands { get; private set; }

    public Context(EventBus events, CommandBus commands) {
        this.events = events;
        this.commands = commands;
    }

    public void HandleEvent(EventType eventType, object[] args = null) {
        events.Publish(eventType, args);
    }

    public Task<CommandResult> HandleCommand(CommandType commandType, DataBag payload) {
        return commands.Execute(commandType, payload);
    }

    public void BindEvent(EventType eventType, Delegate action) {
        if (action == null)
            return;

        events?.Subscribe(eventType, action);
    }
    public void BindEvent(EventType parentEvent, EventType childEvent) {
        if (parentEvent == null || childEvent == null)
            return;

        events?.Subscribe(parentEvent, childEvent);
    }
    public void UnbindEvent(EventType eventType, Delegate action) {
        if (action == null)
            return;

        events?.Unsubscribe(eventType, action);
    }
    public void UnbindEvent(EventType parentEvent, EventType childEvent) {
        if (parentEvent == null || childEvent == null)
            return;

        events?.Unsubscribe(parentEvent, childEvent);
    }

    public void BindCommand(ICommand command) {
        commands?.Register(command);
    }

    public void UnbindCommand(CommandType commandType) {
        commands?.UnRegister(commandType);
    }

    public bool TryGetCommand(CommandType commandType, out ICommand command) {
        command = null;
        return commands != null && commands.TryGetHandler(commandType, out command);
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