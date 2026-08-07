using UnityEngine;
using UnityEngine.UI;

public class HUDView : View {
    GameObject profileCard;
    GameObject coinBar;

    private Text playerNameText;
    private Text levelText;
    private Text currencyText;
    private Text energyText;
    private Text stateHintText;
    private Text toastText;
    public Button profileButton;

    // private string mazeHint = "Maze: explore and collect ingredients.";
    // private string shopHint = "Shop: craft, display, and sell food.";
    // private string menuHint = "Main Menu";
    private string levelFormat = "Lv.{0}";
    private string currencyFormat = "{0:0}";
    private string energyFormat = "{0}/{1}";

    protected override void BindViewUI() {
        profileCard = GetChildByPath("Profile Card");
        coinBar = GetChildByPath("Coin Bar");
        playerNameText = GetChildByPath("Player Name", profileCard)?.GetComponent<Text>();
        levelText = GetChildByPath("Level Text", profileCard)?.GetComponent<Text>();
        energyText = GetChildByPath("Exp Text", profileCard)?.GetComponent<Text>();
        profileButton = profileCard?.GetComponent<Button>();
        currencyText = coinBar.GetChildByPath("Gold Text").GetComponent<Text>();
    }

    public void UpdateProfile(PlayerProfile profile) {
        string displayName = string.IsNullOrEmpty(profile.playerDisplayName) ? string.Empty : profile.playerDisplayName;
        SetText(playerNameText, displayName);
        SetText(levelText, string.Format(levelFormat, Mathf.Max(1, profile.level)));
        UpdateCurrency(profile.currency);
        UpdateEnergy(profile.energy, profile.maxEnergy);
    }

    public void UpdateCurrency(float value) {
        SetText(currencyText, string.Format(currencyFormat, value));
    }

    public void UpdateEnergy(int current, int max) {
        SetText(energyText, string.Format(energyFormat, current, max));
    }

}