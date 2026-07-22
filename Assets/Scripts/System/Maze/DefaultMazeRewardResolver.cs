using System.Collections.Generic;

public sealed class DefaultMazeRewardResolver : IMazeRewardResolver
{
    public MazeRunResult Resolve(MazeRunResult rawResult)
    {
        if (rawResult == null)
            return null;

        rawResult.rewardItems.Clear();
        float multiplier = rawResult.rewardMultiplier >= 0f
            ? rawResult.rewardMultiplier
            : ResolveMultiplier(rawResult.reason);
        foreach (KeyValuePair<string, int> kv in rawResult.collectedItems)
        {
            int amount = UnityEngine.Mathf.FloorToInt(kv.Value * multiplier);
            if (amount > 0)
                rawResult.rewardItems[kv.Key] = amount;
        }

        if (rawResult.reason == MazeRunEndReason.PerfectClear && !string.IsNullOrEmpty(rawResult.perfectBlueprintId))
        {
            if (!rawResult.rewardBlueprintIds.Contains(rawResult.perfectBlueprintId))
                rawResult.rewardBlueprintIds.Add(rawResult.perfectBlueprintId);
        }

        return rawResult;
    }

    private static float ResolveMultiplier(MazeRunEndReason reason)
    {
        switch (reason)
        {
            case MazeRunEndReason.Clear:
                return 1f;
            case MazeRunEndReason.PerfectClear:
                return 2f;
            case MazeRunEndReason.Evacuate:
                return 0.5f;
            case MazeRunEndReason.EnergyEmpty:
                return 0.3f;
            default:
                return 0f;
        }
    }
}
