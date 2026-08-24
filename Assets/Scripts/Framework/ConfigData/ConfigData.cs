using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

/// <summary>
/// 通用配置行：不再绑定具体数据类，内部以「列名 → 值」的字典承载一行 JSON 数据。
/// 嵌套对象/数组在加载时已递归物化为 Dictionary/List，标量按 JSON 类型推断为 int/float/bool/string。
/// </summary>

public class ConfigData {
    private readonly Dictionary<string, object> _data;

    public ConfigData() {
        _data = new Dictionary<string, object>();
    }

    public ConfigData(Dictionary<string, object> data) {
        _data = data ?? new Dictionary<string, object>();
    }

    public Dictionary<string, object> Raw => _data;

    public bool ContainsKey(string key) {
        return !string.IsNullOrEmpty(key) && _data.ContainsKey(key);
    }

    /// <summary>按列名读取值；类型不匹配时先用 Newtonsoft 做宽松转换（如 string→int、嵌套字典），失败返回 defaultValue。</summary>
    public T Get<T>(string key, T defaultValue = default) {
        if (string.IsNullOrEmpty(key) || !_data.TryGetValue(key, out object value))
            return defaultValue;

        if (value is T typed)
            return typed;

        try {
            return JToken.FromObject(value).ToObject<T>();
        }
        catch (Exception) {
            return defaultValue;
        }
    }

    public bool TryGet<T>(string key, out T result) {
        if (string.IsNullOrEmpty(key) || !_data.TryGetValue(key, out object value)) {
            result = default;
            return false;
        }

        if (value is T typed) {
            result = typed;
            return true;
        }

        try {
            result = JToken.FromObject(value).ToObject<T>();
            return true;
        }
        catch (Exception) {
            result = default;
            return false;
        }
    }
}