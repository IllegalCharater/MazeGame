using System.Threading.Tasks;

public interface IAsyncCommandHandler<TCommand> where TCommand : ICommand
{
    Task<CommandResult> HandleAsync(TCommand command);
}
