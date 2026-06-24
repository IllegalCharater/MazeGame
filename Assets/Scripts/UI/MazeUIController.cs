using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MazeUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text lootText;
    [SerializeField] private Text messageText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultTitleText;
    [SerializeField] private Text resultBodyText;
    [SerializeField] private Button evacuateButton;
    [SerializeField] private Button returnToShopButton;

    [Header("Formats")]
    [SerializeField] private string emptyLootText = "Collected: none";

    private void Awake()
    {
        AutoBindMissingReferences();
        HideResult();
    }

    private void OnEnable()
    {
        GameEvents.OnMazeRunChanged += RefreshNow;
        GameEvents.OnMazeRunEnded += ShowResult;
        UIHelper.AddClick(evacuateButton, EvacuateRun);
        UIHelper.AddClick(returnToShopButton, ReturnToShop);
        RefreshNow();
    }

    private void OnDisable()
    {
        GameEvents.OnMazeRunChanged -= RefreshNow;
        GameEvents.OnMazeRunEnded -= ShowResult;
        UIHelper.RemoveClick(evacuateButton, EvacuateRun);
        UIHelper.RemoveClick(returnToShopButton, ReturnToShop);
    }

    public void Bind(Text loot, Text message, GameObject resultPanel, Text resultTitle, Text resultBody, Button evacuate, Button returnToShop)
    {
        lootText = loot;
        messageText = message;
        this.resultPanel = resultPanel;
        resultTitleText = resultTitle;
        resultBodyText = resultBody;
        evacuateButton = evacuate;
        returnToShopButton = returnToShop;
        RefreshNow();
    }

    public void BeginRun()
    {
        if (GameManager.Instance?.Services?.MazeRun == null)
            return;

        bool success = GameManager.Instance.Services.MazeRun.BeginRun();
        if (success)
            HideResult();
        else
            ShowMessage("Not enough energy to enter the maze.");
    }

    public void EvacuateRun()
    {
        if (GameManager.Instance?.Services?.MazeRun == null)
            return;

        MazeRunService mazeRun = GameManager.Instance.Services.MazeRun;
        if (mazeRun.State == MazeRunState.Running)
            mazeRun.EvacuateRun();
    }

    public void ReturnToShop()
    {
        if (SceneFlow.Instance != null)
            SceneFlow.Instance.GoToShop();
    }

    public void RefreshNow()
    {
        MazeRunService mazeRun = GameManager.Instance?.Services?.MazeRun;

        if (mazeRun == null)
        {
            UIHelper.SetText(lootText, emptyLootText);
            return;
        }

        UIHelper.SetText(lootText, BuildLootText(mazeRun.CurrentResult));
        if (evacuateButton != null)
            evacuateButton.interactable = mazeRun.State == MazeRunState.Running;
        if (mazeRun.State == MazeRunState.Running)
            HideResult();
    }

    public void ShowMessage(string message)
    {
        UIHelper.SetText(messageText, message);
    }

    public void ShowResult(MazeRunResult result)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        string title = result != null ? $"Maze {result.reason}" : "Maze Ended";
        UIHelper.SetText(resultTitleText, title);
        UIHelper.SetText(resultBodyText, BuildResultBody(result));
        ShowMessage("Run ended. Return to shop to continue.");
        RefreshNow();
    }

    private void HideResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private string BuildLootText(MazeRunResult result)
    {
        if (result == null || result.collectedItems == null || result.collectedItems.Count == 0)
            return emptyLootText;

        StringBuilder sb = new StringBuilder();
        sb.Append("Collected: ");
        AppendDictionaryInline(sb, result.collectedItems);
        return sb.ToString();
    }

    private string BuildResultBody(MazeRunResult result)
    {
        if (result == null)
            return "No result data.";

        StringBuilder sb = new StringBuilder();
        sb.Append("Collected: ");
        AppendDictionaryInline(sb, result.collectedItems);
        sb.AppendLine();
        sb.Append("Rewards: ");
        AppendDictionaryInline(sb, result.rewardItems);
        return sb.ToString();
    }

    private void AppendDictionaryInline(StringBuilder sb, IReadOnlyDictionary<string, int> values)
    {
        if (values == null || values.Count == 0)
        {
            sb.Append("none");
            return;
        }

        bool first = true;
        foreach (KeyValuePair<string, int> kv in values)
        {
            if (!first)
                sb.Append(", ");
            sb.Append(kv.Key);
            sb.Append(" x");
            sb.Append(kv.Value);
            first = false;
        }
    }

    private void AutoBindMissingReferences()
    {
        if (lootText == null)
            lootText = UIHelper.FindText(this, "LootText");
        if (messageText == null)
            messageText = UIHelper.FindText(this, "MessageText");
        if (resultPanel == null)
            resultPanel = UIHelper.FindDeep(this, "ResultPanel");
        if (resultTitleText == null)
            resultTitleText = UIHelper.FindText(this, "ResultTitleText");
        if (resultBodyText == null)
            resultBodyText = UIHelper.FindText(this, "ResultBodyText");
        if (evacuateButton == null)
            evacuateButton = UIHelper.FindButton(this, "EvacuateButton");
        if (returnToShopButton == null)
            returnToShopButton = UIHelper.FindButton(this, "ReturnToShopButton");
    }

}
