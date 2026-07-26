using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class CommandBus
{
    private interface ICommandHandlerAdapter
    {
        bool CanHandleSync { get; }
        CommandResult Handle(ICommand command);
        Task<CommandResult> HandleAsync(ICommand command);
    }

    private sealed class CommandHandlerAdapter<TCommand> : ICommandHandlerAdapter where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> handler;

        public bool CanHandleSync => true;

        public CommandHandlerAdapter(ICommandHandler<TCommand> handler)
        {
            this.handler = handler;
        }

        public CommandResult Handle(ICommand command)
        {
            if (!(command is TCommand typedCommand))
                return CommandResult.Failed("Command type mismatch.");

            return handler.Handle(typedCommand);
        }

        public Task<CommandResult> HandleAsync(ICommand command)
        {
            return Task.FromResult(Handle(command));
        }
    }

    private sealed class AsyncCommandHandlerAdapter<TCommand> : ICommandHandlerAdapter where TCommand : ICommand
    {
        private readonly IAsyncCommandHandler<TCommand> handler;

        public bool CanHandleSync => false;

        public AsyncCommandHandlerAdapter(IAsyncCommandHandler<TCommand> handler)
        {
            this.handler = handler;
        }

        public CommandResult Handle(ICommand command)
        {
            return CommandResult.Failed("Command handler for " + typeof(TCommand).Name + " is async only. Use ExecuteAsync.");
        }

        public async Task<CommandResult> HandleAsync(ICommand command)
        {
            if (!(command is TCommand typedCommand))
                return CommandResult.Failed("Command type mismatch.");

            return await handler.HandleAsync(typedCommand);
        }
    }

    private readonly Dictionary<Type, ICommandHandlerAdapter> handlers = new Dictionary<Type, ICommandHandlerAdapter>();

    public void Register<TCommand>(ICommandHandler<TCommand> handler) where TCommand : ICommand
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        handlers[typeof(TCommand)] = new CommandHandlerAdapter<TCommand>(handler);
    }

    public void Register<TCommand>(IAsyncCommandHandler<TCommand> handler) where TCommand : ICommand
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        handlers[typeof(TCommand)] = new AsyncCommandHandlerAdapter<TCommand>(handler);
    }

    public CommandResult Execute<TCommand>(TCommand command) where TCommand : ICommand
    {
        return Execute((ICommand)command);
    }

    public CommandResult Execute(ICommand command)
    {
        if (command == null)
            return CommandResult.Failed("Command is null.");

        Type commandType = command.GetType();
        if (!handlers.TryGetValue(commandType, out ICommandHandlerAdapter handler))
            return CommandResult.Failed("No command handler registered for " + commandType.Name + ".");
        if (!handler.CanHandleSync)
            return handler.Handle(command);

        try
        {
            return handler.Handle(command) ?? CommandResult.Failed("Command handler returned null.");
        }
        catch (Exception ex)
        {
            return CommandResult.Failed(ex.Message);
        }
    }

    public Task<CommandResult> ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        return ExecuteAsync((ICommand)command);
    }

    public async Task<CommandResult> ExecuteAsync(ICommand command)
    {
        if (command == null)
            return CommandResult.Failed("Command is null.");

        Type commandType = command.GetType();
        if (!handlers.TryGetValue(commandType, out ICommandHandlerAdapter handler))
            return CommandResult.Failed("No command handler registered for " + commandType.Name + ".");

        try
        {
            return await handler.HandleAsync(command) ?? CommandResult.Failed("Command handler returned null.");
        }
        catch (Exception ex)
        {
            return CommandResult.Failed(ex.Message);
        }
    }

    public bool HasHandler<TCommand>() where TCommand : ICommand
    {
        return handlers.ContainsKey(typeof(TCommand));
    }

    public void Clear()
    {
        handlers.Clear();
    }
}
