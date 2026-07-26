public sealed class CommandResult
{
    public bool success;
    public string message;
    public object payload;

    private CommandResult(bool success, string message, object payload)
    {
        this.success = success;
        this.message = message;
        this.payload = payload;
    }

    public static CommandResult Succeeded(string message = "", object payload = null)
    {
        return new CommandResult(true, message, payload);
    }

    public static CommandResult Failed(string message, object payload = null)
    {
        return new CommandResult(false, message, payload);
    }
}
