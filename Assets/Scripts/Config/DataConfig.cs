using System;
using System.Collections.Generic;

public static class DataConfig
{
    public static string ExcelDir = "../Excel";
    public static string OutDir = "Assets/Configs/Generated/Resources";
    public static string BaseJsonName = "base";

    public static readonly Dictionary<string, Type> DataTypes = new Dictionary<string, Type>
    {
        { "items", typeof(ItemData) },
        { "player_start",typeof(PlayerStartData) },
        { "recipes", typeof(RecipeData) },
        { "foods", typeof(FoodData) },
        { "ingredients", typeof(IngredientData) },
        { "blueprints", typeof(BlueprintData) },
        { "outfits", typeof(OutfitData) },
        { "furniture", typeof(FurnitureData) },
        { "buffs", typeof(BuffData) },
        { "maze_nodes", typeof(MazeNodeData) },
        { "maze_puzzles", typeof(MazePuzzleData) },
        { "maze_traps", typeof(MazeTrapData) },
        { "maze_fragments", typeof(MazeFragmentData) },
        { "maze_rules", typeof(MazeRuleData) },
    };
}
