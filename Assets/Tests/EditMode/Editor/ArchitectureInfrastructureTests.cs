using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ArchitectureInfrastructureTests {
    private sealed class TestCommand : ICommand {
        public int value;
    }

    private sealed class AsyncTestCommand : ICommand {
        public int value;
    }

    private sealed class TestCommandHandler : ICommandHandler<TestCommand> {
        public CommandResult Handle(TestCommand command) {
            return CommandResult.Succeeded("handled", command.value + 1);
        }
    }

    private sealed class AsyncTestCommandHandler : IAsyncCommandHandler<AsyncTestCommand> {
        public Task<CommandResult> HandleAsync(AsyncTestCommand command) {
            return Task.FromResult(CommandResult.Succeeded("async handled", command.value + 2));
        }
    }

    private sealed class TestComponent : IComponent {
        public int value;
    }

    private sealed class SecondTestComponent : IComponent {
    }

    private sealed class TestSystem : ISystem {
        public int initialized;
        public int ticks;
        public int disposed;

        public void Initialize(EcsWorld world) {
            initialized++;
        }

        public void Tick(float deltaTime) {
            ticks++;
        }

        public void Dispose() {
            disposed++;
        }
    }

    [TearDown]
    public void TearDown() {
        if (GameContext.Instance != null)
            GameContext.Instance.Dispose();
    }

    [Test]
    public void CommandBusExecutesRegisteredHandler() {
        CommandBus bus = new CommandBus();
        bus.Register(new TestCommandHandler());

        CommandResult result = bus.Execute(new TestCommand { value = 4 });

        Assert.IsTrue(result.success);
        Assert.AreEqual(5, result.payload);
    }

    [Test]
    public void CommandBusExecutesAsyncHandlerAndRejectsSyncExecution() {
        CommandBus bus = new CommandBus();
        bus.Register(new AsyncTestCommandHandler());

        CommandResult syncResult = bus.Execute(new AsyncTestCommand { value = 4 });
        CommandResult asyncResult = bus.ExecuteAsync(new AsyncTestCommand { value = 4 }).GetAwaiter().GetResult();

        Assert.IsFalse(syncResult.success);
        Assert.IsTrue(syncResult.message.Contains("async only"));
        Assert.IsTrue(asyncResult.success);
        Assert.AreEqual(6, asyncResult.payload);
    }

    [Test]
    public void CommandBusFailsWhenHandlerIsMissing() {
        CommandBus bus = new CommandBus();

        CommandResult result = bus.Execute(new TestCommand());

        Assert.IsFalse(result.success);
        Assert.IsTrue(result.message.Contains("No command handler"));
    }

    [Test]
    public void EventBusPublishesAndUnsubscribesSafely() {
        EventBus bus = new EventBus();
        int count = 0;
        Action<CurrencyChangedEvent> handler = evt => count++;

        bus.Subscribe(handler);
        bus.Publish(new CurrencyChangedEvent(10));
        bus.Unsubscribe(handler);
        bus.Unsubscribe(handler);
        bus.Publish(new CurrencyChangedEvent(20));

        Assert.AreEqual(1, count);
    }

    [Test]
    public void EventBusListenerExceptionDoesNotBlockLaterListeners() {
        EventBus bus = new EventBus();
        int count = 0;

        bus.Subscribe<CurrencyChangedEvent>(evt => throw new InvalidOperationException("boom"));
        bus.Subscribe<CurrencyChangedEvent>(evt => count++);

        LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: boom"));
        bus.Publish(new CurrencyChangedEvent(10));

        Assert.AreEqual(1, count);
    }

    [Test]
    public void EcsWorldManagesEntityComponentsAndQueries() {
        EcsWorld world = new EcsWorld();
        world.Init();
        EntityId entity = world.CreateEntity("Player");

        Assert.IsTrue(world.AddComponent(entity, new TestComponent { value = 7 }));
        Assert.IsTrue(world.AddComponent(entity, new SecondTestComponent()));
        Assert.IsTrue(world.HasComponent<TestComponent>(entity));
        Assert.AreEqual(1, world.GetEntitiesWith<TestComponent>().Count);
        Assert.AreEqual(1, world.GetEntitiesWith<TestComponent, SecondTestComponent>().Count);
        Assert.IsTrue(world.TryGetComponent(entity, out TestComponent component));
        Assert.AreEqual(7, component.value);
        Assert.IsTrue(world.RemoveComponent<TestComponent>(entity));
        Assert.IsFalse(world.TryGetComponent(entity, out component));
        Assert.IsTrue(world.DestroyEntity(entity));
        Assert.IsFalse(world.AddComponent(entity, new TestComponent()));
        world.Dispose();
    }

    [Test]
    public void EcsWorldInitializesSystemsBeforeAndAfterInit() {
        EcsWorld world = new EcsWorld();
        TestSystem beforeInit = new TestSystem();
        TestSystem afterInit = new TestSystem();

        world.RegisterSystem(beforeInit);
        Assert.AreEqual(0, beforeInit.initialized);

        world.Init();
        world.RegisterSystem(afterInit);
        world.Tick(0.16f);

        Assert.AreEqual(1, beforeInit.initialized);
        Assert.AreEqual(1, afterInit.initialized);
        Assert.AreEqual(1, beforeInit.ticks);
        Assert.AreEqual(1, afterInit.ticks);
        world.Dispose();
        Assert.AreEqual(1, beforeInit.disposed);
        Assert.AreEqual(1, afterInit.disposed);
    }

    [Test]
    public void GameTimerAllowsCallbacksToCancelAndScheduleTimersDuringTick() {
        GameTimer timer = new GameTimer();
        int fired = 0;
        int canceledTimerId = 0;

        timer.Schedule(0.1f, () => {
            fired += 1;
            timer.Cancel(canceledTimerId);
            timer.Schedule(0.1f, () => fired += 10);
        });
        canceledTimerId = timer.Schedule(0.1f, () => fired += 100);

        timer.Tick(0.1f);
        Assert.AreEqual(1, fired);

        timer.Tick(0.1f);
        Assert.AreEqual(11, fired);
    }

    [Test]
    public void GameServicesInitializeRegistersServicesAndCommands() {
        GameContext framework = new GameContext();
        framework.Init();
        EcsWorld world = new EcsWorld();
        world.Init();
        PlayerDatabase player = CreatePlayer("player_test", 10);
        GameDatabase database = CreateDatabase(player);
        GameServices services = new GameServices();

        services.Initialize(database, player, framework, world);

        Assert.IsNotNull(services.Shop);
        Assert.IsNotNull(services.Crafting);
        Assert.IsNotNull(services.Blueprints);
        Assert.IsNotNull(services.Buffs);
        Assert.IsNotNull(services.Collections);
        Assert.IsNotNull(services.Outfits);
        Assert.IsNotNull(services.Maze);
        Assert.IsNotNull(services.World);
        Assert.IsNotNull(services.Commands);
        Assert.IsNotNull(services.Events);
        Assert.IsTrue(services.Commands.HasHandler<AddCurrencyCommand>());
        Assert.IsTrue(services.Commands.HasHandler<SpendCurrencyCommand>());
        Assert.IsTrue(services.Commands.HasHandler<OpenViewCommand>());
        Assert.IsTrue(services.Commands.HasHandler<ChangeSceneCommand>());
        Assert.IsTrue(services.Commands.HasHandler<StartMazeRunCommand>());
        Assert.IsTrue(services.Commands.HasHandler<MoveToMazeNodeCommand>());
    }

    [Test]
    public void CurrencyCommandsModifyPlayerAndPublishEvents() {
        GameContext framework = new GameContext();
        framework.Init();
        EcsWorld world = new EcsWorld();
        world.Init();
        PlayerDatabase player = CreatePlayer("player_test", 10);
        GameDatabase database = CreateDatabase(player);
        GameServices services = new GameServices();
        int eventCount = 0;
        float latestCurrency = 0;
        framework.Events.Subscribe<CurrencyChangedEvent>(evt => {
            eventCount++;
            latestCurrency = evt.newAmount;
        });
        services.Initialize(database, player, framework, world);

        CommandResult addResult = services.Commands.Execute(new AddCurrencyCommand(player.playerId, 5));
        CommandResult spendResult = services.Commands.Execute(new SpendCurrencyCommand(player.playerId, 4.5f));

        Assert.IsTrue(addResult.success);
        Assert.IsTrue(spendResult.success);
        Assert.AreEqual(10, player.Profile.currency);
        Assert.AreEqual(2, eventCount);
        Assert.AreEqual(10, latestCurrency);
    }

    private static GameDatabase CreateDatabase(PlayerDatabase player) {
        GameDatabase database = new GameDatabase();
        database.playerDatabases[player.playerId] = player;
        return database;
    }

    private static PlayerDatabase CreatePlayer(string playerId, int currency) {
        PlayerDatabase player = new PlayerDatabase();
        player.playerId = playerId;
        player.Profile = new PlayerProfile();
        player.Profile.init(playerId);
        player.Profile.currency = currency;
        player.Inventory = new PlayerInventory();
        player.Inventory.playerId = playerId;
        player.Collections = new PlayerCollections();
        player.Blueprints = new HashSet<string>();
        player.ActiveBuffs = new List<ActiveBuff>();
        return player;
    }
}



