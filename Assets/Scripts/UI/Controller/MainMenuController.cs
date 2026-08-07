public class MainMenuController : Controller {
    public override void OnOpenView() {

    }
    public override void OnViewRefresh() {

    }
    protected override void BindEvents() {
        var _view = view as MainMenuView;

        BindClickEvent(_view.MazeEntry, GotoMaze);
        BindClickEvent(_view.KitchenEntry, GotoKitchen);
    }

    void GotoMaze() {
        // ExecuteCommand("CurrencyChangedCommand", new DataBag() { { "type", "add" }, { "amount", 1 } });
        _ = UIManager.GotoView("Maze");
    }
    void GotoKitchen() {

    }
    public MainMenuController(Model model, View view) : base(model, view) {
    }
}