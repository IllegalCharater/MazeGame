using System.Collections.Generic;

public sealed class CollectionService {
    private PlayerDatabase player;

    public void Initialize(GameDatabase database, PlayerDatabase player) {
        this.player = player;
    }

    public bool Unlock(CollectionCategory category, string id) {
        if (player == null)
            return false;

        bool changed = player.Collections.Unlock(category, id);
        if (changed)
            GameEvents.RaiseCollectionChanged(category);
        return changed;
    }

    public bool IsUnlocked(CollectionCategory category, string id) {
        return player != null && player.Collections.IsUnlocked(category, id);
    }

    public IReadOnlyCollection<string> GetUnlocked(CollectionCategory category) {
        return player != null ? player.Collections.GetUnlocked(category) : new List<string>();
    }
}
