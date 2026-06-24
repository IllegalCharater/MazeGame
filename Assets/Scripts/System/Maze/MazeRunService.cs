using System.Collections.Generic;

public sealed class MazeRunService : IGameService
{
    private const int BeginRunEnergyCost = 1;
    private const int CollectEnergyCost = 1;
    private const int DefaultActionEnergyCost = 1;

    private PlayerDatabase player;
    private IMazeRewardResolver rewardResolver = new DefaultMazeRewardResolver();
    private bool rewardsApplied;

    public MazeRunState State { get; private set; } = MazeRunState.NotStarted;
    public MazeRunResult CurrentResult { get; private set; }

    public void Initialize(GameDatabase database, PlayerDatabase player)
    {
        this.player = player;
        State = MazeRunState.NotStarted;
        CurrentResult = null;
        rewardsApplied = false;
    }

    public bool BeginRun()
    {
        if (State == MazeRunState.Running)
            return true;
        if (!TryConsumeEnergy(BeginRunEnergyCost))
            return false;

        State = MazeRunState.Running;
        CurrentResult = new MazeRunResult(MazeRunEndReason.Clear);
        rewardsApplied = false;
        GameEvents.RaiseMazeRunChanged();
        if (GetCurrentEnergy() <= 0)
            FailRun();
        return true;
    }

    public bool CollectItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;
        if (State != MazeRunState.Running)
        {
            if (!BeginRun())
                return false;
        }
        if (!ConsumeActionEnergy(CollectEnergyCost))
        {
            return false;
        }

        if (!CurrentResult.collectedItems.TryGetValue(itemId, out int current))
            current = 0;
        CurrentResult.collectedItems[itemId] = current + amount;
        GameEvents.RaiseMazeRunChanged();
        return true;
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
        return FinishRun(MazeRunEndReason.Clear, MazeRunState.Completed);
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
        State = finalState;
        MazeRunResult resolved = rewardResolver.Resolve(CurrentResult);
        ApplyRewards(resolved);
        rewardsApplied = true;
        GameEvents.RaiseMazeRunChanged();
        GameEvents.RaiseMazeRunEnded(resolved);
        return resolved;
    }

    private void ApplyRewards(MazeRunResult result)
    {
        if (player?.inventory == null || result == null)
            return;

        foreach (KeyValuePair<string, int> kv in result.rewardItems)
            player.inventory.TryAdd(kv.Key, kv.Value);

        foreach (string blueprintId in result.rewardBlueprintIds)
            player.blueprints.Add(blueprintId);
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

    private int GetCurrentEnergy()
    {
        return player?.profile != null ? player.profile.energy : 0;
    }
}
