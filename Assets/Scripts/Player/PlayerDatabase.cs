using System.Collections.Generic;
using UnityEngine;

public class PlayerDatabase : BaseData {
    private string playerId = "unknown";
    public PlayerInventory Inventory;
    public PlayerProfile Profile;


    public void Init(string playerId) {
        this.playerId = playerId;

        Inventory = new PlayerInventory();
        Inventory.Init(playerId);

        Profile = new PlayerProfile();
        Profile.Init(playerId);

    }

    //从配置中初始化
    protected override void LoadConfigData() {
        Inventory.LoadConfig(configs);
        Profile.LoadConfig(configs);
    }

    //从外部更新数据
    protected override void LoadUpdateData() {
        PlayerInventory _inventory = databag.Get("Inventory", Inventory);
        PlayerProfile _profile = databag.Get("Profile", Profile);
        // PlayerCollections _collections = databag.Get("Collections", Collections);
        Inventory.UpdateData(new DataBag().FromObject(_inventory));
        Profile.UpdateData(new DataBag().FromObject(_profile));
        // Collections.UpdateData(new DataBag().FromObject(_collections));
    }

}
