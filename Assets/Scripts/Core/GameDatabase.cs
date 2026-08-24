using System.Collections.Generic;
using UnityEngine;

public class GameDatabase {
    public static GameDatabase Instance { get; private set; }

    public Dictionary<string, ConfigDatabase> configdatabases = new Dictionary<string, ConfigDatabase>();
    public Dictionary<string, PlayerDatabase> playerDatabases = new Dictionary<string, PlayerDatabase>();

    private string defaultPlayerId = "player_warrior";
    //默认玩家数据
    public PlayerDatabase GetPlayerData() {
        return playerDatabases[defaultPlayerId];
    }

    public static GameDatabase GetInstance() {
        if (Instance == null)
            Instance = new GameDatabase();
        return Instance;
    }

    public void Init() {
        configdatabases.Clear();
        playerDatabases.Clear();
        //初始化json为对象（rootKey 及数据文件均以 base.json 注册信息为准）
        foreach (var rootKey in DatabaseHelper.GetRegisteredRootKeys()) {
            configdatabases[rootKey] = DatabaseHelper.CreateDataMap(rootKey);
            Debug.Log($"Loaded {rootKey} data, count: {configdatabases[rootKey].GetAllDatas().Count}");
        }
        //初始化玩家数据,默认仅加载一个玩家
        playerDatabases.Add(defaultPlayerId, new PlayerDatabase());
        playerDatabases[defaultPlayerId].init(defaultPlayerId);

        //注册唯一实例
        Injector.Instance.Register(Instance);
    }

    public ConfigData Get(string rootKey, string id) {
        if (string.IsNullOrEmpty(rootKey) || string.IsNullOrEmpty(id))
            return null;
        if (!configdatabases.TryGetValue(rootKey, out ConfigDatabase table))
            return null;
        if (!table.GetData(id, out ConfigData data))
            return null;

        return data;
    }

    public IReadOnlyList<ConfigData> GetAll(string rootKey) {
        List<ConfigData> result = new List<ConfigData>();
        if (string.IsNullOrEmpty(rootKey))
            return result;
        if (!configdatabases.TryGetValue(rootKey, out ConfigDatabase table))
            return result;

        foreach (ConfigData data in table.GetAllDatas())
            result.Add(data);

        return result;
    }

    public bool Contains(string rootKey, string id) {
        return !string.IsNullOrEmpty(rootKey)
            && !string.IsNullOrEmpty(id)
            && configdatabases.TryGetValue(rootKey, out ConfigDatabase table)
            && table.Contains(id);
    }

    public void Dispose() {
        configdatabases.Clear();
        playerDatabases.Clear();
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
