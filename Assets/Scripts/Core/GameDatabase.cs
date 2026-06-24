using System.Collections.Generic;
using UnityEngine;

public class GameDatabase
{
    public static GameDatabase Instance { get; private set; }

    public Dictionary<string, Dictionary<string, BaseData>> databases = new Dictionary<string, Dictionary<string, BaseData>>();
    public Dictionary<string,PlayerDatabase> playerDatabases = new Dictionary<string, PlayerDatabase>();

    private string defaultPlayerId = "player_warrior";
    //默认玩家数据
    public PlayerDatabase GetPlayerData()
    {
        return playerDatabases[defaultPlayerId];
    }
    
    public static GameDatabase GetInstance()
    {
        if (Instance == null)
            Instance = new GameDatabase();
        return Instance;
    }

    public void init()
    {
        databases.Clear();
        playerDatabases.Clear();
        //初始化json为对象
        foreach (var rootKey in DataConfig.DataTypes.Keys)
        {
            databases[rootKey] = DatabaseHelper.CreateDataMap(rootKey);
            Debug.Log($"Loaded {rootKey} data, count: {databases[rootKey].Count}");
        }
        //初始化玩家数据,默认仅加载一个玩家
        playerDatabases.Add(defaultPlayerId, new PlayerDatabase());
        playerDatabases[defaultPlayerId].init(defaultPlayerId);
    }

    public T Get<T>(string rootKey, string id) where T : BaseData
    {
        if (string.IsNullOrEmpty(rootKey) || string.IsNullOrEmpty(id))
            return null;
        if (!databases.TryGetValue(rootKey, out Dictionary<string, BaseData> table))
            return null;
        if (!table.TryGetValue(id, out BaseData data))
            return null;

        return data as T;
    }

    public IReadOnlyList<T> GetAll<T>(string rootKey) where T : BaseData
    {
        List<T> result = new List<T>();
        if (string.IsNullOrEmpty(rootKey))
            return result;
        if (!databases.TryGetValue(rootKey, out Dictionary<string, BaseData> table))
            return result;

        foreach (BaseData data in table.Values)
        {
            if (data is T typed)
                result.Add(typed);
        }

        return result;
    }

    public bool Contains(string rootKey, string id)
    {
        return !string.IsNullOrEmpty(rootKey)
            && !string.IsNullOrEmpty(id)
            && databases.TryGetValue(rootKey, out Dictionary<string, BaseData> table)
            && table.ContainsKey(id);
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
