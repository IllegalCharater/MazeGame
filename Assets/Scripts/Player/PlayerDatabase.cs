using System.Collections.Generic;
using UnityEngine;

public class PlayerDatabase
{
    
    public PlayerInventory inventory;
    public PlayerProfile profile;
    public PlayerCollections collections;
    public HashSet<string> blueprints;
    public List<ActiveBuff> activeBuffs;

    public string playerId ="unknown";

    public void init(string playerId)
    {
        this.playerId=playerId;

        PlayerStartData startData = GameDatabase.Instance.Get<PlayerStartData>("player_start", this.playerId);

        inventory = new PlayerInventory();
        if (startData != null)
        {
            inventory.playerId = this.playerId;
            inventory.ReplaceFrom(startData);
        }
        else
        {
            inventory.init(this.playerId);
        }

        profile = new PlayerProfile();
        if (startData != null)
            profile.ReplaceFrom(startData);
        else
            profile.init(this.playerId);

        collections = new PlayerCollections();
        blueprints = new HashSet<string>();
        activeBuffs = new List<ActiveBuff>();
    }

    
    
}
