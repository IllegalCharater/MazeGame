using System.Collections.Generic;

public class SceneConfig
{
    public static string scenePath = "Assets/Scenes";

    public static string mainSceneName = "Main";
    public static Dictionary<GameState, string> sceneMap = new Dictionary<GameState, string>()
    {
        { GameState.MainMenu,"MainMenu" },
        { GameState.InMaze,"Maze" },
    };
}
