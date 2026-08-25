public class MazePuzzleItemSocketController : Controller {
    private MazePuzzleItemSocketView view => _view as MazePuzzleItemSocketView;
    protected override void BindEvents() {
        BindClickEvent(view.CloseButton, Dispose);
    }

    public MazePuzzleItemSocketController(Model model, View view) : base(model, view) {
    }
}