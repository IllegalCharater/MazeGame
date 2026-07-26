using System.Collections.Generic;
using UnityEngine.UI;

public sealed class ItemSocketPuzzleUIController : MazePuzzleUIControllerBase
{
    private const int OptionCount = 4;
    private readonly List<Button> optionButtons = new List<Button>();
    private readonly List<Text> optionTexts = new List<Text>();
    private Button removeButton;

    protected override string ExpectedPuzzleType => ItemSocketPuzzleSystem.TypeId;
    protected override string DefaultTitle => "解密一 · 圆坛置物";

    protected override void BindPuzzleUI()
    {
        for (int i = 0; i < OptionCount; i++)
        {
            optionButtons.Add(FindButton("OptionButton_" + i));
            optionTexts.Add(FindText("OptionText_" + i));
        }
        removeButton = FindButton("RemoveButton");
    }

    protected override void MapPuzzleEvents()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i;
            BindClickEvent(optionButtons[i], () => Select(index));
        }
        BindClickEvent(removeButton, Clear);
    }

    protected override void RenderPuzzle(MazePuzzleRoomViewModel vm)
    {
        for (int i = 0; i < optionButtons.Count; i++)
            SetOptionButton(optionButtons[i], optionTexts[i], i < vm.options.Count ? vm.options[i] : null, vm.isSolved);
        if (removeButton != null)
            removeButton.interactable = !vm.isSolved && !string.IsNullOrEmpty(vm.selectedInput);
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
