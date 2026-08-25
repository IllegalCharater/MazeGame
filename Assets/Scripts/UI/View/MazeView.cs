using System.Collections.Generic;
using UnityEngine;

public class MazeView : View {

    private List<GameObject> mazeNodes;
    public GameObject puzzle_1;
    public GameObject Exit;

    protected override void BindViewUI() {
        GameObject nodesRoot = GetChildByPath("MazeMapRoot/MazeNodeRoot");
        mazeNodes = nodesRoot.GetChildren();
        puzzle_1 = mazeNodes[3];
        Exit = mazeNodes[15];
    }
}