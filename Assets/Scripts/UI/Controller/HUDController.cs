using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class HUDController : Controller {
    //初始化
    public override void OnOpenView() {
        UpdateHUD();
    }
    //ui刷新
    public override void OnViewRefresh() {
        UpdateHUD();
    }

    private void UpdateHUD() {
        var _model = model as HUDModel;
        PlayerProfile profile = _model.GetPlayerProfile();
        UpdateCurrency(profile.currency);
    }
    //绑定事件
    protected override void BindEvents() {
        BindEvent<int>(EventType.CurrencyChanged, UpdateCurrency);
        BindEvent<DataBag>(EventType.EnergyChanged, UpdateEnergy);

        var _view = view as HUDView;
        Button _profileButton = _view.profileButton;
        BindClickEvent(_profileButton, OpenProfileDetail);
    }

    private void OpenProfileDetail() {
        // _ = UIManager.GotoView("ProfileDetail");
    }

    private void UpdateCurrency(int amount) {
        var _model = model as HUDModel;
        _model.playerCurrency = amount;
        var _view = view as HUDView;
        _view.UpdateCurrency(amount);
    }

    private void UpdateEnergy(DataBag data) {

    }

    public HUDController(Model model, View view) : base(model, view) { }
}
