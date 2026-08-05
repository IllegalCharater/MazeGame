public sealed class OutfitService {
    private PlayerDatabase player;

    public void Initialize(GameDatabase database, PlayerDatabase player) {
        this.player = player;
    }

    public bool EquipOutfit(string outfitId) {
        if (player?.Profile == null)
            return false;
        if (!player.Collections.IsUnlocked(CollectionCategory.Outfit, outfitId))
            return false;

        player.Profile.equippedOutfitId = outfitId;
        GameEvents.RaiseCollectionChanged(CollectionCategory.Outfit);
        return true;
    }
}
