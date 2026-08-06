
using System.Collections.Generic;

public static class DataBagHelper {

    // ========== 字典 ==========
    /// <summary> 比较字典内容，若不同则替换引用，返回是否变更 </summary>
    public static bool UpdateDictionary<TKey, TValue>(
        ref Dictionary<TKey, TValue> target,
        Dictionary<TKey, TValue> source,
        IEqualityComparer<TValue> valueComparer = null) {
        if (target == null || source == null) return false;
        if (ReferenceEquals(target, source)) return false;

        // 数量不同则直接认为不同
        if (target.Count != source.Count) {
            target.Clear();
            foreach (var kvp in source)
                target.Add(kvp.Key, kvp.Value);
            return true;
        }

        // 逐项比较
        valueComparer ??= EqualityComparer<TValue>.Default;
        bool hasChanged = false;
        // 检查 target 中的键值是否与 source 一致，同时收集需要删除的键
        var keysToRemove = new List<TKey>();
        foreach (var kvp in target) {
            if (!source.TryGetValue(kvp.Key, out TValue val) || !valueComparer.Equals(val, kvp.Value)) {
                keysToRemove.Add(kvp.Key);
                hasChanged = true;
            }
        }

        // 检查 source 中是否有 target 没有的键（需要添加）
        if (!hasChanged) {
            // 如果所有键都在且值相同，则无需变化
            foreach (var kvp in source) {
                if (!target.TryGetValue(kvp.Key, out TValue val) || !valueComparer.Equals(val, kvp.Value)) {
                    hasChanged = true;
                    break;
                }
            }
        }

        if (!hasChanged)
            return false;

        // 执行更新：删除多余的键，更新变化的键，添加新键
        // 先删除
        foreach (var key in keysToRemove)
            target.Remove(key);

        // 再添加/更新（注意：source 中的键可能已在 target 中，但值不同，需要更新）
        foreach (var kvp in source) {
            if (target.TryGetValue(kvp.Key, out TValue existing)) {
                // 如果值不同则更新
                if (!valueComparer.Equals(existing, kvp.Value))
                    target[kvp.Key] = kvp.Value;
            }
            else {
                target.Add(kvp.Key, kvp.Value);
            }
        }

        return true;
    }

    /// <summary> 比较列表内容（忽略顺序），若不同则原地清空并复制，返回是否变更 </summary>
    public static bool UpdateList<T>(
        List<T> target,
        List<T> source,
        IEqualityComparer<T> comparer = null) {
        if (target == null || source == null) return false;
        if (ReferenceEquals(target, source)) return false;
        //长度比较
        if (target.Count != source.Count) {
            target.Clear();
            target.AddRange(source);
            return true;
        }

        comparer ??= EqualityComparer<T>.Default;
        // 使用字典计数比较内容
        var countMap = new Dictionary<T, int>(comparer);
        foreach (var item in target) {
            if (countMap.TryGetValue(item, out int c))
                countMap[item] = c + 1;
            else
                countMap[item] = 1;
        }

        foreach (var item in source) {
            if (!countMap.TryGetValue(item, out int c) || c == 0) {
                // 内容不同，直接清空复制
                target.Clear();
                target.AddRange(source);
                return true;
            }
            countMap[item] = c - 1;
        }

        // 检查是否所有计数归零（防止源比目标少的情况）
        foreach (var kvp in countMap) {
            if (kvp.Value != 0) {
                target.Clear();
                target.AddRange(source);
                return true;
            }
        }

        // 内容相同
        return false;
    }
}