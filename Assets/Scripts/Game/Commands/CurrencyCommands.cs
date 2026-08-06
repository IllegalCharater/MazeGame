using System;
using System.Threading.Tasks;

public sealed class ChangeCurrency : ICommand {
    public string Name => "CurrencyChangedCommand";

    // payload：改变金币数量（amount）
    public Task<CommandResult> Handle(DataBag payload = null) {
        // string playerId = payload.Get("playerId", "");
        string changeType = payload.Get<string>("type", null);
        int amount = payload.Get("amount", 0);
        var profile = GameDatabase.Instance.GetPlayerData().Profile;

        int _currency = profile.currency;
        if (changeType == "add") {
            _currency += amount;
        }
        else if (changeType == "sub") {
            _currency -= amount;

            if (_currency < 0) {
                return Task.FromResult(CommandResult.Failed("currency is not enough"));
            }
        }
        else {
            _currency = amount;
        }

        //刷新数据
        DataBag _profileData = new DataBag();
        _profileData.Set("currency", _currency);
        profile.UpdateData(_profileData);

        //派发事件
        GameContext.Instance.DispatchEvent("CurrencyChanged", new object[] { _currency });
        return Task.FromResult(CommandResult.Succeeded("Currency Changed.", _currency));
    }
}
