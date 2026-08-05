[System.Serializable]
public class PlayerProfile {
    public string playerId;
    public string playerDisplayName;
    public int currency;
    public int maxCurrency = 100;
    public int energy;
    public int maxEnergy = 100;
    public int level;
    public string equippedOutfitId;

    public void Init(string playerId) {
        this.playerId = playerId;
        PlayerProfile _profile = GameDatabase.Instance.Get<PlayerStartData>("player_start", this.playerId).profile;
        playerDisplayName = _profile.playerDisplayName;
        currency = _profile.currency;
        maxCurrency = _profile.maxCurrency;
        energy = _profile.energy;
        maxEnergy = _profile.maxEnergy;
        level = _profile.level;
        equippedOutfitId = _profile.equippedOutfitId;
    }

    public void UpdateData(DataBag newdata) {
        string _id = newdata.Get("playerId", "");
        if (_id != "") {
            playerId = _id;
        }
        string _name = newdata.Get("playerDisplayName", "");
        if (_name != "") {
            playerDisplayName = _name;
        }
        int _currency = newdata.Get("currency", currency);
        if (_currency != currency) {
            currency = _currency;
        }
        int _energy = newdata.Get("energy", energy);
        if (_energy != energy) {
            energy = _energy;
        }
        int _level = newdata.Get("level", level);
        if (_level != level) {
            level = _level;
        }
    }
}
