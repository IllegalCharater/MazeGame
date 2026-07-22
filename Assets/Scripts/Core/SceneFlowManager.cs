using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneFlowManager
{
    public static SceneFlowManager Instance { get; private set; }

    public Scene MainScene { get; private set; }
    public Scene DynamicScene { get; private set; }

    private GameState defaultScene = GameState.MainMenu;
    private GameState currentState;
    private bool initialized;
    private bool isChangingState;

    private SceneFlowManager()
    {
        currentState = defaultScene;
    }

    public static SceneFlowManager GetInstance()
    {
        if (Instance == null)
            Instance = new SceneFlowManager();

        return Instance;
    }

    public void Init()
    {
        if (initialized)
            return;

        initialized = true;
        ensureMainScene();

        GoToMain();
    }

    public void GoToMain()
    {
        LoadScene(GameState.MainMenu);
        UIManager.GotoView("MainMenuUI");
    }

    public void GoToMaze()
    {
        LoadScene(GameState.InMaze);
        UIManager.GotoView("MazeUI");
    }

    public void GoToShop()
    {
        LoadScene(GameState.MainMenu);
        UIManager.GotoView("ShopUI");
    }

    private bool isInited = false;
    public void LoadScene(GameState target)
    {
        if (isChangingState)
        {
            Debug.LogWarning($"[SceneFlowManager] Ignore state change while busy: {currentState} -> {target}");
            return;
        }

        if (currentState == target&&isInited)
        {
            SyncGameState(currentState, target);
            return;
        }
        
        isInited = true;
        isChangingState = true;
        GameState previous = currentState;

        try
        {
            UnloadCurrentDynamicScene(target);
            LoadDynamicScene(target);
            currentState = target;
            SyncGameState(previous, target);
        }
        finally
        {
            isChangingState = false;
        }
    }

    private void ensureMainScene()
    {
        string mainSceneName = SceneConfig.mainSceneName;
        MainScene = SceneManager.GetSceneByName(mainSceneName);
        if (MainScene.IsValid() && MainScene.isLoaded)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.name == mainSceneName)
        {
            MainScene = activeScene;
            return;
        }

        if (!CanLoadScene(GetScenePath(GameState.MainMenu)))
        {
            Debug.LogWarning($"[SceneFlowManager] Main scene is not available: {GetScenePath(GameState.MainMenu)}");
            return;
        }

        SceneManager.LoadScene(mainSceneName, LoadSceneMode.Additive);
        MainScene = SceneManager.GetSceneByName(mainSceneName);
    }
    

    private void UnloadCurrentDynamicScene(GameState target)
    {
        Scene targetScene = GetLoadedSceneForState(target);
        if (DynamicScene.IsValid() && DynamicScene.isLoaded && DynamicScene != MainScene && DynamicScene != targetScene)
            SceneManager.UnloadSceneAsync(DynamicScene);

        DynamicScene = targetScene;
    }

    private void LoadDynamicScene(GameState target)
    {
        string scenePath = GetScenePath(target);
        if (string.IsNullOrEmpty(scenePath))
        {
            DynamicScene = default;
            return;
        }

        string sceneName = SceneConfig.sceneMap[target];
        Scene loaded = SceneManager.GetSceneByName(sceneName);
        if (loaded.IsValid() && loaded.isLoaded)
        {
            DynamicScene = loaded;
            SceneManager.SetActiveScene(loaded);
            return;
        }

        if (!CanLoadScene(scenePath))
        {
            DynamicScene = default;
            Debug.LogWarning($"[SceneFlowManager] Dynamic scene is not available, switch UI only: {scenePath}");
            return;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        DynamicScene = SceneManager.GetSceneByName(sceneName);
        if (DynamicScene.IsValid() && DynamicScene.isLoaded)
            SceneManager.SetActiveScene(DynamicScene);
    }

    private void SyncGameState(GameState previous, GameState target)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetGameState(target);
        else if (previous != target)
            GameEvents.RaiseGameStateChanged(previous, target);
    }

    private Scene GetLoadedSceneForState(GameState state)
    {
        string scenePath = GetScenePath(state);
        if (string.IsNullOrEmpty(scenePath))
            return default;

        return SceneManager.GetSceneByName(SceneConfig.sceneMap[state]);
    }

    private static string GetScenePath(GameState state)
    {
        string scenePath = SceneConfig.scenePath;

        return scenePath +"/"+ SceneConfig.sceneMap[state]+".unity";
    }

    // private static string GetSceneNameFromPath(string scenePath)
    // {
    //     int slashIndex = scenePath.LastIndexOf('/');
    //     int dotIndex = scenePath.LastIndexOf('.');
    //     int startIndex = slashIndex >= 0 ? slashIndex + 1 : 0;
    //     int length = dotIndex > startIndex ? dotIndex - startIndex : scenePath.Length - startIndex;
    //     return scenePath.Substring(startIndex, length);
    // }

    private static bool CanLoadScene(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
            return false;

        return SceneUtility.GetBuildIndexByScenePath(scenePath) >= 0;
    }
}
