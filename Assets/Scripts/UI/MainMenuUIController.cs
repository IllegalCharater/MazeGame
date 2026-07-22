using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuUIController : BaseUIController
{
    private Button navRoleButton;
    private Button mazeEntryButton;
    private Button adventureButton;
    private Button kitchenEntryButton;
    private Button shopEntryButton;
    private Button navShopButton;
    private Button settingsButton;
    private Button mailButton;
    private Button gardenButton;
    private Button boardButton;
    private Button navBagButton;
    private Button navCollectionButton;

    public override void BindUI()
    {
        CacheReferences();
    }

    public override void EventMapper()
    {
        WireButtons();
    }

    public void OpenProfileDetail()
    {
        UIManager.GotoView("ProfileDetailUI");
    }

    public void HideProfileDetail()
    {
        UIManager.CloseView("ProfileDetailUI");
    }

    private void CacheReferences()
    {
        navRoleButton = FindButton("Nav Role");
        mazeEntryButton = FindButton("Maze Entry");
        adventureButton = FindButton("Nav Adventure");
        kitchenEntryButton = FindButton("Kitchen Entry");
        shopEntryButton = FindButton("Shop Entry");
        navShopButton = FindButton("Nav Shop");
        settingsButton = FindButton("Settings Button");
        mailButton = FindButton("Mail Button");
        gardenButton = FindButton("Garden Entry");
        boardButton = FindButton("Board Entry");
        navBagButton = FindButton("Nav Bag");
        navCollectionButton = FindButton("Nav Collection");
    }

    private void WireButtons()
    {
        BindClickEvent(navRoleButton, OpenProfileDetail);

        BindClickEvent(mazeEntryButton, GoToMaze);
        BindClickEvent(adventureButton, GoToMaze);
        BindClickEvent(kitchenEntryButton, GoToShop);
        BindClickEvent(shopEntryButton, GoToShop);
        BindClickEvent(navShopButton, GoToShop);

        BindClickEvent(settingsButton, () => ShowLogHint("Settings UI is not connected."));
        BindClickEvent(mailButton, () => ShowLogHint("Mail UI is not connected."));
        BindClickEvent(gardenButton, () => ShowLogHint("Garden gameplay is not connected."));
        BindClickEvent(boardButton, () => ShowLogHint("Notice board is not connected."));
        BindClickEvent(navBagButton, () => ShowLogHint("Backpack detail UI is not connected."));
        BindClickEvent(navCollectionButton, () => ShowLogHint("Collection detail UI is not connected."));
    }

    private void GoToMaze()
    {
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.GoToMaze();
        Debug.Log("GoToMaze");
    }

    private void GoToShop()
    {
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.GoToShop();
        Debug.Log("gotoshop");
    }

    private void ShowLogHint(string message)
    {
        Debug.Log($"[MainMenuUI] {message}");
    }
}
