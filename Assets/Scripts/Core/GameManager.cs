using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    // public int Currency => currency;
    public GameState CurrentState = GameState.MainMenu;
    public GameDatabase gameDatabase;
    
    public GameServices Services { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeGameData();
        DontDestroyOnLoad(gameObject);
    }

    private void InitializeGameData()
    {
        gameDatabase = GameDatabase.GetInstance();
        gameDatabase.init();
        Services = new GameServices();
        Services.Initialize(gameDatabase, gameDatabase.GetPlayerData());
    }

    public void SetGameState(GameState next)
    {
        if (CurrentState == next)
            return;

        GameState prev = CurrentState;
        CurrentState = next;
        GameEvents.RaiseGameStateChanged(prev, next);
    }

    public bool TryAddCurrency(string playerId,int amount)
    {
        if (amount < 0)
            return false;

        if (!TryGetProfile(playerId, out PlayerProfile profile))
            return false;

        var currency=profile.currency;
        long next = (long)currency + amount;
        if (next > int.MaxValue)
            return false;

        SetCurrency(playerId, (int)next);
        return true;
    }

    public bool TrySpendCurrency(string playerId,float amount)
    {
        if (!TryGetProfile(playerId, out PlayerProfile profile))
            return false;

        var currency=profile.currency;
        if (amount < 0 || currency < amount)
            return false;

        SetCurrency(playerId, currency - amount);
        return true;
    }

    public void SetCurrency(float value)
    {
        if (gameDatabase?.GetPlayerData() == null)
            return;

        SetCurrency(gameDatabase.GetPlayerData().playerId, value);
    }

    private void SetCurrency(string playerId, float value)
    {
        if (!TryGetProfile(playerId, out PlayerProfile profile))
            return;

        int currency = Mathf.Max(0, Mathf.FloorToInt(value));
        profile.currency = currency;
        GameEvents.RaiseCurrencyChanged(currency);
    }

    private bool TryGetProfile(string playerId, out PlayerProfile profile)
    {
        profile = null;
        if (gameDatabase == null || string.IsNullOrEmpty(playerId))
            return false;
        if (!gameDatabase.playerDatabases.TryGetValue(playerId, out PlayerDatabase player) || player == null)
            return false;

        profile = player.profile;
        return profile != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // currency = Mathf.Max(0, currency);
        // if (Application.isPlaying && Instance == this)
        //     SetCurrency(currency);
    }
#endif
}
