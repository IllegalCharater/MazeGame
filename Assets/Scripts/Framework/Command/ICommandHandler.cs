public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    CommandResult Handle(TCommand command);
}
