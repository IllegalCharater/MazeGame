using System;
using System.Threading.Tasks;

public sealed class GameContext {
    public static GameContext Instance { get; private set; }
    public Context context;

    public static GameContext GetInstance() {
        if (Instance == null) {
            Instance = new GameContext();
        }
        return Instance;
    }
    private GameContext() {
        context = new Context(new EventBus(), new CommandBus());
    }
    public static void DispatchEvent(EventType eventType, object[] args = null) {
        //参数要按照顺序传入，尽量不在分发事件时传递参数，且当绑定多个事件时，参数须一致
        Instance.context.HandleEvent(eventType, args);
    }
    // 派发 DataBag：直接传 DataBag，无需 object[] 包裹
    public static void DispatchEvent(EventType eventType, DataBag data) {
        Instance.context.HandleEvent(eventType, new object[] { data });
    }
    public static Task<CommandResult> Execute(CommandType commandType, DataBag payload = null) {
        return Instance.context.HandleCommand(commandType, payload);
    }


    // 直接传 lambda 时自动包装成具体委托类型
    public static void AddEvent(EventType eventType, Action action) {
        Instance.context.BindEvent(eventType, action);
    }
    public static void AddEvent(EventType eventType, Action<object> action) {
        Instance.context.BindEvent(eventType, action);
    }
    // 通用入口：支持任意签名（method group / 已实例化委托）
    public static void AddEvent(EventType eventType, Delegate action) {
        Instance.context.BindEvent(eventType, action);
    }

    public static void RemoveEvent(EventType eventType, Delegate action) {
        Instance.context.UnbindEvent(eventType, action);
    }

    public static void AddCommand(ICommand command) {
        Instance.context.BindCommand(command);
    }

    public static void RemoveCommand(CommandType commandType) {
        Instance.context.UnbindCommand(commandType);
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
