using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EventBus {
    // 存储：事件名 -> 委托列表（支持任意签名）
    private readonly Dictionary<string, List<Delegate>> _handlers = new();

    // ---------- 订阅 ----------
    public void Subscribe(string eventName, Delegate handler) {
        if (!_handlers.TryGetValue(eventName, out var list)) {
            list = new List<Delegate>();
            _handlers[eventName] = list;
        }
        // 不检查重复（如果需要，可自行添加）
        if (!list.Contains(handler))
            list.Add(handler);
    }

    // ---------- 取消订阅 ----------
    public void Unsubscribe(string eventName, Delegate handler) {
        if (!_handlers.TryGetValue(eventName, out var list)) return;
        list.Remove(handler);
        if (list.Count == 0) _handlers.Remove(eventName);
    }

    // ---------- 发布 ----------
    public void Publish(string eventName, object[] args) {
        if (string.IsNullOrEmpty(eventName)) return;
        if (!_handlers.TryGetValue(eventName, out var list)) return;

        var parameters = args ?? Array.Empty<object>(); // 防止 null
        // 快照，防止遍历时修改
        var snapshot = list.ToArray();
        foreach (var handler in snapshot) {
            try {
                handler.DynamicInvoke(parameters);
            }
            catch (Exception ex) {
                Debug.LogError($"EventBus 发布 {eventName} 异常: {ex.Message}");
            }
        }
    }

    // ---------- 清空 ----------
    public void Clear() {
        _handlers.Clear();
    }
}
