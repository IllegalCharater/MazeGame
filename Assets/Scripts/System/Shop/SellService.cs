public class SellService {
    public bool SellFromSlot(int slotId) {
        return GameManager.Instance != null
            && GameManager.Instance.services != null
            && GameManager.Instance.services.Shop != null
            && GameManager.Instance.services.Shop.SellFood(slotId);
    }
}
