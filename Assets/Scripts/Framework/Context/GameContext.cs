using System;
using System.Threading.Tasks;

public sealed class GameContext {
    public static GameContext Instance { get; private set; }
    public Context context;

    public GameContext() {
        context = new Context(new EventBus(), new CommandBus());

    }
    public void DispatchEvent(string eventName, object[] args = null) {
        //参数要按照顺序传入，尽量不在分发事件时传递参数，且当绑定多个事件时，参数须一致
        context.HandleEvent(eventName, args);
    }
    public Task<CommandResult> Execute(string commandName, DataBag payload = null) {
        return context.HandleCommand(commandName, payload);
    }


    // 直接传 lambda 时自动包装成具体委托类型
    public void AddEvent(string eventName, Action action) {
        context.BindEvent(eventName, action);
    }
    public void AddEvent(string eventName, Action<object> action) {
        context.BindEvent(eventName, action);
    }
    // 通用入口：支持任意签名（method group / 已实例化委托）
    public void AddEvent(string eventName, Delegate action) {
        context.BindEvent(eventName, action);
    }

    public void RemoveEvent(string eventName, Delegate action) {
        context.UnbindEvent(eventName, action);
    }

    public void AddCommand(ICommand command, string commandName = null) {
        context.BindCommand(command, commandName);
    }

    public void RemoveCommand(string commandName) {
        context.UnbindCommand(commandName);
    }

    public void Init() {
        Instance = this;
        Injector.Instance.Register(this);
    }
    public void Dispose() {
        context.Dispose();
        if (Instance == this)
            Instance = null;
    }
}
