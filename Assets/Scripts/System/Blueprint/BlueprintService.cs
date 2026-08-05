using System.Collections.Generic;

public sealed class BlueprintService {
    private GameDatabase database;
    private PlayerDatabase player;
    private IRequirementChecker requirementChecker = new DefaultRequirementChecker();

    public void Initialize(GameDatabase database, PlayerDatabase player) {
        this.database = database;
        this.player = player;
    }

    public bool HasBlueprint(string blueprintId) {
        return player != null && player.Blueprints.Contains(blueprintId);
    }

    public bool TryBuyBlueprint(string blueprintId) {
        BlueprintData blueprint = database?.Get<BlueprintData>("blueprints", blueprintId);
        if (blueprint == null || player == null)
            return false;
        if (!RequirementsMet(blueprint.requirementIds))
            return false;
        if (GameManager.Instance == null || !GameManager.Instance.TrySpendCurrency(player.playerId, blueprint.price))
            return false;

        player.Blueprints.Add(blueprintId);
        GameEvents.RaiseCollectionChanged(CollectionCategory.Blueprint);
        return true;
    }

    public IReadOnlyCollection<string> GetOwnedBlueprints() {
        if (player != null)
            return player.Blueprints;

        return new List<string>();
    }

    private bool RequirementsMet(List<string> requirementIds) {
        if (requirementIds == null)
            return true;

        foreach (string id in requirementIds) {
            if (!requirementChecker.IsRequirementMet(id, database, player))
                return false;
        }

        return true;
    }
}

public sealed class DefaultRequirementChecker : IRequirementChecker {
    public bool IsRequirementMet(string requirementId, GameDatabase database, PlayerDatabase player) {
        return true;
    }
}
