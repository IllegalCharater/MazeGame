using System.Collections.Generic;

public class GameDatabase {
    public static GameDatabase Instance { get; private set; }
    //当前玩家id
    public string PlayerId;
    public Dictionary<string, ConfigDatabase> configdatabases = new Dictionary<string, ConfigDatabase>();
    public Dictionary<string, PlayerDatabase> playerDatabases = new Dictionary<string, PlayerDatabase>();

    //base.json 中注册的 rootKey 缓存，首次懒加载时构建
    private HashSet<string> registeredKeys;

    private string defaultPlayerId = "player_warrior";

    public PlayerDatabase GetPlayerData() {
        return playerDatabases[PlayerId];
    }

    public static GameDatabase GetInstance() {
        if (Instance == null)
            Instance = new GameDatabase();
        return Instance;
    }

    public void Init() {
        configdatabases.Clear();
        playerDatabases.Clear();
        registeredKeys = null;
        //配置表改为按需懒加载，首次访问时依据 base.json 注册信息加载
        //初始化玩家数据,默认仅加载一个玩家
        PlayerId = defaultPlayerId;//默认玩家数据
        playerDatabases.Add(PlayerId, new PlayerDatabase());
        var _database = playerDatabases[defaultPlayerId];
        _database.Init(defaultPlayerId);
        _database.LoadConfig("player_start");

        //注册唯一实例
        Injector.Instance.Register(Instance);
    }

    private ConfigData getConfigData(string rootKey, string id) {
        if (string.IsNullOrEmpty(id))
            return null;
        var table = getConfigtable(rootKey);
        if (table == null)
            return null;
        if (!table.GetData(id, out ConfigData data))
            return null;

        return data;
    }

    public static ConfigData GetConfigData(string rootKey, string id) {
        return Instance.getConfigData(rootKey, id);
    }

    /// <summary>获取配置表，未加载时按需懒加载并缓存；未注册的 rootKey 返回 null。</summary>
    public ConfigDatabase getConfigtable(string rootKey) {
        if (string.IsNullOrEmpty(rootKey))
            return null;
        if (configdatabases.TryGetValue(rootKey, out ConfigDatabase table))
            return table;
        if (!IsRegisteredRootKey(rootKey))
            return null;

        table = DatabaseHelper.CreateDataMap(rootKey);
        configdatabases[rootKey] = table;
        return table;
    }
    public static ConfigDatabase GetConfigtable(string rootKey) {
        return Instance.getConfigtable(rootKey);
    }

    /// <summary>是否在 base.json 注册的配置表，首次调用时缓存注册信息。</summary>
    private bool IsRegisteredRootKey(string rootKey) {
        if (registeredKeys == null)
            registeredKeys = new HashSet<string>(DatabaseHelper.GetRegisteredRootKeys());
        return registeredKeys.Contains(rootKey);
    }

    public IReadOnlyList<ConfigData> GetAll(string rootKey) {
        List<ConfigData> result = new List<ConfigData>();
        var table = getConfigtable(rootKey);
        if (table == null)
            return result;

        foreach (ConfigData data in table.GetAllDatas())
            result.Add(data);

        return result;
    }

    public bool Contains(string rootKey, string id) {
        if (string.IsNullOrEmpty(id))
            return false;
        var table = getConfigtable(rootKey);
        return table != null && table.Contains(id);
    }

    public void Dispose() {
        configdatabases.Clear();
        playerDatabases.Clear();
        registeredKeys = null;
    }

    // private string ResolveDefaultPlayerId()
    // {
    //     if (Contains("player_items", "player_1"))
    //         return "player_1";
    //     if (databases.TryGetValue("player_items", out Dictionary<string, BaseData> players))
    //     {
    //         foreach (string id in players.Keys)
    //             return id;
    //     }
    //
    //     return "player_1";
    // }
}
