using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class UIHelper {

    /// <summary>
    /// 按路径精确查找子物体（使用 Unity 内置 Transform.Find）
    /// </summary>
    /// <param name="root">查找的根节点</param>
    /// <param name="path">路径，例如 "Canvas/MainPanel/Button"</param>
    /// <returns>找到的 Transform，否则 null</returns>
    public static Transform FindTransformByPath(Transform root, string path) {
        if (root == null || string.IsNullOrEmpty(path))
            return null;

        // 如果路径以 "/" 开头，则从场景根开始查找；否则从当前 root 开始相对查找
        if (path.StartsWith("/"))
            return root.root.Find(path.TrimStart('/'));
        else
            return root.Find(path);
    }

    public static void SetText(Text text, object value) {
        if (text != null)
            text.text = value?.ToString() ?? string.Empty;
    }

    //绑定按键事件
    public static void BindClickEvent(Button button, UnityAction action, Dictionary<Button, UnityAction> clickActions) {
        if (button == null || action == null)
            return;

        if (clickActions.TryGetValue(button, out UnityAction oldAction) && oldAction != null)
            button.onClick.RemoveListener(oldAction);

        clickActions[button] = action;
        button.onClick.AddListener(action);
    }

    public static void ClearClickEvent(Button button, Dictionary<Button, UnityAction> clickActions) {
        if (button == null || !clickActions.TryGetValue(button, out UnityAction action))
            return;

        if (action != null)
            button.onClick.RemoveListener(action);
        clickActions.Remove(button);
    }

    public static void ClearAllClickEvent(Dictionary<Button, UnityAction> clickActions) {
        foreach (KeyValuePair<Button, UnityAction> kv in new Dictionary<Button, UnityAction>(clickActions)) {
            if (kv.Key != null && kv.Value != null)
                kv.Key.onClick.RemoveListener(kv.Value);
        }

        clickActions.Clear();
    }

    public static GameObject GetInputBlocker(GameObject parent) {
        RectTransform parentRect = parent.transform as RectTransform;
        if (parentRect == null)
            return null;

        Transform existing = parent.transform.Find("UIInputBlocker");
        GameObject blocker = existing != null ? existing.gameObject : null;
        if (blocker == null) {
            blocker = new GameObject("UIInputBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blocker.transform.SetParent(parent.transform, false);
        }

        RectTransform rect = blocker.transform as RectTransform;
        if (rect != null) {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        Image image = blocker.GetComponent<Image>();
        if (image != null) {
            image.color = new Color(0f, 0f, 0f, 0.001f);
            image.raycastTarget = true;
        }

        return blocker;
    }

}
