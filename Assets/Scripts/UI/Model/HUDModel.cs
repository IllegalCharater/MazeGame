public class HUDModel : Model {
    public override string Layer => UIConfig.TopLayerName;
    public string playerName;
    public int playerLevel;
    public int playerExp;
    public int playerCurrency;

    public override void Initialize() {
        base.Initialize();

        var _profile = GetPlayerData().Profile;
        playerName = _profile.playerDisplayName;
        playerLevel = _profile.level;
        playerExp = _profile.energy;
        playerCurrency = _profile.currency;
    }

    public HUDModel(string viewName, Context context) : base(viewName, context) { }
}