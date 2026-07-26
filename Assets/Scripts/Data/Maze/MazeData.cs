using System.Collections.Generic;

[System.Serializable]
public class MazeNodeData : BaseData
{
    public string nodeId;
    public int index;
    public string nodeType;
    public string title;
    public string roomId;
    public string puzzleId;
    public string trapId;
    public Dictionary<string, int> rewardItems;
    public int energyDelta;
    public float mapX;
    public float mapY;
    public List<string> nextNodeIds;
    public List<string> unlockRequirementIds;
    public Dictionary<string, int> roomPickupItems;
    public string note;
}

[System.Serializable]
public class MazePuzzleData : BaseData
{
    public string puzzleId;
    public string puzzleType;
    public string answer;
    public string hintText;
    public List<string> optionKeys;
    public List<string> optionLabels;
    public int failEnergyCost = 1;
    public Dictionary<string, int> successRewardItems;
    public string fragmentId;
    public string fragmentText;
}

[System.Serializable]
public class MazeTrapData : BaseData
{
    public string trapId;
    public string trapType;
    public int successEnergyCost;
    public int failEnergyCost;
    public Dictionary<string, int> successRewardItems;
    public bool failEndRun;
    public string guideNodeId;
}

[System.Serializable]
public class MazeFragmentData : BaseData
{
    public string fragmentId;
    public int order;
    public string displayText;
    public string assetKey;
}

[System.Serializable]
public class MazeRuleData : BaseData
{
    public string ruleId;
    public int beginRunEnergyCost;
    public int enterRoomEnergyCost;
    public int timedEnergyCost;
    public int timedEnergySeconds;
    public float evacuateMultiplier;
    public float failMultiplier;
    public float clearMultiplier;
    public float perfectMultiplier;
    public string perfectBlueprintId;
}
