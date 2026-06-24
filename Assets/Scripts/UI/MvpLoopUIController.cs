using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MvpLoopUIController : MonoBehaviour
{
    [Header("MVP Defaults")]
    [SerializeField] private string defaultLootItemId = "ingredient_carrot";
    [SerializeField] private int defaultLootAmount = 2;
    [SerializeField] private string secondaryLootItemId = "ingredient_salt";
    [SerializeField] private int secondaryLootAmount = 1;
    [SerializeField] private string defaultRecipeId = "recipe1";
    [SerializeField] private string defaultFoodId = "food_carrot_soup";
    [SerializeField] private int defaultFoodAmount = 1;

    [Header("Optional UI")]
    [SerializeField] private Text statusText;

    private string lastMessage = "Ready.";

    private void OnEnable()
    {
        GameEvents.OnInventoryChanged += RefreshStatus;
        GameEvents.OnCraftingChanged += RefreshStatus;
        GameEvents.OnShopChanged += RefreshStatus;
        GameEvents.OnMazeRunEnded += OnMazeRunEnded;
        RefreshStatus();
    }

    private void OnDisable()
    {
        GameEvents.OnInventoryChanged -= RefreshStatus;
        GameEvents.OnCraftingChanged -= RefreshStatus;
        GameEvents.OnShopChanged -= RefreshStatus;
        GameEvents.OnMazeRunEnded -= OnMazeRunEnded;
    }

    public void BeginMazeRun()
    {
        if (!TryGetServices(out GameServices services))
            return;

        bool success = services.MazeRun.BeginRun();
        SetMessage(success ? "Maze run started." : "Not enough energy to enter the maze.");
    }

    public void CollectDefaultLoot()
    {
        if (!TryGetServices(out GameServices services))
            return;

        bool success = services.MazeRun.CollectItem(defaultLootItemId, defaultLootAmount);
        if (success && !string.IsNullOrEmpty(secondaryLootItemId) && secondaryLootAmount > 0)
            success = services.MazeRun.CollectItem(secondaryLootItemId, secondaryLootAmount);

        SetMessage(success
            ? "Collected default ingredients in current maze run."
            : "Failed to collect default loot.");
    }

    public void CompleteMazeRun()
    {
        if (!TryGetServices(out GameServices services))
            return;

        MazeRunResult result = services.MazeRun.CompleteRun();
        SetMessage(result != null ? $"Maze completed: {result.reason}" : "Failed to complete maze.");
    }

    public void CraftDefaultRecipe()
    {
        if (!TryGetServices(out GameServices services))
            return;

        CraftingJob job = services.Crafting.StartCraft(defaultRecipeId);
        SetMessage(job != null
            ? $"Crafting started: {defaultRecipeId} ({job.jobId})"
            : $"Cannot craft {defaultRecipeId}.");
    }

    public void CompleteFirstCraftJob()
    {
        if (!TryGetServices(out GameServices services))
            return;

        IReadOnlyList<CraftingJob> queue = services.Crafting.GetQueue();
        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i] == null || queue[i].completed)
                continue;

            bool success = services.Crafting.CompleteCraft(queue[i].jobId);
            SetMessage(success ? $"Crafting completed: {queue[i].recipeId}" : "Failed to complete crafting job.");
            return;
        }

        SetMessage("No pending crafting job.");
    }

    public void PlaceDefaultFood()
    {
        if (!TryGetServices(out GameServices services))
            return;

        bool success = services.Shop.PlaceFood(defaultFoodId, defaultFoodAmount);
        SetMessage(success
            ? $"Placed {defaultFoodId} x{defaultFoodAmount}."
            : $"Failed to place {defaultFoodId}; check inventory or empty shop slots.");
    }

    public void SellFirstShopSlot()
    {
        if (!TryGetServices(out GameServices services))
            return;

        bool success = services.Shop.SellFood(0);
        SetMessage(success ? "Sold first shop slot." : "Failed to sell first shop slot.");
    }

    public void RunFullLoopOnce()
    {
        if (!TryGetServices(out GameServices services))
            return;

        if (!services.MazeRun.BeginRun())
        {
            SetMessage("Full loop stopped: not enough energy to enter the maze.");
            return;
        }

        bool collected = services.MazeRun.CollectItem(defaultLootItemId, defaultLootAmount);
        if (collected && !string.IsNullOrEmpty(secondaryLootItemId) && secondaryLootAmount > 0)
            collected = services.MazeRun.CollectItem(secondaryLootItemId, secondaryLootAmount);
        if (!collected)
        {
            SetMessage("Full loop stopped: failed to collect ingredients.");
            return;
        }

        MazeRunResult result = services.MazeRun.CompleteRun();
        if (result == null)
        {
            SetMessage("Full loop stopped: failed to complete maze.");
            return;
        }

        CraftingJob job = services.Crafting.StartCraft(defaultRecipeId);
        if (job == null)
        {
            SetMessage($"Full loop stopped: cannot craft {defaultRecipeId}.");
            return;
        }

        if (!services.Crafting.CompleteCraft(job.jobId))
        {
            SetMessage("Full loop stopped: failed to complete crafting.");
            return;
        }

        if (!services.Shop.PlaceFood(defaultFoodId, defaultFoodAmount))
        {
            SetMessage($"Full loop stopped: failed to place {defaultFoodId}.");
            return;
        }

        if (!services.Shop.SellFood(0))
        {
            SetMessage("Full loop stopped: failed to sell first shop slot.");
            return;
        }

        SetMessage("Ran full MVP loop once.");
    }

    public void RefreshStatus()
    {
        if (statusText == null)
            return;

        statusText.text = BuildStatus();
    }

    public void BindStatusText(Text text)
    {
        statusText = text;
        RefreshStatus();
    }

    private bool TryGetServices(out GameServices services)
    {
        services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (services != null)
            return true;

        SetMessage("GameServices not ready.");
        return false;
    }

    private void SetMessage(string message)
    {
        lastMessage = message;
        RefreshStatus();
    }

    private void OnMazeRunEnded(MazeRunResult result)
    {
        RefreshStatus();
    }

    private string BuildStatus()
    {
        StringBuilder sb = new StringBuilder();
        GameManager manager = GameManager.Instance;
        GameDatabase database = GameDatabase.Instance;
        GameServices services = manager.Services;

        sb.AppendLine("MVP Debug Loop");
        sb.AppendLine(lastMessage);
        sb.AppendLine();

        if (manager == null || services == null)
        {
            sb.AppendLine("GameManager or services are not ready.");
            return sb.ToString();
        }

        PlayerProfile profile = database.GetPlayerData().profile;
        sb.AppendLine($"Player: {profile.playerDisplayName} ({profile.playerId})");
        AppendMazeStatus(sb, services.MazeRun);
        AppendInventoryStatus(sb, database.GetPlayerData().inventory);
        AppendCraftingStatus(sb, services.Crafting);
        AppendShopStatus(sb, services.Shop);
        return sb.ToString();
    }

    private void AppendMazeStatus(StringBuilder sb, MazeRunService mazeRun)
    {
        sb.AppendLine();
        sb.AppendLine($"Maze: {mazeRun.State}");
        MazeRunResult result = mazeRun.CurrentResult;
        if (result == null)
        {
            sb.AppendLine("Collected: none");
            return;
        }

        sb.Append("Collected: ");
        AppendDictionaryInline(sb, result.collectedItems);
        sb.AppendLine();

        sb.Append("Rewards: ");
        AppendDictionaryInline(sb, result.rewardItems);
        sb.AppendLine();
    }

    private void AppendInventoryStatus(StringBuilder sb, PlayerInventory inventory)
    {
        sb.AppendLine();
        sb.Append("Inventory: ");
        if (inventory == null)
        {
            sb.AppendLine("none");
            return;
        }

        AppendDictionaryInline(sb, inventory.GetSnapshot());
        sb.AppendLine();
    }

    private void AppendCraftingStatus(StringBuilder sb, CraftingService crafting)
    {
        sb.AppendLine();
        sb.AppendLine("Crafting Queue:");
        IReadOnlyList<CraftingJob> queue = crafting.GetQueue();
        if (queue.Count == 0)
        {
            sb.AppendLine("- empty");
            return;
        }

        for (int i = 0; i < queue.Count; i++)
            sb.AppendLine($"- {queue[i].recipeId} ({(queue[i].completed ? "done" : "pending")})");
    }

    private void AppendShopStatus(StringBuilder sb, ShopService shop)
    {
        sb.AppendLine();
        sb.AppendLine("Shop Slots:");
        IReadOnlyList<ShopDisplaySlot> slots = shop.GetDisplaySlots();
        for (int i = 0; i < slots.Count; i++)
        {
            ShopDisplaySlot slot = slots[i];
            string slotText = slot.IsEmpty ? "Empty" : $"{slot.foodId} x{slot.amount}";
            sb.AppendLine($"- Slot {slot.slotId}: {slotText}");
        }
    }

    private void AppendDictionaryInline(StringBuilder sb, IReadOnlyDictionary<string, int> values)
    {
        if (values == null || values.Count == 0)
        {
            sb.Append("none");
            return;
        }

        bool first = true;
        foreach (KeyValuePair<string, int> kv in values)
        {
            if (!first)
                sb.Append(", ");
            sb.Append($"{kv.Key} x{kv.Value}");
            first = false;
        }
    }
}
