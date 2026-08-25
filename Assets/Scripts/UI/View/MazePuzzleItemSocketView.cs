using System.Collections.Generic;
using UnityEngine;

public class MazePuzzleItemSocketView : View {

    private GameObject panel;
    private List<GameObject> slots;
    public GameObject CloseButton;
    public GameObject itemButton0;
    protected override void BindViewUI() {
        panel = GetChildByPath("PuzzlePanel");
        var _slotRoot = panel.GetChildByPath("Slots");
        slots = _slotRoot.GetChildren();
        itemButton0 = panel.GetChildByPath("ItemButton_0");
        List<RectTransform> points = new();
        for (int i = 0; i < slots.Count; i++) {
            points.Add(slots[i].GetComponent<RectTransform>());
        }
        InteractExtend.Attach<IDragable>(itemButton0, new DataBag { { "points", points } });
        CloseButton = panel.GetChildByPath("CloseButton");
    }
}