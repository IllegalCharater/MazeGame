using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public sealed class Injector {
    private static Injector _instance;
    public static Injector Instance {
        get {
            if (_instance == null) {
                _instance = new Injector();
                // 如果容器为空时被访问，自动初始化（防御性编程）
                // Debug.LogWarning("IOCContainer accessed before initialization. Auto-creating empty container.");
            }
            return _instance;
        }
    }
    private Injector() { }
    // ---------- 原有实例缓存（单例/已注册实例） ----------
    private readonly Dictionary<Type, object> singletonInstances = new Dictionary<Type, object>();
    // ---------- 新增：类型映射（类型 -> 如何创建） ----------
    private readonly Dictionary<Type, Func<object>> typeCreators = new Dictionary<Type, Func<object>>();
    private readonly Dictionary<Type, Func<object[], object>> argCreators = new();
    // ---------- 生命周期选项 ----------
    public enum Lifecycle {
        Singleton,   // 全局唯一实例
        Transient    // 每次解析新建
    }

    // 1. 注册已存在的实例（单例）
    public void Register<T>(T instance) {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        singletonInstances[typeof(T)] = instance;
    }
    // 2. 注册类型映射（指定如何创建，支持自定义工厂）
    public void Register<T>(Func<T> factory, Lifecycle lifecycle = Lifecycle.Transient) {
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        typeCreators[typeof(T)] = () => {
            var instance = factory();
            if (lifecycle == Lifecycle.Singleton) {
                // 如果是单例，创建后存入缓存，并返回缓存中的实例
                if (!singletonInstances.ContainsKey(typeof(T)))
                    singletonInstances[typeof(T)] = instance;
                return singletonInstances[typeof(T)];
            }
            return instance;
        };
    }
    // 带参工厂注册
    public void Register<T>(Func<object[], T> factory, Lifecycle lifecycle = Lifecycle.Transient) {
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        Func<object[], object> wrapped = (args) => {
            var instance = factory(args);
            if (lifecycle == Lifecycle.Singleton) {
                if (!singletonInstances.ContainsKey(typeof(T)))
                    singletonInstances[typeof(T)] = instance;
                return singletonInstances[typeof(T)];
            }
            return instance;
        };
        argCreators[typeof(T)] = wrapped;
    }

    public object Resolve(Type type, object[] args = null) {
        return resolve(type, args);
    }
    public T Resolve<T>(object[] args = null) {
        return (T)resolve(typeof(T));
    }
    // 支持传入构造参数的重载
    private object CreateInstanceWithArgs(Type type, object[] providedArgs) {
        // 获取所有公共构造函数，按参数数量从多到少排序（优先匹配参数最多的）
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                               .OrderByDescending(c => c.GetParameters().Length)
                               .ToArray();

        if (constructors.Length == 0)
            throw new InvalidOperationException($"No public constructor found for {type.Name}");

        // 遍历构造函数，尝试找到能匹配传入参数的那个
        foreach (var ctor in constructors) {
            var parameters = ctor.GetParameters();
            var args = new object[parameters.Length];
            bool canUse = true;
            int providedCount = providedArgs?.Length ?? 0;

            for (int i = 0; i < parameters.Length; i++) {
                // 策略：如果外部传入了参数（按顺序匹配），则优先使用外部参数
                if (i < providedCount) {
                    // 检查类型是否匹配（或可赋值）
                    var provided = providedArgs[i];
                    if (provided != null && !parameters[i].ParameterType.IsAssignableFrom(provided.GetType())) {
                        canUse = false; // 类型不匹配，换下一个构造函数尝试
                        break;
                    }
                    args[i] = provided;
                }
                else {
                    // 外部参数不够，剩下的参数从容器自动解析
                    try {
                        args[i] = Resolve(parameters[i].ParameterType); // 递归调用（无参）
                    }
                    catch {
                        canUse = false; // 容器无法提供该依赖，换构造函数
                        break;
                    }
                }
            }

            if (canUse) {
                return ctor.Invoke(args);
            }
        }

        // 如果所有构造函数都无法匹配
        throw new InvalidOperationException(
            $"Cannot create instance of {type.Name}. Ensure constructor parameters are registered or provided.");
    }
    // ---------- 核心：构造函数注入创建实例 ----------
    private object CreateInstance(Type type) {
        // 获取最优构造函数（默认选参数最多的，可根据需要自定义）
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var ctor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

        if (ctor == null)
            throw new InvalidOperationException($"No public constructor found for {type.Name}");

        var parameters = ctor.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++) {
            var paramType = parameters[i].ParameterType;
            // 递归解析依赖
            args[i] = resolve(paramType);
        }

        return ctor.Invoke(args);
    }
    private object resolve(Type type, params object[] constructorArgs) {
        if (type == null) throw new ArgumentNullException(nameof(type));

        // 检查单例缓存（单例不受构造参数影响，直接返回缓存）
        if (singletonInstances.TryGetValue(type, out object cached))
            return cached;

        // 如果有传入参数，优先尝试带参工厂
        bool hasArgs = constructorArgs != null && constructorArgs.Length > 0;
        if (hasArgs && argCreators.TryGetValue(type, out var argCreator)) {
            return argCreator(constructorArgs);
        }
        //检查自定义工厂（工厂忽略了传入的参数，因为工厂自行决定创建逻辑）
        if (typeCreators.TryGetValue(type, out Func<object> creator))
            return creator();

        // 若未注册，则通过构造函数反射创建（此处传入额外参数）
        if (!type.IsAbstract && !type.IsInterface) {
            if (hasArgs) {
                return CreateInstanceWithArgs(type, constructorArgs);
            }
            else {
                // 无参数时，使用默认的无参创建逻辑
                return CreateInstance(type);
            }
        }

        throw new InvalidOperationException($"Cannot resolve type {type.Name}");
    }

    public void Clear() {
        singletonInstances.Clear();
        typeCreators.Clear();
    }
}
