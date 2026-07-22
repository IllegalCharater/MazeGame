using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BaseUIController
{
    public string viewName = "null";
    public virtual string layerName => UIConfig.NormalLayerName;
    public virtual bool hasInputBlocker => false;
    
    public GameObject root;

    private readonly Dictionary<Button, UnityAction> clickActions = new Dictionary<Button, UnityAction>();

    public virtual void Initialize(string viewName, GameObject root)
    {
        this.viewName = viewName;
        this.root = root;
        BindUI();
        EventMapper();
        OnOpen();
    }

    public virtual void OnOpen()
    {
    }

    public virtual void OnRefresh()
    {
        
    }

    public virtual void Dismiss()
    {
        ClearAllClickEvent();
    }

    public virtual void BindUI()
    {
    }

    public virtual void EventMapper()
    {
    }

    public void ClickMapper(Dictionary<Button, UnityAction> buttonMap)
    {
        if (buttonMap == null)
            return;

        foreach (KeyValuePair<Button, UnityAction> kv in buttonMap)
            BindClickEvent(kv.Key, kv.Value);
    }

    protected GameObject FindObject(string objectName)
    {
        
        return UIHelper.FindDeep(root, objectName);
    }

    protected Text FindText(string objectName)
    {
        return UIHelper.FindText(root, objectName);
    }

    protected Button FindButton(string objectName)
    {
        return UIHelper.FindButton(root, objectName);
    }

    protected T FindComponent<T>(GameObject ob) where T : Component
    {
        return ob.GetComponent<T>();
    }
    
    protected T FindComponent<T>(string objectName) where T : Component
    {
        return UIHelper.FindComponent<T>(root, objectName);
    }
    
    protected T FindComponent<T>(GameObject ob,string objectName) where T : Component
    {
        return UIHelper.FindComponent<T>(ob, objectName);
    }

    protected void BindClickEvent(Button button, UnityAction action)
    {
        if (button == null || action == null)
            return;

        if (clickActions.TryGetValue(button, out UnityAction oldAction) && oldAction != null)
            button.onClick.RemoveListener(oldAction);

        clickActions[button] = action;
        button.onClick.AddListener(action);
    }

    protected void ClearClickEvent(Button button)
    {
        if (button == null || !clickActions.TryGetValue(button, out UnityAction action))
            return;

        if (action != null)
            button.onClick.RemoveListener(action);
        clickActions.Remove(button);
    }

    protected void ClearAllClickEvent()
    {
        foreach (KeyValuePair<Button, UnityAction> kv in new Dictionary<Button, UnityAction>(clickActions))
        {
            if (kv.Key != null && kv.Value != null)
                kv.Key.onClick.RemoveListener(kv.Value);
        }

        clickActions.Clear();
    }
}
