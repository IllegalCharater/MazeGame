using UnityEngine;
using UnityEngine.UI;

public sealed class HUDController : BaseUIController
{
    public override string layerName => UIConfig.TopLayerName;
    public override bool hasInputBlocker => false;

    private Text playerNameText;
    private Text levelText;
    private Text currencyText;
    private Text energyText;
    private Text stateHintText;
    private Text toastText;
    private Button profileButton;

    private string mazeHint = "Maze: explore and collect ingredients.";
    private string shopHint = "Shop: craft, display, and sell food.";
    private string menuHint = "Main Menu";
    private string levelFormat = "Lv.{0}";
    private string currencyFormat = "{0:0}";
    private string energyFormat = "{0}/{1}";

    public override void BindUI()
    {
        GameObject profileCard = FindObject("Profile Card");
        playerNameText = FindComponent<Text>(profileCard, "Player Name");
        levelText = FindComponent<Text>(profileCard, "Level Text");
        energyText = FindComponent<Text>(profileCard, "Exp Text");
        profileButton = FindComponent<Button>(profileCard);
        GameObject coinBar = FindObject("CoinBar");
        currencyText = FindComponent<Text>(coinBar, "Currency");
        if (toastText != null)
            toastText.gameObject.SetActive(false);
    }

    public override void EventMapper()
    {
        GameEvents.OnCurrencyChanged += UpdateCurrency;
        GameEvents.OnEnergyChanged += UpdateEnergy;
        GameEvents.OnGameStateChanged += OnGameStateChanged;
        GameEvents.OnInventoryChanged += OnInventoryChanged;
        BindClickEvent(profileButton, OpenProfileDetail);
    }

    public override void OnOpen()
    {
        RefreshNow();
    }

    public override void Dismiss()
    {
        GameEvents.OnCurrencyChanged -= UpdateCurrency;
        GameEvents.OnEnergyChanged -= UpdateEnergy;
        GameEvents.OnGameStateChanged -= OnGameStateChanged;
        GameEvents.OnInventoryChanged -= OnInventoryChanged;
        base.Dismiss();
    }

    public void Bind(Text currency, Text energy, Text stateHint, Text toast)
    {
        currencyText = currency;
        energyText = energy;
        stateHintText = stateHint;
        toastText = toast;
        if (toastText != null)
            toastText.gameObject.SetActive(false);
        RefreshNow();
    }

    private void RefreshNow()
    {
        if (GameManager.Instance == null || GameManager.Instance.gameDatabase?.GetPlayerData()?.profile == null)
            return;

        PlayerProfile profile = GameManager.Instance.gameDatabase.GetPlayerData().profile;
        UpdateProfile(profile);
        UpdateCurrency(profile.currency);
        UpdateEnergy(profile.energy, profile.maxEnergy);
        UpdateStateHint(GameManager.Instance.CurrentState);
        ApplyVisibility(GameManager.Instance.CurrentState);
    }

    private void UpdateProfile(PlayerProfile profile)
    {
        if (profile == null)
            return;

        string displayName = string.IsNullOrEmpty(profile.playerDisplayName)
            ? profile.playerId
            : profile.playerDisplayName;

        UIHelper.SetText(playerNameText, displayName);
        UIHelper.SetText(levelText, string.Format(levelFormat, Mathf.Max(1, profile.level)));
    }

    private void UpdateCurrency(float value)
    {
        UIHelper.SetText(currencyText, string.Format(currencyFormat, value));
    }

    private void UpdateEnergy(int current, int max)
    {
        UIHelper.SetText(energyText, string.Format(energyFormat, current, max));
    }

    private void OnGameStateChanged(GameState from, GameState to)
    {
        UpdateStateHint(to);
        ApplyVisibility(to);
    }

    // 迷宫场景内隐藏 HUD：迷宫界面自带状态栏，两者叠在 TopLayer 上会互相遮挡。
    // 放在这里而不是只靠 SceneFlowManager，是因为 HUD 是异步加载的，
    // 可能在切场景之后才实例化，那时就得自己把状态补上。
    private void ApplyVisibility(GameState state)
    {
        if (root != null)
            root.SetActive(state != GameState.InMaze);
    }

    private void UpdateStateHint(GameState state)
    {
        if (stateHintText == null)
            return;

        switch (state)
        {
            case GameState.InMaze:
                UIHelper.SetText(stateHintText, mazeHint);
                break;
            // case GameState.InShop:
            //     UIHelper.SetText(stateHintText, shopHint);
            //     break;
            default:
                UIHelper.SetText(stateHintText, menuHint);
                break;
        }
    }

    private void OpenProfileDetail()
    {
        // UIManager.GotoView("ProfileDetailUI");
    }

    private void OnInventoryChanged()
    {
        ShowToast("Inventory updated.");
    }

    private void ShowToast(string message)
    {
        if (toastText == null)
            return;

        toastText.gameObject.SetActive(true);
        UIHelper.SetText(toastText, message);
    }

}
