public sealed class GameServices {
    // public GameDatabase Database { get; private set; }
    // public PlayerDatabase Player { get; private set; }
    // public FrameworkContext Framework { get; private set; }
    // public EcsWorld World { get; private set; }

    // public ShopService Shop { get; private set; }
    // public CraftingService Crafting { get; private set; }
    // public BlueprintService Blueprints { get; private set; }
    // public BuffService Buffs { get; private set; }
    // public CollectionService Collections { get; private set; }
    // public OutfitService Outfits { get; private set; }
    // public SellService Sell { get; private set; }
    // public MazeService Maze { get; private set; }

    // public CommandBus Commands => Framework != null ? Framework.Commands : null;
    // public EventBus Events => Framework != null ? Framework.Events : null;

    public void Init() {
        // Database = database;
        // Player = player;
        // Framework = framework;
        // World = world;

        // Shop = new ShopService();
        // Crafting = new CraftingService();
        // Blueprints = new BlueprintService();
        // Buffs = new BuffService();
        // Collections = new CollectionService();
        // Outfits = new OutfitService();
        // Sell = new SellService();
        // Maze = new MazeService();

        // Shop.Initialize(database, player);
        // Crafting.Initialize(database, player);
        // Blueprints.Initialize(database, player);
        // Buffs.Initialize(database, player);
        // Collections.Initialize(database, player);
        // Outfits.Initialize(database, player);
        // Maze.Initialize(database, player, framework, world);

        // RegisterServices();
        // RegisterCommands();
    }

    public void Dispose() {
        //调用各个系统的dispose
    }
    private void RegisterServices() {
        // if (Framework?.Container == null)
        //     return;

        // Framework.Container.Register(this);
        // Framework.Container.Register(Database);
        // Framework.Container.Register(Player);
        // Framework.Container.Register(World);
        // Framework.Container.Register(Framework);
        // Framework.Container.Register(Commands);
        // Framework.Container.Register(Events);
        // Framework.Container.Register(Shop);
        // Framework.Container.Register(Crafting);
        // Framework.Container.Register(Blueprints);
        // Framework.Container.Register(Buffs);
        // Framework.Container.Register(Collections);
        // Framework.Container.Register(Outfits);
        // Framework.Container.Register(Sell);
        // Framework.Container.Register(Maze);
    }

    private void RegisterCommands() {
        // if (Commands == null)
        //     return;

        // Commands.Register(new AddCurrencyCommandHandler(Database, Player));
        // Commands.Register(new SpendCurrencyCommandHandler(Database, Player));
        // Commands.Register(new OpenViewCommandHandler());
        // Commands.Register(new ChangeSceneCommandHandler());
    }
}
