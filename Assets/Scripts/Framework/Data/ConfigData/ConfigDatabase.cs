using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ConfigDatabase {
    private string rootKey = string.Empty;
    private TextAsset JsonSourceAsset = null;
    private Dictionary<string, ConfigData> dataList = new Dictionary<string, ConfigData>();

    public void Init(string rootKey) {
        this.rootKey = rootKey;
    }

    public void LoadData() {
        ReloadFromJson();
    }
    public ConfigData GetData(string key) {
        return dataList[key] ?? null;
    }
    public bool GetData(string key, out ConfigData data) {
        data = dataList[key] ?? null;
        return data == null ? false : true;
    }
    public IReadOnlyCollection<ConfigData> GetAllDatas() {
        return dataList.Values;
    }
    public bool Contains(string key) {
        return dataList.ContainsKey(key);
    }

    private void ReloadFromJson() {
        dataList.Clear();

        string jsonStem = GetJsonName();
        if (string.IsNullOrEmpty(jsonStem)) {
            Debug.LogWarning($"[{GetType().Name}] No JSON mapping found for rootKey: {rootKey}");
            return;
        }

        JsonSourceAsset = Resources.Load<TextAsset>(jsonStem);
        if (JsonSourceAsset == null) {
            Debug.LogWarning($"[{GetType().Name}] JSON not found in Resources: {jsonStem}");
            return;
        }

        try {
            var root = JObject.Parse(JsonSourceAsset.text);
            JToken tableToken = root[rootKey];
            if (tableToken == null || tableToken.Type != JTokenType.Object) {
                Debug.LogWarning($"[{GetType().Name}] Table node not found: {rootKey}");
                return;
            }

            var tableObj = (JObject)tableToken;
            foreach (var prop in tableObj.Properties()) {
                string id = prop.Name;
                if (string.IsNullOrEmpty(id))
                    continue;
                if (prop.Value.Type != JTokenType.Object) {
                    Debug.LogWarning($"[{GetType().Name}] Row is not an object, skipped: {rootKey}.{id}");
                    continue;
                }

                // 行已确认为 JSON 对象，物化结果必为 Dictionary<string, object>
                var rowData = (Dictionary<string, object>)Materialize(prop.Value);
                dataList[id] = new ConfigData(rowData);
            }
        }
        catch (Exception ex) {
            Debug.LogError($"[{GetType().Name}] Failed to read JSON: {ex.Message} {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 递归把 JSON 节点物化为通用 CLR 值：对象→Dictionary&lt;string,object&gt;、数组→List&lt;object&gt;，
    /// 标量按 JSON 类型推断（整数→int/long、浮点→double、布尔→bool、字符串→string），不做具体类型绑定。
    /// </summary>
    private static object Materialize(JToken token) {
        switch (token.Type) {
            case JTokenType.Object: {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in ((JObject)token).Properties())
                        dict[prop.Name] = Materialize(prop.Value);
                    return dict;
                }
            case JTokenType.Array: {
                    var list = new List<object>();
                    foreach (var item in (JArray)token)
                        list.Add(Materialize(item));
                    return list;
                }
            case JTokenType.Integer: {
                    long l = token.Value<long>();
                    return l >= int.MinValue && l <= int.MaxValue ? (int)l : l;
                }
            case JTokenType.Float:
                return token.Value<double>();
            case JTokenType.Boolean:
                return token.Value<bool>();
            case JTokenType.String:
                return token.Value<string>();
            case JTokenType.Null:
            case JTokenType.Undefined:
                return null;
            default:
                return token.ToString();
        }
    }

    /// <summary>从 base.json 读取 rootKey → 数据文件 stem（无扩展名）。</summary>
    private string GetJsonName() {
        var manifest = Resources.Load<TextAsset>(DataConfig.BaseJsonName);
        if (manifest == null)
            return string.Empty;

        try {
            var root = JObject.Parse(manifest.text);
            string stem = root.Value<string>(rootKey);
            return string.IsNullOrEmpty(stem) ? string.Empty : stem;
        }
        catch (Exception ex) {
            Debug.LogWarning($"[{GetType().Name}] Failed to parse {DataConfig.BaseJsonName}.json: {ex.Message}");
            return string.Empty;
        }
    }
}
