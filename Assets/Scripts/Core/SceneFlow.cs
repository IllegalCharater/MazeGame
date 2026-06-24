using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneFlow : MonoBehaviour
{
    public static SceneFlow Instance { get; private set; }

    [Header("Content Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string mazeSceneName = "Maze";
    [SerializeField] private string shopSceneName = "Shop";

    [Header("Bootstrap")]
    [SerializeField] private bool loadInitialStateOnStart = true;
    [SerializeField] private GameState initialState = GameState.MainMenu;

    [Header("Single Scene Regions")]
    [SerializeField] private bool useSingleSceneRegions;
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private GameObject mazeRoot;
    [SerializeField] private GameObject shopRoot;

    [SerializeField] private bool logTransitions = true;

    private bool isLoading;
    private string currentContentSceneName;
    private string pendingContentSceneName;
    private string pendingPreviousSceneName;
    private GameState pendingState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    private void Start()
    {
        if (useSingleSceneRegions)
        {
            if (GameManager.Instance != null)
                ApplyRegionRoots(GameManager.Instance.CurrentState);
            return;
        }

        currentContentSceneName = FindLoadedContentSceneName();
        if (!string.IsNullOrEmpty(currentContentSceneName) && GameManager.Instance != null)
            GameManager.Instance.SetGameState(ResolveState(currentContentSceneName));

        if (loadInitialStateOnStart && string.IsNullOrEmpty(currentContentSceneName))
            GoToState(initialState);
    }

    public void GoToMainMenu() => GoToState(GameState.MainMenu);
    public void GoToMaze() => GoToState(GameState.InMaze);
    public void GoToShop() => GoToState(GameState.InShop);

    public void GoToState(GameState target)
    {
        if (useSingleSceneRegions)
        {
            ApplyRegionRoots(target);
            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(target);
            else if (logTransitions)
                Debug.LogWarning("[SceneFlow] GameManager not found; region switched without state update.");
            return;
        }

        string sceneName = ResolveSceneName(target);
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[SceneFlow] No scene configured for {target}.");
            return;
        }

        if (isLoading)
        {
            if (logTransitions)
                Debug.LogWarning("[SceneFlow] A scene transition is already running; request ignored.");
            return;
        }

        StartCoroutine(SwitchContentSceneRoutine(sceneName, target));
    }

    private string ResolveSceneName(GameState target)
    {
        switch (target)
        {
            case GameState.MainMenu:
                return mainMenuSceneName;
            case GameState.InMaze:
                return mazeSceneName;
            case GameState.InShop:
                return shopSceneName;
            default:
                return mainMenuSceneName;
        }
    }

    private GameState ResolveState(string sceneName)
    {
        if (sceneName == mazeSceneName)
            return GameState.InMaze;
        if (sceneName == shopSceneName)
            return GameState.InShop;
        return GameState.MainMenu;
    }

    private IEnumerator SwitchContentSceneRoutine(string nextSceneName, GameState targetState)
    {
        isLoading = true;
        string previousSceneName = string.IsNullOrEmpty(currentContentSceneName)
            ? FindLoadedContentSceneName()
            : currentContentSceneName;

        if (logTransitions)
            Debug.Log($"[SceneFlow] Loading content scene: {nextSceneName} -> {targetState}");

        Scene nextScene = SceneManager.GetSceneByName(nextSceneName);
        if (nextScene.IsValid() && nextScene.isLoaded)
        {
            yield return CompleteSwitchRoutine(nextScene, previousSceneName, nextSceneName, targetState);
            yield break;
        }

        pendingContentSceneName = nextSceneName;
        pendingPreviousSceneName = previousSceneName;
        pendingState = targetState;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        AsyncOperation load = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
        if (load != null)
            yield break;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        ClearPendingTransition();
        Debug.LogError($"[SceneFlow] Failed to load scene '{nextSceneName}'. Check Build Settings.");
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != pendingContentSceneName)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        StartCoroutine(CompleteSwitchRoutine(scene, pendingPreviousSceneName, pendingContentSceneName, pendingState));
    }

    private IEnumerator CompleteSwitchRoutine(Scene nextScene, string previousSceneName, string nextSceneName, GameState targetState)
    {
        if (!nextScene.IsValid() || !nextScene.isLoaded)
        {
            Debug.LogError($"[SceneFlow] Scene '{nextSceneName}' did not finish loading.");
            ClearPendingTransition();
            yield break;
        }

        if (nextScene.IsValid() && nextScene.isLoaded)
            SceneManager.SetActiveScene(nextScene);

        if (GameManager.Instance != null)
            GameManager.Instance.SetGameState(targetState);
        else if (logTransitions)
            Debug.LogWarning("[SceneFlow] GameManager not found after content scene load.");

        if (!string.IsNullOrEmpty(previousSceneName) && previousSceneName != nextSceneName)
        {
            Scene previousScene = SceneManager.GetSceneByName(previousSceneName);
            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(previousScene);
                if (unload != null)
                {
                    while (!unload.isDone)
                        yield return null;
                }
            }
        }

        currentContentSceneName = nextSceneName;
        ClearPendingTransition();

        if (logTransitions)
            Debug.Log($"[SceneFlow] Content scene active: {nextSceneName}");
    }

    private void ClearPendingTransition()
    {
        isLoading = false;
        pendingContentSceneName = string.Empty;
        pendingPreviousSceneName = string.Empty;
        pendingState = GameState.MainMenu;
    }

    private string FindLoadedContentSceneName()
    {
        string activeName = SceneManager.GetActiveScene().name;
        if (IsContentSceneName(activeName))
            return activeName;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && IsContentSceneName(scene.name))
                return scene.name;
        }

        return string.Empty;
    }

    private bool IsContentSceneName(string sceneName)
    {
        return sceneName == mainMenuSceneName
            || sceneName == mazeSceneName
            || sceneName == shopSceneName;
    }

    private void ApplyRegionRoots(GameState state)
    {
        bool menu = state == GameState.MainMenu;
        bool maze = state == GameState.InMaze;
        bool shop = state == GameState.InShop;

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(menu);
        if (mazeRoot != null)
            mazeRoot.SetActive(maze);
        if (shopRoot != null)
            shopRoot.SetActive(shop);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Main Menu")]
    private void DebugGoMainMenu()
    {
        if (Application.isPlaying)
            GoToMainMenu();
    }

    [ContextMenu("Debug/Maze")]
    private void DebugGoMaze()
    {
        if (Application.isPlaying)
            GoToMaze();
    }

    [ContextMenu("Debug/Shop")]
    private void DebugGoShop()
    {
        if (Application.isPlaying)
            GoToShop();
    }
#endif
}
