using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public sealed class HUDController : Controller {
    public override void OnOpenView() {

    }
    public override void BindEvents() {
        // GameEvents.OnCurrencyChanged += UpdateCurrency;
        // GameEvents.OnEnergyChanged += UpdateEnergy;
        // GameEvents.OnGameStateChanged += OnGameStateChanged;
        // GameEvents.OnInventoryChanged += OnInventoryChanged;
        // BindClickEvent(profileButton, OpenProfileDetail);

        BindEvent("OnCurrencyChanged", UpdateCurrency);
        BindEvent("OnEnergyChanged", UpdateEnergy);
        BindEvent("OnGameStateChanged", OnGameStateChanged);
        BindEvent("OnInventoryChanged", OnInventoryChanged);

        BindClickEvent("Profile Button", OpenProfileDetail);
    }

    private void RefreshNow() {
        PlayerProfile profile = GameManager.Instance.gameDatabase.GetPlayerData().Profile;
        var bag = new DataBag();
        view.UpdateData(bag.FromObject(profile));
    }

    private void OpenProfileDetail() {
        UIManager.GotoView("ProfileDetailUI");
    }

    public void UpdateCurrency() {

    }

    public void UpdateEnergy() {

    }

    public void OnGameStateChanged() {

    }

    private void OnInventoryChanged() {
        // ShowToast("Inventory updated.");
    }

    public HUDController(Model model, View view) : base(model, view) { }
}
