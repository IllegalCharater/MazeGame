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
}