using System.Collections.Generic;
using UnityEngine.UI;

public sealed class FloorChoicePuzzleUIController : MazePuzzleUIControllerBase
{
    private const int OptionCount = 8;
    private readonly List<Button> optionButtons = new List<Button>();
    private readonly List<Text> optionTexts = new List<Text>();

    protected override string ExpectedPuzzleType => FloorChoicePuzzleSystem.TypeId;
    protected override string DefaultTitle => "解密三 · 八方择信";

    protected override void BindPuzzleUI()
    {
        for (int i = 0; i < OptionCount; i++)
        {
            optionButtons.Add(FindButton("OptionButton_" + i));
            optionTexts.Add(FindText("OptionText_" + i));
        }
    }

    protected override void MapPuzzleEvents()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i;
            BindClickEvent(optionButtons[i], () => Select(index));
        }
    }

    protected override void RenderPuzzle(MazePuzzleRoomViewModel vm)
    {
        if (submitButton != null)
            submitButton.gameObject.SetActive(false);
        for (int i = 0; i < optionButtons.Count; i++)
            SetOptionButton(optionButtons[i], optionTexts[i], i < vm.options.Count ? vm.options[i] : null, vm.isSolved);
    }

    private void Select(int index)
    {
        if (commands == null || viewModel == null || index < 0 || index >= viewModel.options.Count)
            return;
        Execute(commands.Execute(new ChooseFloorPuzzleTileCommand(viewModel.puzzleId, viewModel.options[index].key)));
    }
}
