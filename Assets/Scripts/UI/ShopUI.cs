using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ShopUI : MonoBehaviour
{
    [SerializeField] private Text shopStateText;
    [SerializeField] private Text hintText;
    [SerializeField] private Text inventoryText;
    [SerializeField] private Text outfitText;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject outfitPanel;
    [SerializeField] private Button ovenButton;
    [SerializeField] private Button displayButton;
    [SerializeField] private Button mazeButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button backpackButton;
    [SerializeField] private Button outfitButton;
    [SerializeField] private string defaultFoodId = "food_carrot_soup";
    [SerializeField] private string defaultRecipeId = "recipe1";
    [SerializeField] private int defaultPlaceAmount = 1;

    private void Awake()
    {
        AutoBindMissingReferences();
    }

    private void Start()
    {
        Refresh();
    }

    public void BindStateText(Text text)
    {
        shopStateText = text;
        Refresh();
    }

    private void OnEnable()
    {
        GameEvents.OnShopChanged += Refresh;
        GameEvents.OnInventoryChanged += Refresh;
        GameEvents.OnCraftingChanged += Refresh;
        GameEvents.OnCollectionChanged += OnCollectionChanged;
        WireButtons();
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnShopChanged -= Refresh;
        GameEvents.OnInventoryChanged -= Refresh;
        GameEvents.OnCraftingChanged -= Refresh;
        GameEvents.OnCollectionChanged -= OnCollectionChanged;
        UnwireButtons();
    }

    public void PlaceDefaultFood()
    {
        GameServices services = GameManager.Instance?.Services;
        if (services?.Shop == null)
        {
            ShowHint("Shop service is not ready.");
            return;
        }

        if (TryPlaceFirstAvailableFood())
            return;

        if (services.Crafting == null)
        {
            ShowHint("Crafting service is not ready.");
            return;
        }

        CraftingJob job = services.Crafting.StartCraft(defaultRecipeId);
        if (job == null)
        {
            ShowHint("Not enough ingredients. " + BuildRecipeCostText(defaultRecipeId));
            return;
        }

        services.Crafting.CompleteCraft(job.jobId);
        if (!TryPlaceFirstOutput(job))
            ShowHint("Food crafted and stored in backpack.");
    }

    public void SellFirstSlot()
    {
        ShopService shop = GameManager.Instance?.Services?.Shop;
        if (shop == null)
        {
            ShowHint("Shop service is not ready.");
            return;
        }

        foreach (ShopDisplaySlot slot in shop.GetDisplaySlots())
        {
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

    public void GoToMaze()
    {
        if (SceneFlow.Instance != null)
            SceneFlow.Instance.GoToMaze();
    }

    public void GoToMainMenu()
    {
        if (SceneFlow.Instance != null)
            SceneFlow.Instance.GoToMainMenu();
    }

    private void OnCollectionChanged(CollectionCategory category)
    {
        if (category == CollectionCategory.Outfit || category == CollectionCategory.Food)
            Refresh();
    }

    private void Refresh()
    {
        if (shopStateText == null)
            return;

        StringBuilder sb = new StringBuilder();
        if (GameManager.Instance?.Services?.Shop == null)
        {
            sb.AppendLine("Shop service is not ready.");
        }
        else
        {
            sb.AppendLine("Display Slots");
            foreach (ShopDisplaySlot slot in GameManager.Instance.Services.Shop.GetDisplaySlots())
            {
                string slotText = slot.IsEmpty ? "Empty" : $"{slot.foodId} x{slot.amount}";
                sb.AppendLine($"Slot {slot.slotId}: {slotText}");
            }
        }
        shopStateText.text = sb.ToString();
        RefreshInventoryPanel();
        RefreshOutfitPanel();
    }

    private void WireButtons()
    {
        UIHelper.AddClick(ovenButton, PlaceDefaultFood);
        UIHelper.AddClick(displayButton, SellFirstSlot);
        UIHelper.AddClick(mazeButton, GoToMaze);
        UIHelper.AddClick(backButton, GoToMainMenu);
        UIHelper.AddClick(backpackButton, ShowBackpackHint);
        UIHelper.AddClick(outfitButton, ShowOutfitHint);
    }

    private void UnwireButtons()
    {
        UIHelper.RemoveClick(ovenButton, PlaceDefaultFood);
        UIHelper.RemoveClick(displayButton, SellFirstSlot);
        UIHelper.RemoveClick(mazeButton, GoToMaze);
        UIHelper.RemoveClick(backButton, GoToMainMenu);
        UIHelper.RemoveClick(backpackButton, ShowBackpackHint);
        UIHelper.RemoveClick(outfitButton, ShowOutfitHint);
    }

    private void ShowBackpackHint()
    {
        TogglePanel(inventoryPanel);
        if (outfitPanel != null)
            outfitPanel.SetActive(false);
        RefreshInventoryPanel();
        ShowHint("Backpack opened.");
    }

    private void ShowOutfitHint()
    {
        TogglePanel(outfitPanel);
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        RefreshOutfitPanel();
        ShowHint("Outfit panel opened.");
    }

    private void ShowHint(string message)
    {
        if (hintText != null)
            UIHelper.SetText(hintText, message);

        Debug.Log($"[ShopUI] {message}");
    }

    private bool TryPlaceFirstAvailableFood()
    {
        PlayerInventory inventory = GameDatabase.Instance?.GetPlayerData()?.inventory;
        ShopService shop = GameManager.Instance?.Services?.Shop;
        if (inventory == null || shop == null)
            return false;

        if (inventory.GetAmount(defaultFoodId) >= defaultPlaceAmount
            && shop.PlaceFood(defaultFoodId, defaultPlaceAmount))
        {
            ShowHint($"Placed {ResolveDisplayName(defaultFoodId)} on display.");
            return true;
        }

        foreach (KeyValuePair<string, int> kv in inventory.GetSnapshot())
        {
            if (kv.Value < defaultPlaceAmount || !IsSellableFood(kv.Key))
                continue;

            if (shop.PlaceFood(kv.Key, defaultPlaceAmount))
            {
                ShowHint($"Placed {ResolveDisplayName(kv.Key)} on display.");
                return true;
            }
        }

        return false;
    }

    private bool TryPlaceFirstOutput(CraftingJob job)
    {
        if (job?.outputItems == null)
            return false;

        ShopService shop = GameManager.Instance?.Services?.Shop;
        if (shop == null)
            return false;

        foreach (string itemId in job.outputItems)
        {
            if (!IsSellableFood(itemId))
                continue;

            if (shop.PlaceFood(itemId, defaultPlaceAmount))
            {
                ShowHint($"Cooked and placed {ResolveDisplayName(itemId)}.");
                return true;
            }
        }

        return false;
    }

    private void RefreshInventoryPanel()
    {
        if (inventoryText == null)
            return;

        PlayerInventory inventory = GameDatabase.Instance?.GetPlayerData()?.inventory;
        if (inventory == null)
        {
            inventoryText.text = "Backpack is not ready.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Backpack");
        IReadOnlyDictionary<string, int> snapshot = inventory.GetSnapshot();
        if (snapshot.Count == 0)
        {
            sb.AppendLine("Empty");
        }
        else
        {
            foreach (KeyValuePair<string, int> kv in snapshot)
                sb.AppendLine($"{ResolveDisplayName(kv.Key)} x{kv.Value}");
        }

        inventoryText.text = sb.ToString();
    }

    private void RefreshOutfitPanel()
    {
        if (outfitText == null)
            return;

        PlayerDatabase player = GameDatabase.Instance?.GetPlayerData();
        if (player?.collections == null || player.profile == null)
        {
            outfitText.text = "Outfit data is not ready.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Outfits");
        sb.AppendLine($"Equipped: {ResolveDisplayName(player.profile.equippedOutfitId)}");
        IReadOnlyCollection<string> unlocked = player.collections.GetUnlocked(CollectionCategory.Outfit);
        if (unlocked.Count == 0)
        {
            sb.AppendLine("No outfit unlocked.");
        }
        else
        {
            foreach (string outfitId in unlocked)
                sb.AppendLine(ResolveDisplayName(outfitId));
        }

        outfitText.text = sb.ToString();
    }

    private void TogglePanel(GameObject panel)
    {
        UIHelper.ToggleActive(panel);
    }

    private bool IsSellableFood(string itemId)
    {
        return GameDatabase.Instance?.Get<FoodData>("foods", itemId) != null;
    }

    private int ResolvePrice(string itemId)
    {
        FoodData food = GameDatabase.Instance?.Get<FoodData>("foods", itemId);
        if (food != null)
            return food.price;

        ItemData item = GameDatabase.Instance?.Get<ItemData>("items", itemId);
        return item != null ? item.price : 0;
    }

    private string ResolveDisplayName(string id)
    {
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

    private string BuildRecipeCostText(string recipeId)
    {
        RecipeData recipe = GameDatabase.Instance?.Get<RecipeData>("recipes", recipeId);
        if (recipe?.ingredients == null || recipe.ingredients.Count == 0)
            return string.Empty;

        StringBuilder sb = new StringBuilder("Need: ");
        bool first = true;
        foreach (KeyValuePair<string, int> kv in recipe.ingredients)
        {
            if (!first)
                sb.Append(", ");
            sb.Append(ResolveDisplayName(kv.Key));
            sb.Append(" x");
            sb.Append(kv.Value);
            first = false;
        }

        return sb.ToString();
    }

    private void AutoBindMissingReferences()
    {
        if (shopStateText == null)
            shopStateText = UIHelper.FindText(this, "Shop State Text");
        if (hintText == null)
            hintText = UIHelper.FindText(this, "Hint Text");
        if (inventoryText == null)
            inventoryText = UIHelper.FindText(this, "Inventory Text");
        if (outfitText == null)
            outfitText = UIHelper.FindText(this, "Outfit Text");
        if (inventoryPanel == null)
            inventoryPanel = UIHelper.FindDeep(this, "Inventory Panel");
        if (outfitPanel == null)
            outfitPanel = UIHelper.FindDeep(this, "Outfit Panel");
        if (ovenButton == null)
            ovenButton = UIHelper.FindButton(this, "Oven Button");
        if (displayButton == null)
            displayButton = UIHelper.FindButton(this, "Display Button");
        if (mazeButton == null)
            mazeButton = UIHelper.FindButton(this, "Maze Button");
        if (backButton == null)
            backButton = UIHelper.FindButton(this, "Back Button");
        if (backpackButton == null)
            backpackButton = UIHelper.FindButton(this, "Backpack Button");
        if (outfitButton == null)
            outfitButton = UIHelper.FindButton(this, "Outfit Button");
    }

}
