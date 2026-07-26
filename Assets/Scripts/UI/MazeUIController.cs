using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed class MazeUIController : BaseUIController, IMazeView
{
    private const int MaxNodeButtons = 16;

    public override bool hasInputBlocker => true;

    private readonly Dictionary<int, Button> nodeButtons = new Dictionary<int, Button>();
    private readonly Dictionary<int, Text> nodeLabels = new Dictionary<int, Text>();

    private Text energyText;
    private Text lootText;
    private Text fragmentsText;
    private Text nodeDetailText;
    private Text messageText;
    private Text nodeActionTitleText;
    private Text nodeActionBodyText;
    private Text nodeActionPrimaryButtonText;
    private Text nodeActionSecondaryButtonText;
    private Text resultTitleText;
    private Text resultBodyText;

    private GameObject nodeActionPanel;
    private GameObject resultPanel;

    private Button rewardGameplayButton;
    private Button switchGameplayButton;
    private Button puzzleGameplayButton;
    private Button trapGameplayButton;
    private Button evacuateGameplayButton;
    private Button exitGameplayButton;
    private Button evacuateButton;
    private Button perfectExitButton;
    private Button returnToShopButton;
    private Button nodeActionPrimaryButton;
    private Button nodeActionSecondaryButton;
    private Button nodeActionCloseButton;

    private MazeMediator mediator;
    private MazeViewModel lastViewModel;

    public override void BindUI()
    {
        CacheReferences();
    }

    public override void EventMapper()
    {
        WireNodeButtons();
        WireGameplayButtons();
    }

    public override void OnOpen()
    {
        CreateMediator();
        mediator.Initialize();
    }

    public override void OnRefresh()
    {
        mediator?.Refresh();
    }

    public override void Dismiss()
    {
        mediator?.Dispose();
        mediator = null;
        base.Dismiss();
    }

    public void Render(MazeViewModel viewModel)
    {
        lastViewModel = viewModel ?? CreateEmptyViewModel();
        UpdateSummary(lastViewModel);
        UpdateNodeButtons(lastViewModel);
        UpdateGameplayButtons(lastViewModel);

        if (resultPanel != null)
            resultPanel.SetActive(lastViewModel.result != null || lastViewModel.isEnded);
        if (lastViewModel.result != null)
            RenderResult(lastViewModel.result);

        if (nodeActionPanel != null && nodeActionPanel.activeSelf)
            RenderNodeActionPanel(lastViewModel);
    }

    public void ShowNodeActions(MazeViewModel viewModel)
    {
        lastViewModel = viewModel ?? lastViewModel ?? CreateEmptyViewModel();
        if (nodeActionPanel != null)
            nodeActionPanel.SetActive(true);
        RenderNodeActionPanel(lastViewModel);
    }

    public void HideNodeActions()
    {
        if (nodeActionPanel != null)
            nodeActionPanel.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        UIHelper.SetText(messageText, message);
        Debug.Log("[MazeUI] " + message);
    }

    public void ShowResult(MazeRunResult result)
    {
        if (result == null)
            return;

        if (resultPanel != null)
            resultPanel.SetActive(true);
        RenderResult(result);
    }

    private void CacheReferences()
    {
        nodeButtons.Clear();
        nodeLabels.Clear();
        for (int i = 1; i <= MaxNodeButtons; i++)
        {
            Button button = FindButton("MazeNode_" + i);
            Text label = FindText("MazeNode_" + i + "Text");
            if (button != null)
                nodeButtons[i] = button;
            if (label != null)
                nodeLabels[i] = label;
        }

        energyText = FindText("EnergyText");
        lootText = FindText("LootText");
        fragmentsText = FindText("FragmentsText");
        nodeDetailText = FindText("NodeDetailText");
        messageText = FindText("MessageText");
        nodeActionTitleText = FindText("NodeActionTitleText");
        nodeActionBodyText = FindText("NodeActionBodyText");
        nodeActionPrimaryButtonText = FindText("NodeActionPrimaryButtonText");
        nodeActionSecondaryButtonText = FindText("NodeActionSecondaryButtonText");
        resultTitleText = FindText("ResultTitleText");
        resultBodyText = FindText("ResultBodyText");

        nodeActionPanel = FindObject("NodeActionPanel");
        resultPanel = FindObject("ResultPanel");

        rewardGameplayButton = FindButton("RewardGameplayButton");
        switchGameplayButton = FindButton("SwitchGameplayButton");
        puzzleGameplayButton = FindButton("PuzzleGameplayButton");
        trapGameplayButton = FindButton("TrapGameplayButton");
        evacuateGameplayButton = FindButton("EvacuateGameplayButton");
        exitGameplayButton = FindButton("ExitGameplayButton");
        evacuateButton = FindButton("EvacuateButton");
        perfectExitButton = FindButton("PerfectExitButton");
        returnToShopButton = FindButton("ReturnToShopButton");
        nodeActionPrimaryButton = FindButton("NodeActionPrimaryButton");
        nodeActionSecondaryButton = FindButton("NodeActionSecondaryButton");
        nodeActionCloseButton = FindButton("NodeActionCloseButton");

        if (nodeActionPanel != null)
            nodeActionPanel.SetActive(false);
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void WireNodeButtons()
    {
        foreach (KeyValuePair<int, Button> kv in nodeButtons)
        {
            int index = kv.Key;
            BindClickEvent(kv.Value, () => mediator?.OnNodeClicked(index));
        }
    }

    private void WireGameplayButtons()
    {
        BindClickEvent(rewardGameplayButton, () => mediator?.CollectReward());
        BindClickEvent(switchGameplayButton, () => mediator?.ActivateSwitch());
        BindClickEvent(puzzleGameplayButton, () => mediator?.OpenCurrentPuzzle());
        BindClickEvent(trapGameplayButton, () => mediator?.StartTrap());
        BindClickEvent(evacuateGameplayButton, () => mediator?.Evacuate());
        BindClickEvent(exitGameplayButton, () => mediator?.OpenExitPuzzle());
        BindClickEvent(evacuateButton, () => mediator?.Evacuate());
        BindClickEvent(perfectExitButton, () => mediator?.AssembleExitPuzzle());
        BindClickEvent(returnToShopButton, () => mediator?.ReturnToShop());
        BindClickEvent(nodeActionCloseButton, HideNodeActions);
    }

    private void CreateMediator()
    {
        GameServices services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        mediator?.Dispose();
        mediator = new MazeMediator(this, services?.Maze, services?.Commands, services?.Events);
    }

    private void UpdateSummary(MazeViewModel vm)
    {
        UIHelper.SetText(energyText, "Energy: " + vm.currentEnergy + "/" + vm.maxEnergy);
        UIHelper.SetText(lootText, "Loot: " + FormatRewards(vm.loot));
        UIHelper.SetText(fragmentsText, "Fragments: " + vm.fragmentCount + "/" + vm.totalFragmentCount + " " + (vm.fragments.Count == 0 ? "none" : string.Join(", ", vm.fragments)));
        UIHelper.SetText(messageText, string.IsNullOrEmpty(vm.message) ? "Maze is ready." : vm.message);

        if (!vm.hasRun)
        {
            UIHelper.SetText(nodeDetailText, "Maze run has not started.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("Current: #");
        sb.Append(vm.currentNodeIndex);
        sb.Append(" ");
        sb.AppendLine(string.IsNullOrEmpty(vm.currentNodeTitle) ? vm.currentNodeId : vm.currentNodeTitle);
        if (!string.IsNullOrEmpty(vm.currentNodeType))
        {
            sb.Append("Type: ");
            sb.AppendLine(vm.currentNodeType);
        }
        if (!string.IsNullOrEmpty(vm.currentNodeNote))
            sb.AppendLine(vm.currentNodeNote);
        if (vm.reachableNodeIds.Count > 0)
        {
            sb.Append("Next: ");
            for (int i = 0; i < vm.reachableNodeIds.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(ResolveNodeLabel(vm, vm.reachableNodeIds[i]));
            }
            sb.AppendLine();
        }
        if (!string.IsNullOrEmpty(vm.exitPuzzleMessage))
            sb.AppendLine(vm.exitPuzzleMessage);
        if (!string.IsNullOrEmpty(vm.trapMessage))
            sb.AppendLine(vm.trapMessage);

        UIHelper.SetText(nodeDetailText, sb.ToString());
    }

    private void UpdateNodeButtons(MazeViewModel vm)
    {
        Dictionary<int, string> idsByIndex = new Dictionary<int, string>();
        foreach (KeyValuePair<string, int> kv in vm.nodeIndices)
            idsByIndex[kv.Value] = kv.Key;

        for (int i = 1; i <= MaxNodeButtons; i++)
        {
            nodeButtons.TryGetValue(i, out Button button);
            nodeLabels.TryGetValue(i, out Text label);
            bool hasNode = idsByIndex.TryGetValue(i, out string nodeId);
            if (button != null)
                button.interactable = hasNode && vm.hasRun && !vm.isEnded;
            if (label == null)
                continue;

            if (!hasNode)
            {
                label.text = "Node " + i;
                continue;
            }

            string prefix = string.Empty;
            if (vm.currentNodeId == nodeId)
                prefix = "Here\n";
            else if (vm.reachableNodeIds.Contains(nodeId))
                prefix = "Next\n";
            else if (vm.visitedNodeIds.Contains(nodeId))
                prefix = "Seen\n";

            label.text = prefix + ResolveNodeLabel(vm, nodeId);
        }
    }

    private void UpdateGameplayButtons(MazeViewModel vm)
    {
        bool running = vm.hasRun && !vm.isEnded && vm.state == MazeRunState.Running;
        SetButtonState(rewardGameplayButton, running && vm.canCollectReward);
        SetButtonState(switchGameplayButton, running && vm.canActivateSwitch);
        SetButtonState(puzzleGameplayButton, running && vm.canSubmitPuzzle);
        SetButtonState(trapGameplayButton, running && (vm.canStartTrap || vm.canResolveTrap));
        SetButtonState(evacuateGameplayButton, running && vm.canEvacuate);
        SetButtonState(exitGameplayButton, running && vm.canOpenExitPuzzle);
        SetButtonState(evacuateButton, running && vm.canEvacuate);
        SetButtonState(perfectExitButton, running && vm.canAssemblePuzzle);
        SetButtonState(returnToShopButton, true);
    }

    private void RenderNodeActionPanel(MazeViewModel vm)
    {
        UIHelper.SetText(nodeActionTitleText, string.IsNullOrEmpty(vm.currentNodeTitle) ? "Current Node" : vm.currentNodeTitle);
        UIHelper.SetText(nodeActionBodyText, BuildActionBody(vm));

        ClearClickEvent(nodeActionPrimaryButton);
        ClearClickEvent(nodeActionSecondaryButton);

        List<ActionEntry> actions = BuildActions(vm);
        if (actions.Count == 0)
            actions.Add(new ActionEntry("Close", HideNodeActions));

        BindActionButton(nodeActionPrimaryButton, nodeActionPrimaryButtonText, actions[0]);
        if (actions.Count > 1)
            BindActionButton(nodeActionSecondaryButton, nodeActionSecondaryButtonText, actions[1]);
        else
            SetButtonState(nodeActionSecondaryButton, false);
    }

    private List<ActionEntry> BuildActions(MazeViewModel vm)
    {
        List<ActionEntry> actions = new List<ActionEntry>();
        if (vm.canUseFood)
            actions.Add(new ActionEntry("Use Food", () => mediator?.UseRecommendedFood()));
        if (vm.canCollectReward)
            actions.Add(new ActionEntry("Collect", () => mediator?.CollectReward()));
        if (vm.canActivateSwitch)
            actions.Add(new ActionEntry("Activate", () => mediator?.ActivateSwitch()));
        if (vm.canSubmitPuzzle)
            actions.Add(new ActionEntry("Solve", () => mediator?.OpenCurrentPuzzle()));
        if (vm.canStartTrap)
            actions.Add(new ActionEntry("Start Trap", () => mediator?.StartTrap()));
        if (vm.canResolveTrapSuccess)
            actions.Add(new ActionEntry("Trap Success", () => mediator?.ResolveTrapAsSuccess()));
        if (vm.canResolveTrapFailure)
            actions.Add(new ActionEntry("Trap Failure", () => mediator?.ResolveTrapAsFailure()));
        if (vm.canEvacuate)
            actions.Add(new ActionEntry("Evacuate", () => mediator?.Evacuate()));
        if (vm.canOpenExitPuzzle)
            actions.Add(new ActionEntry("Exit Puzzle", () => mediator?.OpenExitPuzzle()));
        if (vm.canAssemblePuzzle)
            actions.Add(new ActionEntry("Perfect Clear", () => mediator?.AssembleExitPuzzle()));
        if (vm.canLeaveWithoutPerfect)
            actions.Add(new ActionEntry(vm.canAssemblePuzzle ? "Leave 100%" : "Leave Now", () => mediator?.LeaveWithoutPerfect()));
        return actions;
    }

    private string BuildActionBody(MazeViewModel vm)
    {
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(vm.currentNodeType))
        {
            sb.Append("Type: ");
            sb.AppendLine(vm.currentNodeType);
        }
        if (!string.IsNullOrEmpty(vm.currentNodeNote))
            sb.AppendLine(vm.currentNodeNote);
        if (!string.IsNullOrEmpty(vm.puzzleId))
        {
            sb.Append("Puzzle: ");
            sb.AppendLine(vm.puzzleId);
        }
        if (!string.IsNullOrEmpty(vm.trapId))
        {
            sb.Append("Trap: ");
            sb.AppendLine(vm.trapId);
        }
        if (!string.IsNullOrEmpty(vm.trapMessage))
            sb.AppendLine(vm.trapMessage);
        if (!string.IsNullOrEmpty(vm.trapGuideNodeId))
        {
            sb.Append("Guide: ");
            sb.AppendLine(vm.trapGuideNodeId);
        }
        if (!string.IsNullOrEmpty(vm.exitPuzzleMessage))
            sb.AppendLine(vm.exitPuzzleMessage);
        if (vm.totalFragmentCount > 0)
        {
            sb.Append("Puzzle progress: ");
            sb.Append(vm.fragmentCount);
            sb.Append("/");
            sb.AppendLine(vm.totalFragmentCount.ToString());
        }
        if (vm.canUseFood && !string.IsNullOrEmpty(vm.recommendedFoodId))
        {
            sb.Append("Food: ");
            sb.AppendLine(vm.recommendedFoodId);
        }
        if (sb.Length == 0)
            sb.Append("No action available.");
        return sb.ToString();
    }

    private void BindActionButton(Button button, Text label, ActionEntry action)
    {
        if (button == null)
            return;

        SetButtonState(button, true);
        UIHelper.SetText(label, action.label);
        BindClickEvent(button, () =>
        {
            HideNodeActions();
            action.action?.Invoke();
        });
    }

    private void RenderResult(MazeRunResult result)
    {
        UIHelper.SetText(resultTitleText, "Maze " + result.endReason);

        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(result.settlementDescription))
            sb.AppendLine(result.settlementDescription);
        sb.Append("State: ");
        sb.AppendLine(result.state.ToString());
        sb.Append("Multiplier: ");
        sb.AppendLine(result.multiplier.ToString("0.##"));
        sb.Append("Perfect: ");
        sb.AppendLine(result.perfectClear ? "yes" : "no");
        sb.Append("Rewards: ");
        sb.AppendLine(FormatRewards(result.finalRewards));
        sb.Append("Fragments: ");
        sb.AppendLine(result.fragments.Count == 0 ? "none" : string.Join(", ", result.fragments));
        if (result.lostFragments.Count > 0)
        {
            sb.Append("Lost: ");
            sb.AppendLine(string.Join(", ", result.lostFragments));
        }
        if (!string.IsNullOrEmpty(result.blueprintId))
        {
            sb.Append("Blueprint: ");
            sb.AppendLine(result.blueprintId);
        }
        UIHelper.SetText(resultBodyText, sb.ToString());
    }

    private static void SetButtonState(Button button, bool visible)
    {
        if (button == null)
            return;

        if (button.gameObject != null)
            button.gameObject.SetActive(visible);
        button.interactable = visible;
    }

    private static string FormatRewards(Dictionary<string, int> rewards)
    {
        if (rewards == null || rewards.Count == 0)
            return "none";

        StringBuilder sb = new StringBuilder();
        bool first = true;
        foreach (KeyValuePair<string, int> kv in rewards)
        {
            if (!first)
                sb.Append(", ");
            sb.Append(kv.Key);
            sb.Append(" x");
            sb.Append(kv.Value);
            first = false;
        }
        return sb.ToString();
    }

    private static string ResolveNodeLabel(MazeViewModel vm, string nodeId)
    {
        if (vm.nodeTitles.TryGetValue(nodeId, out string title) && !string.IsNullOrEmpty(title))
            return title;
        return nodeId;
    }

    private static MazeViewModel CreateEmptyViewModel()
    {
        return new MazeViewModel
        {
            state = MazeRunState.NotStarted,
            message = "Maze is ready."
        };
    }

    private sealed class ActionEntry
    {
        public readonly string label;
        public readonly Action action;

        public ActionEntry(string label, Action action)
        {
            this.label = label;
            this.action = action;
        }
    }
}
