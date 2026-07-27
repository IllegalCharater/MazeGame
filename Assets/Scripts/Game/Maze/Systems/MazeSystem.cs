using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MazeSystem : ISystem
{
    private const string DefaultRuleId = "default";
    private const string DefaultStartNodeId = "node_01";
    private const int TrapFailureEnergyCost = 10;

    private readonly GameDatabase database;
    private readonly PlayerDatabase player;
    private readonly EventBus eventBus;
    private readonly Dictionary<string, EntityId> nodeEntities = new Dictionary<string, EntityId>();
    private readonly Dictionary<string, MazeNodeData> nodeData = new Dictionary<string, MazeNodeData>();
    private readonly Dictionary<string, MazePuzzleData> puzzleData = new Dictionary<string, MazePuzzleData>();
    private readonly Dictionary<string, MazeTrapData> trapData = new Dictionary<string, MazeTrapData>();
    private readonly Dictionary<string, MazeFragmentData> fragmentData = new Dictionary<string, MazeFragmentData>();
    private readonly Dictionary<string, MazeRuleData> ruleData = new Dictionary<string, MazeRuleData>();

    private EcsWorld world;
    private EntityId currentRunEntity;
    private MazeRunResult lastResult;
    private string lastMessage = "Maze is ready.";

    public MazeSystem(GameDatabase database, PlayerDatabase player, EventBus eventBus)
    {
        this.database = database;
        this.player = player;
        this.eventBus = eventBus;
    }

    public void Initialize(EcsWorld world)
    {
        this.world = world;
        LoadConfig();
        BuildNodeEntities();
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f || !TryGetRun(out MazeRunComponent run) || run.state != MazeRunState.Running)
            return;

        MazeRuleData rule = ResolveRule(run.ruleId);
        if (rule == null || rule.timedEnergySeconds <= 0 || rule.timedEnergyCost <= 0)
        {
            run.elapsedSeconds += deltaTime;
            return;
        }

        run.elapsedSeconds += deltaTime;
        run.timedEnergyAccumulator += deltaTime;
        bool changed = false;
        while (run.timedEnergyAccumulator >= rule.timedEnergySeconds && run.state == MazeRunState.Running)
        {
            run.timedEnergyAccumulator -= rule.timedEnergySeconds;
            changed = true;
            if (!SpendEnergy(rule.timedEnergyCost, true))
                break;
        }

        if (changed && run.state == MazeRunState.Running)
            PublishUpdated();
    }

    public void Dispose()
    {
    }

    public CommandResult StartRun(string playerId, string ruleId, string startNodeId)
    {
        LoadConfig();
        BuildNodeEntities();

        if (world == null)
            return Fail("ECS world is not ready.");
        if (player?.profile == null)
            return Fail("Player profile is not ready.");
        if (!string.IsNullOrEmpty(playerId) && player.playerId != playerId)
            return Fail("Player does not match current service context.");

        string resolvedRuleId = string.IsNullOrEmpty(ruleId) ? DefaultRuleId : ruleId;
        string resolvedStartNodeId = string.IsNullOrEmpty(startNodeId) ? DefaultStartNodeId : startNodeId;
        MazeRuleData rule = ResolveRule(resolvedRuleId);
        if (rule == null)
            return Fail("Maze rule not found: " + resolvedRuleId);
        if (!nodeData.ContainsKey(resolvedStartNodeId))
            return Fail("Maze start node not found: " + resolvedStartNodeId);
        if (player.profile.energy < rule.beginRunEnergyCost)
            return Fail("Not enough energy to start maze.");

        if (currentRunEntity.IsValid && world.IsAlive(currentRunEntity))
            world.DestroyEntity(currentRunEntity);

        SpendEnergy(rule.beginRunEnergyCost, false);

        currentRunEntity = world.CreateEntity("MazeRun:" + player.playerId);
        MazeRunComponent run = new MazeRunComponent
        {
            playerId = player.playerId,
            ruleId = resolvedRuleId,
            state = MazeRunState.Running,
            currentNodeId = resolvedStartNodeId
        };
        run.visitedNodeIds.Add(resolvedStartNodeId);
        world.AddComponent(currentRunEntity, run);
        lastResult = null;
        lastMessage = "Maze run started.";

        ApplyNodeEnergyDelta(run, resolvedStartNodeId);
        PublishStarted();
        return CommandResult.Succeeded(lastMessage, GetViewModel());
    }

    public CommandResult MoveToNode(string nodeId)
    {
        if (!TryGetRun(out MazeRunComponent run))
            return Fail("Maze run has not started.");
        if (run.state != MazeRunState.Running)
            return Fail("Maze run is not active.");
        if (string.IsNullOrEmpty(nodeId) || !nodeData.ContainsKey(nodeId))
            return Fail("Maze node not found.");
        if (run.currentNodeId == nodeId)
            return Fail("Already at this node.");
        if (!CanMoveToNode(nodeId, out string reason))
            return Fail(reason);

        MazeRuleData rule = ResolveRule(run.ruleId);
        int enterCost = rule != null ? rule.enterRoomEnergyCost : 0;
        if (!SpendEnergy(enterCost, true))
            return Fail("Energy is empty.");
        if (run.state != MazeRunState.Running)
            return CommandResult.Failed(lastMessage, GetViewModel());

        run.currentNodeId = nodeId;
        run.visitedNodeIds.Add(nodeId);
        run.exitPuzzleOpened = false;
        ClearTrapState(run);
        ApplyNodeEnergyDelta(run, nodeId);
        if (run.state == MazeRunState.Running)
        {
            lastMessage = "Entered " + ResolveNodeTitle(nodeId) + ".";
            PublishUpdated();
        }

        return CommandResult.Succeeded(lastMessage, GetViewModel());
    }

    public CommandResult CollectNodeReward(string nodeId)
    {
        if (!TryGetRun(out MazeRunComponent run))
            return Fail("Maze run has not started.");
        if (run.state != MazeRunState.Running)
            return Fail("Maze run is not active.");
        if (string.IsNullOrEmpty(nodeId))
            nodeId = run.currentNodeId;
        if (run.currentNodeId != nodeId)
            return Fail("You can only collect the current node reward.");
        if (run.rewardedNodeIds.Contains(nodeId))
            return Fail("This node reward has already been collected.");
        if (!nodeEntities.TryGetValue(nodeId, out EntityId entity)
            || !world.TryGetComponent(entity, out MazeRewardComponent reward)
            || !HasRewards(reward))
            return Fail("No reward available on this node.");

        AddRewards(run.collectedItems, reward.rewardItems);
        AddRewards(run.puzzleItems, reward.roomPickupItems);
        run.rewardedNodeIds.Add(nodeId);
        lastMessage = "Collected reward from " + ResolveNodeTitle(nodeId) + ".";
        PublishUpdated();
        return CommandResult.Succeeded(lastMessage, GetViewModel());
    }

    public CommandResult ActivateSwitch(string nodeId)
    {
        if (!TryGetRun(out MazeRunComponent run))
            return Fail("Maze run has not started.");
        if (run.state != MazeRunState.Running)
            return Fail("Maze run is not active.");
        if (string.IsNullOrEmpty(nodeId))
            nodeId = run.currentNodeId;
        if (run.currentNodeId != nodeId)
            return Fail("You can only activate the current node.");
        if (!TryGetNodeComponent(nodeId, out MazeNodeComponent node) || node.nodeType != "switch")
            return Fail("This node is not a switch.");

        run.activatedNodeIds.Add(nodeId);
        lastMessage = "Switch activated.";
        PublishUpdated();
        return CommandResult.Succeeded(lastMessage, GetViewModel());
    }

    public bool TryGetActivePuzzle(string puzzleId, out MazePuzzleData puzzle, out MazeRunComponent run, out string reason)
    {
        puzzle = null;
        run = null;
        reason = string.Empty;
        if (!TryGetRun(out run))
        {
            reason = "Maze run has not started.";
            return false;
        }
        if (run.state != MazeRunState.Running)
        {
            reason = "Maze run is not active.";
            return false;
        }
        if (!TryGetNodeComponent(run.currentNodeId, out MazeNodeComponent currentNode) || string.IsNullOrEmpty(currentNode.puzzleId))
        {
            reason = "Current node has no puzzle.";
            return false;
        }

        string resolvedPuzzleId = string.IsNullOrEmpty(puzzleId) ? currentNode.puzzleId : puzzleId;
        if (currentNode.puzzleId != resolvedPuzzleId)
        {
            reason = "Puzzle does not belong to current node.";
            return false;
        }
        if (run.solvedPuzzleIds.Contains(resolvedPuzzleId))
        {
            reason = "Puzzle is already solved.";
            return false;
        }
        if (!puzzleData.TryGetValue(resolvedPuzzleId, out puzzle) || puzzle == null)
        {
            reason = "Puzzle data is not ready.";
            return false;
        }

        return true;
    }

    public CommandResult ResolvePuzzleEvaluation(string puzzleId, MazePuzzleEvaluation evaluation)
    {
        if (!TryGetActivePuzzle(puzzleId, out MazePuzzleData puzzle, out MazeRunComponent run, out string reason))
            return Fail(reason);
        if (evaluation == null || evaluation.status == MazePuzzleEvaluationStatus.Invalid)
            return CommandResult.Failed(evaluation != null ? evaluation.message : "Puzzle evaluation is invalid.", GetViewModel());

        if (evaluation.status == MazePuzzleEvaluationStatus.Correct)
        {
            run.solvedPuzzleIds.Add(puzzle.puzzleId);
            AddRewards(run.collectedItems, puzzle.successRewardItems);
            AddFragment(run, puzzle.fragmentId);
            lastMessage = string.IsNullOrEmpty(evaluation.message)
                ? "Puzzle solved. Fragment collected."
                : evaluation.message;
            PublishUpdated();
            return CommandResult.Succeeded(lastMessage, GetViewModel());
        }

        SpendEnergy(puzzle.failEnergyCost, true);
        if (run.state == MazeRunState.Running)
        {
            lastMessage = string.IsNullOrEmpty(evaluation.message)
                ? "Puzzle answer is incorrect."
                : evaluation.message;
            PublishUpdated();
        }
        return CommandResult.Failed(lastMessage, GetViewModel());
    }

    public CommandResult UseFood(string foodId)
    {
        if (!TryGetRun(out MazeRunComponent run))
            return Fail("Maze run has not started.");
        if (run.state != MazeRunState.Running)
            return Fail("Maze run is not active.");
        if (string.IsNullOrEmpty(foodId))
            return Fail("Food id is empty.");
        if (player?.profile == null || player.inventory == null)
            return Fail("Player inventory is not ready.");
        if (player.profile.energy >= player.profile.maxEnergy)
            return Fail("Energy is already full.");

        FoodData food = database?.Get<FoodData>("foods", foodId);
        if (food == null || food.energyRestore <= 0)
            return Fail("Food cannot restore energy: " + foodId);
        if (!player.inventory.TryRemove(foodId, 1))
            return Fail("Food is not in backpack: " + foodId);

        player.profile.energy = Mathf.Clamp(player.profile.energy + food.energyRestore, 0, player.profile.maxEnergy);
        run.usedFoodIds.Add(foodId);
        GameEvents.RaiseEnergyChanged(player.profile.energy, player.profile.maxEnergy);
        lastMessage = "Used food " + ResolveDisplayName(foodId) + ", energy restored by " + food.energyRestore + ".";
        PublishUpdated();
        return CommandResult.Succeeded(lastMessage, GetViewModel());
    }

    public CommandResult StartTrap(string trapId)
    {
        if (!TryGetRun(out MazeRunComponent run))
            return Fail("Maze run has not started.");
        if (run.state != MazeRunState.Running)
            return Fail("Maze run is not active.");
        if (!TryGetNodeComponent(run.currentNodeId, out MazeNodeComponent currentNode) || string.IsNullOrEmpty(currentNode.trapId))
            return Fail("Current node has no trap.");

        string resolvedTrapId = string.IsNullOrEmpty(trapId) ? currentNode.trapId : trapId;
        if (currentNode.trapId != resolvedTrapId)
            return Fail("Trap does not belong to current node.");
        if (run.resolvedTrapIds.Contains(resolvedTrapId))
            return Fail("Trap is already resolved.");
        if (!TryGetTrapComponent(resolvedTrapId, out MazeTrapComponent trap))
            return Fail("Trap data is not ready.");

        run.trapActive = true;
        run.activeTrapId = resolvedTrapId;
        run.trapMessage = BuildTrapPrompt(currentNode, trap);
        lastMessage = run.trapMessage;
        PublishUpdated();
        return CommandResult.Succeeded(lastMessage, GetViewModel());
    }

    public CommandResult ResolveTrap(string trapId, bool succeeded)
    {
        if (!TryGetRun(out MazeRunComponent run))
            return Fail("Maze run has not started.");
        if (run.state != MazeRunState.Running)
            return Fail("Maze run is not active.");
        if (!TryGetNodeComponent(run.currentNodeId, out MazeNodeComponent currentNode) || string.IsNullOrEmpty(currentNode.trapId))
            return Fail("Current node has no trap.");

        string resolvedTrapId = string.IsNullOrEmpty(trapId) ? currentNode.trapId : trapId;
        if (currentNode.trapId != resolvedTrapId)
            return Fail("Trap does not belong to current node.");
        if (run.resolvedTrapIds.Contains(resolvedTrapId))
            return Fail("Trap is already resolved.");
        if (!TryGetTrapComponent(resolvedTrapId, out MazeTrapComponent trap))
            return Fail("Trap data is not ready.");

        if (!run.trapActive || run.activeTrapId != resolvedTrapId)
        {
            run.trapActive = true;
            run.activeTrapId = resolvedTrapId;
        }

        run.resolvedTrapIds.Add(resolvedTrapId);
        if (succeeded)
        {
            SpendEnergy(trap.successEnergyCost, true);
            AddRewards(run.collectedItems, trap.successRewardItems);
            ClearTrapState(run);
            if (run.state == MazeRunState.Running)
            {
                lastMessage = "Trap resolved.";
                PublishUpdated();
            }
            return CommandResult.Succeeded(lastMessage, GetViewModel());
        }

        SpendEnergy(TrapFailureEnergyCost, true);
        ClearTrapState(run);
        if (run.state == MazeRunState.Running)
        {
            lastMessage = BuildTrapFailureMessage(currentNode, trap);
            PublishUpdated();
        }
        return CommandResult.Succeeded(lastMessage, GetViewModel());
    }

    public CommandResult OpenExitPuzzle()
    {
        if (!TryGetRun(out MazeRunComponent run))
            return Fail("Maze run has not started.");
        if (run.state != MazeRunState.Running)
            return Fail("Maze run is not active.");
        if (!IsCurrentNodeType(run, "exit"))
            return Fail("You must reach the exit first.");

        run.exitPuzzleOpened = true;
        lastMessage = BuildExitPuzzleMessage(run);
        PublishUpdated();
        return CommandResult.Succeeded(lastMessage, GetViewModel());
    }

    public CommandResult AssembleExitPuzzle()
    {
        if (!TryGetRun(out MazeRunComponent run))
            return Fail("Maze run has not started.");
        if (run.state != MazeRunState.Running)
            return Fail("Maze run is not active.");
        if (!IsCurrentNodeType(run, "exit"))
            return Fail("You must reach the exit first.");
        if (!IsPerfectClear(run))
            return Fail(BuildExitPuzzleMessage(run));

        run.exitPuzzleOpened = true;
        EndRun(MazeRunState.Completed, MazeRunEndReason.PerfectClear);
        return CommandResult.Succeeded(lastMessage, lastResult);
    }

    public CommandResult LeaveWithoutPerfect()
    {
        if (!TryGetRun(out MazeRunComponent run))
            return Fail("Maze run has not started.");
        if (run.state != MazeRunState.Running)
            return Fail("Maze run is not active.");
        if (!IsCurrentNodeType(run, "exit"))
            return Fail("You must reach the exit first.");

        run.exitPuzzleOpened = true;
        run.leftWithoutPerfect = true;
        if (!IsPerfectClear(run) && run.collectedFragments.Count > 0)
        {
            string lost = run.collectedFragments[run.collectedFragments.Count - 1];
            run.collectedFragments.RemoveAt(run.collectedFragments.Count - 1);
            run.lostFragmentIds.Add(lost);
        }

        EndRun(MazeRunState.Completed, MazeRunEndReason.Clear);
        return CommandResult.Succeeded(lastMessage, lastResult);
    }

    public CommandResult EvacuateRun()
    {
        if (!TryGetRun(out MazeRunComponent run))
            return Fail("Maze run has not started.");
        if (run.state != MazeRunState.Running)
            return Fail("Maze run is not active.");
        if (!IsCurrentNodeType(run, "evacuate"))
            return Fail("You must reach an evacuation node first.");

        EndRun(MazeRunState.Evacuated, MazeRunEndReason.Evacuate);
        return CommandResult.Succeeded(lastMessage, lastResult);
    }

    public CommandResult FinishRun()
    {
        return LeaveWithoutPerfect();
    }

    public bool CanMoveToNode(string nodeId, out string reason)
    {
        reason = string.Empty;
        if (!TryGetRun(out MazeRunComponent run))
        {
            reason = "Maze run has not started.";
            return false;
        }
        if (!TryGetNodeComponent(run.currentNodeId, out MazeNodeComponent currentNode))
        {
            reason = "Current node is missing.";
            return false;
        }
        if (!TryGetNodeComponent(nodeId, out MazeNodeComponent targetNode))
        {
            reason = "Target node is missing.";
            return false;
        }

        // 测试期：解密房间不要求与当前节点相连，可从任意节点直达（见 MazeTestSettings）。
        bool isPuzzleRoom = targetNode.nodeType == MazeTestSettings.PuzzleRoomNodeType;
        bool skipConnectivity = isPuzzleRoom && MazeTestSettings.directPuzzleRoomEntry;
        if (!skipConnectivity && !currentNode.nextNodeIds.Contains(nodeId))
        {
            reason = "Node is not connected.";
            return false;
        }

        if (isPuzzleRoom)
            return true;

        for (int i = 0; i < targetNode.unlockRequirementIds.Count; i++)
        {
            string requirementId = targetNode.unlockRequirementIds[i];
            if (string.IsNullOrEmpty(requirementId))
                continue;
            if (run.visitedNodeIds.Contains(requirementId)
                || run.activatedNodeIds.Contains(requirementId)
                || run.solvedPuzzleIds.Contains(requirementId)
                || run.resolvedTrapIds.Contains(requirementId))
                continue;

            reason = "Requirement is not met: " + requirementId;
            return false;
        }

        return true;
    }

    // 测试期：把四个解密房间补进可达列表，让迷宫地图上的按钮直接亮起来。
    // 关掉 MazeTestSettings.directPuzzleRoomEntry 后此方法不产生任何影响。
    private void AppendDirectPuzzleRooms(MazeViewModel vm, MazeRunComponent run)
    {
        if (!MazeTestSettings.directPuzzleRoomEntry || run.state != MazeRunState.Running)
            return;

        foreach (MazeNodeData data in nodeData.Values)
        {
            if (data == null
                || data.nodeType != MazeTestSettings.PuzzleRoomNodeType
                || data.nodeId == run.currentNodeId
                || vm.reachableNodeIds.Contains(data.nodeId))
                continue;

            vm.reachableNodeIds.Add(data.nodeId);
        }
    }

    public bool TryGetNodeIdByIndex(int index, out string nodeId)
    {
        foreach (KeyValuePair<string, MazeNodeData> kv in nodeData)
        {
            if (kv.Value != null && kv.Value.index == index)
            {
                nodeId = kv.Key;
                return true;
            }
        }

        nodeId = string.Empty;
        return false;
    }

    public MazeViewModel GetViewModel()
    {
        MazeViewModel vm = new MazeViewModel();
        FillStaticNodeInfo(vm);
        FillEnergy(vm);
        FillFoods(vm);
        vm.message = lastMessage;
        vm.result = lastResult;

        if (!TryGetRun(out MazeRunComponent run))
        {
            vm.hasRun = false;
            vm.state = MazeRunState.NotStarted;
            vm.message = string.IsNullOrEmpty(lastMessage) ? "Maze run has not started." : lastMessage;
            FillFragmentProgress(vm, null);
            return vm;
        }

        vm.hasRun = true;
        vm.state = run.state;
        vm.endReason = run.endReason;
        vm.isEnded = run.state == MazeRunState.Completed || run.state == MazeRunState.Evacuated || run.state == MazeRunState.Failed;
        vm.currentNodeId = run.currentNodeId;
        vm.visitedNodeIds.AddRange(run.visitedNodeIds);
        vm.activatedNodeIds.AddRange(run.activatedNodeIds);
        vm.solvedPuzzleIds.AddRange(run.solvedPuzzleIds);
        vm.resolvedTrapIds.AddRange(run.resolvedTrapIds);
        vm.fragments.AddRange(run.collectedFragments);
        vm.lostFragments.AddRange(run.lostFragmentIds);
        vm.trapActive = run.trapActive;
        vm.activeTrapId = run.activeTrapId;
        vm.trapMessage = run.trapMessage;
        CopyRewards(run.collectedItems, vm.loot);
        FillFragmentProgress(vm, run);

        if (TryGetNodeComponent(run.currentNodeId, out MazeNodeComponent node))
        {
            vm.currentNodeIndex = node.index;
            vm.currentNodeTitle = node.title;
            vm.currentNodeType = node.nodeType;
            vm.currentNodeNote = node.note;
            vm.puzzleId = node.puzzleId;
            vm.trapId = node.trapId;
            for (int i = 0; i < node.nextNodeIds.Count; i++)
            {
                if (CanMoveToNode(node.nextNodeIds[i], out _))
                    vm.reachableNodeIds.Add(node.nextNodeIds[i]);
            }
            AppendDirectPuzzleRooms(vm, run);

            vm.canCollectReward = CanCollectReward(run, node.nodeId);
            vm.canActivateSwitch = node.nodeType == "switch" && !run.activatedNodeIds.Contains(node.nodeId);
            vm.canSubmitPuzzle = !string.IsNullOrEmpty(node.puzzleId) && !run.solvedPuzzleIds.Contains(node.puzzleId);
            vm.canStartTrap = !string.IsNullOrEmpty(node.trapId) && !run.resolvedTrapIds.Contains(node.trapId) && !run.trapActive;
            vm.canResolveTrap = !string.IsNullOrEmpty(node.trapId) && !run.resolvedTrapIds.Contains(node.trapId);
            vm.canResolveTrapSuccess = vm.canResolveTrap;
            vm.canResolveTrapFailure = vm.canResolveTrap;
            vm.canEvacuate = node.nodeType == "evacuate" && run.state == MazeRunState.Running;
            vm.canFinish = node.nodeType == "exit" && run.state == MazeRunState.Running;
            vm.canOpenExitPuzzle = vm.canFinish;
            vm.canAssemblePuzzle = vm.canFinish && IsPerfectClear(run);
            vm.canLeaveWithoutPerfect = vm.canFinish;
            vm.exitPuzzleMessage = node.nodeType == "exit" ? BuildExitPuzzleMessage(run) : string.Empty;
            if (!string.IsNullOrEmpty(node.trapId) && TryGetTrapComponent(node.trapId, out MazeTrapComponent trap))
                vm.trapGuideNodeId = trap.guideNodeId;
            if (!string.IsNullOrEmpty(node.puzzleId) && TryGetPuzzleComponent(node.puzzleId, out MazePuzzleComponent puzzle))
                vm.puzzleType = puzzle.puzzleType;
        }

        vm.canUseFood = vm.availableFoods.Count > 0 && vm.currentEnergy < vm.maxEnergy && run.state == MazeRunState.Running;
        foreach (KeyValuePair<string, int> kv in vm.availableFoods)
        {
            vm.recommendedFoodId = kv.Key;
            break;
        }

        return vm;
    }

    private void LoadConfig()
    {
        if (database == null)
            return;

        LoadMap(database.GetAll<MazeNodeData>("maze_nodes"), nodeData, data => data.nodeId);
        LoadMap(database.GetAll<MazePuzzleData>("maze_puzzles"), puzzleData, data => data.puzzleId);
        LoadMap(database.GetAll<MazeTrapData>("maze_traps"), trapData, data => data.trapId);
        LoadMap(database.GetAll<MazeFragmentData>("maze_fragments"), fragmentData, data => data.fragmentId);
        LoadMap(database.GetAll<MazeRuleData>("maze_rules"), ruleData, data => data.ruleId);
    }

    private static void LoadMap<T>(IReadOnlyList<T> source, Dictionary<string, T> target, Func<T, string> idGetter) where T : BaseData
    {
        if (source == null || target.Count > 0)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            T data = source[i];
            string id = data != null ? idGetter(data) : string.Empty;
            if (!string.IsNullOrEmpty(id))
                target[id] = data;
        }
    }

    private void BuildNodeEntities()
    {
        if (world == null || nodeEntities.Count > 0)
            return;

        foreach (MazeNodeData data in nodeData.Values)
        {
            EntityId entity = world.CreateEntity("MazeNode:" + data.nodeId);
            nodeEntities[data.nodeId] = entity;
            world.AddComponent(entity, ToNodeComponent(data));
            world.AddComponent(entity, ToRewardComponent(data));
            world.AddComponent(entity, new MazePositionComponent { mapX = data.mapX, mapY = data.mapY });
        }
    }

    private MazeNodeComponent ToNodeComponent(MazeNodeData data)
    {
        return new MazeNodeComponent
        {
            nodeId = data.nodeId,
            index = data.index,
            nodeType = data.nodeType,
            title = data.title,
            roomId = data.roomId,
            puzzleId = data.puzzleId,
            trapId = data.trapId,
            nextNodeIds = data.nextNodeIds != null ? new List<string>(data.nextNodeIds) : new List<string>(),
            unlockRequirementIds = data.unlockRequirementIds != null ? new List<string>(data.unlockRequirementIds) : new List<string>(),
            energyDelta = data.energyDelta,
            note = data.note
        };
    }

    private MazeRewardComponent ToRewardComponent(MazeNodeData data)
    {
        return new MazeRewardComponent
        {
            rewardItems = data.rewardItems != null ? new Dictionary<string, int>(data.rewardItems) : new Dictionary<string, int>(),
            roomPickupItems = data.roomPickupItems != null ? new Dictionary<string, int>(data.roomPickupItems) : new Dictionary<string, int>()
        };
    }

    private bool TryGetRun(out MazeRunComponent run)
    {
        run = null;
        return currentRunEntity.IsValid
            && world != null
            && world.TryGetComponent(currentRunEntity, out run);
    }

    private bool TryGetNodeComponent(string nodeId, out MazeNodeComponent node)
    {
        node = null;
        return !string.IsNullOrEmpty(nodeId)
            && nodeEntities.TryGetValue(nodeId, out EntityId entity)
            && world.TryGetComponent(entity, out node);
    }

    private bool TryGetPuzzleComponent(string puzzleId, out MazePuzzleComponent puzzle)
    {
        puzzle = null;
        if (string.IsNullOrEmpty(puzzleId) || !puzzleData.TryGetValue(puzzleId, out MazePuzzleData data))
            return false;

        puzzle = new MazePuzzleComponent
        {
            puzzleId = data.puzzleId,
            puzzleType = data.puzzleType,
            answer = data.answer,
            hintText = data.hintText,
            optionKeys = data.optionKeys != null ? new List<string>(data.optionKeys) : new List<string>(),
            optionLabels = data.optionLabels != null ? new List<string>(data.optionLabels) : new List<string>(),
            failEnergyCost = data.failEnergyCost,
            successRewardItems = data.successRewardItems != null ? new Dictionary<string, int>(data.successRewardItems) : new Dictionary<string, int>(),
            fragmentId = data.fragmentId,
            fragmentText = data.fragmentText
        };
        return true;
    }

    private bool TryGetTrapComponent(string trapId, out MazeTrapComponent trap)
    {
        trap = null;
        if (string.IsNullOrEmpty(trapId) || !trapData.TryGetValue(trapId, out MazeTrapData data))
            return false;

        trap = new MazeTrapComponent
        {
            trapId = data.trapId,
            trapType = data.trapType,
            successEnergyCost = data.successEnergyCost,
            failEnergyCost = data.failEnergyCost,
            successRewardItems = data.successRewardItems != null ? new Dictionary<string, int>(data.successRewardItems) : new Dictionary<string, int>(),
            failEndRun = data.failEndRun,
            guideNodeId = data.guideNodeId
        };
        return true;
    }

    private MazeRuleData ResolveRule(string ruleId)
    {
        if (!string.IsNullOrEmpty(ruleId) && ruleData.TryGetValue(ruleId, out MazeRuleData rule))
            return rule;
        if (ruleData.TryGetValue(DefaultRuleId, out rule))
            return rule;
        foreach (MazeRuleData candidate in ruleData.Values)
            return candidate;
        return null;
    }

    private bool SpendEnergy(int amount, bool endWhenEmpty)
    {
        if (amount <= 0)
            return true;
        if (player?.profile == null)
            return false;

        if (player.profile.energy < amount)
        {
            player.profile.energy = 0;
            GameEvents.RaiseEnergyChanged(player.profile.energy, player.profile.maxEnergy);
            if (endWhenEmpty)
                EndRun(MazeRunState.Failed, MazeRunEndReason.EnergyEmpty);
            return false;
        }

        player.profile.energy -= amount;
        GameEvents.RaiseEnergyChanged(player.profile.energy, player.profile.maxEnergy);
        return true;
    }

    private void ApplyNodeEnergyDelta(MazeRunComponent run, string nodeId)
    {
        if (!TryGetNodeComponent(nodeId, out MazeNodeComponent node) || node.energyDelta == 0 || player?.profile == null)
            return;

        player.profile.energy = Mathf.Clamp(player.profile.energy + node.energyDelta, 0, player.profile.maxEnergy);
        GameEvents.RaiseEnergyChanged(player.profile.energy, player.profile.maxEnergy);
        if (player.profile.energy <= 0 && run.state == MazeRunState.Running)
            EndRun(MazeRunState.Failed, MazeRunEndReason.EnergyEmpty);
    }

    private bool CanCollectReward(MazeRunComponent run, string nodeId)
    {
        if (run.rewardedNodeIds.Contains(nodeId))
            return false;
        return nodeEntities.TryGetValue(nodeId, out EntityId entity)
            && world.TryGetComponent(entity, out MazeRewardComponent reward)
            && HasRewards(reward);
    }

    private static bool HasRewards(MazeRewardComponent reward)
    {
        return HasRewards(reward.rewardItems) || HasRewards(reward.roomPickupItems);
    }

    private static bool HasRewards(Dictionary<string, int> rewards)
    {
        if (rewards == null)
            return false;
        foreach (KeyValuePair<string, int> kv in rewards)
        {
            if (!string.IsNullOrEmpty(kv.Key) && kv.Value > 0)
                return true;
        }
        return false;
    }

    private static void AddRewards(Dictionary<string, int> target, Dictionary<string, int> rewards)
    {
        if (target == null || rewards == null)
            return;

        foreach (KeyValuePair<string, int> kv in rewards)
        {
            if (string.IsNullOrEmpty(kv.Key) || kv.Value <= 0)
                continue;
            if (!target.TryGetValue(kv.Key, out int current))
                current = 0;
            target[kv.Key] = current + kv.Value;
        }
    }

    private static void CopyRewards(Dictionary<string, int> source, Dictionary<string, int> target)
    {
        if (source == null || target == null)
            return;
        foreach (KeyValuePair<string, int> kv in source)
            target[kv.Key] = kv.Value;
    }

    private void AddFragment(MazeRunComponent run, string fragmentId)
    {
        if (string.IsNullOrEmpty(fragmentId) || run.collectedFragments.Contains(fragmentId))
            return;

        run.collectedFragments.Add(fragmentId);
    }

    private void EndRun(MazeRunState state, MazeRunEndReason reason)
    {
        if (!TryGetRun(out MazeRunComponent run) || run.settled)
            return;

        run.state = state;
        run.endReason = reason;
        run.settled = true;
        ClearTrapState(run);
        float multiplier = ResolveMultiplier(run.ruleId, reason);
        MazeRunResult result = new MazeRunResult
        {
            state = state,
            endReason = reason,
            multiplier = multiplier,
            perfectClear = reason == MazeRunEndReason.PerfectClear,
            lostFragment = run.lostFragmentIds.Count > 0,
            settlementDescription = BuildSettlementDescription(reason, multiplier, run)
        };
        result.fragments.AddRange(run.collectedFragments);
        result.lostFragments.AddRange(run.lostFragmentIds);

        foreach (KeyValuePair<string, int> kv in run.collectedItems)
        {
            int finalAmount = Mathf.FloorToInt(kv.Value * multiplier);
            if (finalAmount <= 0)
                continue;
            result.finalRewards[kv.Key] = finalAmount;
            player?.inventory?.TryAdd(kv.Key, finalAmount);
        }

        if (result.perfectClear)
            result.blueprintId = UnlockPerfectBlueprint(run.ruleId);

        lastResult = result;
        lastMessage = result.settlementDescription;
        GameEvents.RaiseMazeRunChanged();
        GameEvents.RaiseMazeRunEnded(result);
    }

    private float ResolveMultiplier(string ruleId, MazeRunEndReason reason)
    {
        MazeRuleData rule = ResolveRule(ruleId);
        if (rule == null)
            return 1f;

        switch (reason)
        {
            case MazeRunEndReason.Evacuate:
                return rule.evacuateMultiplier;
            case MazeRunEndReason.EnergyEmpty:
            case MazeRunEndReason.Failed:
                return rule.failMultiplier;
            case MazeRunEndReason.PerfectClear:
                return rule.perfectMultiplier;
            default:
                return rule.clearMultiplier;
        }
    }

    private string UnlockPerfectBlueprint(string ruleId)
    {
        MazeRuleData rule = ResolveRule(ruleId);
        if (rule == null || string.IsNullOrEmpty(rule.perfectBlueprintId) || player == null)
            return string.Empty;

        if (player.blueprints == null)
            player.blueprints = new HashSet<string>();
        if (player.blueprints.Add(rule.perfectBlueprintId))
            GameEvents.RaiseCollectionChanged(CollectionCategory.Blueprint);
        return rule.perfectBlueprintId;
    }

    private bool IsPerfectClear(MazeRunComponent run)
    {
        List<string> required = GetRequiredFragmentIds();
        if (required.Count == 0)
            return true;
        for (int i = 0; i < required.Count; i++)
        {
            if (!run.collectedFragments.Contains(required[i]))
                return false;
        }
        return true;
    }

    private List<string> GetRequiredFragmentIds()
    {
        List<string> required = new List<string>();
        foreach (MazePuzzleData puzzle in puzzleData.Values)
        {
            if (puzzle != null && !string.IsNullOrEmpty(puzzle.fragmentId) && !required.Contains(puzzle.fragmentId))
                required.Add(puzzle.fragmentId);
        }
        if (required.Count > 0)
            return required;
        foreach (MazeFragmentData fragment in fragmentData.Values)
        {
            if (fragment != null && !string.IsNullOrEmpty(fragment.fragmentId) && !required.Contains(fragment.fragmentId))
                required.Add(fragment.fragmentId);
        }
        return required;
    }

    private void FillFragmentProgress(MazeViewModel vm, MazeRunComponent run)
    {
        List<string> required = GetRequiredFragmentIds();
        vm.totalFragmentCount = required.Count;
        if (run == null)
        {
            vm.fragmentCount = 0;
            vm.perfectProgress = required.Count == 0 ? 1f : 0f;
            vm.perfectMissingPercent = required.Count == 0 ? 0 : 100;
            return;
        }

        int collected = 0;
        for (int i = 0; i < required.Count; i++)
        {
            if (run.collectedFragments.Contains(required[i]))
                collected++;
        }
        vm.fragmentCount = collected;
        vm.perfectProgress = required.Count == 0 ? 1f : Mathf.Clamp01((float)collected / required.Count);
        vm.perfectMissingPercent = Mathf.Clamp(Mathf.CeilToInt((1f - vm.perfectProgress) * 100f), 0, 100);
    }

    private void FillFoods(MazeViewModel vm)
    {
        if (database == null || player?.inventory == null)
            return;

        IReadOnlyList<FoodData> foods = database.GetAll<FoodData>("foods");
        for (int i = 0; i < foods.Count; i++)
        {
            FoodData food = foods[i];
            if (food == null || string.IsNullOrEmpty(food.foodId) || food.energyRestore <= 0)
                continue;
            int amount = player.inventory.GetAmount(food.foodId);
            if (amount > 0)
                vm.availableFoods[food.foodId] = amount;
        }
    }

    private bool IsCurrentNodeType(MazeRunComponent run, string nodeType)
    {
        return TryGetNodeComponent(run.currentNodeId, out MazeNodeComponent node)
            && node.nodeType == nodeType;
    }

    private string BuildExitPuzzleMessage(MazeRunComponent run)
    {
        if (IsPerfectClear(run))
            return "All puzzle fragments are collected. Assemble the exit puzzle for double rewards.";
        int missing = Mathf.Clamp(Mathf.CeilToInt((1f - GetPerfectProgress(run)) * 100f), 0, 100);
        string suffix = run.collectedFragments.Count > 0
            ? " Leaving now will lose one puzzle fragment."
            : string.Empty;
        return "You are " + missing + "% away from perfect clear. Perfect clear grants double rewards." + suffix;
    }

    private float GetPerfectProgress(MazeRunComponent run)
    {
        List<string> required = GetRequiredFragmentIds();
        if (required.Count == 0)
            return 1f;
        int collected = 0;
        for (int i = 0; i < required.Count; i++)
        {
            if (run.collectedFragments.Contains(required[i]))
                collected++;
        }
        return Mathf.Clamp01((float)collected / required.Count);
    }

    private string BuildTrapPrompt(MazeNodeComponent node, MazeTrapComponent trap)
    {
        if (node.nodeType == "trap_corridor" || string.Equals(trap.trapType, "rockfall", StringComparison.OrdinalIgnoreCase))
            return "Rockfall trap started. Resolve it to escape.";
        return "Trap started. Resolve it to escape.";
    }

    private string BuildTrapFailureMessage(MazeNodeComponent node, MazeTrapComponent trap)
    {
        string baseMessage = "Trap failed. Energy -" + TrapFailureEnergyCost + ".";
        if (node.nodeType == "trap_corridor" || string.Equals(trap.trapType, "rockfall", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(trap.guideNodeId))
                return baseMessage + " Hint: go to " + trap.guideNodeId + ".";
            return baseMessage + " Hint: go to puzzle room 4.";
        }
        return baseMessage;
    }

    private void ClearTrapState(MazeRunComponent run)
    {
        run.trapActive = false;
        run.activeTrapId = string.Empty;
        run.trapMessage = string.Empty;
    }

    private string BuildSettlementDescription(MazeRunEndReason reason, float multiplier, MazeRunComponent run)
    {
        string description = "Maze ended: " + reason + ". Rewards x" + multiplier.ToString("0.##") + ".";
        if (run.lostFragmentIds.Count > 0)
            description += " Lost fragment: " + run.lostFragmentIds[run.lostFragmentIds.Count - 1] + ".";
        return description;
    }

    private string ResolveDisplayName(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "None";

        FoodData food = database?.Get<FoodData>("foods", id);
        if (food != null && !string.IsNullOrEmpty(food.displayName))
            return food.displayName;

        ItemData item = database?.Get<ItemData>("items", id);
        if (item != null && !string.IsNullOrEmpty(item.name))
            return item.name;

        return id;
    }

    private string ResolveNodeTitle(string nodeId)
    {
        return nodeData.TryGetValue(nodeId, out MazeNodeData data) && !string.IsNullOrEmpty(data.title)
            ? data.title
            : nodeId;
    }

    private void FillStaticNodeInfo(MazeViewModel vm)
    {
        foreach (MazeNodeData node in nodeData.Values)
        {
            if (node == null || string.IsNullOrEmpty(node.nodeId))
                continue;
            vm.nodeTitles[node.nodeId] = string.IsNullOrEmpty(node.title) ? node.nodeId : node.title;
            vm.nodeIndices[node.nodeId] = node.index;
        }
    }

    private void FillEnergy(MazeViewModel vm)
    {
        PlayerProfile profile = player?.profile;
        vm.currentEnergy = profile != null ? profile.energy : 0;
        vm.maxEnergy = profile != null ? profile.maxEnergy : 0;
    }

    private void PublishStarted()
    {
        MazeViewModel vm = GetViewModel();
        eventBus?.Publish(new MazeRunStartedEvent(vm));
        eventBus?.Publish(new MazeRunUpdatedEvent(vm));
        GameEvents.RaiseMazeRunChanged();
    }

    private void PublishUpdated()
    {
        eventBus?.Publish(new MazeRunUpdatedEvent(GetViewModel()));
        GameEvents.RaiseMazeRunChanged();
    }

    private CommandResult Fail(string message)
    {
        lastMessage = message;
        eventBus?.Publish(new MazeRunUpdatedEvent(GetViewModel()));
        return CommandResult.Failed(message, GetViewModel());
    }
}
