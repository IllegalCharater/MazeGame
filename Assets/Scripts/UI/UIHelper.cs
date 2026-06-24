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

    public static Button FindButton(Component owner, string objectName)
    {
        GameObject go = FindDeep(owner, objectName);
        return go != null ? go.GetComponent<Button>() : null;
    }

    public static GameObject FindDeep(Component owner, string objectName)
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
            return null;
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
