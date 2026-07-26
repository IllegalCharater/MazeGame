using System.Collections.Generic;

public sealed class MazeRunComponent : IComponent
{
    public string playerId;
    public string ruleId;
    public MazeRunState state = MazeRunState.NotStarted;
    public MazeRunEndReason endReason = MazeRunEndReason.Clear;
    public string currentNodeId;
    public HashSet<string> visitedNodeIds = new HashSet<string>();
    public HashSet<string> activatedNodeIds = new HashSet<string>();
    public HashSet<string> solvedPuzzleIds = new HashSet<string>();
    public HashSet<string> resolvedTrapIds = new HashSet<string>();
    public HashSet<string> rewardedNodeIds = new HashSet<string>();
    public HashSet<string> usedFoodIds = new HashSet<string>();
    public Dictionary<string, int> collectedItems = new Dictionary<string, int>();
    public Dictionary<string, int> puzzleItems = new Dictionary<string, int>();
    public List<string> collectedFragments = new List<string>();
    public List<string> lostFragmentIds = new List<string>();
    public bool exitPuzzleOpened;
    public bool leftWithoutPerfect;
    public bool trapActive;
    public string activeTrapId;
    public string trapMessage;
    public float elapsedSeconds;
    public float timedEnergyAccumulator;
    public bool settled;
}

public sealed class MazeNodeComponent : IComponent
{
    public string nodeId;
    public int index;
    public string nodeType;
    public string title;
    public string roomId;
    public string puzzleId;
    public string trapId;
    public List<string> nextNodeIds = new List<string>();
    public List<string> unlockRequirementIds = new List<string>();
    public int energyDelta;
    public string note;
}

public sealed class MazeRewardComponent : IComponent
{
    public Dictionary<string, int> rewardItems = new Dictionary<string, int>();
    public Dictionary<string, int> roomPickupItems = new Dictionary<string, int>();
}

public sealed class MazePuzzleComponent : IComponent
{
    public string puzzleId;
    public string puzzleType;
    public string answer;
    public string hintText;
    public List<string> optionKeys = new List<string>();
    public List<string> optionLabels = new List<string>();
    public int failEnergyCost;
    public Dictionary<string, int> successRewardItems = new Dictionary<string, int>();
    public string fragmentId;
    public string fragmentText;
}

public sealed class MazeTrapComponent : IComponent
{
    public string trapId;
    public string trapType;
    public int successEnergyCost;
    public int failEnergyCost;
    public Dictionary<string, int> successRewardItems = new Dictionary<string, int>();
    public bool failEndRun;
    public string guideNodeId;
}

public sealed class MazePositionComponent : IComponent
{
    public float mapX;
    public float mapY;
}
