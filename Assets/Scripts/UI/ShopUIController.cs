using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopUIController : BaseUIController {
    private Text shopStateText;
    private Text hintText;
    private Text inventoryText;
    private Text outfitText;
    private GameObject inventoryPanel;
    private GameObject outfitPanel;
    private Button ovenButton;
    private Button displayButton;
    private Button mazeButton;
    private Button backButton;
    private Button backpackButton;
    private Button outfitButton;
    private string defaultFoodId = "food_carrot_soup";
    private string defaultRecipeId = "recipe1";
    private int defaultPlaceAmount = 1;

    public override void BindUI() {
        AutoBindMissingReferences();
    }

    public override void EventMapper() {
        GameEvents.OnShopChanged += Refresh;
        GameEvents.OnInventoryChanged += Refresh;
        GameEvents.OnCraftingChanged += Refresh;
        GameEvents.OnCollectionChanged += OnCollectionChanged;
        WireButtons();
    }

    public override void OnOpen() {
        Refresh();
    }

    public override void Dismiss() {
        GameEvents.OnShopChanged -= Refresh;
        GameEvents.OnInventoryChanged -= Refresh;
        GameEvents.OnCraftingChanged -= Refresh;
        GameEvents.OnCollectionChanged -= OnCollectionChanged;
        base.Dismiss();
    }

    public void PlaceDefaultFood() {
        var services = GameManager.Instance?.services;
        if (services?.Shop == null) {
            ShowHint("Shop service is not ready.");
            return;
        }

        if (TryPlaceFirstAvailableFood())
            return;

        if (services.Crafting == null) {
            ShowHint("Crafting service is not ready.");
            return;
        }

        CraftingJob job = services.Crafting.StartCraft(defaultRecipeId);
        if (job == null) {
            ShowHint("Not enough ingredients. " + BuildRecipeCostText(defaultRecipeId));
            return;
        }

        services.Crafting.CompleteCraft(job.jobId);
        if (!TryPlaceFirstOutput(job))
            ShowHint("Food crafted and stored in backpack.");
    }

    public void SellFirstSlot() {
        ShopService shop = GameManager.Instance?.services?.Shop;
        if (shop == null) {
            ShowHint("Shop service is not ready.");
            return;
        }

        foreach (ShopDisplaySlot slot in shop.GetDisplaySlots()) {
            if (slot.IsEmpty)
                continue;

            string soldName = ResolveDisplayName(slot.foodId);
            int price = ResolvePrice(slot.foodId);
            if (shop.SellFood(slot.slotId))
                ShowHint($"Sold {soldName} +{price} gold.");
            return;
        }

        ShowHint("No food on display.");
    }

    public void GoToMaze() {
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.GoToMaze();
        Debug.Log("GoToMaze");
    }

    public void GoToMainMenu() {
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.GoToMain();
        Debug.Log("GoToMainMenu");
    }

    private void OnCollectionChanged(CollectionCategory category) {
        if (category == CollectionCategory.Outfit || category == CollectionCategory.Food)
            Refresh();
    }

    private void Refresh() {
        if (shopStateText == null)
            return;

        StringBuilder sb = new StringBuilder();
        if (GameManager.Instance?.services?.Shop == null) {
            sb.AppendLine("Shop service is not ready.");
        }
        else {
            sb.AppendLine("Display Slots");
            foreach (ShopDisplaySlot slot in GameManager.Instance.services.Shop.GetDisplaySlots()) {
                string slotText = slot.IsEmpty ? "Empty" : $"{slot.foodId} x{slot.amount}";
                sb.AppendLine($"Slot {slot.slotId}: {slotText}");
            }
        }
        shopStateText.text = sb.ToString();
        RefreshInventoryPanel();
        RefreshOutfitPanel();
    }

    private void WireButtons() {
        BindClickEvent(ovenButton, PlaceDefaultFood);
        BindClickEvent(displayButton, SellFirstSlot);
        BindClickEvent(mazeButton, GoToMaze);
        BindClickEvent(backButton, GoToMainMenu);
        BindClickEvent(backpackButton, ShowBackpackHint);
        BindClickEvent(outfitButton, ShowOutfitHint);
    }

    private void ShowBackpackHint() {
        TogglePanel(inventoryPanel);
        if (outfitPanel != null)
            outfitPanel.SetActive(false);
        RefreshInventoryPanel();
        ShowHint("Backpack opened.");
    }

    private void ShowOutfitHint() {
        TogglePanel(outfitPanel);
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        RefreshOutfitPanel();
        ShowHint("Outfit panel opened.");
    }

    private void ShowHint(string message) {
        if (hintText != null)
            UIHelper.SetText(hintText, message);

        Debug.Log($"[ShopUI] {message}");
    }

    private bool TryPlaceFirstAvailableFood() {
        PlayerInventory inventory = GameDatabase.Instance?.GetPlayerData()?.Inventory;
        ShopService shop = GameManager.Instance?.services?.Shop;
        if (inventory == null || shop == null)
            return false;

        if (inventory.GetAmount(defaultFoodId) >= defaultPlaceAmount
            && shop.PlaceFood(defaultFoodId, defaultPlaceAmount)) {
            ShowHint($"Placed {ResolveDisplayName(defaultFoodId)} on display.");
            return true;
        }

        foreach (KeyValuePair<string, int> kv in inventory.GetSnapshot()) {
            if (kv.Value < defaultPlaceAmount || !IsSellableFood(kv.Key))
                continue;

            if (shop.PlaceFood(kv.Key, defaultPlaceAmount)) {
                ShowHint($"Placed {ResolveDisplayName(kv.Key)} on display.");
                return true;
            }
        }

        return false;
    }

    private bool TryPlaceFirstOutput(CraftingJob job) {
        if (job?.outputItems == null)
            return false;

        ShopService shop = GameManager.Instance?.services?.Shop;
        if (shop == null)
            return false;

        foreach (string itemId in job.outputItems) {
            if (!IsSellableFood(itemId))
                continue;

            if (shop.PlaceFood(itemId, defaultPlaceAmount)) {
                ShowHint($"Cooked and placed {ResolveDisplayName(itemId)}.");
                return true;
            }
        }

        return false;
    }

    private void RefreshInventoryPanel() {
        if (inventoryText == null)
            return;

        PlayerInventory inventory = GameDatabase.Instance?.GetPlayerData()?.Inventory;
        if (inventory == null) {
            inventoryText.text = "Backpack is not ready.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Backpack");
        IReadOnlyDictionary<string, int> snapshot = inventory.GetSnapshot();
        if (snapshot.Count == 0) {
            sb.AppendLine("Empty");
        }
        else {
            foreach (KeyValuePair<string, int> kv in snapshot)
                sb.AppendLine($"{ResolveDisplayName(kv.Key)} x{kv.Value}");
        }

        inventoryText.text = sb.ToString();
    }

    private void RefreshOutfitPanel() {
        if (outfitText == null)
            return;

        PlayerDatabase player = GameDatabase.Instance?.GetPlayerData();
        if (player?.Collections == null || player.Profile == null) {
            outfitText.text = "Outfit data is not ready.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Outfits");
        sb.AppendLine($"Equipped: {ResolveDisplayName(player.Profile.equippedOutfitId)}");
        IReadOnlyCollection<string> unlocked = player.Collections.GetUnlocked(CollectionCategory.Outfit);
        if (unlocked.Count == 0) {
            sb.AppendLine("No outfit unlocked.");
        }
        else {
            foreach (string outfitId in unlocked)
                sb.AppendLine(ResolveDisplayName(outfitId));
        }

        outfitText.text = sb.ToString();
    }

    private void TogglePanel(GameObject panel) {
        UIHelper.ToggleActive(panel);
    }

    private bool IsSellableFood(string itemId) {
        return GameDatabase.Instance?.Get<FoodData>("foods", itemId) != null;
    }

    private int ResolvePrice(string itemId) {
        FoodData food = GameDatabase.Instance?.Get<FoodData>("foods", itemId);
        if (food != null)
            return food.price;

        ItemData item = GameDatabase.Instance?.Get<ItemData>("items", itemId);
        return item != null ? item.price : 0;
    }

    private string ResolveDisplayName(string id) {
        if (string.IsNullOrEmpty(id))
            return "None";

        FoodData food = GameDatabase.Instance?.Get<FoodData>("foods", id);
        if (food != null && !string.IsNullOrEmpty(food.displayName))
            return food.displayName;

        IngredientData ingredient = GameDatabase.Instance?.Get<IngredientData>("ingredients", id);
        if (ingredient != null && !string.IsNullOrEmpty(ingredient.displayName))
            return ingredient.displayName;

        ItemData item = GameDatabase.Instance?.Get<ItemData>("items", id);
        if (item != null && !string.IsNullOrEmpty(item.name))
            return item.name;

        OutfitData outfit = GameDatabase.Instance?.Get<OutfitData>("outfits", id);
        if (outfit != null && !string.IsNullOrEmpty(outfit.displayName))
            return outfit.displayName;

        return id;
    }

    private string BuildRecipeCostText(string recipeId) {
        RecipeData recipe = GameDatabase.Instance?.Get<RecipeData>("recipes", recipeId);
        if (recipe?.ingredients == null || recipe.ingredients.Count == 0)
            return string.Empty;

        StringBuilder sb = new StringBuilder("Need: ");
        bool first = true;
        foreach (KeyValuePair<string, int> kv in recipe.ingredients) {
            if (!first)
                sb.Append(", ");
            sb.Append(ResolveDisplayName(kv.Key));
            sb.Append(" x");
            sb.Append(kv.Value);
            first = false;
        }

        return sb.ToString();
    }

    private void AutoBindMissingReferences() {
        if (shopStateText == null)
            shopStateText = FindText("Shop State Text");
        if (hintText == null)
            hintText = FindText("Hint Text");
        if (inventoryText == null)
            inventoryText = FindText("Inventory Text");
        if (outfitText == null)
            outfitText = FindText("Outfit Text");
        if (inventoryPanel == null)
            inventoryPanel = FindObject("Inventory Panel");
        if (outfitPanel == null)
            outfitPanel = FindObject("Outfit Panel");
        if (ovenButton == null)
            ovenButton = FindButton("Oven Button");
        if (displayButton == null)
            displayButton = FindButton("Display Button");
        if (mazeButton == null)
            mazeButton = FindButton("Maze Button");
        if (backButton == null)
            backButton = FindButton("Back Button");
        if (backpackButton == null)
            backpackButton = FindButton("Backpack Button");
        if (outfitButton == null)
            outfitButton = FindButton("Outfit Button");
    }
}
