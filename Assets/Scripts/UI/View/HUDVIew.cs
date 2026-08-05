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
    // public Button profileButton;

    private string mazeHint = "Maze: explore and collect ingredients.";
    private string shopHint = "Shop: craft, display, and sell food.";
    private string menuHint = "Main Menu";
    private string levelFormat = "Lv.{0}";
    private string currencyFormat = "{0:0}";
    private string energyFormat = "{0}/{1}";

    public override void BindViewUI() {
        profileCard = GetChildByPath("ProfileCard");
        coinBar = GetChildByPath("CoinBar");
        playerNameText = GetChildByPath("Player Name", profileCard)?.GetComponent<Text>();
        levelText = GetChildByPath("Level Text", profileCard)?.GetComponent<Text>();
        energyText = GetChildByPath("Exp Text", profileCard)?.GetComponent<Text>();
        // profileButton = GetChildByPath("Profile Button", profileCard)?.GetComponent<Button>();
    }

    public override void OnViewShow() {
        if (_data == null) return;
        var profile = _data.Get<PlayerProfile>("PlayerProfile");
        UpdateProfile(profile);
        UpdateCurrency(profile.currency);
        UpdateEnergy(profile.energy, profile.maxEnergy);
        UpdateStateHint(GameManager.Instance.CurrentState);
    }

    private void UpdateProfile(PlayerProfile profile) {
        if (profile == null)
            return;

        string displayName = string.IsNullOrEmpty(profile.playerDisplayName)
            ? profile.playerId
            : profile.playerDisplayName;

        SetText(playerNameText, displayName);
        SetText(levelText, string.Format(levelFormat, Mathf.Max(1, profile.level)));
    }

    private void UpdateCurrency(float value) {
        SetText(currencyText, string.Format(currencyFormat, value));
    }

    private void UpdateEnergy(int current, int max) {
        SetText(energyText, string.Format(energyFormat, current, max));
    }

    private void UpdateStateHint(GameState state) {
        if (stateHintText == null)
            return;

        switch (state) {
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

}