public sealed class GameServices
{
    public EnergyService Energy { get; } = new EnergyService();
    public MazeRunService MazeRun { get; } = new MazeRunService();
    public CraftingService Crafting { get; } = new CraftingService();
    public ShopService Shop { get; } = new ShopService();
    public BlueprintService Blueprints { get; } = new BlueprintService();
    public BuffService Buffs { get; } = new BuffService();
    public CollectionService Collections { get; } = new CollectionService();
    public OutfitService Outfits { get; } = new OutfitService();

    public void Initialize(GameDatabase database, PlayerDatabase player)
    {
        Energy.Initialize(database, player);
        MazeRun.Initialize(database, player);
        Crafting.Initialize(database, player);
        Shop.Initialize(database, player);
        Blueprints.Initialize(database, player);
        Buffs.Initialize(database, player);
        Collections.Initialize(database, player);
        Outfits.Initialize(database, player);
    }
}
