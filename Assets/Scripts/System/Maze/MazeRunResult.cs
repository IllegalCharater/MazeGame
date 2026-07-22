using System.Collections.Generic;

public class MazeRunResult
{
    public MazeRunEndReason reason;
    public Dictionary<string, int> collectedItems = new Dictionary<string, int>();
    public Dictionary<string, int> rewardItems = new Dictionary<string, int>();
    public List<string> rewardBlueprintIds = new List<string>();
    public HashSet<string> visitedNodeIds = new HashSet<string>();
    public HashSet<string> completedPuzzleIds = new HashSet<string>();
    public HashSet<string> triggeredTrapIds = new HashSet<string>();
    public HashSet<string> collectedFragmentIds = new HashSet<string>();
    public Dictionary<string, string> fragmentTexts = new Dictionary<string, string>();
    public string guideNodeId;
    public bool exitPuzzleComposed;
    public float rewardMultiplier = -1f;
    public string perfectBlueprintId;

    public MazeRunResult(MazeRunEndReason reason)
    {
        this.reason = reason;
    }
}
