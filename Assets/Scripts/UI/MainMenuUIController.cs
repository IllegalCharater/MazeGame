using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuUIController : MonoBehaviour
{
    [Header("Fallback Values")]
    [SerializeField] private string defaultJoinDate = "2026/04/21";
    [SerializeField] private int defaultAdventureDays = 17;

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

    private GameObject profileDetailPanel;

    private Button navRoleButton;
    private Button closeButton;
    private Button mazeEntryButton;
    private Button adventureButton;
    private Button kitchenEntryButton;
    private Button shopEntryButton;
    private Button navShopButton;
    private Button settingsButton;
    private Button mailButton;
    private Button gardenButton;
    private Button boardButton;
    private Button navBagButton;
    private Button navCollectionButton;

    private void Awake()
    {
        CacheReferences();
        WireButtons();
        HideProfileDetail();
        RefreshAll();
    }

    private void OnEnable()
    {
        GameEvents.OnCollectionChanged += OnCollectionChanged;
        GameEvents.OnInventoryChanged += RefreshRecentItems;
        RefreshAll();
    }

    private void OnDisable()
    {
        GameEvents.OnCollectionChanged -= OnCollectionChanged;
        GameEvents.OnInventoryChanged -= RefreshRecentItems;
    }

    public void OpenProfileDetail()
    {
        if (profileDetailPanel == null)
            return;

        RefreshAll();
        profileDetailPanel.SetActive(true);
    }

    public void HideProfileDetail()
    {
        if (profileDetailPanel != null)
            profileDetailPanel.SetActive(false);
    }

    public void RefreshAll()
    {
        RefreshProfile();
        RefreshCollectionProgress();
        RefreshRecentItems();
    }

    private void CacheReferences()
    {
        profileDetailPanel = UIHelper.FindDeep(this, "ProfileDetailPanel");

        detailNameText = UIHelper.FindText(this, "Detail Name");
        uidText = UIHelper.FindText(this, "UID");
        detailLevelText = UIHelper.FindText(this, "Detail Level");
        detailExpText = UIHelper.FindText(this, "Detail Exp");
        basicInfoValuesText = UIHelper.FindText(this, "Basic Info Values");
        joinDateText = UIHelper.FindText(this, "Join Date");
        totalDaysText = UIHelper.FindText(this, "Total Days");
        totalProfitText = UIHelper.FindText(this, "Total Profit");

        collectTexts = new[]
        {
            UIHelper.FindText(this, "Collect Text 0"),
            UIHelper.FindText(this, "Collect Text 1"),
            UIHelper.FindText(this, "Collect Text 2"),
            UIHelper.FindText(this, "Collect Text 3")
        };

        List<Text> tags = new List<Text>();
        for (int i = 0; i < 8; i++)
        {
            Text tag = UIHelper.FindText(this, "New Tag " + i);
            if (tag != null)
                tags.Add(tag);
        }
        recentTags = tags.ToArray();

        navRoleButton = UIHelper.FindButton(this, "Nav Role");
        closeButton = UIHelper.FindButton(this, "Profile Close");
        mazeEntryButton = UIHelper.FindButton(this, "Maze Entry");
        adventureButton = UIHelper.FindButton(this, "Nav Adventure");
        kitchenEntryButton = UIHelper.FindButton(this, "Kitchen Entry");
        shopEntryButton = UIHelper.FindButton(this, "Shop Entry");
        navShopButton = UIHelper.FindButton(this, "Nav Shop");
        settingsButton = UIHelper.FindButton(this, "Settings Button");
        mailButton = UIHelper.FindButton(this, "Mail Button");
        gardenButton = UIHelper.FindButton(this, "Garden Entry");
        boardButton = UIHelper.FindButton(this, "Board Entry");
        navBagButton = UIHelper.FindButton(this, "Nav Bag");
        navCollectionButton = UIHelper.FindButton(this, "Nav Collection");
    }

    private void WireButtons()
    {
        UIHelper.AddClick(navRoleButton, OpenProfileDetail);
        UIHelper.AddClick(closeButton, HideProfileDetail);

        UIHelper.AddClick(mazeEntryButton, GoToMaze);
        UIHelper.AddClick(adventureButton, GoToMaze);
        UIHelper.AddClick(kitchenEntryButton, GoToShop);
        UIHelper.AddClick(shopEntryButton, GoToShop);
        UIHelper.AddClick(navShopButton, GoToShop);

        UIHelper.AddClick(settingsButton, () => ShowLogHint("设置界面尚未接入。"));
        UIHelper.AddClick(mailButton, () => ShowLogHint("邮件界面尚未接入。"));
        UIHelper.AddClick(gardenButton, () => ShowLogHint("花园玩法尚未接入。"));
        UIHelper.AddClick(boardButton, () => ShowLogHint("公告板尚未接入。"));
        UIHelper.AddClick(navBagButton, () => ShowLogHint("背包详情界面尚未接入。"));
        UIHelper.AddClick(navCollectionButton, () => ShowLogHint("图鉴详情界面尚未接入。"));
    }

    private void RefreshProfile()
    {
        PlayerDatabase player = GameDatabase.Instance?.GetPlayerData();
        PlayerProfile profile = player?.profile;
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
        UIHelper.SetText(basicInfoValuesText, $"角色等级:        {level}              金币总量:        {gold}");
        UIHelper.SetText(joinDateText, $"{defaultJoinDate}");
        UIHelper.SetText(totalDaysText, $"{defaultAdventureDays}天");
        UIHelper.SetText(totalProfitText, $"{gold}");
    }

    private void RefreshCollectionProgress()
    {
        PlayerDatabase player = GameDatabase.Instance?.GetPlayerData();
        if (player == null || collectTexts == null)
            return;

        SetTextSafe(0, $"食物图鉴\n{CountCollection(player, CollectionCategory.Food)}/{CountTable<FoodData>("foods")}");
        SetTextSafe(1, $"服饰收集\n{CountCollection(player, CollectionCategory.Outfit)}/{CountTable<OutfitData>("outfits")}");
        SetTextSafe(2, $"家具收集\n{CountCollection(player, CollectionCategory.Furniture)}/{CountTable<FurnitureData>("furniture")}");
        SetTextSafe(3, $"菜谱收集\n{(player.blueprints != null ? player.blueprints.Count : 0)}/{CountTable<BlueprintData>("blueprints")}");
    }

    private void RefreshRecentItems()
    {
        if (recentTags == null || recentTags.Length == 0)
            return;

        List<string> recent = BuildRecentLabels();
        for (int i = 0; i < recentTags.Length; i++)
            recentTags[i].text = i < recent.Count ? recent[i] : "NEW";
    }

    private List<string> BuildRecentLabels()
    {
        List<string> labels = new List<string>();
        PlayerDatabase player = GameDatabase.Instance?.GetPlayerData();
        if (player?.inventory != null)
        {
            foreach (KeyValuePair<string, int> kv in player.inventory.GetSnapshot())
            {
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

    private void AddUnlockedLabels(List<string> labels, CollectionCategory category)
    {
        PlayerDatabase player = GameDatabase.Instance?.GetPlayerData();
        if (player?.collections == null || labels.Count >= recentTags.Length)
            return;

        foreach (string id in player.collections.GetUnlocked(category))
        {
            labels.Add(ResolveDisplayName(id));
            if (labels.Count >= recentTags.Length)
                return;
        }
    }

    private string ResolveDisplayName(string id)
    {
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

    private int CountTable<T>(string rootKey) where T : BaseData
    {
        IReadOnlyList<T> all = GameDatabase.Instance?.GetAll<T>(rootKey);
        return all != null ? all.Count : 0;
    }

    private int CountCollection(PlayerDatabase player, CollectionCategory category)
    {
        IReadOnlyCollection<string> unlocked = player.collections?.GetUnlocked(category);
        return unlocked != null ? unlocked.Count : 0;
    }

    private void SetTextSafe(int index, string value)
    {
        if (index >= 0 && index < collectTexts.Length)
            UIHelper.SetText(collectTexts[index], value);
    }

    private void OnCollectionChanged(CollectionCategory category)
    {
        RefreshCollectionProgress();
        RefreshRecentItems();
    }

    private void GoToMaze()
    {
        if (SceneFlow.Instance != null)
            SceneFlow.Instance.GoToMaze();
    }

    private void GoToShop()
    {
        if (SceneFlow.Instance != null)
            SceneFlow.Instance.GoToShop();
    }

    private void ShowLogHint(string message)
    {
        Debug.Log($"[MainMenuUI] {message}");
    }

}
