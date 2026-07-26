using System.Collections.Generic;

public sealed class ShopService
{
    private GameDatabase database;
    private PlayerDatabase player;
    private readonly List<ShopDisplaySlot> displaySlots = new List<ShopDisplaySlot>();

    public void Initialize(GameDatabase database, PlayerDatabase player)
    {
        this.database = database;
        this.player = player;
        EnsureDefaultSlots();
    }

    public bool PlaceFood(string foodId, int amount)
    {
        if (string.IsNullOrEmpty(foodId) || amount <= 0 || player?.inventory == null)
            return false;
        if (!player.inventory.TryRemove(foodId, amount))
            return false;

        ShopDisplaySlot slot = displaySlots.Find(s => s.IsEmpty);
        if (slot == null)
        {
            player.inventory.TryAdd(foodId, amount);
            return false;
        }

        slot.foodId = foodId;
        slot.amount = amount;
        slot.isSelling = true;
        GameEvents.RaiseShopChanged();
        return true;
    }

    public bool SellFood(int slotId)
    {
        ShopDisplaySlot slot = displaySlots.Find(s => s.slotId == slotId);
        if (slot == null || slot.IsEmpty || slot.amount <= 0)
            return false;

        int price = ResolvePrice(slot.foodId);
        slot.amount -= 1;
        if (slot.amount <= 0)
        {
            slot.foodId = string.Empty;
            slot.isSelling = false;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.TryAddCurrency(player.playerId,price);
        GameEvents.RaiseShopChanged();
        return true;
    }

    public IReadOnlyList<ShopDisplaySlot> GetDisplaySlots()
    {
        return displaySlots;
    }

    private int ResolvePrice(string foodId)
    {
        FoodData food = database?.Get<FoodData>("foods", foodId);
        if (food != null)
            return food.price;

        ItemData item = database?.Get<ItemData>("items", foodId);
        return item != null ? item.price : 0;
    }

    private void EnsureDefaultSlots()
    {
        if (displaySlots.Count > 0)
            return;

        for (int i = 0; i < 4; i++)
            displaySlots.Add(new ShopDisplaySlot { slotId = i });
    }
}
