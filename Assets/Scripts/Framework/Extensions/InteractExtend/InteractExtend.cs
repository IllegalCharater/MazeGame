using UnityEngine;

public static class InteractExtend {
    // 通用挂载方法，泛型约束确保只挂载交互组件
    public static T Attach<T>(GameObject target, DataBag initOptions = null) where T : MonoBehaviour, Interactable {
        if (target == null) return null;

        // 先尝试获取，避免重复挂载
        T component = target.GetComponent<T>();
        if (component == null) {
            component = target.AddComponent<T>();
        }

        // 初始化回调（用于设置参数，比如长按时间）
        component.Init(initOptions);
        return component;
    }

    // 更便捷的销毁封装（注意，不能直接 Destroy 正在处理事件的对象，建议在合适时机）
    public static void Detach<T>(GameObject target) where T : MonoBehaviour, Interactable {
        if (target == null) return;
        T component = target.GetComponent<T>();
        if (component != null) {
            component.Dispose();
            // 注意：如果正在拖拽中，直接 Destroy 可能会导致报错，建议在 OnEndDrag 里做标记，或者在 Detach 前先禁用
            Object.Destroy(component);
        }
    }
}