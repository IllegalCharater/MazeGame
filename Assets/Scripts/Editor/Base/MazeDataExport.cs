using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public partial class ExcelToJsonExporter
{
    public sealed class MazeNodeExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string nodeId = Str(dt, r, "nodeId");
                if (string.IsNullOrWhiteSpace(nodeId))
                    continue;
                if (dataList.ContainsKey(nodeId))
                    throw new Exception($"maze_nodes primary key duplicated nodeId={nodeId}, row={r + 3}");

                string nodeType = Str(dt, r, "nodeType");
                List<string> unlockRequirementIds = nodeType == "puzzle_room"
                    ? new List<string>()
                    : ParseList(dt, r, "unlockRequirementIds");

                dataList[nodeId] = new MazeNodeData
                {
                    nodeId = nodeId,
                    index = Mathf.Max(0, Int(dt, r, "index", 0)),
                    nodeType = nodeType,
                    title = Str(dt, r, "title"),
                    roomId = Str(dt, r, "roomId"),
                    puzzleId = Str(dt, r, "puzzleId"),
                    trapId = Str(dt, r, "trapId"),
                    rewardItems = ParseIntDictionary(dt, r, "rewardItems"),
                    roomPickupItems = ParseIntDictionary(dt, r, "roomPickupItems"),
                    energyDelta = Int(dt, r, "energyDelta", 0),
                    mapX = Mathf.Clamp01(Float(dt, r, "mapX", 0.5f)),
                    mapY = Mathf.Clamp01(Float(dt, r, "mapY", 0.5f)),
                    nextNodeIds = ParseList(dt, r, "nextNodeIds"),
                    unlockRequirementIds = unlockRequirementIds,
                    note = Str(dt, r, "note")
                };
            }
        }
    }

    public sealed class MazePuzzleExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string puzzleId = Str(dt, r, "puzzleId");
                if (string.IsNullOrWhiteSpace(puzzleId))
                    continue;
                if (dataList.ContainsKey(puzzleId))
                    throw new Exception($"maze_puzzles primary key duplicated puzzleId={puzzleId}, row={r + 3}");

                List<string> optionKeys = ParseList(dt, r, "optionKeys");
                List<string> optionLabels = dt.Columns.Contains("optionLabels")
                    ? ParseList(dt, r, "optionLabels")
                    : new List<string>();
                if (optionLabels.Count > 0 && optionLabels.Count != optionKeys.Count)
                    throw new Exception($"maze_puzzles optionLabels count mismatch puzzleId={puzzleId}, row={r + 3}");

                dataList[puzzleId] = new MazePuzzleData
                {
                    puzzleId = puzzleId,
                    puzzleType = Str(dt, r, "puzzleType"),
                    answer = Str(dt, r, "answer"),
                    hintText = Str(dt, r, "hintText"),
                    optionKeys = optionKeys,
                    optionLabels = optionLabels,
                    failEnergyCost = Mathf.Max(0, Int(dt, r, "failEnergyCost", 1)),
                    successRewardItems = ParseIntDictionary(dt, r, "successRewardItems"),
                    fragmentId = Str(dt, r, "fragmentId"),
                    fragmentText = Str(dt, r, "fragmentText")
                };
            }
        }
    }

    public sealed class MazeTrapExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string trapId = Str(dt, r, "trapId");
                if (string.IsNullOrWhiteSpace(trapId))
                    continue;
                if (dataList.ContainsKey(trapId))
                    throw new Exception($"maze_traps primary key duplicated trapId={trapId}, row={r + 3}");

                dataList[trapId] = new MazeTrapData
                {
                    trapId = trapId,
                    trapType = Str(dt, r, "trapType"),
                    successEnergyCost = Mathf.Max(0, Int(dt, r, "successEnergyCost", 0)),
                    failEnergyCost = Mathf.Max(0, Int(dt, r, "failEnergyCost", 0)),
                    successRewardItems = ParseIntDictionary(dt, r, "successRewardItems"),
                    failEndRun = Bool(dt, r, "failEndRun", false),
                    guideNodeId = Str(dt, r, "guideNodeId")
                };
            }
        }
    }

    public sealed class MazeFragmentExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string fragmentId = Str(dt, r, "fragmentId");
                if (string.IsNullOrWhiteSpace(fragmentId))
                    continue;
                if (dataList.ContainsKey(fragmentId))
                    throw new Exception($"maze_fragments primary key duplicated fragmentId={fragmentId}, row={r + 3}");

                dataList[fragmentId] = new MazeFragmentData
                {
                    fragmentId = fragmentId,
                    order = Mathf.Max(0, Int(dt, r, "order", 0)),
                    displayText = Str(dt, r, "displayText"),
                    assetKey = Str(dt, r, "assetKey")
                };
            }
        }
    }

    public sealed class MazeRuleExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string ruleId = Str(dt, r, "ruleId");
                if (string.IsNullOrWhiteSpace(ruleId))
                    continue;
                if (dataList.ContainsKey(ruleId))
                    throw new Exception($"maze_rules primary key duplicated ruleId={ruleId}, row={r + 3}");

                dataList[ruleId] = new MazeRuleData
                {
                    ruleId = ruleId,
                    beginRunEnergyCost = Mathf.Max(0, Int(dt, r, "beginRunEnergyCost", 1)),
                    enterRoomEnergyCost = Mathf.Max(0, Int(dt, r, "enterRoomEnergyCost", 5)),
                    timedEnergyCost = Mathf.Max(0, Int(dt, r, "timedEnergyCost", 2)),
                    timedEnergySeconds = Mathf.Max(1, Int(dt, r, "timedEnergySeconds", 60)),
                    evacuateMultiplier = Mathf.Max(0f, Float(dt, r, "evacuateMultiplier", 0.5f)),
                    failMultiplier = Mathf.Max(0f, Float(dt, r, "failMultiplier", 0.3f)),
                    clearMultiplier = Mathf.Max(0f, Float(dt, r, "clearMultiplier", 1f)),
                    perfectMultiplier = Mathf.Max(0f, Float(dt, r, "perfectMultiplier", 2f)),
                    perfectBlueprintId = Str(dt, r, "perfectBlueprintId")
                };
            }
        }
    }
}
