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

        gamecontext = new GameContext();
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
        var context = GameContext.Instance;
        //金钱变更全局指令
        var command = new ChangeCurrency();
        context.AddCommand(command);

        //金钱变更全局事件
        // context.AddEvent("CurrencyChanged", (amount) => { context.Execute(command.Name, new DataBag().Set("amount", amount)); });会导致循环引用

        //初始化游戏场景
        sceneFlowManager.GoToMain();
        _ = UIManager.GotoView("HUD");
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

    // public void SetGameState(GameState next) {
    //     if (CurrentState == next)
    //         return;

    //     GameState prev = CurrentState;
    //     CurrentState = next;
    //     GameEvents.RaiseGameStateChanged(prev, next);
    // }

    //     public bool TryAddCurrency(string playerId, int amount) {
    //         if (amount < 0)
    //             return false;

    //         if (!TryGetProfile(playerId, out PlayerProfile profile))
    //             return false;

    //         var currency = profile.currency;
    //         long next = (long)currency + amount;
    //         if (next > int.MaxValue)
    //             return false;

    //         SetCurrency(playerId, (int)next);
    //         return true;
    //     }

    //     public bool TrySpendCurrency(string playerId, float amount) {
    //         if (!TryGetProfile(playerId, out PlayerProfile profile))
    //             return false;

    //         var currency = profile.currency;
    //         if (amount < 0 || currency < amount)
    //             return false;

    //         SetCurrency(playerId, currency - amount);
    //         return true;
    //     }

    //     public void SetCurrency(float value) {
    //         if (gameDatabase?.GetPlayerData() == null)
    //             return;

    //         SetCurrency(gameDatabase.GetPlayerData().playerId, value);
    //     }

    //     private void SetCurrency(string playerId, float value) {
    //         if (!TryGetProfile(playerId, out PlayerProfile profile))
    //             return;

    //         int currency = Mathf.Max(0, Mathf.FloorToInt(value));
    //         profile.currency = currency;
    //         GameEvents.RaiseCurrencyChanged(currency);
    //     }

    //     private bool TryGetProfile(string playerId, out PlayerProfile profile) {
    //         profile = null;
    //         if (gameDatabase == null || string.IsNullOrEmpty(playerId))
    //             return false;
    //         if (!gameDatabase.playerDatabases.TryGetValue(playerId, out PlayerDatabase player) || player == null)
    //             return false;

    //         profile = player.profile;
    //         return profile != null;
    //     }

    // #if UNITY_EDITOR
    //     private void OnValidate() {
    //         // currency = Mathf.Max(0, currency);
    //         // if (Application.isPlaying && Instance == this)
    //         //     SetCurrency(currency);
    //     }
    // #endif
}
