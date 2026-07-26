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
    EnergyEmpty,
    Failed
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
    Furniture,
    Blueprint
}

public interface IReadOnlyInventory
{
    int GetAmount(string itemId);
    bool Has(string itemId, int amount);
    IReadOnlyDictionary<string, int> GetSnapshot();
}

public interface IRequirementChecker
{
    bool IsRequirementMet(string requirementId, GameDatabase database, PlayerDatabase player);
}
