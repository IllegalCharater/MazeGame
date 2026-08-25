public class MainMenuController : Controller {
    MainMenuView view => _view as MainMenuView;

    public override void OnOpenView() {

    }
    public override void OnViewRefresh() {

    }
    protected override void BindEvents() {
        BindClickEvent(view.MazeEntry, GotoMaze);
        BindClickEvent(view.KitchenEntry, GotoKitchen);
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