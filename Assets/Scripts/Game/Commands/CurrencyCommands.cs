using System;

public sealed class AddCurrencyCommand : ICommand
{
    public string playerId;
    public int amount;

    public AddCurrencyCommand(string playerId, int amount)
    {
        this.playerId = playerId;
        this.amount = amount;
    }
}

public sealed class SpendCurrencyCommand : ICommand
{
    public string playerId;
    public float amount;

    public SpendCurrencyCommand(string playerId, float amount)
    {
        this.playerId = playerId;
        this.amount = amount;
    }
}

public sealed class AddCurrencyCommandHandler : ICommandHandler<AddCurrencyCommand>
{
    private readonly GameDatabase database;
    private readonly PlayerDatabase fallbackPlayer;

    public AddCurrencyCommandHandler(GameDatabase database, PlayerDatabase fallbackPlayer)
    {
        this.database = database;
        this.fallbackPlayer = fallbackPlayer;
    }

    public CommandResult Handle(AddCurrencyCommand command)
    {
        if (command.amount < 0)
            return CommandResult.Failed("Currency amount must be non-negative.");
        if (!TryGetProfile(command.playerId, out PlayerProfile profile))
            return CommandResult.Failed("Player profile not found.");

        long next = (long)profile.currency + command.amount;
        if (next > int.MaxValue)
            return CommandResult.Failed("Currency exceeds int max value.");

        profile.currency = (int)next;
        GameEvents.RaiseCurrencyChanged(profile.currency);
        return CommandResult.Succeeded("Currency added.", profile.currency);
    }

    private bool TryGetProfile(string playerId, out PlayerProfile profile)
    {
        profile = null;
        PlayerDatabase player = ResolvePlayer(playerId);
        if (player?.profile == null)
            return false;

        profile = player.profile;
        return true;
    }

    private PlayerDatabase ResolvePlayer(string playerId)
    {
        if (!string.IsNullOrEmpty(playerId))
        {
            if (database != null && database.playerDatabases.TryGetValue(playerId, out PlayerDatabase player))
                return player;
            return null;
        }

        if (fallbackPlayer != null)
            return fallbackPlayer;

        if (database == null)
            return null;

        foreach (PlayerDatabase candidate in database.playerDatabases.Values)
            return candidate;

        return null;
    }
}

public sealed class SpendCurrencyCommandHandler : ICommandHandler<SpendCurrencyCommand>
{
    private readonly GameDatabase database;
    private readonly PlayerDatabase fallbackPlayer;

    public SpendCurrencyCommandHandler(GameDatabase database, PlayerDatabase fallbackPlayer)
    {
        this.database = database;
        this.fallbackPlayer = fallbackPlayer;
    }

    public CommandResult Handle(SpendCurrencyCommand command)
    {
        if (command.amount < 0f)
            return CommandResult.Failed("Currency amount must be non-negative.");
        if (!TryGetProfile(command.playerId, out PlayerProfile profile))
            return CommandResult.Failed("Player profile not found.");
        if (profile.currency < command.amount)
            return CommandResult.Failed("Not enough currency.");

        profile.currency = Math.Max(0, (int)Math.Floor(profile.currency - command.amount));
        GameEvents.RaiseCurrencyChanged(profile.currency);
        return CommandResult.Succeeded("Currency spent.", profile.currency);
    }

    private bool TryGetProfile(string playerId, out PlayerProfile profile)
    {
        profile = null;
        PlayerDatabase player = ResolvePlayer(playerId);
        if (player?.profile == null)
            return false;

        profile = player.profile;
        return true;
    }

    private PlayerDatabase ResolvePlayer(string playerId)
    {
        if (!string.IsNullOrEmpty(playerId))
        {
            if (database != null && database.playerDatabases.TryGetValue(playerId, out PlayerDatabase player))
                return player;
            return null;
        }

        if (fallbackPlayer != null)
            return fallbackPlayer;

        if (database == null)
            return null;

        foreach (PlayerDatabase candidate in database.playerDatabases.Values)
            return candidate;

        return null;
    }
}
