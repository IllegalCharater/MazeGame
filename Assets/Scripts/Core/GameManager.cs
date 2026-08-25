using System.Threading;
using UnityEngine;

public sealed class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }
    // public int Currency => currency;
    public GameDatabase gameDatabase;
    public UIManager uiManager;
    public SceneFlowManager sceneFlowManager;
    public GameContext gamecontext;
    public GameTimer timer;
    public EcsWorld world;

    public GameServices services { get; private set; }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeGame();
        DontDestroyOnLoad(gameObject);
    }

    private void InitializeGame() {
        //初始化系统单例
        gameDatabase = GameDatabase.GetInstance();
        gameDatabase.Init();

        gamecontext = GameContext.GetInstance();
        gamecontext.Init();

        timer = new GameTimer();
        timer.Init();

        world = new EcsWorld();
        world.Init();

        services = new GameServices();
        services.Init();

        uiManager = UIManager.GetInstance();
        uiManager.Init();

        sceneFlowManager = SceneFlowManager.GetInstance();
        sceneFlowManager.Init();
        //绑定全局指令和事件
        //全局指令
        InitCommands();

        //全局事件
        InitEvents();

        //初始化游戏场景
        sceneFlowManager.GoToMain();
        _ = UIManager.GotoView("HUD");
    }
    void InitCommands() {
        GameContext.AddCommand(new ChangeCurrencyCommand());
        GameContext.AddCommand(new AfterViewCommand());
    }

    void InitEvents() {
        // GameContext.AddEvent(EventType.OnViewLoaded, (string name) => {
        //     Debug.Log(name + " has loaded");
        // });
    }

    private void Update() {
        float deltaTime = Time.deltaTime;
        timer?.Tick(deltaTime);
    }

    private void OnDestroy() {
        if (Instance != this)
            return;

        gameDatabase?.Dispose();
        gamecontext?.Dispose();
        timer?.Dispose();
        world?.Dispose();
        services?.Dispose();
        uiManager?.Dispose();
        sceneFlowManager?.Dispose();
        Instance = null;
    }
}
