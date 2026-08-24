using UnityEngine;

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
        ConfigData _data = GameDatabase.Instance.Get("player_start", this.playerId);
        if (_data == null) {
            Debug.LogWarning($"[PlayerProfile] player_start row not found: {playerId}");
            return;
        }
        playerDisplayName = _data.Get("playerDisplayName", playerDisplayName);
        currency = _data.Get("currency", currency);
        maxCurrency = _data.Get("maxCurrency", maxCurrency);
        energy = _data.Get("energy", energy);
        maxEnergy = _data.Get("maxEnergy", maxEnergy);
        level = _data.Get("level", level);
        equippedOutfitId = _data.Get("equippedOutfitId", equippedOutfitId);
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
