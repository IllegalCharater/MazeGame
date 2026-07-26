using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CandleNumberPuzzleUIController : MazePuzzleUIControllerBase
{
    private const int CandleCount = 9;
    private readonly List<Button> candleButtons = new List<Button>();
    private readonly List<Button> numberButtons = new List<Button>();
    private Button poolButton;
    private Button clearButton;

    protected override string ExpectedPuzzleType => CandleNumberPuzzleSystem.TypeId;
    protected override string DefaultTitle => "解密二 · 九烛深潭";

    protected override void BindPuzzleUI()
    {
        for (int i = 0; i < CandleCount; i++)
            candleButtons.Add(FindButton("CandleButton_" + i));
        for (int i = 0; i <= 9; i++)
            numberButtons.Add(FindButton("NumberButton_" + i));
        poolButton = FindButton("PoolButton");
        clearButton = FindButton("ClearButton");
    }

    protected override void MapPuzzleEvents()
    {
        for (int i = 0; i < candleButtons.Count; i++)
        {
            int index = i;
            BindClickEvent(candleButtons[i], () => ToggleCandle(index));
        }
        for (int i = 0; i < numberButtons.Count; i++)
        {
            int number = i;
            BindClickEvent(numberButtons[i], () => SelectNumber(number));
        }
        BindClickEvent(poolButton, OpenPool);
        BindClickEvent(clearButton, Clear);
    }

    protected override void RenderPuzzle(MazePuzzleRoomViewModel vm)
    {
        for (int i = 0; i < candleButtons.Count; i++)
        {
            Button button = candleButtons[i];
            if (button == null)
                continue;
            button.interactable = !vm.isSolved;
            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = i < vm.candleStates.Count && vm.candleStates[i]
                    ? new Color(1f, 0.74f, 0.2f, 1f)
                    : new Color(0.35f, 0.31f, 0.28f, 1f);
        }
        for (int i = 0; i < numberButtons.Count; i++)
        {
            if (numberButtons[i] != null)
                numberButtons[i].interactable = vm.poolOpened && !vm.isSolved;
        }
        if (poolButton != null)
            poolButton.interactable = !vm.isSolved;
        if (clearButton != null)
            clearButton.interactable = vm.poolOpened && !vm.isSolved && !string.IsNullOrEmpty(vm.selectedInput);
    }

    private void ToggleCandle(int index)
    {
        if (commands != null && viewModel != null)
            Execute(commands.Execute(new ToggleCandlePuzzleCommand(viewModel.puzzleId, index)));
    }

    private void OpenPool()
    {
        if (commands != null && viewModel != null)
            Execute(commands.Execute(new OpenCandlePuzzlePoolCommand(viewModel.puzzleId)));
    }

    private void SelectNumber(int number)
    {
        if (commands != null && viewModel != null)
            Execute(commands.Execute(new SelectCandlePuzzleNumberCommand(viewModel.puzzleId, number.ToString())));
    }

    private void Clear()
    {
        if (commands != null && viewModel != null)
            Execute(commands.Execute(new ClearCandlePuzzleNumberCommand(viewModel.puzzleId)));
    }
}
