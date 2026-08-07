using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExtensions {
    /// <summary>
    /// 根据相对路径查找子物体（从当前 GameObject 的 Transform 开始）
    /// </summary>
    public static GameObject GetChildByPath(this GameObject go, string path) {
        if (go == null) {
            Debug.LogWarning("GameObject is null, cannot find child.");
            return null;
        }
        Transform childTransform = UIHelper.FindTransformByPath(go.transform, path);
        if (childTransform == null) {
            Debug.LogWarning($"Child not found at path: {path} in gameobject: {go.name}");
            return null;
        }
        return childTransform.gameObject;
    }

    // 返回所有直接子物体的列表
    public static List<GameObject> GetChildren(this GameObject obj) {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in obj.transform) {
            children.Add(child.gameObject);
        }
        return children;
    }
}