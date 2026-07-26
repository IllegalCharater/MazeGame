using System;
using System.Collections.Generic;

public sealed class IOCContainer
{
    private readonly Dictionary<Type, object> instances = new Dictionary<Type, object>();

    public void Register<T>(T instance)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        instances[typeof(T)] = instance;
    }

    public bool TryResolve<T>(out T instance)
    {
        if (instances.TryGetValue(typeof(T), out object value) && value is T typed)
        {
            instance = typed;
            return true;
        }

        instance = default;
        return false;
    }

    public T Resolve<T>()
    {
        if (TryResolve(out T instance))
            return instance;

        throw new InvalidOperationException("No instance registered for " + typeof(T).Name + ".");
    }

    public void Clear()
    {
        instances.Clear();
    }
}
