public class SellService
{
    public bool SellFromSlot(int slotId)
    {
        return GameManager.Instance != null
            && GameManager.Instance.Services != null
            && GameManager.Instance.Services.Shop.SellFood(slotId);
    }
}
