using System.Collections.Generic;

public sealed class DefaultMazeRewardResolver : IMazeRewardResolver
{
    public MazeRunResult Resolve(MazeRunResult rawResult)
    {
        if (rawResult == null)
            return null;

        rawResult.rewardItems.Clear();
        float multiplier = ResolveMultiplier(rawResult.reason);
        foreach (KeyValuePair<string, int> kv in rawResult.collectedItems)
        {
            int amount = UnityEngine.Mathf.FloorToInt(kv.Value * multiplier);
            if (amount > 0)
                rawResult.rewardItems[kv.Key] = amount;
        }

        return rawResult;
    }

    private static float ResolveMultiplier(MazeRunEndReason reason)
    {
        switch (reason)
        {
            case MazeRunEndReason.Clear:
                return 1f;
            case MazeRunEndReason.Evacuate:
                return 0.5f;
            case MazeRunEndReason.EnergyEmpty:
                return 0.3f;
            default:
                return 0f;
        }
    }
}
