using System.Collections.Generic;

public sealed class CollectionService
{
    private PlayerDatabase player;

    public void Initialize(GameDatabase database, PlayerDatabase player)
    {
        this.player = player;
    }

    public bool Unlock(CollectionCategory category, string id)
    {
        if (player == null)
            return false;

        bool changed = player.collections.Unlock(category, id);
        if (changed)
            GameEvents.RaiseCollectionChanged(category);
        return changed;
    }

    public bool IsUnlocked(CollectionCategory category, string id)
    {
        return player != null && player.collections.IsUnlocked(category, id);
    }

    public IReadOnlyCollection<string> GetUnlocked(CollectionCategory category)
    {
        return player != null ? player.collections.GetUnlocked(category) : new List<string>();
    }
}
