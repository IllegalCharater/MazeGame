using System.Collections.Generic;
using UnityEngine.UI;

public sealed class RockWordPuzzleUIController : MazePuzzleUIControllerBase
{
    private const int OptionCount = 7;
    private readonly List<Button> optionButtons = new List<Button>();
    private readonly List<Text> optionTexts = new List<Text>();
    private Button lightButton;

    protected override string ExpectedPuzzleType => RockWordPuzzleSystem.TypeId;
    protected override string DefaultTitle => "解密四 · 礁石遮字";

    protected override void BindPuzzleUI()
    {
        for (int i = 0; i < OptionCount; i++)
        {
            optionButtons.Add(FindButton("OptionButton_" + i));
            optionTexts.Add(FindText("OptionText_" + i));
        }
        lightButton = FindButton("LightButton");
    }

    protected override void MapPuzzleEvents()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i;
            BindClickEvent(optionButtons[i], () => Toggle(index));
        }
        BindClickEvent(lightButton, ActivateLight);
    }

    protected override void RenderPuzzle(MazePuzzleRoomViewModel vm)
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            MazePuzzleOptionViewModel option = i < vm.options.Count ? vm.options[i] : null;
            SetOptionButton(optionButtons[i], optionTexts[i], option, vm.isSolved || !vm.lightActivated);
            if (optionButtons[i] != null && option != null)
                optionButtons[i].interactable = vm.lightActivated && !vm.isSolved;
        }
        if (lightButton != null)
        {
            lightButton.interactable = !vm.isSolved && !vm.lightActivated;
            Text label = UIHelper.FindText(lightButton.transform, "LightButtonText");
            UIHelper.SetText(label, vm.lightActivated ? "光束已开启" : "点亮烛台");
        }
    }

    private void Toggle(int index)
    {
        if (commands == null || viewModel == null || index < 0 || index >= viewModel.options.Count)
            return;
        Execute(commands.Execute(new ToggleRockWordPuzzleBlockCommand(viewModel.puzzleId, viewModel.options[index].key)));
    }

    private void ActivateLight()
    {
        if (commands != null && viewModel != null)
            Execute(commands.Execute(new ActivateRockWordPuzzleLightCommand(viewModel.puzzleId)));
    }
}
