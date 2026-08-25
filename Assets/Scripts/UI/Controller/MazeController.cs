using UnityEngine;

public class MazeController : Controller {
    MazeView view => _view as MazeView;
    protected override void BindEvents() {
        // Debug.Log("node name" + _view.mazeNodes[3].name);
        BindClickEvent(view.puzzle_1, EnterPuzzle1);
        BindClickEvent(view.Exit, Dispose);
    }
    void EnterPuzzle1() {
        // ExecuteCommand(CommandType.ChangeCurrency, new DataBag { { "type", "add" }, { "amount", 2 } });
        _ = UIManager.GotoView("MazePuzzleItemSocket");
    }
    public MazeController(Model model, View view) : base(model, view) {
    }
}