using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HUDController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text playerNameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text currencyText;
    [SerializeField] private Text energyText;
    [SerializeField] private Text stateHintText;
    [SerializeField] private Text toastText;
    [SerializeField] private Button profileButton;

    [Header("Hints")]
    [SerializeField] private string mazeHint = "Maze: explore and collect ingredients.";
    [SerializeField] private string shopHint = "Shop: craft, display, and sell food.";
    [SerializeField] private string menuHint = "Main Menu";
    [SerializeField] private string levelFormat = "Lv.{0}";
    [SerializeField] private string currencyFormat = "{0:0}";
    [SerializeField] private string energyFormat = "{0}/{1}";
    [SerializeField] private float toastDuration = 2f;

    private Coroutine toastRoutine;

    private void Awake()
    {
        AutoBindMissingReferences();
    }

    public void Bind(Text currency, Text energy, Text stateHint, Text toast)
    {
        currencyText = currency;
        energyText = energy;
        stateHintText = stateHint;
        toastText = toast;
        if (toastText != null)
            toastText.gameObject.SetActive(false);
        RefreshNow();
    }

    private void OnEnable()
    {
        GameEvents.OnCurrencyChanged += UpdateCurrency;
        GameEvents.OnEnergyChanged += UpdateEnergy;
        GameEvents.OnGameStateChanged += OnGameStateChanged;
        GameEvents.OnInventoryChanged += OnInventoryChanged;
        GameEvents.OnMazeRunEnded += OnMazeRunEnded;
        if (profileButton != null)
            profileButton.onClick.AddListener(OpenProfileDetail);

        if (GameManager.Instance != null)
            RefreshNow();
    }

    private void RefreshNow()
    {
        if (GameManager.Instance == null || GameManager.Instance.gameDatabase?.GetPlayerData()?.profile == null)
            return;

        PlayerProfile profile = GameManager.Instance.gameDatabase.GetPlayerData().profile;
        UpdateProfile(profile);
        UpdateCurrency(profile.currency);
        UpdateEnergy(profile.energy, profile.maxEnergy);
        UpdateStateHint(GameManager.Instance.CurrentState);
    }

    private void OnDisable()
    {
        GameEvents.OnCurrencyChanged -= UpdateCurrency;
        GameEvents.OnEnergyChanged -= UpdateEnergy;
        GameEvents.OnGameStateChanged -= OnGameStateChanged;
        GameEvents.OnInventoryChanged -= OnInventoryChanged;
        GameEvents.OnMazeRunEnded -= OnMazeRunEnded;
        if (profileButton != null)
            profileButton.onClick.RemoveListener(OpenProfileDetail);
    }

    private void UpdateProfile(PlayerProfile profile)
    {
        if (profile == null)
            return;

        string displayName = string.IsNullOrEmpty(profile.playerDisplayName)
            ? profile.playerId
            : profile.playerDisplayName;

        UIHelper.SetText(playerNameText, displayName);
        UIHelper.SetText(levelText, string.Format(levelFormat, Mathf.Max(1, profile.level)));
    }

    private void UpdateCurrency(float value)
    {
        UIHelper.SetText(currencyText, string.Format(currencyFormat, value));
    }

    private void UpdateEnergy(int current, int max)
    {
        UIHelper.SetText(energyText, string.Format(energyFormat, current, max));
    }

    private void OnGameStateChanged(GameState from, GameState to)
    {
        UpdateStateHint(to);
    }

    private void UpdateStateHint(GameState state)
    {
        if (stateHintText == null)
            return;

        switch (state)
        {
            case GameState.InMaze:
                UIHelper.SetText(stateHintText, mazeHint);
                break;
            case GameState.InShop:
                UIHelper.SetText(stateHintText, shopHint);
                break;
            default:
                UIHelper.SetText(stateHintText, menuHint);
                break;
        }
    }

    private void OpenProfileDetail()
    {
        MainMenuUIController mainMenu = FindObjectOfType<MainMenuUIController>(true);
        if (mainMenu != null)
            mainMenu.OpenProfileDetail();
    }

    private void OnInventoryChanged()
    {
        ShowToast("Inventory updated.");
    }

    private void OnMazeRunEnded(MazeRunResult result)
    {
        if (result != null)
            ShowToast($"Maze ended: {result.reason}");
    }

    private void ShowToast(string message)
    {
        if (toastText == null)
            return;

        if (toastRoutine != null)
            StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastText.gameObject.SetActive(true);
        UIHelper.SetText(toastText, message);
        yield return new WaitForSeconds(Mathf.Max(0.1f, toastDuration));
        toastText.gameObject.SetActive(false);
        toastRoutine = null;
    }

    private void AutoBindMissingReferences()
    {
        if (playerNameText == null)
            playerNameText = UIHelper.FindText(this, "Player Name");
        if (levelText == null)
            levelText = UIHelper.FindText(this, "Level Text");
        if (currencyText == null)
            currencyText = UIHelper.FindText(this, "Gold Text");
        if (energyText == null)
            energyText = UIHelper.FindText(this, "Exp Text");
        if (stateHintText == null)
            stateHintText = UIHelper.FindText(this, "State Hint Text");
        if (toastText == null)
            toastText = UIHelper.FindText(this, "Toast Text");
        if (profileButton == null)
            profileButton = UIHelper.FindButton(this, "Profile Card");
    }

}
