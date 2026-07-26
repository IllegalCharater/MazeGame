using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null)
            return;

        Type eventType = typeof(TEvent);
        if (!handlers.TryGetValue(eventType, out List<Delegate> eventHandlers))
        {
            eventHandlers = new List<Delegate>();
            handlers[eventType] = eventHandlers;
        }

        if (!eventHandlers.Contains(handler))
            eventHandlers.Add(handler);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null)
            return;

        Type eventType = typeof(TEvent);
        if (!handlers.TryGetValue(eventType, out List<Delegate> eventHandlers))
            return;

        eventHandlers.Remove(handler);
        if (eventHandlers.Count == 0)
            handlers.Remove(eventType);
    }

    public void Publish<TEvent>(TEvent evt)
    {
        Type eventType = typeof(TEvent);
        if (!handlers.TryGetValue(eventType, out List<Delegate> eventHandlers))
            return;

        Delegate[] snapshot = eventHandlers.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            if (!(snapshot[i] is Action<TEvent> handler))
                continue;

            try
            {
                handler(evt);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    public void Clear()
    {
        handlers.Clear();
    }
}
