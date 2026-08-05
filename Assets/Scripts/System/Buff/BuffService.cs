using System.Collections.Generic;

public sealed class BuffService {
    private GameDatabase database;
    private PlayerDatabase player;

    public void Initialize(GameDatabase database, PlayerDatabase player) {
        this.database = database;
        this.player = player;
    }

    public bool TryUseBuffItem(string buffId) {
        BuffData data = database?.Get<BuffData>("buffs", buffId);
        if (data == null || player == null)
            return false;

        player.ActiveBuffs.Add(new ActiveBuff(buffId, data.category, data.value, data.durationSeconds));
        GameEvents.RaiseBuffChanged();
        return true;
    }

    public IReadOnlyList<ActiveBuff> GetActiveBuffs() {
        return player != null ? player.ActiveBuffs : new List<ActiveBuff>();
    }

    public void ClearExpiredBuffs() {
        if (player == null)
            return;

        player.ActiveBuffs.RemoveAll(buff => buff.remainingSeconds <= 0f);
        GameEvents.RaiseBuffChanged();
    }
}
