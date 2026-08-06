public class MainMenuController : Controller {
    public override void OnOpenView() {

    }
    public override void OnViewRefresh() {

    }
    public override void BindEvents() {
        var _view = view as MainMenuView;

        BindClickEvent(_view.MazeEntry, GotoMaze);
        BindClickEvent(_view.KitchenEntry, GotoKitchen);
    }

    void GotoMaze() {
        ExecuteCommand("CurrencyChangedCommand", new DataBag().Set<string>("type", "add").Set<int>("amount", 1));
    }

    void GotoKitchen() {

    }
    public MainMenuController(Model model, View view) : base(model, view) {
    }
}