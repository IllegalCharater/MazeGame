using UnityEngine;

[System.Serializable]
public class PlayerProfile : BaseData {
    private string playerId;
    public string playerDisplayName;
    public int currency;
    public int maxCurrency = 100;
    public int energy;
    public int maxEnergy = 100;
    public int level;
    public string equippedOutfitId;

    public void Init(string playerId) {
        this.playerId = playerId;
    }
    protected override void LoadConfigData() {
        var config = configs["player_start"];
        var _data = config.GetData(playerId);
        playerDisplayName = _data.Get("playerDisplayName", playerDisplayName);
        currency = _data.Get("currency", currency);
        maxCurrency = _data.Get("maxCurrency", maxCurrency);
        energy = _data.Get("energy", energy);
        maxEnergy = _data.Get("maxEnergy", maxEnergy);
        level = _data.Get("level", level);
        equippedOutfitId = _data.Get("equippedOutfitId", equippedOutfitId);
    }

    protected override void LoadUpdateData() {
        // string _id = databag.Get("playerId", "");
        // if (_id != "") {
        //     playerId = _id;
        // }
        string _name = databag.Get("playerDisplayName", "");
        if (_name != "") {
            playerDisplayName = _name;
        }
        int _currency = databag.Get("currency", currency);
        if (_currency != currency) {
            currency = _currency;
        }
        int _energy = databag.Get("energy", energy);
        if (_energy != energy) {
            energy = _energy;
        }
        int _level = databag.Get("level", level);
        if (_level != level) {
            level = _level;
        }
    }
}
