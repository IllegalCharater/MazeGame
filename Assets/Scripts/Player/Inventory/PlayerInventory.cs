using System.Collections.Generic;

public class PlayerInventory : IReadOnlyInventory
{
    public string playerId="unknown";
    private readonly Dictionary<string, int> items = new Dictionary<string, int>();

    public void init(string playerId)
    {
        this.playerId = playerId;
        items.Clear();
        //背包数据初始化
        PlayerStartData startStarts = GameDatabase.Instance.Get<PlayerStartData>("player_start", this.playerId);
        if (startStarts != null) ReplaceFrom(startStarts);
    }

    public bool TryAdd(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;

        if (!items.TryGetValue(itemId, out int current))
            current = 0;

        long sum = (long)current + amount;
        if (sum > int.MaxValue)
            return false;

        items[itemId] = (int)sum;
        GameEvents.RaiseInventoryChanged();
        return true;
    }

    public bool TryRemove(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;
        if (!ApplyRemoveSilent(itemId, amount))
            return false;

        GameEvents.RaiseInventoryChanged();
        return true;
    }

    public bool TryConsume(IReadOnlyDictionary<string, int> costs)
    {
        if (costs == null || costs.Count == 0)
            return true;

        foreach (var kv in costs)
        {
            if (kv.Value < 0 || !Has(kv.Key, kv.Value))
                return false;
        }

        foreach (var kv in costs)
        {
            if (kv.Value > 0)
                ApplyRemoveSilent(kv.Key, kv.Value);
        }

        GameEvents.RaiseInventoryChanged();
        return true;
    }

    public int GetAmount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return 0;
        return items.TryGetValue(itemId, out int amount) ? amount : 0;
    }

    public bool Has(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount < 0)
            return false;
        return amount == 0 || GetAmount(itemId) >= amount;
    }

    public bool HasAll(IReadOnlyDictionary<string, int> costs)
    {
        if (costs == null)
            return true;

        foreach (var kv in costs)
        {
            if (kv.Value < 0 || !Has(kv.Key, kv.Value))
                return false;
        }

        return true;
    }

    public IReadOnlyDictionary<string, int> GetSnapshot()
    {
        return new Dictionary<string, int>(items);
    }

    public void ReplaceFrom(PlayerStartData data)
    {
        items.Clear();
        if (data?.items != null)
        {
            foreach (var kv in data.items)
            {
                if (!string.IsNullOrEmpty(kv.Key) && kv.Value > 0)
                    items[kv.Key] = kv.Value;
            }
        }

        GameEvents.RaiseInventoryChanged();
    }

    private bool ApplyRemoveSilent(string itemId, int amount)
    {
        if (!items.TryGetValue(itemId, out int current) || current < amount)
            return false;

        current -= amount;
        if (current <= 0)
            items.Remove(itemId);
        else
            items[itemId] = current;
        return true;
    }
}
