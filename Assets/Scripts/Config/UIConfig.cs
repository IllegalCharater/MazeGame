public class UIConfig {
    public static string PrefabDir = "Assets/UI/Prefabs";

    public static string Root = "UILayers";
    public static string BackgroundLayer = "BackgroundLayer";
    public static string NormalLayer = "NormalLayer";
    public static string PopupLayer = "PopupLayer";
    public static string TopLayer = "TopLayer";
    public static string ToastLayer = "ToastLayer";

    public static string GetAddress(string viewName) {
        return $"{PrefabDir}/{viewName}.prefab";
    }
}
