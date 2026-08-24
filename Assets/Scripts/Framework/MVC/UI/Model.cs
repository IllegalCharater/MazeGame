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

    //预留接口,当ui挂载在动态加载场景中时，为真切换场景保留此ui
    public virtual bool keepAlive => false;
    private readonly Dictionary<Button, UnityAction> clickActions = new Dictionary<Button, UnityAction>();

    // 本 Model 自己注册的事件（事件枚举 -> 委托实例列表），Dispose 时逐个解绑，不触碰共享总线上的其他注册
    private readonly Dictionary<EventType, List<Delegate>> boundEvents = new Dictionary<EventType, List<Delegate>>();
    // 本 Model 自己注册的指令（指令枚举 -> 指令实例），Dispose 时按身份校验后解绑
    private readonly Dictionary<CommandType, ICommand> boundCommands = new Dictionary<CommandType, ICommand>();

    public Model(string viewName, Context context) {
        this.context = context;
        this.viewName = viewName;
        this.state = ViewState.None;
    }
    //初始化，读取配置表数据或者静态数据
    public virtual void Initialize() {

    }
    protected DataBag _data;
    //外部数据入口，动态数据，子类重写同步数据办法
    public void UpdateData(DataBag data) {
        if (data != null) {
            _data = data.Get<DataBag>("Model", null);
        }

        //分发并同步数据
        SyncModel();
    }
    protected virtual void SyncModel() {

    }

    // 触发事件（带参数）
    public void DispatchEvent<T>(EventType eventType, T payload) {
        context.HandleEvent(eventType, new object[] { payload });
    }
    // 触发事件（无参数）
    public void DispatchEvent(EventType eventType) {
        context.HandleEvent(eventType, null);
    }
    // 执行指令（返回结果，可 await 或忽略）
    public Task<CommandResult> ExecuteCommand(CommandType commandType, DataBag payload = null) {
        return context.HandleCommand(commandType, payload);
    }
    // 直接传 lambda 时自动包装成具体委托类型（与 GameContext 用法一致）
    public void AddEvent(EventType eventType, Action action) {
        _AddEvent(eventType, action);
    }
    public void AddEvent(EventType eventType, Action<object> action) {
        _AddEvent(eventType, action);
    }
    // 通用入口：支持任意签名（method group / 已实例化委托）
    public void AddEvent(EventType eventType, Delegate action) {
        _AddEvent(eventType, action);
    }
    private void _AddEvent(EventType eventType, Delegate action) {
        if (action == null)
            return;

        context?.BindEvent(eventType, action);
        if (!boundEvents.TryGetValue(eventType, out List<Delegate> list)) {
            list = new List<Delegate>();
            boundEvents[eventType] = list;
        }
        if (!list.Contains(action))
            list.Add(action);
    }

    public void RemoveEvent(EventType eventType, Delegate action) {
        if (action == null)
            return;

        context?.UnbindEvent(eventType, action);
        if (boundEvents.TryGetValue(eventType, out List<Delegate> list)) {
            list.Remove(action);
            if (list.Count == 0)
                boundEvents.Remove(eventType);
        }
    }

    public void AddCommand(ICommand command) {
        if (command == null)
            return;

        context?.BindCommand(command);
        boundCommands[command.Type] = command;
    }

    public void RemoveCommand(CommandType commandType) {
        // 身份校验：只有"总线上当前仍是我注册的那个实例"才解绑，防止误删他人同名指令
        if (boundCommands.TryGetValue(commandType, out ICommand mine)
            && context != null
            && context.TryGetCommand(commandType, out ICommand current)
            && ReferenceEquals(current, mine))
            context.UnbindCommand(commandType);

        boundCommands.Remove(commandType);
    }

    // 只解绑本 Model 记录的事件，不再清空整个共享总线
    public void RemoveAllEvents() {
        if (context != null) {
            foreach (KeyValuePair<EventType, List<Delegate>> kv in boundEvents) {
                for (int i = 0; i < kv.Value.Count; i++)
                    context.UnbindEvent(kv.Key, kv.Value[i]);
            }
        }
        boundEvents.Clear();
    }

    // 只解绑本 Model 记录的指令（按身份校验），不再清空整个共享总线
    public void RemoveAllCommands() {
        if (context != null) {
            foreach (KeyValuePair<CommandType, ICommand> kv in boundCommands) {
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

    public ConfigData GetConfigData(string rootKey, string id) {
        return GameDatabase.GetConfigData(rootKey, id);
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