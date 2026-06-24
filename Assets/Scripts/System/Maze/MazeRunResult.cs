using System.Collections.Generic;

public class MazeRunResult
{
    public MazeRunEndReason reason;
    public Dictionary<string, int> collectedItems = new Dictionary<string, int>();
    public Dictionary<string, int> rewardItems = new Dictionary<string, int>();
    public List<string> rewardBlueprintIds = new List<string>();

    public MazeRunResult(MazeRunEndReason reason)
    {
        this.reason = reason;
    }
}
