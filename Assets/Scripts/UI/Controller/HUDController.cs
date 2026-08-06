using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public sealed class HUDController : Controller {
    //初始化
    public override void OnOpenView() {

    }
    //ui刷新
    public override void OnViewRefresh() {

    }
    //绑定事件
    public override void BindEvents() {
        BindEvent<int>("CurrencyChanged", UpdateCurrency);
        // BindEvent("EnergyChanged", UpdateEnergy);

        var _view = view as HUDView;
        Button _profileButton = _view.profileButton;
        BindClickEvent(_profileButton, OpenProfileDetail);
    }

    private void OpenProfileDetail() {
        _ = UIManager.GotoView("ProfileDetailUI");
    }

    private void UpdateCurrency(int amount) {
        var _model = model as HUDModel;
        _model.playerCurrency = amount;
        var _view = view as HUDView;
        _view.UpdateCurrency(amount);
    }

    private void UpdateEnergy() {

    }

    public HUDController(Model model, View view) : base(model, view) { }
}
