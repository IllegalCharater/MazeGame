using System;

public static class GameEvents
{
    public static event Action<float> OnCurrencyChanged;
    public static event Action<GameState, GameState> OnGameStateChanged;
    public static event Action OnInventoryChanged;
    public static event Action<int, int> OnEnergyChanged;
    public static event Action OnMazeRunChanged;
    public static event Action OnMazeRunEnded;
    public static event Action<MazeRunResult> OnMazeRunEndedWithResult;
    public static event Action OnCraftingChanged;
    public static event Action OnShopChanged;
    public static event Action<CollectionCategory> OnCollectionChanged;
    public static event Action OnBuffChanged;

    public static void RaiseCurrencyChanged(float newAmount)
    {
        OnCurrencyChanged?.Invoke(newAmount);
        Publish(new CurrencyChangedEvent(newAmount));
    }

    public static void RaiseGameStateChanged(GameState from, GameState to)
    {
        OnGameStateChanged?.Invoke(from, to);
        Publish(new GameStateChangedEvent(from, to));
    }

    public static void RaiseInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
        Publish(new InventoryChangedEvent());
    }

    public static void RaiseEnergyChanged(int current, int max)
    {
        OnEnergyChanged?.Invoke(current, max);
        Publish(new EnergyChangedEvent(current, max));
    }

    public static void RaiseMazeRunChanged()
    {
        OnMazeRunChanged?.Invoke();
        Publish(new MazeRunChangedEvent());
    }

    public static void RaiseMazeRunEnded()
    {
        RaiseMazeRunEnded(null);
    }

    public static void RaiseMazeRunEnded(MazeRunResult result)
    {
        OnMazeRunEnded?.Invoke();
        OnMazeRunEndedWithResult?.Invoke(result);
        Publish(new MazeRunEndedEvent(result));
    }

    public static void RaiseCraftingChanged()
    {
        OnCraftingChanged?.Invoke();
        Publish(new CraftingChangedEvent());
    }

    public static void RaiseShopChanged()
    {
        OnShopChanged?.Invoke();
        Publish(new ShopChangedEvent());
    }

    public static void RaiseCollectionChanged(CollectionCategory category)
    {
        OnCollectionChanged?.Invoke(category);
        Publish(new CollectionChangedEvent(category));
    }

    public static void RaiseBuffChanged()
    {
        OnBuffChanged?.Invoke();
        Publish(new BuffChangedEvent());
    }

    private static void Publish<TEvent>(TEvent evt)
    {
        EventBus bus = FrameworkContext.Instance != null ? FrameworkContext.Instance.Events : null;
        if (bus != null)
            bus.Publish(evt);
    }
}
