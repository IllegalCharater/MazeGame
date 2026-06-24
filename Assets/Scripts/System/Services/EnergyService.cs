using UnityEngine;

public sealed class EnergyService : IGameService
{
    private PlayerDatabase player;

    public int Current => player?.profile != null ? player.profile.energy : 0;
    public int Max => player?.profile != null ? player.profile.maxEnergy : 0;
    public bool CanAct => Current > 0;

    public void Initialize(GameDatabase database, PlayerDatabase player)
    {
        this.player = player;
        RaiseChanged();
    }

    public bool TryConsume(int amount)
    {
        if (player?.profile == null || amount <= 0)
            return false;
        if (!HasEnough(amount))
            return false;

        player.profile.energy -= amount;
        RaiseChanged();
        return true;
    }

    public bool HasEnough(int amount)
    {
        if (player?.profile == null || amount < 0)
            return false;

        return player.profile.energy >= amount;
    }

    public bool Restore(int amount)
    {
        if (player?.profile == null || amount <= 0)
            return false;

        player.profile.energy = Mathf.Clamp(player.profile.energy + amount, 0, player.profile.maxEnergy);
        RaiseChanged();
        return true;
    }

    public void SetEnergy(int value)
    {
        if (player?.profile == null)
            return;

        player.profile.energy = Mathf.Clamp(value, 0, player.profile.maxEnergy);
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        if (player?.profile != null)
            GameEvents.RaiseEnergyChanged(player.profile.energy, player.profile.maxEnergy);
    }
}
