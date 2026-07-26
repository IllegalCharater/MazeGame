using System;
using System.Collections.Generic;

public sealed class EcsWorld
{
    private readonly HashSet<int> entities = new HashSet<int>();
    private readonly Dictionary<int, Dictionary<Type, IComponent>> components = new Dictionary<int, Dictionary<Type, IComponent>>();
    private readonly List<ISystem> systems = new List<ISystem>();
    private readonly HashSet<ISystem> initializedSystems = new HashSet<ISystem>();
    private int nextEntityId = 1;
    private bool initialized;

    public void Init()
    {
        if (initialized)
            return;

        initialized = true;
        for (int i = 0; i < systems.Count; i++)
            InitializeSystem(systems[i]);
    }

    public EntityId CreateEntity(string debugName = null)
    {
        EntityId entity = new EntityId(nextEntityId++, debugName);
        entities.Add(entity.value);
        components[entity.value] = new Dictionary<Type, IComponent>();
        return entity;
    }

    public bool DestroyEntity(EntityId entity)
    {
        if (!IsAlive(entity))
            return false;

        components.Remove(entity.value);
        entities.Remove(entity.value);
        return true;
    }

    public bool IsAlive(EntityId entity)
    {
        return entity.IsValid && entities.Contains(entity.value);
    }

    public bool AddComponent<T>(EntityId entity, T component) where T : IComponent
    {
        if (!IsAlive(entity) || object.Equals(component, null))
            return false;

        if (!components.TryGetValue(entity.value, out Dictionary<Type, IComponent> entityComponents))
        {
            entityComponents = new Dictionary<Type, IComponent>();
            components[entity.value] = entityComponents;
        }

        entityComponents[typeof(T)] = component;
        return true;
    }

    public T GetComponent<T>(EntityId entity) where T : IComponent
    {
        return TryGetComponent(entity, out T component) ? component : default;
    }

    public bool TryGetComponent<T>(EntityId entity, out T component) where T : IComponent
    {
        component = default;
        if (!IsAlive(entity))
            return false;
        if (!components.TryGetValue(entity.value, out Dictionary<Type, IComponent> entityComponents))
            return false;
        if (!entityComponents.TryGetValue(typeof(T), out IComponent raw))
            return false;
        if (!(raw is T typed))
            return false;

        component = typed;
        return true;
    }

    public bool HasComponent<T>(EntityId entity) where T : IComponent
    {
        if (!IsAlive(entity))
            return false;
        return components.TryGetValue(entity.value, out Dictionary<Type, IComponent> entityComponents)
            && entityComponents.ContainsKey(typeof(T));
    }

    public bool RemoveComponent<T>(EntityId entity) where T : IComponent
    {
        if (!IsAlive(entity))
            return false;
        if (!components.TryGetValue(entity.value, out Dictionary<Type, IComponent> entityComponents))
            return false;

        return entityComponents.Remove(typeof(T));
    }

    public IReadOnlyList<EntityId> GetEntitiesWith<T>() where T : IComponent
    {
        List<EntityId> result = new List<EntityId>();
        Type componentType = typeof(T);
        foreach (int entityValue in entities)
        {
            if (components.TryGetValue(entityValue, out Dictionary<Type, IComponent> entityComponents)
                && entityComponents.ContainsKey(componentType))
            {
                result.Add(new EntityId(entityValue));
            }
        }

        return result;
    }

    public IReadOnlyList<EntityId> GetEntitiesWith<T1, T2>()
        where T1 : IComponent
        where T2 : IComponent
    {
        List<EntityId> result = new List<EntityId>();
        Type componentType1 = typeof(T1);
        Type componentType2 = typeof(T2);
        foreach (int entityValue in entities)
        {
            if (components.TryGetValue(entityValue, out Dictionary<Type, IComponent> entityComponents)
                && entityComponents.ContainsKey(componentType1)
                && entityComponents.ContainsKey(componentType2))
            {
                result.Add(new EntityId(entityValue));
            }
        }

        return result;
    }

    public void RegisterSystem(ISystem system)
    {
        if (system == null || systems.Contains(system))
            return;

        systems.Add(system);
        if (initialized)
            InitializeSystem(system);
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        ISystem[] snapshot = systems.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            snapshot[i].Tick(deltaTime);
    }

    public void Dispose()
    {
        for (int i = systems.Count - 1; i >= 0; i--)
            systems[i].Dispose();

        initializedSystems.Clear();
        systems.Clear();
        components.Clear();
        entities.Clear();
        initialized = false;
    }

    private void InitializeSystem(ISystem system)
    {
        if (initializedSystems.Contains(system))
            return;

        system.Initialize(this);
        initializedSystems.Add(system);
    }
}
