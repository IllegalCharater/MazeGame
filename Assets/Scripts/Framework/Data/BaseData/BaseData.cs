using System.Collections.Generic;
using UnityEngine;

public abstract class BaseData {
    protected Dictionary<string, ConfigDatabase> configs;
    protected DataBag databag;

    // public abstract void PreLoad<T>(params T[] args);

    public void LoadConfig(Dictionary<string, ConfigDatabase> configs) {
        if (configs != null) {
            this.configs = configs;
        }
        LoadConfigData();
    }
    public void LoadConfig(params string[] tables) {
        configs ??= new();
        foreach (var table in tables) {
            // 追加到列表，而不是覆盖
            if (configs.ContainsKey(table)) {
                Debug.Log("config table name duplicate" + table);
                continue;
            }
            configs[table] = GameDatabase.GetConfigtable(table);
        }
        LoadConfigData();
    }

    protected virtual void LoadConfigData() {

    }

    public void UpdateData(DataBag newdata = null) {
        if (newdata != null) {
            databag = newdata;
        }
        else {
            databag = new DataBag();
        }
        LoadUpdateData();
    }

    protected virtual void LoadUpdateData() {

    }
}