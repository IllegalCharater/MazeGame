using UnityEngine;

public class MazeController : Controller {

    protected override void BindEvents() {
        var _view = view as MazeView;
        // Debug.Log("node name" + _view.mazeNodes[3].name);
        BindClickEvent(_view.mazeNodes[3], nodeFucTest);
        BindClickEvent(_view.mazeNodes[15], () => {
            UIManager.CloseView(model.viewName);
        });
    }
    void nodeFucTest() {
        ExecuteCommand(CommandType.ChangeCurrency, new DataBag { { "type", "add" }, { "amount", 2 } });
    }
    public MazeController(Model model, View view) : base(model, view) {
    }
}