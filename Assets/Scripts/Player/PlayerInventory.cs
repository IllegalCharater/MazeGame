using System.Collections.Generic;

public class PlayerInventory : BaseData {
    private string playerId = "unknown";
    private Dictionary<string, int> items;

    public void Init(string playerId) {
        this.playerId = playerId;
        items = new();
    }
    protected override void LoadConfigData() {
        var _data = configs["player_start"].GetData(playerId);
        if (_data != null)
            items = _data.Get("items", items);
    }
    protected override void LoadUpdateData() {
        // string _id = databag.Get("playerId", playerId);
        Dictionary<string, int> _items = databag.Get("items", items);
        DataBagHelper.UpdateDictionary(ref items, _items);
    }

    public bool TryAdd(string itemId, int amount) {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;

        if (!items.TryGetValue(itemId, out int current))
            current = 0;

        long sum = (long)current + amount;
        if (sum > int.MaxValue)
            return false;

        items[itemId] = (int)sum;
        return true;
    }

    public bool TryRemove(string itemId, int amount) {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;
        if (!ApplyRemoveSilent(itemId, amount))
            return false;
        return true;
    }

    public bool TryConsume(IReadOnlyDictionary<string, int> costs) {
        if (costs == null || costs.Count == 0)
            return true;

        foreach (var kv in costs) {
            if (kv.Value < 0 || !Has(kv.Key, kv.Value))
                return false;
        }

        foreach (var kv in costs) {
            if (kv.Value > 0)
                ApplyRemoveSilent(kv.Key, kv.Value);
        }

        return true;
    }

    public int GetAmount(string itemId) {
        if (string.IsNullOrEmpty(itemId))
            return 0;
        return items.TryGetValue(itemId, out int amount) ? amount : 0;
    }

    public bool Has(string itemId, int amount) {
        if (string.IsNullOrEmpty(itemId) || amount < 0)
            return false;
        return amount == 0 || GetAmount(itemId) >= amount;
    }

    public bool HasAll(IReadOnlyDictionary<string, int> costs) {
        if (costs == null)
            return true;

        foreach (var kv in costs) {
            if (kv.Value < 0 || !Has(kv.Key, kv.Value))
                return false;
        }

        return true;
    }

    public IReadOnlyDictionary<string, int> GetSnapshot() {
        return items;
    }

    private bool ApplyRemoveSilent(string itemId, int amount) {
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
