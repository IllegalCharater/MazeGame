using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class DatabaseHelper {
    public static ConfigDatabase CreateDataMap(string rootKey) {
        var database = new ConfigDatabase();
        database.Init(rootKey);
        database.LoadData();
        return database;
    }

    /// <summary>读取 base.json 中注册的全部逻辑表名（rootKey），供 GameDatabase 逐表加载。</summary>
    public static List<string> GetRegisteredRootKeys() {
        var list = new List<string>();
        var manifest = Resources.Load<TextAsset>(DataConfig.BaseJsonName);
        if (manifest == null)
            return list;

        try {
            var root = JObject.Parse(manifest.text);
            foreach (var prop in root.Properties())
                list.Add(prop.Name);
        }
        catch (Exception ex) {
            Debug.LogWarning($"[DatabaseHelper] Failed to parse {DataConfig.BaseJsonName}.json: {ex.Message}");
        }
        return list;
    }
}
