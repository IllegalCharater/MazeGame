using System.Collections.Generic;

public enum MazeRunState
{
    NotStarted,
    Running,
    Completed,
    Evacuated,
    Failed
}

public enum MazeRunEndReason
{
    Clear,
    PerfectClear,
    Evacuate,
    EnergyEmpty
}

public enum ItemCategory
{
    Ingredient,
    Food,
    Blueprint,
    Tool,
    Outfit,
    Furniture
}

public enum BuffCategory
{
    Maze,
    Crafting,
    Shop
}

public enum CollectionCategory
{
    Food,
    Outfit,
    Furniture
}

public interface IGameService
{
    void Initialize(GameDatabase database, PlayerDatabase player);
}

public interface IReadOnlyInventory
{
    int GetAmount(string itemId);
    bool Has(string itemId, int amount);
    IReadOnlyDictionary<string, int> GetSnapshot();
}

public interface IMazeRewardResolver
{
    MazeRunResult Resolve(MazeRunResult rawResult);
}

public interface IRequirementChecker
{
    bool IsRequirementMet(string requirementId, GameDatabase database, PlayerDatabase player);
}
