using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EventBus {
    // 存储：事件名 -> 委托列表（支持任意签名）
    private readonly Dictionary<string, List<Delegate>> _handlers = new();
    // 事件绑定：父事件 -> 子事件列表（当父事件触发时，自动发布这些子事件）
    private readonly Dictionary<string, List<string>> _eventBindings = new();

    // ---------- 订阅 ----------
    public void Subscribe(string eventName, Delegate handler) {
        if (!_handlers.TryGetValue(eventName, out var list)) {
            list = new List<Delegate>();
            _handlers[eventName] = list;
        }
        // 检查重复
        if (!list.Contains(handler))
            list.Add(handler);
    }
    // ---------- 绑定父子事件 ----------
    /// <summary>
    /// 将 childEvent 绑定到 parentEvent，当 parentEvent 发布时，会自动发布 childEvent。
    /// 支持多级绑定，但会检测循环依赖。
    /// </summary>
    public void Subscribe(string parentEvent, string childEvent) {
        if (string.IsNullOrEmpty(parentEvent) || string.IsNullOrEmpty(childEvent))
            throw new ArgumentException("Event names cannot be null or empty.");

        // 检测循环依赖：如果 childEvent 已绑定到 parentEvent 链，则拒绝添加
        if (IsCircularDependency(parentEvent, childEvent))
            throw new InvalidOperationException($"Circular event binding detected: {parentEvent} -> {childEvent}");

        if (!_eventBindings.TryGetValue(parentEvent, out var children)) {
            children = new List<string>();
            _eventBindings[parentEvent] = children;
        }

        if (!children.Contains(childEvent))
            children.Add(childEvent);
    }

    // ---------- 取消订阅 ----------
    public void Unsubscribe(string eventName, Delegate handler) {
        if (!_handlers.TryGetValue(eventName, out var list)) return;
        list.Remove(handler);
        if (list.Count == 0) _handlers.Remove(eventName);
    }
    /// <summary>
    /// 移除指定的一对父子绑定
    /// </summary>
    public void Unsubscribe(string parentEvent, string childEvent) {
        if (_eventBindings.TryGetValue(parentEvent, out var children)) {
            children.Remove(childEvent);
            if (children.Count == 0)
                _eventBindings.Remove(parentEvent);
        }
    }
    //取消某个事件的全部订阅
    public void UnsubscribeAll(string eventName) {
        if (_handlers.TryGetValue(eventName, out var list)) {
            list.Clear();
            _handlers.Remove(eventName);
        }
        if (_eventBindings.TryGetValue(eventName, out var eventList)) {
            eventList.Clear();
            _eventBindings.Remove(eventName);
        }
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

    // ---------- 循环依赖检测 ----------
    private bool IsCircularDependency(string parent, string child) {
        // 检测从 child 出发是否能回到 parent
        var visited = new HashSet<string>();
        return IsReachable(child, parent, visited);
    }
    private bool IsReachable(string from, string target, HashSet<string> visited) {
        if (from == target) return true;
        if (!_eventBindings.TryGetValue(from, out var children)) return false;
        if (!visited.Add(from)) return false;

        foreach (var child in children) {
            if (IsReachable(child, target, visited))
                return true;
        }
        return false;
    }

    // ---------- 清空 ----------
    public void Clear() {
        _handlers.Clear();
        _eventBindings.Clear();
    }
}
