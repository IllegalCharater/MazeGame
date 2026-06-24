using NUnit.Framework;
using System.Reflection;
using UnityEngine;

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
    public void CollectItemAccumulatesCurrentRunLoot()
    {
        MazeRunService service = CreateService(10);

        Assert.IsTrue(service.BeginRun());
        Assert.IsTrue(service.CollectItem("ingredient_carrot", 2));
        Assert.IsTrue(service.CollectItem("ingredient_carrot", 3));

        Assert.AreEqual(5, service.CurrentResult.collectedItems["ingredient_carrot"]);
    }

    [Test]
    public void CompleteRunAppliesAllCollectedRewardsOnlyOnce()
    {
        PlayerDatabase player;
        MazeRunService service = CreateService(10, out player);

        service.BeginRun();
        service.CollectItem("ingredient_carrot", 4);
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
        service.CollectItem("ingredient_salt", 5);
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
        service.CollectItem("ingredient_meat", 10);
        service.FailRun();

        Assert.AreEqual(MazeRunState.Failed, service.State);
        Assert.AreEqual(3, player.inventory.GetAmount("ingredient_meat"));
    }

    [Test]
    public void PickupConfigureControlsMazeLoot()
    {
        GameObject pickupObject = new GameObject("Pickup");
        try
        {
            Pickup pickup = pickupObject.AddComponent<Pickup>();
            pickup.Configure("ingredient_meat", 3);

            Assert.AreEqual("ingredient_meat", GetPrivateField<string>(pickup, "itemId"));
            Assert.AreEqual(3, GetPrivateField<int>(pickup, "amount"));
            Assert.AreEqual(PickupType.MazeRunLoot, GetPrivateField<PickupType>(pickup, "pickupType"));

            pickup.Configure("", 0, PickupType.DirectToInventory);

            Assert.AreEqual("ingredient_meat", GetPrivateField<string>(pickup, "itemId"));
            Assert.AreEqual(3, GetPrivateField<int>(pickup, "amount"));
            Assert.AreEqual(PickupType.MazeRunLoot, GetPrivateField<PickupType>(pickup, "pickupType"));
        }
        finally
        {
            Object.DestroyImmediate(pickupObject);
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

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        return (T)target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }
}
