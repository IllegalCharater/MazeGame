public class HUDModel : Model {
    public override string Layer => UIConfig.TopLayerName;

    public string playerName;
    public int playerLevel;
    public int playerExp;
    public int playerCurrency;

    public HUDModel(string viewName, EventBus events, CommandBus commands) : base(viewName, events, commands) { }
}