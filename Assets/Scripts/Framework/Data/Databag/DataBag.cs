using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class DataBag : IEnumerable<KeyValuePair<string, object>> {
    private Dictionary<string, object> _data = new Dictionary<string, object>();

    // 支持集合初始化器语法：new DataBag { { "energy", 1 }, { "name", xxx }, ... }
    public void Add(string key, object value) {
        _data[key] = value;
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
        return _data.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    // 设置数据（支持链式调用）
    public DataBag Set<T>(string key, T value) {
        _data[key] = value;
        return this;
    }

    // 获取数据（带类型转换），异常时返回 defaultValue 而不上抛
    public T Get<T>(string key, T defaultValue = default) {
        try {
            if (_data.TryGetValue(key, out object value)) {
                if (value is T typed) {
                    return typed;
                }
            }
            return defaultValue;
        }
        catch (Exception) {
            // 类型转换失败、属性读取异常等一律按默认值处理，保证取数不中断流程
            return defaultValue;
        }

    }

    // 尝试获取
    public bool TryGet<T>(string key, out T result) {
        try {
            if (_data.TryGetValue(key, out object value) && value is T typed) {
                result = typed;
                return true;
            }
        }
        catch (Exception) {
            // 异常时按未取到处理
        }
        result = default;
        return false;
    }
    public bool TryGet(Type type, string key, out object result) {
        try {
            if (_data.TryGetValue(key, out object value) && type.IsAssignableFrom(value.GetType())) {
                result = value;
                return true;
            }
        }
        catch (Exception) {
            // 异常时按未取到处理
        }
        result = null;
        return false;
    }

    // ---------- 缓存（避免每帧反射） ----------
    private Dictionary<Type, List<MemberInfo>> _cache = new Dictionary<Type, List<MemberInfo>>();
    // ---------- 新增：从对象自动展开 ----------
    /// <summary>
    /// 从任意对象自动提取所有公有实例属性和字段，填充到 DataBag,浅拷贝
    /// </summary>
    /// <param name="obj">数据源对象</param>
    /// <param name="includeNull">是否包含值为 null 的成员</param>
    /// <param name="bindingFlags">指定要提取的成员（默认 Public | Instance）</param>
    public DataBag FromObject(object obj, bool includeNull = false, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance) {
        if (obj == null) return null;

        Type type = obj.GetType();

        // 使用缓存提高性能（可选）
        var members = GetCachedMembers(type, bindingFlags);

        foreach (var member in members) {
            object value = null;
            if (member is PropertyInfo prop)
                value = prop.GetValue(obj);
            else if (member is FieldInfo field)
                value = field.GetValue(obj);

            if (!includeNull && value == null) continue;

            Set(member.Name, value);
        }

        return this;
    }
    private List<MemberInfo> GetCachedMembers(Type type, BindingFlags flags) {
        if (!_cache.TryGetValue(type, out var members)) {
            members = new List<MemberInfo>();
            // 添加属性（只读的也可以，只要可读）
            var props = type.GetProperties(flags).Where(p => p.CanRead);
            members.AddRange(props);
            // 添加字段
            var fields = type.GetFields(flags);
            members.AddRange(fields);
            _cache[type] = members;
        }
        return members;
    }
    // 清空
    public void Clear() {
        _data.Clear();
        _cache.Clear();
    }

    // 合并另一个 DataBag
    public void Merge(DataBag other) {
        foreach (var kv in other._data)
            _data[kv.Key] = kv.Value;
    }
}

public static class DataBagBinder {
    // 缓存：View 类型 -> 绑定的字段列表
    private static Dictionary<Type, List<FieldInfo>> _cache = new();

    public static void BindFromDataBag(this MonoBehaviour view, DataBag data) {
        if (data == null) return;

        Type type = view.GetType();
        if (!_cache.TryGetValue(type, out var fields)) {
            fields = new List<FieldInfo>();
            // 获取所有实例字段（含私有，但要标记 DataBind）
            var allFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in allFields) {
                if (Attribute.IsDefined(field, typeof(DataBindAttribute)))
                    fields.Add(field);
            }
            _cache[type] = fields;
        }

        // 遍历字段，从 DataBag 中取值并赋值
        foreach (var field in fields) {
            // 获取特性中指定的键名，如果没有则使用字段名
            var attr = field.GetCustomAttribute<DataBindAttribute>();
            string key = string.IsNullOrEmpty(attr?.SourceField) ? field.Name : attr.SourceField;

            // 从 DataBag 中取值（带类型转换）
            if (data.TryGet(field.FieldType, key, out object value)) {
                field.SetValue(view, value);
            }
        }
    }
}

/// <summary>
/// 标记字段/属性，表示它可以从数据源（DataBag 或 DTO）自动获取值
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public sealed class DataBindAttribute : Attribute {
    /// <summary>
    /// 数据源中的键名/字段名。如果为 null，则使用目标字段名作为键名。
    /// </summary>
    public string SourceField { get; }

    public DataBindAttribute(string sourceField = null) {
        SourceField = sourceField;
    }
}