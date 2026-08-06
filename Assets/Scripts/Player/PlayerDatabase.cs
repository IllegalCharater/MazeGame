using System.Collections.Generic;
using UnityEngine;

public class PlayerDatabase {
    public PlayerInventory Inventory;
    public PlayerProfile Profile;
    public PlayerCollections Collections;
    public HashSet<string> Blueprints;
    // public List<ActiveBuff> ActiveBuffs;

    public string playerId = "unknown";

    public void init(string playerId) {
        this.playerId = playerId;

        Inventory = new PlayerInventory();
        Inventory.Init(this.playerId);

        Profile = new PlayerProfile();
        Profile.Init(this.playerId);

        Collections = new PlayerCollections();
        Blueprints = new HashSet<string>();
        // ActiveBuffs = new List<ActiveBuff>();
    }

    //从外部更新数据
    public void UpdataData(DataBag newData) {
        PlayerInventory _inventory = newData.Get("Inventory", Inventory);
        PlayerProfile _profile = newData.Get("Profile", Profile);
        PlayerCollections _collections = newData.Get("Collections", Collections);
        Inventory.UpdateData(new DataBag().FromObject(_inventory));
        Profile.UpdateData(new DataBag().FromObject(_profile));
        Collections.UpdateData(new DataBag().FromObject(_collections));
    }

}
