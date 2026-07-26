public struct CurrencyChangedEvent
{
    public float newAmount;

    public CurrencyChangedEvent(float newAmount)
    {
        this.newAmount = newAmount;
    }
}

public struct GameStateChangedEvent
{
    public GameState from;
    public GameState to;

    public GameStateChangedEvent(GameState from, GameState to)
    {
        this.from = from;
        this.to = to;
    }
}

public struct InventoryChangedEvent
{
}

public struct EnergyChangedEvent
{
    public int current;
    public int max;

    public EnergyChangedEvent(int current, int max)
    {
        this.current = current;
        this.max = max;
    }
}

public struct MazeRunChangedEvent
{
}

public struct MazeRunEndedEvent
{
    public MazeRunResult result;

    public MazeRunEndedEvent(MazeRunResult result)
    {
        this.result = result;
    }
}

public struct CraftingChangedEvent
{
}

public struct ShopChangedEvent
{
}

public struct CollectionChangedEvent
{
    public CollectionCategory category;

    public CollectionChangedEvent(CollectionCategory category)
    {
        this.category = category;
    }
}

public struct BuffChangedEvent
{
}
