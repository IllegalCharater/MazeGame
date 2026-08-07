using System.Collections.Generic;
using UnityEngine;

public class MazeView : View {

    public List<GameObject> mazeNodes;

    protected override void BindViewUI() {
        GameObject nodesRoot = GetChildByPath("MazeMapRoot/MazeNodeRoot");
        mazeNodes = nodesRoot.GetChildren();
    }
}