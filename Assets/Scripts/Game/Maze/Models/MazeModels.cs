using System.Collections.Generic;

public sealed class MazeRunResult
{
    public MazeRunState state;
    public MazeRunEndReason endReason;
    public float multiplier;
    public Dictionary<string, int> finalRewards = new Dictionary<string, int>();
    public List<string> fragments = new List<string>();
    public List<string> lostFragments = new List<string>();
    public bool perfectClear;
    public bool lostFragment;
    public string blueprintId;
    public string settlementDescription;
}

public enum MazePuzzleEvaluationStatus
{
    Invalid,
    Incorrect,
    Correct
}

public sealed class MazePuzzleEvaluation
{
    public MazePuzzleEvaluationStatus status;
    public string message;

    public static MazePuzzleEvaluation Invalid(string message)
    {
        return new MazePuzzleEvaluation
        {
            status = MazePuzzleEvaluationStatus.Invalid,
            message = message
        };
    }

    public static MazePuzzleEvaluation Incorrect(string message)
    {
        return new MazePuzzleEvaluation
        {
            status = MazePuzzleEvaluationStatus.Incorrect,
            message = message
        };
    }

    public static MazePuzzleEvaluation Correct(string message)
    {
        return new MazePuzzleEvaluation
        {
            status = MazePuzzleEvaluationStatus.Correct,
            message = message
        };
    }
}

public sealed class MazePuzzleOptionViewModel
{
    public string key;
    public string label;
    public bool selected;
    public bool available = true;
    public bool failed;
}

public sealed class MazePuzzleRoomViewModel
{
    public string puzzleId;
    public string puzzleType;
    public string hintText;
    public string selectedInput;
    public string feedback;
    public string fragmentText;
    public bool isSolved;
    public bool canSubmit;
    public bool poolOpened;
    public bool lightActivated;
    public int currentEnergy;
    public int maxEnergy;
    public List<bool> candleStates = new List<bool>();
    public List<MazePuzzleOptionViewModel> options = new List<MazePuzzleOptionViewModel>();
    public Dictionary<string, int> successRewards = new Dictionary<string, int>();
}

public sealed class MazeViewModel
{
    public bool hasRun;
    public bool isEnded;
    public MazeRunState state;
    public MazeRunEndReason endReason;
    public string currentNodeId;
    public int currentNodeIndex;
    public string currentNodeTitle;
    public string currentNodeType;
    public string currentNodeNote;
    public string puzzleId;
    public string puzzleType;
    public string trapId;
    public int currentEnergy;
    public int maxEnergy;
    public List<string> reachableNodeIds = new List<string>();
    public List<string> visitedNodeIds = new List<string>();
    public List<string> activatedNodeIds = new List<string>();
    public List<string> solvedPuzzleIds = new List<string>();
    public List<string> resolvedTrapIds = new List<string>();
    public Dictionary<string, int> loot = new Dictionary<string, int>();
    public List<string> fragments = new List<string>();
    public List<string> lostFragments = new List<string>();
    public Dictionary<string, string> nodeTitles = new Dictionary<string, string>();
    public Dictionary<string, int> nodeIndices = new Dictionary<string, int>();
    public Dictionary<string, int> availableFoods = new Dictionary<string, int>();
    public bool canCollectReward;
    public bool canActivateSwitch;
    public bool canSubmitPuzzle;
    public bool canStartTrap;
    public bool canResolveTrap;
    public bool canResolveTrapSuccess;
    public bool canResolveTrapFailure;
    public bool canEvacuate;
    public bool canFinish;
    public bool canOpenExitPuzzle;
    public bool canAssemblePuzzle;
    public bool canLeaveWithoutPerfect;
    public bool canUseFood;
    public bool trapActive;
    public string activeTrapId;
    public string trapMessage;
    public string trapGuideNodeId;
    public int fragmentCount;
    public int totalFragmentCount;
    public int perfectMissingPercent;
    public float perfectProgress;
    public string exitPuzzleMessage;
    public string recommendedFoodId;
    public string message;
    public MazeRunResult result;
}
