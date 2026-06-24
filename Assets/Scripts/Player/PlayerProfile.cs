[System.Serializable]
public class PlayerProfile
{
    public string playerId;
    public string playerDisplayName;
    public int currency;
    public int maxCurrency=100;
    public int energy;
    public int maxEnergy=100;
    public int level;
    public string equippedOutfitId;

    public void init(string playerId)
    {
        this.playerId = playerId;
        playerDisplayName = playerId;
        currency = 0;
        maxCurrency = 100;
        energy = maxEnergy;
        level = 1;
        equippedOutfitId = string.Empty;
    }

    public void ReplaceFrom(PlayerStartData data)
    {
        init(data?.profile != null && !string.IsNullOrEmpty(data.profile.playerId)
            ? data.profile.playerId
            : playerId);

        if (data?.profile == null)
            return;

        PlayerProfile source = data.profile;
        playerDisplayName = string.IsNullOrEmpty(source.playerDisplayName) ? playerDisplayName : source.playerDisplayName;
        currency = source.currency;
        maxCurrency = source.maxCurrency > 0 ? source.maxCurrency : maxCurrency;
        energy = source.energy;
        maxEnergy = source.maxEnergy > 0 ? source.maxEnergy : maxEnergy;
        if (maxEnergy > 0 && energy <= 0)
            energy = maxEnergy;
        if (energy > maxEnergy)
            energy = maxEnergy;
        level = source.level > 0 ? source.level : level;
        equippedOutfitId = source.equippedOutfitId;
    }
}
