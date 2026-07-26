using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public abstract class MazePuzzleUIControllerBase : BaseUIController
{
    public override string layerName => UIConfig.PopupLayerName;
    public override bool hasInputBlocker => true;

    protected MazeService maze;
    protected CommandBus commands;
    protected EventBus events;
    protected MazePuzzleRoomViewModel viewModel;

    private Text titleText;
    private Text hintText;
    private Text selectionText;
    private Text feedbackText;
    private Text rewardText;
    private Text energyText;
    protected Button submitButton;
    private Button closeButton;

    protected abstract string ExpectedPuzzleType { get; }
    protected abstract string DefaultTitle { get; }

    public override void BindUI()
    {
        titleText = FindText("TitleText");
        hintText = FindText("HintText");
        selectionText = FindText("SelectionText");
        feedbackText = FindText("FeedbackText");
        rewardText = FindText("RewardText");
        energyText = FindText("EnergyText");
        submitButton = FindButton("SubmitButton");
        closeButton = FindButton("CloseButton");
        BindPuzzleUI();
    }

    public override void EventMapper()
    {
        BindClickEvent(closeButton, Close);
        BindClickEvent(submitButton, Submit);
        MapPuzzleEvents();
    }

    public override void OnOpen()
    {
        GameServices services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        maze = services?.Maze;
        commands = services?.Commands;
        events = services?.Events;
        events?.Subscribe<MazePuzzleStateChangedEvent>(OnPuzzleStateChanged);
        RefreshPuzzle();
    }

    public override void OnRefresh()
    {
        RefreshPuzzle();
    }

    public override void Dismiss()
    {
        events?.Unsubscribe<MazePuzzleStateChangedEvent>(OnPuzzleStateChanged);
        base.Dismiss();
    }

    protected abstract void BindPuzzleUI();
    protected abstract void MapPuzzleEvents();
    protected abstract void RenderPuzzle(MazePuzzleRoomViewModel vm);

    protected void Execute(CommandResult result)
    {
        if (result?.payload is MazePuzzleRoomViewModel puzzleViewModel)
            Render(puzzleViewModel);
        else
            RefreshPuzzle();
    }

    protected string CurrentPuzzleId()
    {
        return viewModel != null ? viewModel.puzzleId : string.Empty;
    }

    protected static void SetOptionButton(Button button, Text label, MazePuzzleOptionViewModel option, bool solved)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(option != null);
        if (option == null)
            return;

        UIHelper.SetText(label, option.label);
        button.interactable = !solved && option.available;
        Image image = button.GetComponent<Image>();
        if (image == null)
            return;
        if (!option.available)
            image.color = new Color(0.28f, 0.28f, 0.28f, 1f);
        else if (option.failed)
            image.color = new Color(0.72f, 0.18f, 0.18f, 1f);
        else if (option.selected)
            image.color = new Color(0.85f, 0.72f, 0.25f, 1f);
        else
            image.color = new Color(0.32f, 0.38f, 0.46f, 1f);
    }

    private void Submit()
    {
        if (commands == null || viewModel == null)
            return;
        Execute(commands.Execute(new SubmitMazePuzzleCommand(viewModel.puzzleId)));
    }

    private void Close()
    {
        UIManager.CloseView(viewName);
    }

    private void RefreshPuzzle()
    {
        MazePuzzleRoomViewModel current = maze != null ? maze.GetCurrentPuzzleViewModel() : null;
        if (current == null || current.puzzleType != ExpectedPuzzleType)
        {
            UIHelper.SetText(feedbackText, "Current maze node does not contain this puzzle.");
            if (submitButton != null)
                submitButton.interactable = false;
            return;
        }
        Render(current);
    }

    private void Render(MazePuzzleRoomViewModel vm)
    {
        if (vm == null || vm.puzzleType != ExpectedPuzzleType)
            return;

        viewModel = vm;
        UIHelper.SetText(titleText, DefaultTitle);
        UIHelper.SetText(hintText, vm.hintText);
        UIHelper.SetText(selectionText, string.IsNullOrEmpty(vm.selectedInput) ? "Selection: none" : "Selection: " + vm.selectedInput);
        UIHelper.SetText(feedbackText, vm.feedback);
        UIHelper.SetText(rewardText, "Reward: " + FormatRewards(vm.successRewards) + "\nFragment: " + vm.fragmentText);
        UIHelper.SetText(energyText, "Energy: " + vm.currentEnergy + "/" + vm.maxEnergy);
        if (submitButton != null)
            submitButton.interactable = vm.canSubmit && !vm.isSolved;
        RenderPuzzle(vm);
    }

    private void OnPuzzleStateChanged(MazePuzzleStateChangedEvent evt)
    {
        if (evt.puzzleType == ExpectedPuzzleType)
            Render(evt.viewModel);
    }

    private static string FormatRewards(Dictionary<string, int> rewards)
    {
        if (rewards == null || rewards.Count == 0)
            return "none";

        StringBuilder builder = new StringBuilder();
        foreach (KeyValuePair<string, int> reward in rewards)
        {
            if (builder.Length > 0)
                builder.Append(", ");
            builder.Append(reward.Key);
            builder.Append(" x");
            builder.Append(reward.Value);
        }
        return builder.ToString();
    }
}

