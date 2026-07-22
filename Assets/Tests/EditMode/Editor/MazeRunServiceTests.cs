using NUnit.Framework;
using System.Collections.Generic;

public sealed class MazeRunServiceTests
{
    [Test]
    public void BeginRunFailsWhenEnergyIsNotEnough()
    {
        MazeRunService service = CreateService(0);

        bool started = service.BeginRun();

        Assert.IsFalse(started);
        Assert.AreEqual(MazeRunState.NotStarted, service.State);
    }

    [Test]
    public void AddRunLootAccumulatesCurrentRunLoot()
    {
        MazeRunService service = CreateService(10);

        Assert.IsTrue(service.BeginRun());
        Assert.IsTrue(service.AddRunLoot(new Dictionary<string, int> { { "ingredient_carrot", 2 } }));
        Assert.IsTrue(service.AddRunLoot(new Dictionary<string, int> { { "ingredient_carrot", 3 } }));

        Assert.AreEqual(5, service.CurrentResult.collectedItems["ingredient_carrot"]);
    }

    [Test]
    public void InteractionsDoNotImplicitlyStartRun()
    {
        MazeRunService service = CreateConfiguredService(10);

        Assert.IsFalse(service.AddRunLoot(new Dictionary<string, int> { { "ingredient_carrot", 1 } }));
        Assert.IsFalse(service.InteractNode("node_reward"));
        Assert.IsFalse(service.SubmitPuzzleAnswer("puzzle_01", "answer"));
        Assert.IsFalse(service.ResolveTrap("trap_01", true));

        Assert.AreEqual(MazeRunState.NotStarted, service.State);
    }

    [Test]
    public void CompleteRunAppliesAllCollectedRewardsOnlyOnce()
    {
        PlayerDatabase player;
        MazeRunService service = CreateService(10, out player);

        service.BeginRun();
        service.AddRunLoot(new Dictionary<string, int> { { "ingredient_carrot", 4 } });
        service.CompleteRun();
        service.CompleteRun();

        Assert.AreEqual(MazeRunState.Completed, service.State);
        Assert.AreEqual(4, player.inventory.GetAmount("ingredient_carrot"));
    }

    [Test]
    public void EvacuateRunAppliesHalfCollectedRewards()
    {
        PlayerDatabase player;
        MazeRunService service = CreateService(10, out player);

        service.BeginRun();
        service.AddRunLoot(new Dictionary<string, int> { { "ingredient_salt", 5 } });
        service.EvacuateRun();

        Assert.AreEqual(MazeRunState.Evacuated, service.State);
        Assert.AreEqual(2, player.inventory.GetAmount("ingredient_salt"));
    }

    [Test]
    public void FailRunAppliesThirtyPercentCollectedRewards()
    {
        PlayerDatabase player;
        MazeRunService service = CreateService(10, out player);

        service.BeginRun();
        service.AddRunLoot(new Dictionary<string, int> { { "ingredient_meat", 10 } });
        service.FailRun();

        Assert.AreEqual(MazeRunState.Failed, service.State);
        Assert.AreEqual(3, player.inventory.GetAmount("ingredient_meat"));
    }

    [Test]
    public void ConfiguredPerfectRunAppliesDoubleRewardsAndBlueprint()
    {
        PlayerDatabase player;
        MazeRunService service = CreateConfiguredService(30, out player);

        Assert.IsTrue(service.BeginRun());
        Assert.IsTrue(service.AddRunLoot(new Dictionary<string, int> { { "ingredient_carrot", 3 } }));
        Assert.IsTrue(service.CompletePuzzle("puzzle_01"));
        Assert.IsTrue(service.CompletePuzzle("puzzle_02"));
        Assert.IsTrue(service.CompletePuzzle("puzzle_03"));
        Assert.IsTrue(service.CompletePuzzle("puzzle_04"));
        Assert.IsTrue(service.TryComposeExitPuzzle());

        MazeRunResult result = service.CompletePerfectRun();

        Assert.AreEqual(MazeRunEndReason.PerfectClear, result.reason);
        Assert.AreEqual(10, player.inventory.GetAmount("ingredient_carrot"));
        Assert.IsTrue(player.blueprints.Contains("blueprint_maze_energy"));
    }

    [Test]
    public void ConfiguredNodeRewardsAndEnergyDeltaAreAppliedOnce()
    {
        PlayerDatabase player;
        MazeRunService service = CreateConfiguredService(20, out player);

        Assert.IsTrue(service.BeginRun());
        Assert.IsTrue(service.InteractNode("node_reward"));
        Assert.IsFalse(service.InteractNode("node_reward"));

        Assert.AreEqual(3, service.CurrentResult.collectedItems["ingredient_salt"]);
        Assert.AreEqual(24, player.profile.energy);
    }

    [Test]
    public void EnteringPuzzleRoomConsumesConfiguredRoomEnergy()
    {
        PlayerDatabase player;
        MazeRunService service = CreateConfiguredService(20, out player);

        Assert.IsTrue(service.BeginRun());
        Assert.IsTrue(service.InteractNode("node_puzzle"));

        Assert.AreEqual(14, player.profile.energy);
        Assert.IsTrue(service.CurrentResult.visitedNodeIds.Contains("node_puzzle"));
    }

    [Test]
    public void PuzzleCannotGrantFragmentAndLootTwice()
    {
        MazeRunService service = CreateConfiguredService(20);

        Assert.IsTrue(service.BeginRun());
        Assert.IsTrue(service.SubmitPuzzleAnswer("puzzle_01", "answer"));
        Assert.IsFalse(service.SubmitPuzzleAnswer("puzzle_01", "answer"));

        Assert.AreEqual(1, service.CurrentResult.completedPuzzleIds.Count);
        Assert.AreEqual(1, service.CurrentResult.collectedFragmentIds.Count);
        Assert.AreEqual(2, service.CurrentResult.collectedItems["ingredient_carrot"]);
    }

    [Test]
    public void PuzzleWrongAnswerConsumesConfiguredFailEnergy()
    {
        PlayerDatabase player;
        MazeRunService service = CreateConfiguredService(20, out player);

        Assert.IsTrue(service.BeginRun());
        Assert.IsFalse(service.SubmitPuzzleAnswer("puzzle_01", "wrong"));

        Assert.AreEqual(15, player.profile.energy);
        Assert.AreEqual(0, service.CurrentResult.completedPuzzleIds.Count);
    }

    [TestCase("puzzle_01", "answer")]
    [TestCase("puzzle_02", "9")]
    [TestCase("puzzle_03", "trust")]
    [TestCase("puzzle_04", "渊,剑,信")]
    public void ConfiguredPuzzleTypesCanBeSolved(string puzzleId, string answer)
    {
        MazeRunService service = CreateConfiguredService(30);

        Assert.IsTrue(service.BeginRun());
        Assert.IsTrue(service.SubmitPuzzleAnswer(puzzleId, answer));

        Assert.IsTrue(service.CurrentResult.completedPuzzleIds.Contains(puzzleId));
    }

    [Test]
    public void TrapFailureConsumesConfiguredEnergyWithoutEndingWhenConfigured()
    {
        PlayerDatabase player;
        MazeRunService service = CreateConfiguredService(20, out player);

        Assert.IsTrue(service.BeginRun());
        Assert.IsFalse(service.ResolveTrap("trap_01", false));

        Assert.AreEqual(MazeRunState.Running, service.State);
        Assert.AreEqual(14, player.profile.energy);
        Assert.AreEqual("node_02", service.CurrentResult.guideNodeId);
    }

    [Test]
    public void TimedEnergyCostCanFailRun()
    {
        PlayerDatabase player;
        MazeRunService service = CreateConfiguredService(4, out player);

        Assert.IsTrue(service.BeginRun());
        service.TickRun(10f);

        Assert.AreEqual(MazeRunState.Failed, service.State);
        Assert.AreEqual(0, player.profile.energy);
    }

    [Test]
    public void FoodRestoresEnergyAndConsumesInventoryItem()
    {
        PlayerDatabase player;
        MazeRunService service = CreateConfiguredService(50, out player);
        player.inventory.TryAdd("food_carrot_soup", 1);

        Assert.IsTrue(service.BeginRun());
        Assert.IsTrue(service.UseFoodForEnergy("food_carrot_soup", 25));

        Assert.AreEqual(74, player.profile.energy);
        Assert.AreEqual(0, player.inventory.GetAmount("food_carrot_soup"));
    }

    [Test]
    public void RuntimeMazeConfigurationHasFixedSixteenNodeRouteAndValidReferences()
    {
        Dictionary<string, BaseData> nodes = LoadResourceTable("maze_nodes");
        Dictionary<string, BaseData> puzzles = LoadResourceTable("maze_puzzles");
        Dictionary<string, BaseData> traps = LoadResourceTable("maze_traps");
        Dictionary<string, BaseData> fragments = LoadResourceTable("maze_fragments");
        Dictionary<string, BaseData> rules = LoadResourceTable("maze_rules");
        Dictionary<string, BaseData> blueprints = LoadResourceTable("blueprints");

        Assert.AreEqual(16, nodes.Count);
        Assert.IsTrue(rules.ContainsKey("default"));
        MazeRuleData rule = rules["default"] as MazeRuleData;
        Assert.NotNull(rule);
        Assert.AreEqual(1, rule.beginRunEnergyCost);
        Assert.AreEqual(5, rule.enterRoomEnergyCost);
        Assert.AreEqual(2, rule.timedEnergyCost);
        Assert.AreEqual(60, rule.timedEnergySeconds);
        Assert.AreEqual(0.5f, rule.evacuateMultiplier);
        Assert.AreEqual(0.3f, rule.failMultiplier);
        Assert.AreEqual(1f, rule.clearMultiplier);
        Assert.AreEqual(2f, rule.perfectMultiplier);
        Assert.IsFalse(string.IsNullOrEmpty(rule.perfectBlueprintId));
        Assert.IsTrue(blueprints.ContainsKey(rule.perfectBlueprintId), $"Missing perfect blueprint {rule.perfectBlueprintId}");

        for (int i = 1; i <= 16; i++)
            Assert.IsTrue(nodes.ContainsKey($"node_{i:00}"), $"Missing node_{i:00}");

        foreach (BaseData data in nodes.Values)
        {
            MazeNodeData node = data as MazeNodeData;
            Assert.NotNull(node);
            Assert.IsFalse(string.IsNullOrEmpty(node.nodeId));
            if (node.nextNodeIds != null)
            {
                foreach (string nextNodeId in node.nextNodeIds)
                    Assert.IsTrue(nodes.ContainsKey(nextNodeId), $"{node.nodeId} links missing node {nextNodeId}");
            }
            if (!string.IsNullOrEmpty(node.puzzleId))
                Assert.IsTrue(puzzles.ContainsKey(node.puzzleId), $"{node.nodeId} missing puzzle {node.puzzleId}");
            if (!string.IsNullOrEmpty(node.trapId))
                Assert.IsTrue(traps.ContainsKey(node.trapId), $"{node.nodeId} missing trap {node.trapId}");
        }

        foreach (BaseData data in puzzles.Values)
        {
            MazePuzzleData puzzle = data as MazePuzzleData;
            Assert.NotNull(puzzle);
            Assert.IsFalse(string.IsNullOrEmpty(puzzle.answer));
            Assert.IsNotNull(puzzle.optionKeys);
            Assert.Greater(puzzle.optionKeys.Count, 0);
            if (!string.IsNullOrEmpty(puzzle.fragmentId))
                Assert.IsTrue(fragments.ContainsKey(puzzle.fragmentId), $"{puzzle.puzzleId} missing fragment {puzzle.fragmentId}");
        }
    }

    private static MazeRunService CreateService(int energy)
    {
        PlayerDatabase player;
        return CreateService(energy, out player);
    }

    private static MazeRunService CreateService(int energy, out PlayerDatabase player)
    {
        player = new PlayerDatabase
        {
            inventory = new PlayerInventory(),
            profile = new PlayerProfile
            {
                playerId = "test_player",
                playerDisplayName = "Test",
                energy = energy,
                maxEnergy = 100,
                level = 1
            },
            blueprints = new System.Collections.Generic.HashSet<string>()
        };

        player.inventory.playerId = "test_player";

        MazeRunService service = new MazeRunService();
        service.Initialize(null, player);
        return service;
    }

    private static MazeRunService CreateConfiguredService(int energy)
    {
        PlayerDatabase player;
        return CreateConfiguredService(energy, out player);
    }

    private static MazeRunService CreateConfiguredService(int energy, out PlayerDatabase player)
    {
        MazeRunService service = CreateService(energy, out player);
        GameDatabase database = new GameDatabase();
        database.databases["maze_rules"] = new Dictionary<string, BaseData>
        {
            {
                "default",
                new MazeRuleData
                {
                    ruleId = "default",
                    beginRunEnergyCost = 1,
                    enterRoomEnergyCost = 5,
                    timedEnergyCost = 3,
                    timedEnergySeconds = 10,
                    evacuateMultiplier = 0.5f,
                    failMultiplier = 0.3f,
                    clearMultiplier = 1f,
                    perfectMultiplier = 2f,
                    perfectBlueprintId = "blueprint_maze_energy"
                }
            }
        };
        database.databases["maze_nodes"] = new Dictionary<string, BaseData>
        {
            {
                "node_reward",
                new MazeNodeData
                {
                    nodeId = "node_reward",
                    index = 1,
                    nodeType = "corridor_reward",
                    rewardItems = new Dictionary<string, int> { { "ingredient_salt", 3 } },
                    energyDelta = 5,
                    nextNodeIds = new List<string>(),
                    unlockRequirementIds = new List<string>()
                }
            },
            {
                "node_puzzle",
                new MazeNodeData
                {
                    nodeId = "node_puzzle",
                    index = 2,
                    nodeType = "puzzle_room",
                    roomId = "room_puzzle",
                    puzzleId = "puzzle_01",
                    rewardItems = new Dictionary<string, int>(),
                    nextNodeIds = new List<string>(),
                    unlockRequirementIds = new List<string>()
                }
            }
        };
        database.databases["maze_puzzles"] = new Dictionary<string, BaseData>
        {
            {
                "puzzle_01",
                new MazePuzzleData
                {
                    puzzleId = "puzzle_01",
                    puzzleType = "item_socket",
                    answer = "answer",
                    failEnergyCost = 4,
                    optionKeys = new List<string> { "answer", "wrong" },
                    successRewardItems = new Dictionary<string, int> { { "ingredient_carrot", 2 } },
                    fragmentId = "fragment_01",
                    fragmentText = "fragment one"
                }
            },
            {
                "puzzle_02",
                new MazePuzzleData
                {
                    puzzleId = "puzzle_02",
                    puzzleType = "numeric_input",
                    answer = "9",
                    failEnergyCost = 1,
                    optionKeys = new List<string> { "1", "9" },
                    successRewardItems = new Dictionary<string, int>(),
                    fragmentId = "fragment_02",
                    fragmentText = "fragment two"
                }
            },
            {
                "puzzle_03",
                new MazePuzzleData
                {
                    puzzleId = "puzzle_03",
                    puzzleType = "floor_choice",
                    answer = "trust",
                    failEnergyCost = 1,
                    optionKeys = new List<string> { "profit", "trust" },
                    successRewardItems = new Dictionary<string, int>(),
                    fragmentId = "fragment_03",
                    fragmentText = "fragment three"
                }
            },
            {
                "puzzle_04",
                new MazePuzzleData
                {
                    puzzleId = "puzzle_04",
                    puzzleType = "block_words",
                    answer = "渊,剑,信",
                    failEnergyCost = 1,
                    optionKeys = new List<string> { "旧", "渊", "剑", "信" },
                    successRewardItems = new Dictionary<string, int>(),
                    fragmentId = "fragment_04",
                    fragmentText = "fragment four"
                }
            }
        };
        database.databases["maze_fragments"] = new Dictionary<string, BaseData>
        {
            { "fragment_01", new MazeFragmentData { fragmentId = "fragment_01", order = 1 } },
            { "fragment_02", new MazeFragmentData { fragmentId = "fragment_02", order = 2 } },
            { "fragment_03", new MazeFragmentData { fragmentId = "fragment_03", order = 3 } },
            { "fragment_04", new MazeFragmentData { fragmentId = "fragment_04", order = 4 } }
        };
        database.databases["maze_traps"] = new Dictionary<string, BaseData>
        {
            {
                "trap_01",
                new MazeTrapData
                {
                    trapId = "trap_01",
                    failEnergyCost = 5,
                    failEndRun = false,
                    guideNodeId = "node_02",
                    successRewardItems = new Dictionary<string, int>()
                }
            }
        };

        service.Initialize(database, player);
        return service;
    }

    private static Dictionary<string, BaseData> LoadResourceTable(string rootKey)
    {
        BaseDatabase database = new BaseDatabase();
        database.Init(rootKey);
        database.LoadData();
        return database.dataList;
    }
}
