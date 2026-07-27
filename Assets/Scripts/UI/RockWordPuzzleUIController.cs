using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class RockWordPuzzleUIController : MazePuzzleUIControllerBase
{
    private const int OptionCount = 7;
    private const int RockCount = 3;
    // 策划案图十一的纸条原文。配置表的 hintText 目前写的是文档正文的描述性说明，
    // 直接把答案写了出来；等第二阶段修表后这里改读 vm.hintText。
    private const string NoteText = "帝名相触深渊路，旧讳当封";

    private readonly List<Button> optionButtons = new List<Button>();
    private readonly List<Text> optionTexts = new List<Text>();
    private readonly List<Text> rockIndicators = new List<Text>();
    private Button lightButton;
    private Button noteButton;

    protected override string ExpectedPuzzleType => RockWordPuzzleSystem.TypeId;
    protected override string DefaultTitle => "解密四 · 礁石遮字";

    protected override void BindPuzzleUI()
    {
        for (int i = 0; i < OptionCount; i++)
        {
            optionButtons.Add(FindButton("OptionButton_" + i));
            optionTexts.Add(FindText("OptionText_" + i));
        }
        for (int i = 0; i < RockCount; i++)
            rockIndicators.Add(FindText("RockIndicator_" + i));
        lightButton = FindButton("LightButton");
        noteButton = FindButton("NoteButton");
    }

    protected override void MapPuzzleEvents()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i;
            BindClickEvent(optionButtons[i], () => Toggle(index));
        }
        BindClickEvent(lightButton, ActivateLight);
        BindClickEvent(noteButton, ReadNote);
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

        RenderRocks(vm);
        if (noteButton != null)
            noteButton.interactable = !vm.isSolved;
        if (lightButton != null)
        {
            lightButton.interactable = !vm.isSolved && !vm.lightActivated;
            Text label = UIHelper.FindText(lightButton.transform, "LightButtonText");
            UIHelper.SetText(label, vm.lightActivated ? "光束已开启" : "一排烛台（点亮）");
        }
    }

    // 三块礁石对应"最多只能遮三个字"，把这条资源约束显示出来。
    private void RenderRocks(MazePuzzleRoomViewModel vm)
    {
        int used = CountSelected(vm.selectedInput);
        for (int i = 0; i < rockIndicators.Count; i++)
        {
            Text rock = rockIndicators[i];
            if (rock == null)
                continue;

            bool placed = i < used;
            rock.text = placed ? "礁石\n已用" : "礁石";
            rock.color = placed
                ? new Color(0.85f, 0.72f, 0.25f, 1f)
                : new Color(0.94f, 0.92f, 0.86f, 1f);
        }
    }

    private static int CountSelected(string selectedInput)
    {
        if (string.IsNullOrEmpty(selectedInput))
            return 0;
        return selectedInput.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
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

    private void ReadNote()
    {
        UIHelper.SetText(FindText("FeedbackText"), NoteText);
    }
}
