using System;
using System.Threading.Tasks;

public sealed class ChangeCurrency : ICommand {
    // payload：增加金币数量（amount）
    public Task<CommandResult> Handle(DataBag payload = null) {
        // string playerId = payload.Get("playerId", "");
        int amount = payload.Get("amount", -1);
        if (amount < 0)
            return Task.FromResult(CommandResult.Failed("Currency changed failed"));

        //刷新数据
        DataBag _profileData = new DataBag();
        _profileData.Set("currency", amount);
        GameDatabase.Instance.GetPlayerData().Profile.UpdateData(_profileData);

        //派发事件
        GameContext.Instance.DispatchEvent("OnCurrencyChanged");
        return Task.FromResult(CommandResult.Succeeded("Currency Changed.", amount));
    }
}
