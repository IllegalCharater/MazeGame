public sealed class ShopDisplaySlot
{
    public int slotId;
    public string foodId;
    public int amount;
    public bool isSelling;

    public bool IsEmpty => string.IsNullOrEmpty(foodId) || amount <= 0;
}
