using System.Collections.Generic;

public class SceneConfig {
    public static string scenePath = "Assets/Scenes";

    public static string mainSceneName = "Main";
    public static Dictionary<SceneState, string> sceneMap = new Dictionary<SceneState, string>()
    {
        { SceneState.MainMenu,"MainMenu" },
        { SceneState.InMaze,"Maze" },
    };
}
