using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class Model : IDisposable {
    public Context context { get; private set; }
    public string viewName;
    public ViewState state;
    public virtual string Layer => UIConfig.NormalLayerName;
    public virtual bool HasInputBlocker => false;
    public virtual bool keepAlive => false;
    private readonly Dictionary<Button, UnityAction> clickActions = new Dictionary<Button, UnityAction>();
    public Model(string viewName, Context context) {
        this.context = context;
        this.viewName = viewName;
        this.state = ViewState.None;
    }
    public void AddEvent(string eventName, Delegate action) {
        context.BindEvent(eventName, action);
    }

    public void RemoveEvent(string eventName, Delegate action) {
        context.UnbindEvent(eventName, action);
    }

    public void AddCommand(string commandName, ICommand command) {
        context.BindCommand(commandName, command);
    }

    public void RemoveCommand(string commandName) {
        context.UnbindCommand(commandName);
    }

    public void RemoveAllEvents() {
        context.ClearAllEvents();
    }

    public void RemoveAllCommands() {
        context.ClearAllCommands();
    }
    public void AddClickEvent(Button button, UnityAction action) {
        UIHelper.BindClickEvent(button, action, clickActions);
    }

    public void RemoveClickEvent(Button button) {
        UIHelper.ClearClickEvent(button, clickActions);
    }

    public void RemoveAllClickEvents() {
        UIHelper.ClearAllClickEvent(clickActions);
    }

    public T GetConfigData<T>(string rootKey, string id) where T : BaseData {
        return GameDatabase.GetInstance().Get<T>(rootKey, id);
    }

    public virtual void Initialize() {

    }
    public virtual void Dispose() {
        UIHelper.ClearAllClickEvent(clickActions);
        context?.Dispose();
        context = null;
    }
}