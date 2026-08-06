using UnityEngine;
using UnityEngine.UI;

public class MainMenuView : View {
    GameObject bottomArea;
    public Button MazeEntry;
    public Button KitchenEntry;

    public override void BindViewUI() {
        bottomArea = GetChildByPath("Bottom Navigation");
        MazeEntry = GetChildByPath("Maze Entry").GetComponent<Button>();
        KitchenEntry = GetChildByPath("Kitchen Entry").GetComponent<Button>();
    }



}