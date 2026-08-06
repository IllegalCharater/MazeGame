using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    // 本 Model 自己注册的事件（事件名 -> 委托实例列表），Dispose 时逐个解绑，不触碰共享总线上的其他注册
    private readonly Dictionary<string, List<Delegate>> boundEvents = new Dictionary<string, List<Delegate>>();
    // 本 Model 自己注册的指令（指令名 -> 指令实例），Dispose 时按身份校验后解绑
    private readonly Dictionary<string, ICommand> boundCommands = new Dictionary<string, ICommand>();

    public Model(string viewName, Context context) {
        this.context = context;
        this.viewName = viewName;
        this.state = ViewState.None;
    }
    //初始化
    public virtual void Initialize() {

    }
    protected DataBag _data = new DataBag();
    //外部数据入口
    public void UpdateData(DataBag data) {
        _data = data.Get<DataBag>("Model", null);

        //分发并同步数据
        SyncData();
    }
    protected virtual void SyncData() {

    }

    // 触发事件（带参数）
    public void DispatchEvent<T>(string eventName, T payload) {
        context.HandleEvent(eventName, new object[] { payload });
    }
    // 触发事件（无参数）
    public void DispatchEvent(string eventName) {
        context.HandleEvent(eventName, null);
    }
    // 执行指令（返回结果，可 await 或忽略）
    public Task<CommandResult> ExecuteCommand(string commandName, DataBag payload = null) {
        return context.HandleCommand(commandName, payload);
    }
    // 直接传 lambda 时自动包装成具体委托类型（与 GameContext 用法一致）
    public void AddEvent(string eventName, Action action) {
        _AddEvent(eventName, action);
    }
    public void AddEvent(string eventName, Action<object> action) {
        _AddEvent(eventName, action);
    }
    // 通用入口：支持任意签名（method group / 已实例化委托）
    public void AddEvent(string eventName, Delegate action) {
        _AddEvent(eventName, action);
    }
    private void _AddEvent(string eventName, Delegate action) {
        if (string.IsNullOrEmpty(eventName) || action == null)
            return;

        context?.BindEvent(eventName, action);
        if (!boundEvents.TryGetValue(eventName, out List<Delegate> list)) {
            list = new List<Delegate>();
            boundEvents[eventName] = list;
        }
        if (!list.Contains(action))
            list.Add(action);
    }

    public void RemoveEvent(string eventName, Delegate action) {
        if (string.IsNullOrEmpty(eventName) || action == null)
            return;

        context?.UnbindEvent(eventName, action);
        if (boundEvents.TryGetValue(eventName, out List<Delegate> list)) {
            list.Remove(action);
            if (list.Count == 0)
                boundEvents.Remove(eventName);
        }
    }

    public void AddCommand(ICommand command, string commandName = null) {
        if (command == null)
            return;

        context?.BindCommand(command, commandName);
        string resolvedName = string.IsNullOrEmpty(commandName) ? command.Name : commandName;
        boundCommands[resolvedName] = command;
    }

    public void RemoveCommand(string commandName) {
        if (string.IsNullOrEmpty(commandName))
            return;

        // 身份校验：只有"总线上当前仍是我注册的那个实例"才解绑，防止误删他人同名指令
        if (boundCommands.TryGetValue(commandName, out ICommand mine)
            && context != null
            && context.TryGetCommand(commandName, out ICommand current)
            && ReferenceEquals(current, mine))
            context.UnbindCommand(commandName);

        boundCommands.Remove(commandName);
    }

    // 只解绑本 Model 记录的事件，不再清空整个共享总线
    public void RemoveAllEvents() {
        if (context != null) {
            foreach (KeyValuePair<string, List<Delegate>> kv in boundEvents) {
                for (int i = 0; i < kv.Value.Count; i++)
                    context.UnbindEvent(kv.Key, kv.Value[i]);
            }
        }
        boundEvents.Clear();
    }

    // 只解绑本 Model 记录的指令（按身份校验），不再清空整个共享总线
    public void RemoveAllCommands() {
        if (context != null) {
            foreach (KeyValuePair<string, ICommand> kv in boundCommands) {
                if (context.TryGetCommand(kv.Key, out ICommand current) && ReferenceEquals(current, kv.Value))
                    context.UnbindCommand(kv.Key);
            }
        }
        boundCommands.Clear();
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
        return GameDatabase.Instance.Get<T>(rootKey, id);
    }

    public PlayerDatabase GetPlayerData() {
        return GameDatabase.Instance.GetPlayerData();
    }

    public virtual void Dispose() {
        RemoveAllEvents();
        RemoveAllCommands();
        UIHelper.ClearAllClickEvent(clickActions);
        // 不再 Dispose 共享的全局 context，只解除本 Model 自己注册的条目
        context = null;
    }
}