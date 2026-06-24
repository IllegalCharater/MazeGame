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
    };
}
