using System.Collections.Generic;
using System;
public partial class ExcelToJsonExporter
{
    ///rootkey-moduleType
    public static Dictionary<string, ExcelExportModule> ExportModules = new()
    {
        { "items", new ItemExportModule() },
        { "player_start", new PlayerStartExportModule() },
        { "recipes", new RecipeExportModule() },
        { "ingredients", new IngredientExportModule() },
        { "foods", new FoodExportModule() },
        { "blueprints", new BlueprintExportModule() },
        { "outfits", new OutfitExportModule() },
        { "furniture", new FurnitureExportModule() },
        { "buffs", new BuffExportModule() },
        { "maze_nodes", new MazeNodeExportModule() },
        { "maze_puzzles", new MazePuzzleExportModule() },
        { "maze_traps", new MazeTrapExportModule() },
        { "maze_fragments", new MazeFragmentExportModule() },
        { "maze_rules", new MazeRuleExportModule() },
    };
}
