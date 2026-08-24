using System;
using System.Threading.Tasks;

public sealed class GameContext {
    public static GameContext Instance { get; private set; }
    public Context context;

    public GameContext() {
        context = new Context(new EventBus(), new CommandBus());

    }
    public void DispatchEvent(EventType eventType, object[] args = null) {
        //参数要按照顺序传入，尽量不在分发事件时传递参数，且当绑定多个事件时，参数须一致
        context.HandleEvent(eventType, args);
    }
    // 派发 DataBag：直接传 DataBag，无需 object[] 包裹
    public void DispatchEvent(EventType eventType, DataBag data) {
        context.HandleEvent(eventType, new object[] { data });
    }
    public Task<CommandResult> Execute(CommandType commandType, DataBag payload = null) {
        return context.HandleCommand(commandType, payload);
    }


    // 直接传 lambda 时自动包装成具体委托类型
    public void AddEvent(EventType eventType, Action action) {
        context.BindEvent(eventType, action);
    }
    public void AddEvent(EventType eventType, Action<object> action) {
        context.BindEvent(eventType, action);
    }
    // 通用入口：支持任意签名（method group / 已实例化委托）
    public void AddEvent(EventType eventType, Delegate action) {
        context.BindEvent(eventType, action);
    }

    public void RemoveEvent(EventType eventType, Delegate action) {
        context.UnbindEvent(eventType, action);
    }

    public void AddCommand(ICommand command) {
        context.BindCommand(command);
    }

    public void RemoveCommand(CommandType commandType) {
        context.UnbindCommand(commandType);
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
