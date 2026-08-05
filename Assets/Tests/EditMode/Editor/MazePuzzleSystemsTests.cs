using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public sealed class MazePuzzleSystemsTests {
    private EcsWorld world;
    private GameContext framework;
    private MazeService service;
    private PlayerDatabase player;

    [SetUp]
    public void SetUp() {
        player = CreatePlayer();
        GameDatabase database = CreateDatabase(player);
        framework = new GameContext();
        framework.Init();
        world = new EcsWorld();
        world.Init();
        service = new MazeService();
        service.Initialize(database, player, framework, world);
        Assert.IsTrue(service.StartRun(player.playerId, "default", "node_01").success);
    }

    [TearDown]
    public void TearDown() {
        world?.Dispose();
        framework?.Dispose();
    }

    [Test]
    public void FourPuzzleSystemsRegisterAndOwnIndependentComponents() {
        Assert.AreEqual(4, service.PuzzleSystems.Count);
        Assert.AreEqual(1, world.GetEntitiesWith<ItemSocketPuzzleStateComponent>().Count);
        Assert.AreEqual(1, world.GetEntitiesWith<CandleNumberPuzzleStateComponent>().Count);
        Assert.AreEqual(1, world.GetEntitiesWith<FloorChoicePuzzleStateComponent>().Count);
        Assert.AreEqual(1, world.GetEntitiesWith<RockWordPuzzleStateComponent>().Count);
    }

    [Test]
    public void FourPuzzleRoomsCompleteThroughTheirOwnCommands() {
        Assert.IsTrue(service.CollectCurrentNodeReward().success);
        Assert.IsTrue(service.MoveToNode("node_02").success);

        int energyBeforeInvalid = player.Profile.energy;
        Assert.IsFalse(service.SelectItemSocketItem("puzzle_01", "missing").success);
        Assert.AreEqual(energyBeforeInvalid, player.Profile.energy);

        Assert.IsTrue(service.SelectItemSocketItem("puzzle_01", "wrong_items").success);
        Assert.IsFalse(service.SubmitPuzzle("puzzle_01").success);
        Assert.AreEqual(energyBeforeInvalid - 1, player.Profile.energy);
        Assert.IsTrue(service.SelectItemSocketItem("puzzle_01", "correct_items").success);
        Assert.IsTrue(service.SubmitPuzzle("puzzle_01").success);
        Assert.Contains("fragment_01", service.GetViewModel().fragments);

        Assert.IsTrue(service.MoveToNode("node_03").success);
        int energyBeforePool = player.Profile.energy;
        Assert.IsFalse(service.SelectPuzzleNumber("puzzle_02", "9").success);
        Assert.AreEqual(energyBeforePool, player.Profile.energy);
        Assert.IsTrue(service.TogglePuzzleCandle("puzzle_02", 0).success);
        Assert.IsTrue(service.OpenPuzzlePool("puzzle_02").success);
        Assert.IsTrue(service.SelectPuzzleNumber("puzzle_02", "9").success);
        Assert.IsTrue(service.SubmitPuzzle("puzzle_02").success);
        Assert.Contains("fragment_02", service.GetViewModel().fragments);

        Assert.IsTrue(service.MoveToNode("node_04").success);
        int energyBeforeWrongFloor = player.Profile.energy;
        Assert.IsFalse(service.ChoosePuzzleFloor("puzzle_03", "profit").success);
        Assert.AreEqual(energyBeforeWrongFloor - 1, player.Profile.energy);
        Assert.IsTrue(service.ChoosePuzzleFloor("puzzle_03", "trust").success);
        Assert.Contains("fragment_03", service.GetViewModel().fragments);

        Assert.IsTrue(service.MoveToNode("node_05").success);
        int energyBeforeLight = player.Profile.energy;
        Assert.IsFalse(service.ToggleRockWordBlock("puzzle_04", "渊").success);
        Assert.AreEqual(energyBeforeLight, player.Profile.energy);
        Assert.IsTrue(service.ActivateRockWordLight("puzzle_04").success);
        Assert.IsTrue(service.ToggleRockWordBlock("puzzle_04", "信").success);
        Assert.IsTrue(service.ToggleRockWordBlock("puzzle_04", "渊").success);
        Assert.IsTrue(service.ToggleRockWordBlock("puzzle_04", "剑").success);
        Assert.IsTrue(service.SubmitPuzzle("puzzle_04").success);

        MazeViewModel vm = service.GetViewModel();
        Assert.AreEqual(4, vm.fragmentCount);
        Assert.Contains("fragment_04", vm.fragments);
        Assert.IsFalse(vm.loot.ContainsKey("correct_items"));
        Assert.IsFalse(vm.loot.ContainsKey("wrong_items"));
    }

    [Test]
    public void PuzzleStatePersistsAcrossViewReadsAndResetsForNewRun() {
        Assert.IsTrue(service.CollectCurrentNodeReward().success);
        Assert.IsTrue(service.MoveToNode("node_02").success);
        Assert.IsTrue(service.SelectItemSocketItem("puzzle_01", "correct_items").success);
        Assert.AreEqual("correct_items", service.GetCurrentPuzzleViewModel().selectedInput);
        Assert.AreEqual("correct_items", service.GetCurrentPuzzleViewModel().selectedInput);

        Assert.IsTrue(service.StartRun(player.playerId, "default", "node_01").success);
        Assert.IsTrue(service.CollectCurrentNodeReward().success);
        Assert.IsTrue(service.MoveToNode("node_02").success);
        Assert.IsTrue(string.IsNullOrEmpty(service.GetCurrentPuzzleViewModel().selectedInput));
    }

    [Test]
    public void ItemSocketPuzzleTracksSevenOrderedSlots() {
        Assert.IsTrue(service.CollectCurrentNodeReward().success);
        Assert.IsTrue(service.MoveToNode("node_02").success);

        MazePuzzleRoomViewModel vm = service.GetCurrentPuzzleViewModel();
        Assert.AreEqual(ItemSocketPuzzleSystem.SlotCount, vm.slotItemKeys.Count);
        Assert.IsTrue(vm.slotItemKeys.TrueForAll(string.IsNullOrEmpty));

        // 不指定槽位＝落进第一个空槽；显式指定则落进该槽。
        Assert.IsTrue(service.SelectItemSocketItem("puzzle_01", "correct_items").success);
        Assert.IsTrue(service.SelectItemSocketItem("puzzle_01", "wrong_items", 4).success);
        vm = service.GetCurrentPuzzleViewModel();
        Assert.AreEqual("correct_items", vm.slotItemKeys[0]);
        Assert.AreEqual("wrong_items", vm.slotItemKeys[4]);

        // 同一件道具换槽不会占两个槽。
        Assert.IsTrue(service.SelectItemSocketItem("puzzle_01", "wrong_items", 6).success);
        vm = service.GetCurrentPuzzleViewModel();
        Assert.IsEmpty(vm.slotItemKeys[4]);
        Assert.AreEqual("wrong_items", vm.slotItemKeys[6]);

        // 越界槽位与未拾取道具都要被拒绝，且不改动已有摆放。
        Assert.IsFalse(service.SelectItemSocketItem("puzzle_01", "correct_items", ItemSocketPuzzleSystem.SlotCount).success);
        Assert.IsFalse(service.SelectItemSocketItem("puzzle_01", "missing", 1).success);
        Assert.AreEqual("correct_items", service.GetCurrentPuzzleViewModel().slotItemKeys[0]);

        // 单槽清空只影响该槽，整体清空抹掉全部。
        Assert.IsTrue(service.RemoveItemSocketItem("puzzle_01", 6).success);
        vm = service.GetCurrentPuzzleViewModel();
        Assert.IsEmpty(vm.slotItemKeys[6]);
        Assert.AreEqual("correct_items", vm.slotItemKeys[0]);
        Assert.IsTrue(service.RemoveItemSocketItem("puzzle_01").success);
        Assert.IsTrue(service.GetCurrentPuzzleViewModel().slotItemKeys.TrueForAll(string.IsNullOrEmpty));

        // 槽位为空时提交属于 Invalid，不扣体力也不算一次尝试。
        int energyBefore = player.Profile.energy;
        Assert.IsFalse(service.SubmitPuzzle("puzzle_01").success);
        Assert.AreEqual(energyBefore, player.Profile.energy);

        // 多摆一件即与单件答案长度不符，判错并复位祭坛。
        Assert.IsTrue(service.SelectItemSocketItem("puzzle_01", "correct_items", 0).success);
        Assert.IsTrue(service.SelectItemSocketItem("puzzle_01", "wrong_items", 1).success);
        Assert.IsFalse(service.SubmitPuzzle("puzzle_01").success);
        Assert.AreEqual(energyBefore - 1, player.Profile.energy);
        Assert.IsTrue(service.GetCurrentPuzzleViewModel().slotItemKeys.TrueForAll(string.IsNullOrEmpty));

        Assert.IsTrue(service.SelectItemSocketItem("puzzle_01", "correct_items", 3).success);
        Assert.IsTrue(service.SubmitPuzzle("puzzle_01").success);
        Assert.Contains("fragment_01", service.GetViewModel().fragments);
    }

    [Test]
    public void FourPuzzlePrefabsMatchUiMappingsAndAddressableEntries() {
        // 关 1 已按策划案重做成七槽 + 七枚具名道具托盘，节点名从 OptionButton_* 改成
        // SlotButton_* / ItemButton_*，这里跟着改；其余三关节点名未变。
        AssertPuzzlePrefab("MazePuzzleItemSocketUI", typeof(ItemSocketPuzzleUIController),
            "CloseButton", "SubmitButton", "SlotButton_0", "SlotButton_6",
            "ItemButton_0", "ItemButton_6", "RemoveButton");
        AssertPuzzlePrefab("MazePuzzleCandleNumberUI", typeof(CandleNumberPuzzleUIController),
            "CloseButton", "SubmitButton", "CandleButton_0", "CandleButton_8",
            "PoolButton", "NumberPanel", "NumberButton_0", "NumberButton_9", "ClearButton");
        AssertPuzzlePrefab("MazePuzzleFloorChoiceUI", typeof(FloorChoicePuzzleUIController),
            "CloseButton", "OptionButton_0", "OptionButton_7");
        AssertPuzzlePrefab("MazePuzzleRockWordUI", typeof(RockWordPuzzleUIController),
            "CloseButton", "SubmitButton", "LightButton", "NoteButton",
            "RockIndicator_0", "RockIndicator_2", "OptionButton_0", "OptionButton_6");
    }

    private static PlayerDatabase CreatePlayer() {
        PlayerDatabase result = new PlayerDatabase {
            playerId = "puzzle_test_player",
            Profile = new PlayerProfile(),
            Inventory = new PlayerInventory(),
            Collections = new PlayerCollections(),
            Blueprints = new HashSet<string>(),
            ActiveBuffs = new List<ActiveBuff>()
        };
        result.Profile.init(result.playerId);
        result.Profile.energy = 100;
        result.Profile.maxEnergy = 100;
        result.Inventory.playerId = result.playerId;
        return result;
    }

    private static GameDatabase CreateDatabase(PlayerDatabase player) {
        GameDatabase database = new GameDatabase();
        database.playerDatabases[player.playerId] = player;
        database.configdatabases["maze_nodes"] = new Dictionary<string, BaseData>
        {
            { "node_01", Node("node_01", 1, "entrance", "node_02", null, new Dictionary<string, int> { { "correct_items", 1 }, { "wrong_items", 1 } }) },
            { "node_02", Node("node_02", 2, "puzzle_room", "node_03", "puzzle_01") },
            { "node_03", Node("node_03", 3, "puzzle_room", "node_04", "puzzle_02") },
            { "node_04", Node("node_04", 4, "puzzle_room", "node_05", "puzzle_03") },
            { "node_05", Node("node_05", 5, "puzzle_room", null, "puzzle_04") }
        };
        database.configdatabases["maze_puzzles"] = new Dictionary<string, BaseData>
        {
            { "puzzle_01", Puzzle("puzzle_01", ItemSocketPuzzleSystem.TypeId, "correct_items", new[] { "correct_items", "wrong_items" }, "fragment_01") },
            { "puzzle_02", Puzzle("puzzle_02", CandleNumberPuzzleSystem.TypeId, "9", new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "clear" }, "fragment_02") },
            { "puzzle_03", Puzzle("puzzle_03", FloorChoicePuzzleSystem.TypeId, "trust", new[] { "profit", "trust" }, "fragment_03") },
            { "puzzle_04", Puzzle("puzzle_04", RockWordPuzzleSystem.TypeId, "渊,剑,信", new[] { "旧", "名", "渊", "剑", "信", "清", "泉" }, "fragment_04") }
        };
        database.configdatabases["maze_fragments"] = new Dictionary<string, BaseData>();
        for (int i = 1; i <= 4; i++) {
            string id = "fragment_0" + i;
            database.configdatabases["maze_fragments"][id] = new MazeFragmentData { fragmentId = id, order = i, displayText = id, assetKey = id };
        }
        database.configdatabases["maze_traps"] = new Dictionary<string, BaseData>();
        database.configdatabases["foods"] = new Dictionary<string, BaseData>();
        database.configdatabases["maze_rules"] = new Dictionary<string, BaseData>
        {
            { "default", new MazeRuleData { ruleId = "default", beginRunEnergyCost = 0, enterRoomEnergyCost = 0, timedEnergyCost = 0, timedEnergySeconds = 60, evacuateMultiplier = 0.5f, failMultiplier = 0.3f, clearMultiplier = 1f, perfectMultiplier = 2f } }
        };
        return database;
    }

    private static MazeNodeData Node(string id, int index, string type, string next, string puzzleId, Dictionary<string, int> puzzleItems = null) {
        return new MazeNodeData {
            nodeId = id,
            index = index,
            nodeType = type,
            title = id,
            puzzleId = puzzleId,
            rewardItems = new Dictionary<string, int>(),
            roomPickupItems = puzzleItems ?? new Dictionary<string, int>(),
            nextNodeIds = string.IsNullOrEmpty(next) ? new List<string>() : new List<string> { next },
            unlockRequirementIds = new List<string>(),
            note = string.Empty
        };
    }

    private static MazePuzzleData Puzzle(string id, string type, string answer, string[] options, string fragmentId) {
        return new MazePuzzleData {
            puzzleId = id,
            puzzleType = type,
            answer = answer,
            hintText = id + " hint",
            optionKeys = new List<string>(options),
            optionLabels = new List<string>(),
            failEnergyCost = 1,
            successRewardItems = new Dictionary<string, int> { { "ingredient_mushroom", 1 } },
            fragmentId = fragmentId,
            fragmentText = fragmentId + " text"
        };
    }

    private static void AssertPuzzlePrefab(string viewName, System.Type controllerType, params string[] requiredObjects) {
        Assert.AreEqual(controllerType, UIConfig.viewMap[viewName]);
        string controllerPath = "Assets/Scripts/UI/" + controllerType.Name + ".cs";
        MonoScript controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(controllerPath);
        Assert.IsNotNull(controllerScript, controllerPath);
        Assert.AreEqual(controllerType, controllerScript.GetClass());

        string path = UIConfig.GetAddress(viewName);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.IsNotNull(prefab, path);
        Assert.AreEqual(viewName, prefab.name);

        string guid = AssetDatabase.AssetPathToGUID(path);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        Assert.IsNotNull(settings);
        var entry = settings.FindAssetEntry(guid);
        Assert.IsNotNull(entry, path + " is not Addressable");
        Assert.AreEqual(path, entry.address);

        Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
        foreach (string requiredObject in requiredObjects) {
            Assert.IsTrue(System.Array.Exists(transforms, item => item.name == requiredObject),
                viewName + " is missing " + requiredObject);
        }
    }
}
