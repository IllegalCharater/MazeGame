using System;

public static class GameEvents
{
    public static event Action<float> OnCurrencyChanged;
    public static event Action<GameState, GameState> OnGameStateChanged;
    public static event Action OnInventoryChanged;
    public static event Action<int, int> OnEnergyChanged;
    public static event Action OnMazeRunChanged;
    public static event Action<MazeRunResult> OnMazeRunEnded;
    public static event Action OnCraftingChanged;
    public static event Action OnShopChanged;
    public static event Action<CollectionCategory> OnCollectionChanged;
    public static event Action OnBuffChanged;

    public static void RaiseCurrencyChanged(float newAmount) => OnCurrencyChanged?.Invoke(newAmount);

    public static void RaiseGameStateChanged(GameState from, GameState to) =>
        OnGameStateChanged?.Invoke(from, to);

    public static void RaiseInventoryChanged() => OnInventoryChanged?.Invoke();
    public static void RaiseEnergyChanged(int current, int max) => OnEnergyChanged?.Invoke(current, max);
    public static void RaiseMazeRunChanged() => OnMazeRunChanged?.Invoke();
    public static void RaiseMazeRunEnded(MazeRunResult result) => OnMazeRunEnded?.Invoke(result);
    public static void RaiseCraftingChanged() => OnCraftingChanged?.Invoke();
    public static void RaiseShopChanged() => OnShopChanged?.Invoke();
    public static void RaiseCollectionChanged(CollectionCategory category) => OnCollectionChanged?.Invoke(category);
    public static void RaiseBuffChanged() => OnBuffChanged?.Invoke();
}
