public sealed class OutfitService : IGameService
{
    private PlayerDatabase player;

    public void Initialize(GameDatabase database, PlayerDatabase player)
    {
        this.player = player;
    }

    public bool EquipOutfit(string outfitId)
    {
        if (player?.profile == null)
            return false;
        if (!player.collections.IsUnlocked(CollectionCategory.Outfit, outfitId))
            return false;

        player.profile.equippedOutfitId = outfitId;
        GameEvents.RaiseCollectionChanged(CollectionCategory.Outfit);
        return true;
    }
}
