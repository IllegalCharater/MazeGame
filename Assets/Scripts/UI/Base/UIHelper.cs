using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class UIHelper
{
    public static Text FindText(Component owner, string objectName)
    {
        GameObject go = FindDeep(owner, objectName);
        return go != null ? go.GetComponent<Text>() : null;
    }

    public static Text FindText(GameObject owner, string objectName)
    {
        GameObject go = FindDeep(owner, objectName);
        return go != null ? go.GetComponent<Text>() : null;
    }

    public static Text FindText(Transform root, string objectName)
    {
        GameObject go = FindDeep(root, objectName);
        return go != null ? go.GetComponent<Text>() : null;
    }

    public static Button FindButton(Component owner, string objectName)
    {
        GameObject go = FindDeep(owner, objectName);
        return go != null ? go.GetComponent<Button>() : null;
    }

    public static Button FindButton(GameObject owner, string objectName)
    {
        GameObject go = FindDeep(owner, objectName);
        return go != null ? go.GetComponent<Button>() : null;
    }

    public static Button FindButton(Transform root, string objectName)
    {
        GameObject go = FindDeep(root, objectName);
        return go != null ? go.GetComponent<Button>() : null;
    }

    public static T FindComponent<T>(GameObject owner, string objectName) where T : Component
    {
        GameObject go = FindDeep(owner, objectName);
        return go != null ? go.GetComponent<T>() : null;
    }

    public static T FindComponent<T>(Transform root, string objectName) where T : Component
    {
        GameObject go = FindDeep(root, objectName);
        return go != null ? go.GetComponent<T>() : null;
    }

    public static GameObject FindDeep(Component owner, string objectName)
    {
        return owner != null ? FindDeep(owner.transform, objectName) : null;
    }

    public static GameObject FindDeep(GameObject owner, string objectName)
    {
        return owner != null ? FindDeep(owner.transform, objectName) : null;
    }

    public static GameObject FindDeep(Transform root, string objectName)
    {
        Transform found = FindDeepTransform(root, objectName);
        return found != null ? found.gameObject : null;
    }

    public static Transform FindDeepTransform(Transform root, string objectName)
    {
        if (root == null)
        {
            Debug.LogWarning("root is null");
            return null;
        }
        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepTransform(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }
        
        return null;
    }

    public static void AddClick(Button button, UnityAction action)
    {
        if (button != null && action != null)
            button.onClick.AddListener(action);
    }

    public static void RemoveClick(Button button, UnityAction action)
    {
        if (button != null && action != null)
            button.onClick.RemoveListener(action);
    }

    public static void SetText(Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    public static void ToggleActive(GameObject target)
    {
        if (target != null)
            target.SetActive(!target.activeSelf);
    }
}
