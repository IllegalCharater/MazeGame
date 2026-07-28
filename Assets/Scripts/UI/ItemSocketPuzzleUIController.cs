using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ItemSocketPuzzleUIController : MazePuzzleUIControllerBase
{
    private const int SlotCount = ItemSocketPuzzleSystem.SlotCount;
    private readonly List<Button> slotButtons = new List<Button>();
    private readonly List<Text> slotTexts = new List<Text>();
    private readonly List<Button> itemButtons = new List<Button>();
    private readonly List<Text> itemTexts = new List<Text>();
    private Button removeButton;

    protected override string ExpectedPuzzleType => ItemSocketPuzzleSystem.TypeId;
    protected override string DefaultTitle => "解密一 · 圆坛置物";

    protected override void BindPuzzleUI()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            slotButtons.Add(FindButton("SlotButton_" + i));
            slotTexts.Add(FindText("SlotText_" + i));
            itemButtons.Add(FindButton("ItemButton_" + i));
            itemTexts.Add(FindText("ItemText_" + i));
        }
        removeButton = FindButton("RemoveButton");
    }

    protected override void MapPuzzleEvents()
    {
        for (int i = 0; i < itemButtons.Count; i++)
        {
            int index = i;
            BindClickEvent(itemButtons[i], () => Select(index));
        }
        // 点已填充的槽位＝把那件道具取回托盘，省掉"清空重摆"的来回。
        for (int i = 0; i < slotButtons.Count; i++)
        {
            int index = i;
            BindClickEvent(slotButtons[i], () => ClearSlot(index));
        }
        BindClickEvent(removeButton, ClearAll);
    }

    protected override void RenderPuzzle(MazePuzzleRoomViewModel vm)
    {
        for (int i = 0; i < itemButtons.Count; i++)
            SetOptionButton(itemButtons[i], itemTexts[i], i < vm.options.Count ? vm.options[i] : null, vm.isSolved);

        RenderSlots(vm);
        if (removeButton != null)
            removeButton.interactable = !vm.isSolved && HasAnyPlaced(vm);
    }

    private void RenderSlots(MazePuzzleRoomViewModel vm)
    {
        for (int i = 0; i < slotButtons.Count; i++)
        {
            Button slot = slotButtons[i];
            if (slot == null)
                continue;

            string placed = i < vm.slotItemKeys.Count ? vm.slotItemKeys[i] : string.Empty;
            bool filled = !string.IsNullOrEmpty(placed);
            UIHelper.SetText(slotTexts[i], filled ? ResolvePlacedLabel(vm, placed) : "+");
            // 空槽不接受点击：摆放入口是托盘按钮，槽位只负责取回。
            slot.interactable = filled && !vm.isSolved;
            Image image = slot.GetComponent<Image>();
            if (image != null)
                image.color = filled
                    ? new Color(0.85f, 0.72f, 0.25f, 1f)
                    : new Color(0.32f, 0.38f, 0.46f, 1f);
        }
    }

    private static bool HasAnyPlaced(MazePuzzleRoomViewModel vm)
    {
        for (int i = 0; i < vm.slotItemKeys.Count; i++)
        {
            if (!string.IsNullOrEmpty(vm.slotItemKeys[i]))
                return true;
        }
        return false;
    }

    private static string ResolvePlacedLabel(MazePuzzleRoomViewModel vm, string key)
    {
        for (int i = 0; i < vm.options.Count; i++)
        {
            if (vm.options[i] != null && vm.options[i].key == key)
                return vm.options[i].label;
        }
        return key;
    }

    private void Select(int index)
    {
        if (commands == null || viewModel == null || index < 0 || index >= viewModel.options.Count)
            return;
        Execute(commands.Execute(new SelectItemSocketPuzzleItemCommand(viewModel.puzzleId, viewModel.options[index].key)));
    }

    private void ClearSlot(int slotIndex)
    {
        if (commands != null && viewModel != null)
            Execute(commands.Execute(new RemoveItemSocketPuzzleItemCommand(viewModel.puzzleId, slotIndex)));
    }

    private void ClearAll()
    {
        if (commands != null && viewModel != null)
            Execute(commands.Execute(new RemoveItemSocketPuzzleItemCommand(viewModel.puzzleId)));
    }
}
