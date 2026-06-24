using UnityEngine;

[DisallowMultipleComponent]
public sealed class Pickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemId = "ingredient_carrot";
    [SerializeField] private int amount = 1;
    [SerializeField] private PickupType pickupType = PickupType.MazeRunLoot;
    [SerializeField] private bool destroyOnPickup = true;

    public void Configure(string itemId, int amount, PickupType pickupType = PickupType.MazeRunLoot)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            return;

        this.itemId = itemId;
        this.amount = amount;
        this.pickupType = pickupType;
    }

    public void Interact(GameObject interactor)
    {
        bool success = TryPickup();
        if (success && destroyOnPickup)
            Destroy(gameObject);
    }

    private bool TryPickup()
    {
        if (GameManager.Instance == null || string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;

        if (pickupType == PickupType.MazeRunLoot && GameManager.Instance.Services?.MazeRun != null)
            return GameManager.Instance.Services.MazeRun.CollectItem(itemId, amount);

        return GameManager.Instance.gameDatabase.GetPlayerData()?.inventory != null
            && GameManager.Instance.gameDatabase.GetPlayerData().inventory.TryAdd(itemId, amount);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        amount = Mathf.Max(1, amount);
    }
#endif
}
