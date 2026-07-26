public sealed class FrameworkContext
{
    public static FrameworkContext Instance { get; private set; }

    public CommandBus Commands { get; private set; }
    public EventBus Events { get; private set; }
    public IOCContainer Container { get; private set; }
    public GameTimer Timer { get; private set; }

    public FrameworkContext()
    {
        Commands = new CommandBus();
        Events = new EventBus();
        Container = new IOCContainer();
        Timer = new GameTimer();
    }

    public void Init()
    {
        Instance = this;
        Container.Register(this);
        Container.Register(Commands);
        Container.Register(Events);
        Container.Register(Timer);
    }

    public void Tick(float deltaTime)
    {
        Timer.Tick(deltaTime);
    }

    public void Dispose()
    {
        Timer.Clear();
        Commands.Clear();
        Events.Clear();
        Container.Clear();
        if (Instance == this)
            Instance = null;
    }
}
