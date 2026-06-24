using System.Collections.Generic;

public sealed class PlayerCollections
{
    private readonly HashSet<string> foods = new HashSet<string>();
    private readonly HashSet<string> outfits = new HashSet<string>();
    private readonly HashSet<string> furniture = new HashSet<string>();

    public bool Unlock(CollectionCategory category, string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        return GetSet(category).Add(id);
    }

    public bool IsUnlocked(CollectionCategory category, string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        return GetSet(category).Contains(id);
    }

    public IReadOnlyCollection<string> GetUnlocked(CollectionCategory category)
    {
        return GetSet(category);
    }

    private HashSet<string> GetSet(CollectionCategory category)
    {
        switch (category)
        {
            case CollectionCategory.Food:
                return foods;
            case CollectionCategory.Outfit:
                return outfits;
            case CollectionCategory.Furniture:
                return furniture;
            default:
                return foods;
        }
    }
}
