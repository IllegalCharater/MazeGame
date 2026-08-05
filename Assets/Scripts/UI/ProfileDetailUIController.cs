using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ProfileDetailUIController : BaseUIController {
    public override string layerName => UIConfig.PopupLayerName;

    private readonly string defaultJoinDate = "2026/04/21";
    private readonly int defaultAdventureDays = 17;

    private Text detailNameText;
    private Text uidText;
    private Text detailLevelText;
    private Text detailExpText;
    private Text basicInfoValuesText;
    private Text joinDateText;
    private Text totalDaysText;
    private Text totalProfitText;
    private Text[] collectTexts;
    private Text[] recentTags;
    private Button closeButton;

    public override void BindUI() {
        CacheReferences();
    }

    public override void EventMapper() {
        GameEvents.OnCollectionChanged += OnCollectionChanged;
        GameEvents.OnInventoryChanged += RefreshRecentItems;
        GameEvents.OnCurrencyChanged += OnCurrencyChanged;
        GameEvents.OnEnergyChanged += OnEnergyChanged;
        BindClickEvent(closeButton, Close);
    }

    public override void OnOpen() {
        RefreshAll();
    }

    public override void Dismiss() {
        GameEvents.OnCollectionChanged -= OnCollectionChanged;
        GameEvents.OnInventoryChanged -= RefreshRecentItems;
        GameEvents.OnCurrencyChanged -= OnCurrencyChanged;
        GameEvents.OnEnergyChanged -= OnEnergyChanged;
        base.Dismiss();
    }

    public void RefreshAll() {
        RefreshProfile();
        RefreshCollectionProgress();
        RefreshRecentItems();
    }

    private void CacheReferences() {
        detailNameText = FindText("Detail Name");
        uidText = FindText("UID");
        detailLevelText = FindText("Detail Level");
        detailExpText = FindText("Detail Exp");
        basicInfoValuesText = FindText("Basic Info Values");
        joinDateText = FindText("Join Date");
        totalDaysText = FindText("Total Days");
        totalProfitText = FindText("Total Profit");
        closeButton = FindButton("Profile Close");

        collectTexts = new[]
        {
            FindText("Collect Text 0"),
            FindText("Collect Text 1"),
            FindText("Collect Text 2"),
            FindText("Collect Text 3")
        };

        List<Text> tags = new List<Text>();
        for (int i = 0; i < 8; i++) {
            Text tag = FindText("New Tag " + i);
            if (tag != null)
                tags.Add(tag);
        }
        recentTags = tags.ToArray();
    }

    private void RefreshProfile() {
        PlayerDatabase player = GameDatabase.Instance?.GetPlayerData();
        PlayerProfile profile = player?.Profile;
        if (profile == null)
            return;

        string displayName = string.IsNullOrEmpty(profile.playerDisplayName)
            ? profile.playerId
            : profile.playerDisplayName;
        string level = $"Lv.{Mathf.Max(1, profile.level)}";
        string energy = $"{profile.energy}/{profile.maxEnergy}";
        string gold = profile.currency.ToString("N0");

        UIHelper.SetText(detailNameText, displayName);
        UIHelper.SetText(uidText, $"UID: {profile.playerId}");
        UIHelper.SetText(detailLevelText, level);
        UIHelper.SetText(detailExpText, energy);
        UIHelper.SetText(basicInfoValuesText, $"Level: {level}        Gold: {gold}");
        UIHelper.SetText(joinDateText, defaultJoinDate);
        UIHelper.SetText(totalDaysText, $"{defaultAdventureDays} days");
        UIHelper.SetText(totalProfitText, gold);
    }

    private void RefreshCollectionProgress() {
        PlayerDatabase player = GameDatabase.Instance?.GetPlayerData();
        if (player == null || collectTexts == null)
            return;

        SetTextSafe(0, $"Food Collection\n{CountCollection(player, CollectionCategory.Food)}/{CountTable<FoodData>("foods")}");
        SetTextSafe(1, $"Outfit Collection\n{CountCollection(player, CollectionCategory.Outfit)}/{CountTable<OutfitData>("outfits")}");
        SetTextSafe(2, $"Furniture Collection\n{CountCollection(player, CollectionCategory.Furniture)}/{CountTable<FurnitureData>("furniture")}");
        SetTextSafe(3, $"Blueprint Collection\n{(player.Blueprints != null ? player.Blueprints.Count : 0)}/{CountTable<BlueprintData>("blueprints")}");
    }

    private void RefreshRecentItems() {
        if (recentTags == null || recentTags.Length == 0)
            return;

        List<string> recent = BuildRecentLabels();
        for (int i = 0; i < recentTags.Length; i++)
            recentTags[i].text = i < recent.Count ? recent[i] : "NEW";
    }

    private List<string> BuildRecentLabels() {
        List<string> labels = new List<string>();
        PlayerDatabase player = GameDatabase.Instance?.GetPlayerData();
        if (player?.Inventory != null) {
            foreach (KeyValuePair<string, int> kv in player.Inventory.GetSnapshot()) {
                labels.Add(ResolveDisplayName(kv.Key));
                if (labels.Count >= recentTags.Length)
                    return labels;
            }
        }

        AddUnlockedLabels(labels, CollectionCategory.Food);
        AddUnlockedLabels(labels, CollectionCategory.Outfit);
        AddUnlockedLabels(labels, CollectionCategory.Furniture);

        while (labels.Count < recentTags.Length)
            labels.Add("NEW");
        return labels;
    }

    private void AddUnlockedLabels(List<string> labels, CollectionCategory category) {
        PlayerDatabase player = GameDatabase.Instance?.GetPlayerData();
        if (player?.Collections == null || labels.Count >= recentTags.Length)
            return;

        foreach (string id in player.Collections.GetUnlocked(category)) {
            labels.Add(ResolveDisplayName(id));
            if (labels.Count >= recentTags.Length)
                return;
        }
    }

    private string ResolveDisplayName(string id) {
        GameDatabase database = GameDatabase.Instance;
        if (database == null || string.IsNullOrEmpty(id))
            return "NEW";

        FoodData food = database.Get<FoodData>("foods", id);
        if (food != null && !string.IsNullOrEmpty(food.displayName))
            return food.displayName;

        IngredientData ingredient = database.Get<IngredientData>("ingredients", id);
        if (ingredient != null && !string.IsNullOrEmpty(ingredient.displayName))
            return ingredient.displayName;

        OutfitData outfit = database.Get<OutfitData>("outfits", id);
        if (outfit != null && !string.IsNullOrEmpty(outfit.displayName))
            return outfit.displayName;

        FurnitureData furniture = database.Get<FurnitureData>("furniture", id);
        if (furniture != null && !string.IsNullOrEmpty(furniture.displayName))
            return furniture.displayName;

        BlueprintData blueprint = database.Get<BlueprintData>("blueprints", id);
        if (blueprint != null && !string.IsNullOrEmpty(blueprint.displayName))
            return blueprint.displayName;

        return id;
    }

    private int CountTable<T>(string rootKey) where T : BaseData {
        IReadOnlyList<T> all = GameDatabase.Instance?.GetAll<T>(rootKey);
        return all != null ? all.Count : 0;
    }

    private int CountCollection(PlayerDatabase player, CollectionCategory category) {
        IReadOnlyCollection<string> unlocked = player.Collections?.GetUnlocked(category);
        return unlocked != null ? unlocked.Count : 0;
    }

    private void SetTextSafe(int index, string value) {
        if (index >= 0 && index < collectTexts.Length)
            UIHelper.SetText(collectTexts[index], value);
    }

    private void OnCollectionChanged(CollectionCategory category) {
        RefreshCollectionProgress();
        RefreshRecentItems();
    }

    private void OnCurrencyChanged(float value) {
        RefreshProfile();
    }

    private void OnEnergyChanged(int current, int max) {
        RefreshProfile();
    }

    private void Close() {
        UIManager.CloseView(viewName);
    }
}
