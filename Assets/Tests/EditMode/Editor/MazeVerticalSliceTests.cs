using System.Collections.Generic;
using NUnit.Framework;

public sealed class MazeVerticalSliceTests
{
    private sealed class MazeFixture
    {
        public GameDatabase database;
        public PlayerDatabase player;
        public EventBus events;
        public EcsWorld world;
        public MazeSystem system;
    }

    private sealed class FakeMazeView : IMazeView
    {
        public MazeViewModel lastViewModel;
        public MazeRunResult lastResult;
        public string lastMessage;
        public int renderCount;
        public int actionCount;

        public void Render(MazeViewModel viewModel)
        {
            lastViewModel = viewModel;
            renderCount++;
        }

        public void ShowNodeActions(MazeViewModel viewModel)
        {
            lastViewModel = viewModel;
            actionCount++;
        }

        public void HideNodeActions()
        {
        }

        public void ShowMessage(string message)
        {
            lastMessage = message;
        }

        public void ShowResult(MazeRunResult result)
        {
            lastResult = result;
        }
    }

    private sealed class RecordingMoveHandler : ICommandHandler<MoveToMazeNodeCommand>
    {
        public string nodeId;
        public int count;

        public CommandResult Handle(MoveToMazeNodeCommand command)
        {
            nodeId = command.nodeId;
            count++;
            return CommandResult.Succeeded("recorded");
        }
    }

    private sealed class RecordingCollectHandler : ICommandHandler<CollectMazeNodeRewardCommand>
    {
        public string nodeId;
        public int count;

        public CommandResult Handle(CollectMazeNodeRewardCommand command)
        {
            nodeId = command.nodeId;
            count++;
            return CommandResult.Succeeded("recorded");
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (FrameworkContext.Instance != null)
            FrameworkContext.Instance.Dispose();
    }

    [Test]
    public void MazeSystemImportsConfigStartsRunAndConsumesBeginEnergy()
    {
        MazeFixture fixture = CreateMazeFixture();
        int startedEvents = 0;
        fixture.events.Subscribe<MazeRunStartedEvent>(evt => startedEvents++);

        CommandResult result = fixture.system.StartRun(fixture.player.playerId, "default", "node_01");
        MazeViewModel vm = fixture.system.GetViewModel();

        Assert.IsTrue(result.success);
        Assert.AreEqual(1, startedEvents);
        Assert.AreEqual(99, fixture.player.profile.energy);
        Assert.AreEqual(7, fixture.world.GetEntitiesWith<MazeNodeComponent>().Count);
        Assert.IsTrue(vm.hasRun);
        Assert.AreEqual(MazeRunState.Running, vm.state);
        Assert.AreEqual("node_01", vm.currentNodeId);
    }

    [Test]
    public void MazeSystemAllowsPuzzleRoomWithoutUnlockRequirementButRejectsOtherDisconnectedNodes()
    {
        MazeFixture fixture = CreateMazeFixture();
        fixture.system.StartRun(fixture.player.playerId, "default", "node_01");

        CommandResult disconnected = fixture.system.MoveToNode("node_07");
        Assert.IsFalse(disconnected.success);
        Assert.AreEqual("node_01", fixture.system.GetViewModel().currentNodeId);
        Assert.AreEqual(99, fixture.player.profile.energy);

        Assert.IsTrue(fixture.system.MoveToNode("node_04").success);
        Assert.AreEqual("node_04", fixture.system.GetViewModel().currentNodeId);
    }

    [Test]
    public void MazeSystemCollectsRewardOnceAndSettlesOnEvacuate()
    {
        MazeFixture fixture = CreateMazeFixture();
        fixture.system.StartRun(fixture.player.playerId, "default", "node_01");
        fixture.system.MoveToNode("node_02");

        CommandResult firstCollect = fixture.system.CollectNodeReward("node_02");
        CommandResult secondCollect = fixture.system.CollectNodeReward("node_02");

        Assert.IsTrue(firstCollect.success);
        Assert.IsFalse(secondCollect.success);
        Assert.AreEqual(0, fixture.player.inventory.GetAmount("ingredient_carrot"));
        Assert.AreEqual(4, fixture.system.GetViewModel().loot["ingredient_carrot"]);

        fixture.system.MoveToNode("node_03");
        fixture.system.MoveToNode("node_04");
        fixture.system.MoveToNode("node_05");
        fixture.system.MoveToNode("node_06");
        CommandResult result = fixture.system.EvacuateRun();
        MazeRunResult runResult = result.payload as MazeRunResult;

        Assert.IsTrue(result.success);
        Assert.IsNotNull(runResult);
        Assert.AreEqual(MazeRunState.Evacuated, runResult.state);
        Assert.AreEqual(0.5f, runResult.multiplier);
        Assert.AreEqual(2, fixture.player.inventory.GetAmount("ingredient_carrot"));
    }

    [Test]
    public void MazeSystemHandlesPuzzleFailureAndSuccess()
    {
        MazeFixture fixture = CreateMazeFixture();
        MoveToPuzzle(fixture);
        int energyBeforeFailure = fixture.player.profile.energy;

        CommandResult wrong = fixture.system.ResolvePuzzleEvaluation(
            "puzzle_01",
            MazePuzzleEvaluation.Incorrect("Puzzle answer is incorrect."));
        CommandResult correct = fixture.system.ResolvePuzzleEvaluation(
            "puzzle_01",
            MazePuzzleEvaluation.Correct("Puzzle solved."));
        MazeViewModel vm = fixture.system.GetViewModel();

        Assert.IsFalse(wrong.success);
        Assert.AreEqual(energyBeforeFailure - 3, fixture.player.profile.energy);
        Assert.IsTrue(correct.success);
        Assert.Contains("puzzle_01", vm.solvedPuzzleIds);
        Assert.Contains("fragment_01", vm.fragments);
        Assert.AreEqual(1, vm.loot["ingredient_mushroom"]);
    }

    [Test]
    public void MazeSystemHandlesTrapSuccessAndFailure()
    {
        MazeFixture successFixture = CreateMazeFixture();
        MoveToTrap(successFixture);
        int energyBeforeSuccess = successFixture.player.profile.energy;

        CommandResult success = successFixture.system.ResolveTrap("trap_01", true);
        MazeViewModel successVm = successFixture.system.GetViewModel();

        Assert.IsTrue(success.success);
        Assert.AreEqual(energyBeforeSuccess - 2, successFixture.player.profile.energy);
        Assert.AreEqual(MazeRunState.Running, successVm.state);
        Assert.AreEqual(2, successVm.loot["ingredient_salt"]);

        MazeFixture failFixture = CreateMazeFixture();
        MoveToTrap(failFixture);
        int energyBeforeFailure = failFixture.player.profile.energy;
        CommandResult startTrap = failFixture.system.StartTrap("trap_01");
        CommandResult failed = failFixture.system.ResolveTrap("trap_01", false);
        MazeViewModel failedVm = failed.payload as MazeViewModel;

        Assert.IsTrue(startTrap.success);
        Assert.IsTrue(failed.success);
        Assert.IsNotNull(failedVm);
        Assert.AreEqual(MazeRunState.Running, failedVm.state);
        Assert.AreEqual(energyBeforeFailure - 10, failFixture.player.profile.energy);
        Assert.IsFalse(failedVm.trapActive);
    }

    [Test]
    public void MazeSystemPerfectClearSettlesRewardsAndBlueprint()
    {
        MazeFixture fixture = CreateMazeFixture();
        fixture.system.StartRun(fixture.player.playerId, "default", "node_01");
        fixture.system.MoveToNode("node_02");
        fixture.system.CollectNodeReward("node_02");
        fixture.system.MoveToNode("node_03");
        fixture.system.ActivateSwitch("node_03");
        fixture.system.MoveToNode("node_04");
        fixture.system.ResolvePuzzleEvaluation("puzzle_01", MazePuzzleEvaluation.Correct("Puzzle solved."));
        fixture.system.MoveToNode("node_05");
        fixture.system.ResolveTrap("trap_01", true);
        fixture.system.MoveToNode("node_06");
        fixture.system.MoveToNode("node_07");

        CommandResult result = fixture.system.AssembleExitPuzzle();
        MazeRunResult runResult = result.payload as MazeRunResult;

        Assert.IsTrue(result.success);
        Assert.IsNotNull(runResult);
        Assert.IsTrue(runResult.perfectClear);
        Assert.AreEqual(MazeRunEndReason.PerfectClear, runResult.endReason);
        Assert.AreEqual(8, fixture.player.inventory.GetAmount("ingredient_carrot"));
        Assert.AreEqual(2, fixture.player.inventory.GetAmount("ingredient_mushroom"));
        Assert.AreEqual(4, fixture.player.inventory.GetAmount("ingredient_salt"));
        Assert.IsTrue(fixture.player.blueprints.Contains("blueprint_maze_energy"));
    }

    [Test]
    public void MazeSystemTickConsumesTimedEnergy()
    {
        MazeFixture fixture = CreateMazeFixture();
        fixture.system.StartRun(fixture.player.playerId, "default", "node_01");

        fixture.world.Tick(10f);

        Assert.AreEqual(97, fixture.player.profile.energy);
    }


    [Test]
    public void MazeSystemUsesFoodAndRejectsEvacuateOutsideEvacuationNode()
    {
        MazeFixture fixture = CreateMazeFixture();
        fixture.player.inventory.TryAdd("food_test", 1);
        fixture.system.StartRun(fixture.player.playerId, "default", "node_01");
        fixture.system.MoveToNode("node_02");
        int energyBeforeFood = fixture.player.profile.energy;

        CommandResult badEvacuate = fixture.system.EvacuateRun();
        CommandResult food = fixture.system.UseFood("food_test");

        Assert.IsFalse(badEvacuate.success);
        Assert.IsTrue(food.success);
        Assert.AreEqual(0, fixture.player.inventory.GetAmount("food_test"));
        Assert.AreEqual(100, fixture.player.profile.energy);
    }

    [Test]
    public void MazeSystemExitPuzzleSupportsNormalAndPerfectClear()
    {
        MazeFixture normalFixture = CreateMazeFixture();
        normalFixture.system.StartRun(normalFixture.player.playerId, "default", "node_01");
        normalFixture.system.MoveToNode("node_02");
        normalFixture.system.CollectNodeReward("node_02");
        normalFixture.system.MoveToNode("node_03");
        normalFixture.system.ActivateSwitch("node_03");
        normalFixture.system.MoveToNode("node_04");
        normalFixture.system.MoveToNode("node_05");
        normalFixture.system.MoveToNode("node_06");
        normalFixture.system.MoveToNode("node_07");

        CommandResult open = normalFixture.system.OpenExitPuzzle();
        CommandResult normal = normalFixture.system.LeaveWithoutPerfect();
        MazeRunResult normalResult = normal.payload as MazeRunResult;

        Assert.IsTrue(open.success);
        Assert.IsTrue(normal.success);
        Assert.IsNotNull(normalResult);
        Assert.AreEqual(MazeRunEndReason.Clear, normalResult.endReason);
        Assert.AreEqual(1f, normalResult.multiplier);
        Assert.IsFalse(normalResult.perfectClear);
        Assert.AreEqual(4, normalFixture.player.inventory.GetAmount("ingredient_carrot"));

        MazeFixture perfectFixture = CreateMazeFixture();
        perfectFixture.system.StartRun(perfectFixture.player.playerId, "default", "node_01");
        perfectFixture.system.MoveToNode("node_02");
        perfectFixture.system.CollectNodeReward("node_02");
        perfectFixture.system.MoveToNode("node_03");
        perfectFixture.system.ActivateSwitch("node_03");
        perfectFixture.system.MoveToNode("node_04");
        perfectFixture.system.ResolvePuzzleEvaluation("puzzle_01", MazePuzzleEvaluation.Correct("Puzzle solved."));
        perfectFixture.system.MoveToNode("node_05");
        perfectFixture.system.ResolveTrap("trap_01", true);
        perfectFixture.system.MoveToNode("node_06");
        perfectFixture.system.MoveToNode("node_07");

        CommandResult perfect = perfectFixture.system.AssembleExitPuzzle();
        MazeRunResult perfectResult = perfect.payload as MazeRunResult;

        Assert.IsTrue(perfect.success);
        Assert.IsNotNull(perfectResult);
        Assert.AreEqual(MazeRunEndReason.PerfectClear, perfectResult.endReason);
        Assert.AreEqual(2f, perfectResult.multiplier);
        Assert.AreEqual("blueprint_maze_energy", perfectResult.blueprintId);
        Assert.IsTrue(perfectFixture.player.blueprints.Contains("blueprint_maze_energy"));
    }
    [Test]
    public void MazeServiceRegistersCommandsAndReturnsStableViewModels()
    {
        FrameworkContext framework = new FrameworkContext();
        framework.Init();
        EcsWorld world = new EcsWorld();
        world.Init();
        PlayerDatabase player = CreatePlayer("player_test", 100);
        GameDatabase database = CreateMazeDatabase(player);
        MazeService service = new MazeService();

        service.Initialize(database, player, framework, world);
        MazeViewModel notStarted = service.GetViewModel();
        CommandResult started = framework.Commands.Execute(new StartMazeRunCommand(player.playerId));
        framework.Commands.Execute(new MoveToMazeNodeCommand("node_02"));
        framework.Commands.Execute(new MoveToMazeNodeCommand("node_03"));
        framework.Commands.Execute(new ActivateMazeSwitchCommand("node_03"));
        framework.Commands.Execute(new MoveToMazeNodeCommand("node_04"));
        framework.Commands.Execute(new MoveToMazeNodeCommand("node_05"));
        framework.Commands.Execute(new MoveToMazeNodeCommand("node_06"));
        CommandResult ended = framework.Commands.Execute(new EvacuateMazeRunCommand());
        MazeViewModel endedVm = service.GetViewModel();

        Assert.IsFalse(notStarted.hasRun);
        Assert.IsTrue(framework.Commands.HasHandler<StartMazeRunCommand>());
        Assert.IsTrue(framework.Commands.HasHandler<FinishMazeRunCommand>());
        Assert.IsTrue(framework.Commands.HasHandler<UseMazeFoodCommand>());
        Assert.IsTrue(framework.Commands.HasHandler<AssembleMazePuzzleCommand>());
        Assert.IsTrue(framework.Commands.HasHandler<LeaveMazeWithoutPerfectCommand>());
        Assert.IsTrue(started.success);
        Assert.IsTrue(ended.success);
        Assert.IsTrue(endedVm.isEnded);
    }


    [Test]
    public void RealMazeConfigPuzzleCommandsGrantAllFragmentsWithoutExposingAnswers()
    {
        GameDatabase database = GameDatabase.GetInstance();
        database.Init();
        Assert.IsNotNull(database.databases);
        Assert.IsTrue(database.databases.ContainsKey("maze_nodes"));
        Assert.IsTrue(database.databases.ContainsKey("maze_puzzles"));
        Assert.IsTrue(database.databases.ContainsKey("maze_fragments"));
        Assert.IsTrue(database.databases.ContainsKey("maze_rules"));
        PlayerDatabase player = CreatePlayer("player_real_config", 100);
        database.playerDatabases[player.playerId] = player;
        Assert.IsNotNull(player);
        Assert.IsNotNull(player.profile);
        Assert.IsNotNull(player.inventory);
        FrameworkContext framework = new FrameworkContext();
        framework.Init();
        EcsWorld world = new EcsWorld();
        world.Init();
        MazeService service = new MazeService();
        service.Initialize(database, player, framework, world);
        CommandBus commands = framework.Commands;

        Assert.AreEqual(16, database.GetAll<MazeNodeData>("maze_nodes").Count);
        Assert.AreEqual(4, database.GetAll<MazePuzzleData>("maze_puzzles").Count);
        Assert.AreEqual(4, database.GetAll<MazeFragmentData>("maze_fragments").Count);
        foreach (MazeNodeData node in database.GetAll<MazeNodeData>("maze_nodes"))
        {
            if (node.nodeType == "puzzle_room")
                Assert.IsEmpty(node.unlockRequirementIds, node.nodeId);
        }

        CommandResult start = commands.Execute(new StartMazeRunCommand(player.playerId));
        Assert.IsTrue(start.success, start.message);
        Assert.IsTrue(commands.Execute(new MoveToMazeNodeCommand("node_02")).success);
        Assert.IsTrue(commands.Execute(new MoveToMazeNodeCommand("node_03")).success);
        Assert.IsTrue(commands.Execute(new CollectMazeNodeRewardCommand("node_03")).success);
        Assert.IsTrue(commands.Execute(new MoveToMazeNodeCommand("node_04")).success);
        Assert.IsTrue(commands.Execute(new ActivateMazeSwitchCommand("node_04")).success);

        Assert.IsTrue(commands.Execute(new MoveToMazeNodeCommand("node_05")).success);
        Assert.IsTrue(commands.Execute(new SelectItemSocketPuzzleItemCommand("puzzle_01", "correct_items")).success);
        Assert.IsTrue(commands.Execute(new SubmitMazePuzzleCommand("puzzle_01")).success);

        Assert.IsTrue(commands.Execute(new MoveToMazeNodeCommand("node_06")).success);
        Assert.IsTrue(commands.Execute(new OpenCandlePuzzlePoolCommand("puzzle_02")).success);
        Assert.IsTrue(commands.Execute(new SelectCandlePuzzleNumberCommand("puzzle_02", "9")).success);
        Assert.IsTrue(commands.Execute(new SubmitMazePuzzleCommand("puzzle_02")).success);

        Assert.IsTrue(commands.Execute(new MoveToMazeNodeCommand("node_07")).success);
        Assert.IsTrue(commands.Execute(new MoveToMazeNodeCommand("node_08")).success);
        Assert.IsTrue(commands.Execute(new CollectMazeNodeRewardCommand("node_08")).success);
        Assert.IsTrue(commands.Execute(new MoveToMazeNodeCommand("node_09")).success);
        Assert.IsTrue(commands.Execute(new MoveToMazeNodeCommand("node_10")).success);
        Assert.IsTrue(commands.Execute(new ResolveMazeTrapCommand("trap_chest", true)).success);

        Assert.IsTrue(commands.Execute(new MoveToMazeNodeCommand("node_11")).success);
        Assert.IsTrue(commands.Execute(new ChooseFloorPuzzleTileCommand("puzzle_03", "trust")).success);

        Assert.IsTrue(commands.Execute(new MoveToMazeNodeCommand("node_12")).success);
        Assert.IsTrue(commands.Execute(new ActivateRockWordPuzzleLightCommand("puzzle_04")).success);
        Assert.IsTrue(commands.Execute(new ToggleRockWordPuzzleBlockCommand("puzzle_04", "信")).success);
        Assert.IsTrue(commands.Execute(new ToggleRockWordPuzzleBlockCommand("puzzle_04", "渊")).success);
        Assert.IsTrue(commands.Execute(new ToggleRockWordPuzzleBlockCommand("puzzle_04", "剑")).success);
        Assert.IsTrue(commands.Execute(new SubmitMazePuzzleCommand("puzzle_04")).success);

        MazeViewModel vm = service.GetViewModel();
        Assert.AreEqual(4, vm.fragmentCount);
        Assert.Contains("fragment_01", vm.fragments);
        Assert.Contains("fragment_02", vm.fragments);
        Assert.Contains("fragment_03", vm.fragments);
        Assert.Contains("fragment_04", vm.fragments);
        Assert.IsFalse(vm.loot.ContainsKey("correct_items"));
        Assert.IsFalse(vm.loot.ContainsKey("wrong_items"));
        world.Dispose();
        framework.Dispose();
    }
    [Test]
    public void MazeMediatorNodeAndActionClicksDispatchCommandsOnly()
    {
        FrameworkContext framework = new FrameworkContext();
        framework.Init();
        EcsWorld world = new EcsWorld();
        world.Init();
        PlayerDatabase player = CreatePlayer("player_test", 100);
        GameDatabase database = CreateMazeDatabase(player);
        MazeService service = new MazeService();
        service.Initialize(database, player, framework, world);
        service.StartRun(player.playerId, "default", "node_01");

        CommandBus commands = new CommandBus();
        RecordingMoveHandler moveHandler = new RecordingMoveHandler();
        RecordingCollectHandler collectHandler = new RecordingCollectHandler();
        commands.Register(moveHandler);
        commands.Register(collectHandler);
        FakeMazeView view = new FakeMazeView();
        MazeMediator mediator = new MazeMediator(view, service, commands, framework.Events);

        mediator.Refresh();
        int energyBeforeMoveClick = player.profile.energy;
        mediator.OnNodeClicked(2);

        Assert.AreEqual("node_02", moveHandler.nodeId);
        Assert.AreEqual(1, moveHandler.count);
        Assert.AreEqual(energyBeforeMoveClick, player.profile.energy);
        Assert.AreEqual("node_01", service.GetViewModel().currentNodeId);

        service.MoveToNode("node_02");
        mediator.Refresh();
        int inventoryBeforeCollectClick = player.inventory.GetAmount("ingredient_carrot");
        mediator.CollectReward();

        Assert.AreEqual("node_02", collectHandler.nodeId);
        Assert.AreEqual(1, collectHandler.count);
        Assert.AreEqual(inventoryBeforeCollectClick, player.inventory.GetAmount("ingredient_carrot"));
        mediator.Dispose();
    }


    private static void MoveToPuzzle(MazeFixture fixture)
    {
        fixture.system.StartRun(fixture.player.playerId, "default", "node_01");
        fixture.system.MoveToNode("node_02");
        fixture.system.MoveToNode("node_03");
        fixture.system.MoveToNode("node_04");
    }

    private static void MoveToTrap(MazeFixture fixture)
    {
        MoveToPuzzle(fixture);
        fixture.system.MoveToNode("node_05");
    }

    private static MazeFixture CreateMazeFixture()
    {
        PlayerDatabase player = CreatePlayer("player_test", 100);
        GameDatabase database = CreateMazeDatabase(player);
        EventBus events = new EventBus();
        EcsWorld world = new EcsWorld();
        MazeSystem system = new MazeSystem(database, player, events);
        world.RegisterSystem(system);
        world.Init();

        return new MazeFixture
        {
            database = database,
            player = player,
            events = events,
            world = world,
            system = system
        };
    }

    private static GameDatabase CreateMazeDatabase(PlayerDatabase player)
    {
        GameDatabase database = new GameDatabase();
        database.playerDatabases[player.playerId] = player;

        database.databases["maze_nodes"] = new Dictionary<string, BaseData>
        {
            { "node_01", Node("node_01", 1, "entrance", new[] { "node_02", "node_04" }) },
            { "node_02", Node("node_02", 2, "corridor_reward", new[] { "node_03" }, rewardItems: new Dictionary<string, int> { { "ingredient_carrot", 4 } }) },
            { "node_03", Node("node_03", 3, "switch", new[] { "node_04" }) },
            { "node_04", Node("node_04", 4, "puzzle_room", new[] { "node_05" }, puzzleId: "puzzle_01", unlockRequirementIds: new[] { "missing_gate" }) },
            { "node_05", Node("node_05", 5, "trap_room", new[] { "node_06" }, trapId: "trap_01") },
            { "node_06", Node("node_06", 6, "evacuate", new[] { "node_07" }) },
            { "node_07", Node("node_07", 7, "exit", new string[0]) }
        };
        database.databases["maze_puzzles"] = new Dictionary<string, BaseData>
        {
            {
                "puzzle_01",
                new MazePuzzleData
                {
                    puzzleId = "puzzle_01",
                    puzzleType = "item_socket",
                    answer = "answer_key",
                    hintText = "test puzzle",
                    optionKeys = new List<string> { "answer_key", "wrong" },
                    failEnergyCost = 3,
                    successRewardItems = new Dictionary<string, int> { { "ingredient_mushroom", 1 } },
                    fragmentId = "fragment_01",
                    fragmentText = "fragment text"
                }
            }
        };
        database.databases["maze_traps"] = new Dictionary<string, BaseData>
        {
            {
                "trap_01",
                new MazeTrapData
                {
                    trapId = "trap_01",
                    trapType = "qte",
                    successEnergyCost = 2,
                    failEnergyCost = 4,
                    successRewardItems = new Dictionary<string, int> { { "ingredient_salt", 2 } },
                    failEndRun = true,
                    guideNodeId = "node_04"
                }
            }
        };
        database.databases["maze_fragments"] = new Dictionary<string, BaseData>
        {
            {
                "fragment_01",
                new MazeFragmentData
                {
                    fragmentId = "fragment_01",
                    order = 1,
                    displayText = "fragment text",
                    assetKey = "fragment_asset"
                }
            }
        };
        database.databases["foods"] = new Dictionary<string, BaseData>
        {
            {
                "food_test",
                new FoodData
                {
                    foodId = "food_test",
                    displayName = "Test Food",
                    energyRestore = 20,
                    price = 1,
                    ingredientItems = new List<string>(),
                    ingredientAmounts = new List<int>()
                }
            }
        };
        database.databases["maze_rules"] = new Dictionary<string, BaseData>
        {
            {
                "default",
                new MazeRuleData
                {
                    ruleId = "default",
                    beginRunEnergyCost = 1,
                    enterRoomEnergyCost = 5,
                    timedEnergyCost = 2,
                    timedEnergySeconds = 10,
                    evacuateMultiplier = 0.5f,
                    failMultiplier = 0.3f,
                    clearMultiplier = 1f,
                    perfectMultiplier = 2f,
                    perfectBlueprintId = "blueprint_maze_energy"
                }
            }
        };

        return database;
    }

    private static MazeNodeData Node(
        string nodeId,
        int index,
        string nodeType,
        IEnumerable<string> nextNodeIds,
        string puzzleId = null,
        string trapId = null,
        IEnumerable<string> unlockRequirementIds = null,
        Dictionary<string, int> rewardItems = null)
    {
        return new MazeNodeData
        {
            nodeId = nodeId,
            index = index,
            nodeType = nodeType,
            title = nodeId,
            roomId = string.Empty,
            puzzleId = puzzleId,
            trapId = trapId,
            rewardItems = rewardItems ?? new Dictionary<string, int>(),
            roomPickupItems = new Dictionary<string, int>(),
            energyDelta = 0,
            mapX = index,
            mapY = index,
            nextNodeIds = new List<string>(nextNodeIds ?? new string[0]),
            unlockRequirementIds = new List<string>(unlockRequirementIds ?? new string[0]),
            note = "test node"
        };
    }

    private static PlayerDatabase CreatePlayer(string playerId, int energy)
    {
        PlayerDatabase player = new PlayerDatabase();
        player.playerId = playerId;
        player.profile = new PlayerProfile();
        player.profile.init(playerId);
        player.profile.energy = energy;
        player.profile.maxEnergy = 100;
        player.inventory = new PlayerInventory();
        player.inventory.playerId = playerId;
        player.collections = new PlayerCollections();
        player.blueprints = new HashSet<string>();
        player.activeBuffs = new List<ActiveBuff>();
        return player;
    }
}







