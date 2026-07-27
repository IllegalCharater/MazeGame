using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ItemSocketPuzzleUIController : MazePuzzleUIControllerBase
{
    private const int SlotCount = 7;
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
        BindClickEvent(removeButton, Clear);
    }

    protected override void RenderPuzzle(MazePuzzleRoomViewModel vm)
    {
        for (int i = 0; i < itemButtons.Count; i++)
            SetOptionButton(itemButtons[i], itemTexts[i], i < vm.options.Count ? vm.options[i] : null, vm.isSolved);

        RenderSlots(vm);
        if (removeButton != null)
            removeButton.interactable = !vm.isSolved && !string.IsNullOrEmpty(vm.selectedInput);
    }

    // 槽位当前只反映"已放置的道具"这一件事：判定层仍是单选语义，
    // 七槽有序放置要等 ItemSocketPuzzleStateComponent 支持多槽后才能接上。
    private void RenderSlots(MazePuzzleRoomViewModel vm)
    {
        string placed = vm.selectedInput;
        for (int i = 0; i < slotButtons.Count; i++)
        {
            Button slot = slotButtons[i];
            if (slot == null)
                continue;

            bool filled = !string.IsNullOrEmpty(placed) && i == 0;
            UIHelper.SetText(slotTexts[i], filled ? ResolvePlacedLabel(vm, placed) : "+");
            slot.interactable = false;
            Image image = slot.GetComponent<Image>();
            if (image != null)
                image.color = filled
                    ? new Color(0.85f, 0.72f, 0.25f, 1f)
                    : new Color(0.32f, 0.38f, 0.46f, 1f);
        }
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

    private void Clear()
    {
        if (commands != null && viewModel != null)
            Execute(commands.Execute(new RemoveItemSocketPuzzleItemCommand(viewModel.puzzleId)));
    }
}
