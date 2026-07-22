using System.Collections.Generic;
using UnityEngine;

public sealed class MazeRunService : IGameService
{
    private const int DefaultActionEnergyCost = 1;
    private const string DefaultRuleId = "default";

    private static readonly MazeRuleData FallbackRule = new MazeRuleData
    {
        ruleId = DefaultRuleId,
        beginRunEnergyCost = 1,
        enterRoomEnergyCost = 5,
        timedEnergyCost = 2,
        timedEnergySeconds = 60,
        evacuateMultiplier = 0.5f,
        failMultiplier = 0.3f,
        clearMultiplier = 1f,
        perfectMultiplier = 2f,
        perfectBlueprintId = string.Empty
    };

    private GameDatabase database;
    private PlayerDatabase player;
    private IMazeRewardResolver rewardResolver = new DefaultMazeRewardResolver();
    private MazeRuleData currentRule = FallbackRule;
    private bool rewardsApplied;
    private float timedEnergyAccumulator;

    public MazeRunState State { get; private set; } = MazeRunState.NotStarted;
    public MazeRunResult CurrentResult { get; private set; }

    public void Initialize(GameDatabase database, PlayerDatabase player)
    {
        this.database = database;
        this.player = player;
        currentRule = ResolveRule();
        State = MazeRunState.NotStarted;
        CurrentResult = null;
        rewardsApplied = false;
        timedEnergyAccumulator = 0f;
    }

    public bool BeginRun()
    {
        if (State == MazeRunState.Running)
            return true;

        currentRule = ResolveRule();
        if (!TryConsumeEnergy(currentRule.beginRunEnergyCost))
            return false;

        State = MazeRunState.Running;
        CurrentResult = new MazeRunResult(MazeRunEndReason.Clear);
        rewardsApplied = false;
        timedEnergyAccumulator = 0f;
        GameEvents.RaiseMazeRunChanged();
        if (GetCurrentEnergy() <= 0)
            FailRun();
        return true;
    }

    public bool AddRunLoot(IReadOnlyDictionary<string, int> rewardItems)
    {
        if (!IsRunning())
            return false;

        AddCollectedItems(rewardItems);
        GameEvents.RaiseMazeRunChanged();
        return true;
    }

    public bool InteractNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || !IsRunning())
            return false;

        MazeNodeData node = GetData<MazeNodeData>("maze_nodes", nodeId);
        if (node == null)
            return false;
        if (CurrentResult.visitedNodeIds.Contains(nodeId))
            return false;
        if (!AreRequirementsMet(node.unlockRequirementIds))
            return false;
        if (!IsNodeReachable(nodeId))
            return false;

        if (ConsumesRoomEnergy(node) && !ConsumeActionEnergy(currentRule.enterRoomEnergyCost))
            return false;

        CurrentResult.visitedNodeIds.Add(nodeId);
        AddCollectedItems(node.rewardItems);
        ApplyEnergyDelta(node.energyDelta);
        GameEvents.RaiseMazeRunChanged();
        return State == MazeRunState.Running;
    }

    public bool CanInteractNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || State != MazeRunState.Running || CurrentResult == null)
            return false;

        MazeNodeData node = GetData<MazeNodeData>("maze_nodes", nodeId);
        return node != null
            && !CurrentResult.visitedNodeIds.Contains(nodeId)
            && AreRequirementsMet(node.unlockRequirementIds)
            && IsNodeReachable(nodeId);
    }

    public bool SubmitPuzzleAnswer(string puzzleId, string answer)
    {
        if (!IsRunning())
            return false;

        MazePuzzleData puzzle = GetData<MazePuzzleData>("maze_puzzles", puzzleId);
        if (puzzle == null)
            return false;

        string submitted = NormalizeAnswer(answer);
        string expected = NormalizeAnswer(puzzle.answer);
        if (!string.Equals(submitted, expected))
        {
            ConsumeActionEnergy(Mathf.Max(0, puzzle.failEnergyCost));
            return false;
        }

        return CompletePuzzle(puzzleId);
    }

    public bool CompletePuzzle(string puzzleId)
    {
        if (string.IsNullOrEmpty(puzzleId) || !IsRunning())
            return false;
        if (CurrentResult.completedPuzzleIds.Contains(puzzleId))
            return false;

        MazePuzzleData puzzle = GetData<MazePuzzleData>("maze_puzzles", puzzleId);
        if (puzzle == null)
            return false;

        CurrentResult.completedPuzzleIds.Add(puzzleId);
        AddCollectedItems(puzzle.successRewardItems);
        AddFragment(puzzle.fragmentId, string.IsNullOrEmpty(puzzle.fragmentText) ? null : puzzle.fragmentText);
        GameEvents.RaiseMazeRunChanged();
        return true;
    }

    public bool ResolveTrap(string trapId, bool success)
    {
        if (string.IsNullOrEmpty(trapId) || !IsRunning())
            return false;
        if (CurrentResult.triggeredTrapIds.Contains(trapId))
            return false;

        MazeTrapData trap = GetData<MazeTrapData>("maze_traps", trapId);
        if (trap == null)
            return false;

        CurrentResult.triggeredTrapIds.Add(trapId);
        CurrentResult.guideNodeId = trap.guideNodeId;

        int energyCost = success ? trap.successEnergyCost : trap.failEnergyCost;
        bool stillRunning = ConsumeActionEnergy(energyCost);
        if (success && stillRunning)
            AddCollectedItems(trap.successRewardItems);
        if (!success && trap.failEndRun && State == MazeRunState.Running)
            FailRun();

        GameEvents.RaiseMazeRunChanged();
        return success && State == MazeRunState.Running;
    }

    public bool AddFragment(string fragmentId, string displayText = null)
    {
        if (string.IsNullOrEmpty(fragmentId) || !IsRunning())
            return false;

        bool added = CurrentResult.collectedFragmentIds.Add(fragmentId);
        if (!string.IsNullOrEmpty(displayText))
            CurrentResult.fragmentTexts[fragmentId] = displayText;
        else
        {
            MazeFragmentData fragment = GetData<MazeFragmentData>("maze_fragments", fragmentId);
            if (!string.IsNullOrEmpty(fragment?.displayText))
                CurrentResult.fragmentTexts[fragmentId] = fragment.displayText;
        }

        if (added)
            GameEvents.RaiseMazeRunChanged();
        return added;
    }

    public bool TryComposeExitPuzzle()
    {
        if (!IsRunning())
            return false;
        if (!HasAllConfiguredFragments())
            return false;

        CurrentResult.exitPuzzleComposed = true;
        GameEvents.RaiseMazeRunChanged();
        return true;
    }

    public int GetCollectedFragmentCount()
    {
        return CurrentResult?.collectedFragmentIds != null ? CurrentResult.collectedFragmentIds.Count : 0;
    }

    public int GetTotalFragmentCount()
    {
        IReadOnlyList<MazeFragmentData> fragments = database?.GetAll<MazeFragmentData>("maze_fragments");
        if (fragments == null)
            return 0;

        int count = 0;
        for (int i = 0; i < fragments.Count; i++)
        {
            if (!string.IsNullOrEmpty(fragments[i]?.fragmentId))
                count++;
        }

        return count;
    }

    public void TickRun(float deltaSeconds)
    {
        if (State != MazeRunState.Running || deltaSeconds <= 0f)
            return;
        if (currentRule.timedEnergyCost <= 0 || currentRule.timedEnergySeconds <= 0)
            return;

        timedEnergyAccumulator += deltaSeconds;
        while (timedEnergyAccumulator >= currentRule.timedEnergySeconds && State == MazeRunState.Running)
        {
            timedEnergyAccumulator -= currentRule.timedEnergySeconds;
            ConsumeActionEnergy(currentRule.timedEnergyCost);
        }
    }

    public bool RestoreEnergy(int amount)
    {
        if (amount <= 0 || player?.profile == null)
            return false;

        player.profile.energy = Mathf.Clamp(player.profile.energy + amount, 0, player.profile.maxEnergy);
        GameEvents.RaiseEnergyChanged(player.profile.energy, player.profile.maxEnergy);
        GameEvents.RaiseMazeRunChanged();
        return true;
    }

    public bool UseFoodForEnergy(string itemId, int restoreAmount)
    {
        if (player?.inventory == null || string.IsNullOrEmpty(itemId) || restoreAmount <= 0)
            return false;
        if (!player.inventory.TryRemove(itemId, 1))
            return false;

        return RestoreEnergy(restoreAmount);
    }

    public bool ConsumeActionEnergy(int amount)
    {
        if (amount <= 0)
            return true;
        if (State != MazeRunState.Running)
            return false;
        if (!TryConsumeEnergy(amount))
        {
            FailRun();
            return false;
        }

        GameEvents.RaiseMazeRunChanged();
        if (GetCurrentEnergy() <= 0)
            FailRun();
        return State == MazeRunState.Running;
    }

    public bool ConsumeActionEnergy()
    {
        return ConsumeActionEnergy(DefaultActionEnergyCost);
    }

    public MazeRunResult CompleteRun()
    {
        MazeRunEndReason reason = CurrentResult != null && CurrentResult.exitPuzzleComposed
            ? MazeRunEndReason.PerfectClear
            : MazeRunEndReason.Clear;
        return FinishRun(reason, MazeRunState.Completed);
    }

    public MazeRunResult CompletePerfectRun()
    {
        TryComposeExitPuzzle();
        return FinishRun(CurrentResult != null && CurrentResult.exitPuzzleComposed
            ? MazeRunEndReason.PerfectClear
            : MazeRunEndReason.Clear, MazeRunState.Completed);
    }

    public MazeRunResult EvacuateRun()
    {
        return FinishRun(MazeRunEndReason.Evacuate, MazeRunState.Evacuated);
    }

    public MazeRunResult FailRun()
    {
        return FinishRun(MazeRunEndReason.EnergyEmpty, MazeRunState.Failed);
    }

    private MazeRunResult FinishRun(MazeRunEndReason reason, MazeRunState finalState)
    {
        if (rewardsApplied)
            return CurrentResult;

        if (CurrentResult == null)
            CurrentResult = new MazeRunResult(reason);

        CurrentResult.reason = reason;
        CurrentResult.rewardMultiplier = ResolveMultiplier(reason);
        CurrentResult.perfectBlueprintId = currentRule.perfectBlueprintId;
        State = finalState;
        MazeRunResult resolved = rewardResolver.Resolve(CurrentResult);
        ApplyRewards(resolved);
        rewardsApplied = true;
        GameEvents.RaiseMazeRunChanged();
        GameEvents.RaiseMazeRunEnded(resolved);
        return resolved;
    }

    private void AddCollectedItems(IReadOnlyDictionary<string, int> items)
    {
        if (items == null)
            return;

        foreach (KeyValuePair<string, int> kv in items)
            AddCollectedItem(kv.Key, kv.Value);
    }

    private void AddCollectedItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0 || CurrentResult == null)
            return;
        if (!CurrentResult.collectedItems.TryGetValue(itemId, out int current))
            current = 0;
        CurrentResult.collectedItems[itemId] = current + amount;
    }

    private void ApplyRewards(MazeRunResult result)
    {
        if (player?.inventory == null || result == null)
            return;

        foreach (KeyValuePair<string, int> kv in result.rewardItems)
            player.inventory.TryAdd(kv.Key, kv.Value);

        if (player.blueprints == null)
            player.blueprints = new HashSet<string>();
        foreach (string blueprintId in result.rewardBlueprintIds)
            player.blueprints.Add(blueprintId);
    }

    private bool IsRunning()
    {
        return State == MazeRunState.Running && CurrentResult != null;
    }

    private bool TryConsumeEnergy(int amount)
    {
        if (amount <= 0)
            return true;
        if (player?.profile == null)
            return false;
        if (player.profile.energy < amount)
            return false;

        player.profile.energy -= amount;
        GameEvents.RaiseEnergyChanged(player.profile.energy, player.profile.maxEnergy);
        return true;
    }

    private void ApplyEnergyDelta(int amount)
    {
        if (amount > 0)
        {
            RestoreEnergy(amount);
            return;
        }

        if (amount < 0)
            ConsumeActionEnergy(-amount);
    }

    private int GetCurrentEnergy()
    {
        return player?.profile != null ? player.profile.energy : 0;
    }

    private MazeRuleData ResolveRule()
    {
        MazeRuleData rule = GetData<MazeRuleData>("maze_rules", DefaultRuleId);
        if (rule != null)
            return NormalizeRule(rule);

        IReadOnlyList<MazeRuleData> allRules = database?.GetAll<MazeRuleData>("maze_rules");
        if (allRules != null && allRules.Count > 0)
            return NormalizeRule(allRules[0]);

        return FallbackRule;
    }

    private static MazeRuleData NormalizeRule(MazeRuleData rule)
    {
        if (rule == null)
            return FallbackRule;

        rule.beginRunEnergyCost = Mathf.Max(0, rule.beginRunEnergyCost);
        rule.enterRoomEnergyCost = Mathf.Max(0, rule.enterRoomEnergyCost);
        rule.timedEnergyCost = Mathf.Max(0, rule.timedEnergyCost);
        rule.timedEnergySeconds = Mathf.Max(1, rule.timedEnergySeconds);
        rule.evacuateMultiplier = Mathf.Max(0f, rule.evacuateMultiplier);
        rule.failMultiplier = Mathf.Max(0f, rule.failMultiplier);
        rule.clearMultiplier = rule.clearMultiplier <= 0f ? FallbackRule.clearMultiplier : rule.clearMultiplier;
        rule.perfectMultiplier = rule.perfectMultiplier <= 0f ? FallbackRule.perfectMultiplier : rule.perfectMultiplier;
        return rule;
    }

    private float ResolveMultiplier(MazeRunEndReason reason)
    {
        switch (reason)
        {
            case MazeRunEndReason.PerfectClear:
                return currentRule.perfectMultiplier;
            case MazeRunEndReason.Evacuate:
                return currentRule.evacuateMultiplier;
            case MazeRunEndReason.EnergyEmpty:
                return currentRule.failMultiplier;
            case MazeRunEndReason.Clear:
            default:
                return currentRule.clearMultiplier;
        }
    }

    private bool HasAllConfiguredFragments()
    {
        IReadOnlyList<MazeFragmentData> fragments = database?.GetAll<MazeFragmentData>("maze_fragments");
        if (fragments == null || fragments.Count == 0)
            return false;

        for (int i = 0; i < fragments.Count; i++)
        {
            MazeFragmentData fragment = fragments[i];
            if (string.IsNullOrEmpty(fragment?.fragmentId))
                continue;
            if (!CurrentResult.collectedFragmentIds.Contains(fragment.fragmentId))
                return false;
        }

        return true;
    }

    private bool AreRequirementsMet(IReadOnlyList<string> requirementIds)
    {
        if (requirementIds == null || requirementIds.Count == 0 || CurrentResult == null)
            return true;

        for (int i = 0; i < requirementIds.Count; i++)
        {
            string requirementId = requirementIds[i];
            if (string.IsNullOrEmpty(requirementId))
                continue;
            if (CurrentResult.visitedNodeIds.Contains(requirementId))
                continue;
            if (CurrentResult.completedPuzzleIds.Contains(requirementId))
                continue;
            if (CurrentResult.collectedFragmentIds.Contains(requirementId))
                continue;
            if (player?.blueprints != null && player.blueprints.Contains(requirementId))
                continue;

            return false;
        }

        return true;
    }

    private bool IsNodeReachable(string nodeId)
    {
        if (CurrentResult == null)
            return false;
        if (!HasConfiguredNodeLinks())
            return true;
        if (CurrentResult.visitedNodeIds.Count == 0)
            return IsStartNode(nodeId);
        if (!string.IsNullOrEmpty(CurrentResult.guideNodeId) && CurrentResult.guideNodeId == nodeId)
            return true;

        IReadOnlyList<MazeNodeData> nodes = database?.GetAll<MazeNodeData>("maze_nodes");
        if (nodes == null)
            return false;

        for (int i = 0; i < nodes.Count; i++)
        {
            MazeNodeData visitedNode = nodes[i];
            if (visitedNode == null || !CurrentResult.visitedNodeIds.Contains(visitedNode.nodeId))
                continue;
            if (visitedNode.nextNodeIds != null && visitedNode.nextNodeIds.Contains(nodeId))
                return true;
        }

        return false;
    }

    private bool HasConfiguredNodeLinks()
    {
        IReadOnlyList<MazeNodeData> nodes = database?.GetAll<MazeNodeData>("maze_nodes");
        if (nodes == null)
            return false;

        for (int i = 0; i < nodes.Count; i++)
        {
            MazeNodeData node = nodes[i];
            if (node?.nextNodeIds != null && node.nextNodeIds.Count > 0)
                return true;
        }

        return false;
    }

    private bool IsStartNode(string nodeId)
    {
        MazeNodeData node = GetData<MazeNodeData>("maze_nodes", nodeId);
        if (node == null)
            return false;
        if (!string.IsNullOrEmpty(node.nodeType) && node.nodeType.Contains("entrance"))
            return true;

        IReadOnlyList<MazeNodeData> nodes = database?.GetAll<MazeNodeData>("maze_nodes");
        int minIndex = int.MaxValue;
        if (nodes != null)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].index < minIndex)
                    minIndex = nodes[i].index;
            }
        }

        return node.index == minIndex;
    }

    private static bool ConsumesRoomEnergy(MazeNodeData node)
    {
        if (node == null)
            return false;
        if (!string.IsNullOrEmpty(node.roomId) || !string.IsNullOrEmpty(node.puzzleId) || !string.IsNullOrEmpty(node.trapId))
            return true;

        string type = node.nodeType ?? string.Empty;
        return type.Contains("room") || type.Contains("puzzle") || type.Contains("trap");
    }

    private T GetData<T>(string rootKey, string id) where T : BaseData
    {
        if (database == null || string.IsNullOrEmpty(rootKey) || string.IsNullOrEmpty(id))
            return null;

        return database.Get<T>(rootKey, id);
    }

    private static string NormalizeAnswer(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .Trim()
            .Replace("，", ",")
            .Replace("、", ",")
            .Replace(" ", string.Empty)
            .ToLowerInvariant();
    }
}
