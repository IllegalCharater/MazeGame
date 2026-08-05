using System;
using System.Collections.Generic;

public class UIConfig {
    public static string PrefabDir = "Assets/UI/Prefabs";

    public static string RootName = "UILayers";
    public static string BackgroundLayerName = "BackgroundLayer";
    public static string NormalLayerName = "NormalLayer";
    public static string PopupLayerName = "PopupLayer";
    public static string TopLayerName = "TopLayer";
    public static string ToastLayerName = "ToastLayer";

    // public static readonly Dictionary<string, Type> viewMap = new Dictionary<string, Type>()
    // {
    //     { "HUD", typeof(HUDController) },
    //     { "MainMenuUI", typeof(MainMenuUIController) },
    //     { "MazeUI", typeof(MazeUIController) },
    //     { "MazePuzzleItemSocketUI", typeof(ItemSocketPuzzleUIController) },
    //     { "MazePuzzleCandleNumberUI", typeof(CandleNumberPuzzleUIController) },
    //     { "MazePuzzleFloorChoiceUI", typeof(FloorChoicePuzzleUIController) },
    //     { "MazePuzzleRockWordUI", typeof(RockWordPuzzleUIController) },
    //     { "ShopUI", typeof(ShopUIController) },
    //     { "InventoryUI", typeof(InventoryUIController) },
    //     { "CollectionUI", typeof(CollectionUIController) },
    //     { "CraftingUI", typeof(CraftingUIController) },
    //     { "ProfileUI", typeof(ProfileUIController) },
    //     { "ProfileDetailUI", typeof(ProfileDetailUIController) },
    // };

    public static string GetAddress(string viewName) {
        return $"{PrefabDir}/{viewName}.prefab";
    }
}
