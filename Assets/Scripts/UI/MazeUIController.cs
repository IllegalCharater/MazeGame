using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class MazeUIController : BaseUIController
{
    public override bool hasInputBlocker => true;

    private const string PuzzleTypeItemSocket = "item_socket";
    private const string PuzzleTypeNumericInput = "numeric_input";
    private const string PuzzleTypeFloorChoice = "floor_choice";
    private const string PuzzleTypeBlockWords = "block_words";
    private const string TrapTypeButtonMash = "button_mash";
    private const string TrapTypeRunEscape = "run_escape";

    private Text lootText;
    private Text messageText;
    private Text energyText;
    private Text fragmentsText;
    private Text nodeDetailText;
    private RectTransform mazeMapRoot;
    private Image mazeMapBackground;
    private GameObject resultPanel;
    private Text resultTitleText;
    private Text resultBodyText;
    private Button evacuateButton;
    private Button perfectExitButton;
    private Button returnToShopButton;
    private Transform nodeButtonRoot;
    private Button[] nodeButtons;
    private GameObject nodeActionPanel;
    private Text nodeActionTitleText;
    private Text nodeActionBodyText;
    private Button nodeActionPrimaryButton;
    private Button nodeActionSecondaryButton;
    private Button nodeActionCloseButton;
    private Button rewardGameplayButton;
    private Button switchGameplayButton;
    private Button puzzleGameplayButton;
    private Button trapGameplayButton;
    private Button evacuateGameplayButton;
    private Button exitGameplayButton;
    private Button foodGameplayButton;

    private GameObject puzzlePanel;
    private Text puzzleTitleText;
    private Text puzzleBodyText;
    private Text puzzleInputText;
    private Transform puzzleOptionsRoot;
    private Button puzzleConfirmButton;
    private Button puzzleCloseButton;

    private GameObject trapPanel;
    private Text trapTitleText;
    private Text trapBodyText;
    private Button trapActionButton;
    private Button trapCloseButton;

    private GameObject exitPanel;
    private Text exitTitleText;
    private Text exitBodyText;
    private Button exitNormalButton;
    private Button exitPerfectButton;
    private Button exitCloseButton;

    private GameObject foodPanel;
    private Text foodTitleText;
    private Text foodBodyText;
    private Transform foodOptionsRoot;
    private Button foodCloseButton;

    private MazeUIRuntimeDriver runtimeDriver;

    private string emptyLootText = "Collected: none";
    private Color nodeNormalColor = new Color(0.1f, 0.65f, 1f, 0.55f);
    private Color nodeLockedColor = new Color(0.18f, 0.2f, 0.26f, 0.35f);
    private Color nodeVisitedColor = new Color(0.45f, 0.45f, 0.45f, 0.35f);
    private Color nodePendingColor = new Color(1f, 0.65f, 0.18f, 0.72f);
    private Vector2 nodeButtonSize = new Vector2(156f, 64f);
    private Vector2 gameplayButtonMinSize = new Vector2(156f, 54f);

    private readonly Dictionary<Button, UnityAction> nodeButtonActions = new Dictionary<Button, UnityAction>();
    private readonly Dictionary<Button, UnityAction> actionPanelActions = new Dictionary<Button, UnityAction>();
    private readonly List<Button> dynamicPuzzleButtons = new List<Button>();
    private readonly List<Button> dynamicFoodButtons = new List<Button>();
    private readonly List<string> puzzleSelectedOptions = new List<string>();

    private string currentPuzzleId;
    private string currentPuzzleInput = string.Empty;
    private string currentTrapId;
    private float trapTimer;
    private float trapProgress;
    private float trapGoal;
    private bool trapActive;

    public override void BindUI()
    {
        AutoBindMissingReferences();
        EnsureDynamicControls();
        ApplyStaticMazeLayout();
        HideResult();
        HideNodeActionPanel();
        HidePuzzlePanel();
        HideTrapPanel();
        HideExitPanel();
        HideFoodPanel();
    }

    public override void EventMapper()
    {
        GameEvents.OnMazeRunChanged += RefreshNow;
        GameEvents.OnMazeRunEnded += ShowResult;
        BindClickEvent(evacuateButton, EvacuateRun);
        BindClickEvent(perfectExitButton, ShowExitPanel);
        BindClickEvent(returnToShopButton, ReturnToShop);
        BindClickEvent(nodeActionCloseButton, HideNodeActionPanel);
        BindFunctionButtons();
        BindMazeNodeButtons();
        RefreshNow();
    }

    public override void OnOpen()
    {
        RefreshNow();
    }

    public override void Dismiss()
    {
        GameEvents.OnMazeRunChanged -= RefreshNow;
        GameEvents.OnMazeRunEnded -= ShowResult;
        UnbindMazeNodeButtons();
        ClearActionPanelEvents();
        base.Dismiss();
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

    public void Tick(float deltaSeconds)
    {
        if (trapActive && trapPanel != null && trapPanel.activeSelf)
        {
            trapTimer -= Mathf.Max(0f, deltaSeconds);
            if (Input.GetKeyDown(KeyCode.Space))
                AdvanceTrapProgress();

            if (trapProgress >= trapGoal)
                ResolveTrapOutcome(true);
            else if (trapTimer <= 0f)
                ResolveTrapOutcome(false);
            else
                RefreshTrapPanelText();
        }
    }

    public void BeginRun()
    {
        MazeRunService mazeRun = GameManager.Instance?.Services?.MazeRun;
        if (mazeRun == null)
            return;

        bool success = mazeRun.BeginRun();
        if (success)
        {
            HideResult();
            ShowMessage("Maze run started.");
        }
        else
            ShowMessage("Not enough energy to enter the maze.");
    }

    public void EvacuateRun()
    {
        MazeRunService mazeRun = GameManager.Instance?.Services?.MazeRun;
        if (mazeRun == null || mazeRun.State != MazeRunState.Running)
            return;

        ShowExitChoice(false);
    }

    public void InteractNode(string nodeId)
    {
        MazeRunService mazeRun = GameManager.Instance?.Services?.MazeRun;
        if (mazeRun == null)
            return;

        MazeNodeData node = GameDatabase.Instance?.Get<MazeNodeData>("maze_nodes", nodeId);
        if (node == null)
        {
            ShowMessage($"Cannot find {nodeId}.");
            return;
        }

        if (!mazeRun.CanInteractNode(nodeId))
        {
            if (HasPendingNodeAction(node, mazeRun))
            {
                ShowNodeActionPanel(node);
                return;
            }

            ShowMessage($"Cannot visit {GetNodeDisplayName(node)} yet.");
            RefreshNow();
            return;
        }

        bool success = mazeRun.InteractNode(nodeId);
        if (success)
        {
            ShowMessage(!string.IsNullOrEmpty(node.note) ? node.note : $"Visited {GetNodeDisplayName(node)}.");
            if (HasNodeAction(node))
                ShowNodeActionPanel(node);
        }
        else
        {
            ShowMessage($"Cannot visit {GetNodeDisplayName(node)}.");
            RefreshNow();
        }
    }

    public void SubmitPuzzleAnswer(string puzzleId, string answer)
    {
        MazeRunService mazeRun = GameManager.Instance?.Services?.MazeRun;
        if (mazeRun == null)
            return;

        MazePuzzleData puzzle = GameDatabase.Instance?.Get<MazePuzzleData>("maze_puzzles", puzzleId);
        bool success = mazeRun.SubmitPuzzleAnswer(puzzleId, answer);
        ShowMessage(success ? $"Puzzle solved: {ResolvePuzzleLabel(puzzle)}" : $"Puzzle failed: {ResolvePuzzleLabel(puzzle)}");
        if (success)
        {
            HidePuzzlePanel();
            HideNodeActionPanel();
        }
        RefreshNow();
    }

    public void CompletePuzzle(string puzzleId)
    {
        if (GameManager.Instance?.Services?.MazeRun == null)
            return;

        bool success = GameManager.Instance.Services.MazeRun.CompletePuzzle(puzzleId);
        ShowMessage(success ? $"Puzzle solved: {puzzleId}" : $"Puzzle already solved or unavailable: {puzzleId}");
        RefreshNow();
    }

    public void ResolveTrap(string trapId, bool success)
    {
        if (GameManager.Instance?.Services?.MazeRun == null)
            return;

        bool survived = GameManager.Instance.Services.MazeRun.ResolveTrap(trapId, success);
        ShowMessage(survived ? $"Trap cleared: {trapId}" : $"Trap resolved: {trapId}");
        RefreshNow();
    }

    public void CompletePerfectRun()
    {
        MazeRunService mazeRun = GameManager.Instance?.Services?.MazeRun;
        if (mazeRun == null)
            return;

        bool composed = mazeRun.TryComposeExitPuzzle();
        MazeRunResult result = composed ? mazeRun.CompletePerfectRun() : mazeRun.CompleteRun();
        ShowResult(result);
    }

    public void ReturnToShop()
    {
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.GoToShop();
        else
            Debug.Log("Return to shop");
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
        UIHelper.SetText(energyText, BuildEnergyText());
        UIHelper.SetText(fragmentsText, BuildFragmentsText(mazeRun));
        UIHelper.SetText(nodeDetailText, BuildNodeDetailText(mazeRun.CurrentResult));
        EnsureNodeButtonsBound();
        if (evacuateButton != null)
            evacuateButton.interactable = mazeRun.State == MazeRunState.Running;
        if (perfectExitButton != null)
            perfectExitButton.interactable = mazeRun.State == MazeRunState.Running;
        if (foodGameplayButton != null)
            foodGameplayButton.interactable = mazeRun.State == MazeRunState.Running;
        RefreshNodeButtonStates(mazeRun);
        RefreshFunctionButtonStates(mazeRun);
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
        HidePuzzlePanel();
        HideTrapPanel();
        HideExitPanel();
        HideFoodPanel();
        RefreshNow();
    }

    private void AutoBindMissingReferences()
    {
        if (lootText == null)
            lootText = FindPrefabComponent<Text>("LootText");
        if (messageText == null)
            messageText = FindPrefabComponent<Text>("MessageText");
        if (energyText == null)
            energyText = FindPrefabComponent<Text>("EnergyText");
        if (fragmentsText == null)
            fragmentsText = FindPrefabComponent<Text>("FragmentsText");
        if (nodeDetailText == null)
            nodeDetailText = FindPrefabComponent<Text>("NodeDetailText");
        if (resultPanel == null)
            resultPanel = FindPrefabObject("ResultPanel");
        if (resultTitleText == null)
            resultTitleText = FindPrefabComponent<Text>("ResultTitleText");
        if (resultBodyText == null)
            resultBodyText = FindPrefabComponent<Text>("ResultBodyText");
        if (evacuateButton == null)
            evacuateButton = FindPrefabComponent<Button>("EvacuateButton");
        if (perfectExitButton == null)
            perfectExitButton = FindPrefabComponent<Button>("PerfectExitButton");
        if (returnToShopButton == null)
            returnToShopButton = FindPrefabComponent<Button>("ReturnToShopButton");
        if (nodeActionPanel == null)
            nodeActionPanel = FindPrefabObject("NodeActionPanel");
        if (nodeActionTitleText == null)
            nodeActionTitleText = FindPrefabComponent<Text>("NodeActionTitleText");
        if (nodeActionBodyText == null)
            nodeActionBodyText = FindPrefabComponent<Text>("NodeActionBodyText");
        if (nodeActionPrimaryButton == null)
            nodeActionPrimaryButton = FindPrefabComponent<Button>("NodeActionPrimaryButton");
        if (nodeActionSecondaryButton == null)
            nodeActionSecondaryButton = FindPrefabComponent<Button>("NodeActionSecondaryButton");
        if (nodeActionCloseButton == null)
            nodeActionCloseButton = FindPrefabComponent<Button>("NodeActionCloseButton");
        if (rewardGameplayButton == null)
            rewardGameplayButton = FindPrefabComponent<Button>("RewardGameplayButton");
        if (switchGameplayButton == null)
            switchGameplayButton = FindPrefabComponent<Button>("SwitchGameplayButton");
        if (puzzleGameplayButton == null)
            puzzleGameplayButton = FindPrefabComponent<Button>("PuzzleGameplayButton");
        if (trapGameplayButton == null)
            trapGameplayButton = FindPrefabComponent<Button>("TrapGameplayButton");
        if (evacuateGameplayButton == null)
            evacuateGameplayButton = FindPrefabComponent<Button>("EvacuateGameplayButton");
        if (exitGameplayButton == null)
            exitGameplayButton = FindPrefabComponent<Button>("ExitGameplayButton");
        if (foodGameplayButton == null)
            foodGameplayButton = FindPrefabComponent<Button>("FoodGameplayButton");
        if (nodeButtonRoot == null)
        {
            GameObject nodeRoot = FindPrefabObject("MazeNodeRoot") ?? FindPrefabObject("NodeButtonRoot");
            if (nodeRoot != null)
                nodeButtonRoot = nodeRoot.transform;
        }
        if (mazeMapRoot == null)
        {
            GameObject mapRoot = FindPrefabObject("MazeMapRoot");
            if (mapRoot != null)
                mazeMapRoot = mapRoot.transform as RectTransform;
        }
        if (mazeMapBackground == null)
            mazeMapBackground = FindPrefabComponent<Image>("MazeMapBackground");
    }

    private void EnsureDynamicControls()
    {
        EnsureRuntimeDriver();
        EnsureTextFallbacks();
        EnsureFoodButton();
        EnsurePuzzlePanel();
        EnsureTrapPanel();
        EnsureExitPanel();
        EnsureFoodPanel();
    }

    private void EnsureRuntimeDriver()
    {
        if (root == null)
            return;

        runtimeDriver = root.GetComponent<MazeUIRuntimeDriver>();
        if (runtimeDriver == null)
            runtimeDriver = root.AddComponent<MazeUIRuntimeDriver>();
        runtimeDriver.Bind(this);
    }

    private void EnsureTextFallbacks()
    {
        if (root == null)
            return;

        if (messageText == null)
            messageText = CreateText(root.transform, "MessageText", "Explore the maze.", 22, TextAnchor.MiddleCenter);
        if (lootText == null)
            lootText = CreateText(root.transform, "LootText", emptyLootText, 20, TextAnchor.UpperLeft);
        if (energyText == null)
            energyText = CreateText(root.transform, "EnergyText", string.Empty, 20, TextAnchor.UpperRight);
        if (fragmentsText == null)
            fragmentsText = CreateText(root.transform, "FragmentsText", "Fragments: none", 18, TextAnchor.UpperLeft);
        if (nodeDetailText == null)
            nodeDetailText = CreateText(root.transform, "NodeDetailText", string.Empty, 18, TextAnchor.UpperLeft);
    }

    private void EnsureFoodButton()
    {
        if (foodGameplayButton != null || root == null)
            return;

        Transform parent = FindPrefabTransform("MazeFunctionBar") ?? root.transform;
        foodGameplayButton = CreateButton(parent, "FoodGameplayButton", "Food");
    }

    private void EnsurePuzzlePanel()
    {
        if (puzzlePanel != null || root == null)
            return;

        puzzlePanel = CreatePanel("PuzzlePanel", "Puzzle", out puzzleTitleText, out puzzleBodyText, out Transform content);
        puzzleInputText = CreateText(content, "PuzzleInputText", "Input: none", 20, TextAnchor.MiddleCenter);
        puzzleOptionsRoot = CreateVerticalGroup(content, "PuzzleOptionsRoot", 8f);
        puzzleConfirmButton = CreateButton(content, "PuzzleConfirmButton", "Confirm");
        puzzleCloseButton = CreateButton(content, "PuzzleCloseButton", "Close");
        BindClickEvent(puzzleConfirmButton, ConfirmPuzzleAnswer);
        BindClickEvent(puzzleCloseButton, HidePuzzlePanel);
    }

    private void EnsureTrapPanel()
    {
        if (trapPanel != null || root == null)
            return;

        trapPanel = CreatePanel("TrapPanel", "Trap", out trapTitleText, out trapBodyText, out Transform content);
        trapActionButton = CreateButton(content, "TrapActionButton", "Act");
        trapCloseButton = CreateButton(content, "TrapCloseButton", "Give Up");
        BindClickEvent(trapActionButton, AdvanceTrapProgress);
        BindClickEvent(trapCloseButton, () => ResolveTrapOutcome(false));
    }

    private void EnsureExitPanel()
    {
        if (exitPanel != null || root == null)
            return;

        exitPanel = CreatePanel("ExitPanel", "Exit", out exitTitleText, out exitBodyText, out Transform content);
        exitNormalButton = CreateButton(content, "ExitNormalButton", "Leave");
        exitPerfectButton = CreateButton(content, "ExitPerfectButton", "Compose Puzzle");
        exitCloseButton = CreateButton(content, "ExitCloseButton", "Keep Exploring");
        BindClickEvent(exitNormalButton, CompleteNormalExit);
        BindClickEvent(exitPerfectButton, CompletePerfectRun);
        BindClickEvent(exitCloseButton, HideExitPanel);
    }

    private void EnsureFoodPanel()
    {
        if (foodPanel != null || root == null)
            return;

        foodPanel = CreatePanel("FoodPanel", "Food", out foodTitleText, out foodBodyText, out Transform content);
        foodOptionsRoot = CreateVerticalGroup(content, "FoodOptionsRoot", 8f);
        foodCloseButton = CreateButton(content, "FoodCloseButton", "Close");
        BindClickEvent(foodCloseButton, HideFoodPanel);
    }

    private GameObject CreatePanel(string panelName, string title, out Text titleText, out Text bodyText, out Transform content)
    {
        GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRect = panel.transform as RectTransform;
        StretchRect(panelRect);

        Image overlay = panel.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.58f);
        overlay.raycastTarget = true;

        GameObject box = new GameObject(panelName + "Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
        box.transform.SetParent(panel.transform, false);
        RectTransform boxRect = box.transform as RectTransform;
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition = Vector2.zero;
        boxRect.sizeDelta = new Vector2(620f, 620f);

        Image boxImage = box.GetComponent<Image>();
        boxImage.color = new Color(0.08f, 0.1f, 0.12f, 0.96f);

        VerticalLayoutGroup layout = box.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        titleText = CreateText(box.transform, panelName + "TitleText", title, 30, TextAnchor.MiddleCenter);
        bodyText = CreateText(box.transform, panelName + "BodyText", string.Empty, 20, TextAnchor.UpperLeft);
        LayoutElement bodyLayout = bodyText.gameObject.AddComponent<LayoutElement>();
        bodyLayout.preferredHeight = 170f;

        content = CreateVerticalGroup(box.transform, panelName + "Content", 8f);
        return panel;
    }

    private Transform CreateVerticalGroup(Transform parent, string name, float spacing)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return go.transform;
    }

    private Button CreateButton(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.16f, 0.38f, 0.46f, 0.95f);
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.25f, 0.55f, 0.62f, 1f);
        colors.pressedColor = new Color(0.12f, 0.72f, 0.62f, 1f);
        colors.disabledColor = new Color(0.24f, 0.24f, 0.24f, 0.55f);
        button.colors = colors;

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.minHeight = 46f;
        layout.preferredHeight = 52f;
        layout.minWidth = 120f;

        Text text = CreateText(go.transform, name + "Text", label, 20, TextAnchor.MiddleCenter);
        StretchRect(text.transform as RectTransform);
        return button;
    }

    private Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = value;
        RectTransform rect = go.transform as RectTransform;
        rect.sizeDelta = new Vector2(260f, Mathf.Max(40f, size * 2f));
        return text;
    }

    private GameObject FindPrefabObject(string objectName)
    {
        Transform transform = FindPrefabTransform(objectName);
        return transform != null ? transform.gameObject : null;
    }

    private T FindPrefabComponent<T>(string objectName) where T : Component
    {
        Transform transform = FindPrefabTransform(objectName);
        return transform != null ? transform.GetComponent<T>() : null;
    }

    private Transform FindPrefabTransform(string objectName)
    {
        if (root == null || string.IsNullOrEmpty(objectName))
            return null;

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform != null && transform.name == objectName)
                return transform;
        }

        return null;
    }

    private void ApplyStaticMazeLayout()
    {
        StretchRect(root != null ? root.transform as RectTransform : null);
        StretchRect(mazeMapRoot);
        StretchRect(nodeButtonRoot as RectTransform);

        RectTransform backgroundRect = mazeMapBackground != null
            ? mazeMapBackground.transform as RectTransform
            : null;
        StretchRect(backgroundRect);

        Image mapRootImage = mazeMapRoot != null ? mazeMapRoot.GetComponent<Image>() : null;
        if (mapRootImage != null)
            mapRootImage.raycastTarget = false;

        if (mazeMapBackground != null)
        {
            mazeMapBackground.preserveAspect = false;
            mazeMapBackground.raycastTarget = false;
        }

        ApplyButtonMinimumSize(evacuateButton, new Vector2(160f, 52f));
        ApplyButtonMinimumSize(perfectExitButton, new Vector2(160f, 52f));
        ApplyButtonMinimumSize(returnToShopButton, new Vector2(170f, 52f));
        ApplyButtonMinimumSize(rewardGameplayButton, gameplayButtonMinSize);
        ApplyButtonMinimumSize(switchGameplayButton, gameplayButtonMinSize);
        ApplyButtonMinimumSize(puzzleGameplayButton, gameplayButtonMinSize);
        ApplyButtonMinimumSize(trapGameplayButton, gameplayButtonMinSize);
        ApplyButtonMinimumSize(evacuateGameplayButton, gameplayButtonMinSize);
        ApplyButtonMinimumSize(exitGameplayButton, gameplayButtonMinSize);
        ApplyButtonMinimumSize(foodGameplayButton, gameplayButtonMinSize);

        EnsureButtonRaycast(evacuateButton);
        EnsureButtonRaycast(perfectExitButton);
        EnsureButtonRaycast(returnToShopButton);
        EnsureButtonRaycast(rewardGameplayButton);
        EnsureButtonRaycast(switchGameplayButton);
        EnsureButtonRaycast(puzzleGameplayButton);
        EnsureButtonRaycast(trapGameplayButton);
        EnsureButtonRaycast(evacuateGameplayButton);
        EnsureButtonRaycast(exitGameplayButton);
        EnsureButtonRaycast(foodGameplayButton);
    }

    private static void StretchRect(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void ApplyButtonMinimumSize(Button button, Vector2 minSize)
    {
        RectTransform rect = button != null ? button.transform as RectTransform : null;
        if (rect == null)
            return;

        rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, minSize.x), Mathf.Max(rect.sizeDelta.y, minSize.y));
    }

    private static void EnsureButtonRaycast(Button button)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;
        if (image == null)
            image = button.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;
    }

    private void BindFunctionButtons()
    {
        BindClickEvent(rewardGameplayButton, () => InteractFirstGameplayNode(MazeGameplayCategory.Reward));
        BindClickEvent(switchGameplayButton, () => InteractFirstGameplayNode(MazeGameplayCategory.Switch));
        BindClickEvent(puzzleGameplayButton, () => InteractFirstGameplayNode(MazeGameplayCategory.Puzzle));
        BindClickEvent(trapGameplayButton, () => InteractFirstGameplayNode(MazeGameplayCategory.Trap));
        BindClickEvent(evacuateGameplayButton, () => InteractFirstGameplayNode(MazeGameplayCategory.Evacuate));
        BindClickEvent(exitGameplayButton, () => InteractFirstGameplayNode(MazeGameplayCategory.Exit));
        BindClickEvent(foodGameplayButton, OpenFoodPanel);
    }

    private void BindMazeNodeButtons()
    {
        UnbindMazeNodeButtons();

        List<MazeNodeData> nodes = GetConfiguredNodesSorted();
        EnsureNodeButtonArray(nodes.Count);
        if (nodeButtons == null)
            return;

        for (int i = 0; i < nodes.Count && i < nodeButtons.Length; i++)
        {
            MazeNodeData node = nodes[i];
            Button button = nodeButtons[i];
            if (button == null || node == null || string.IsNullOrEmpty(node.nodeId))
                continue;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
                label.text = GetNodeDisplayName(node);
            ApplyNodeButtonPosition(button, node);
            ApplyNodeButtonVisual(button, false, false, false);

            string capturedNodeId = node.nodeId;
            UnityAction action = () => InteractNode(capturedNodeId);
            button.onClick.AddListener(action);
            nodeButtonActions[button] = action;
        }
    }

    private void UnbindMazeNodeButtons()
    {
        foreach (KeyValuePair<Button, UnityAction> kv in nodeButtonActions)
        {
            if (kv.Key != null && kv.Value != null)
                kv.Key.onClick.RemoveListener(kv.Value);
        }
        nodeButtonActions.Clear();
    }

    private void EnsureNodeButtonsBound()
    {
        List<MazeNodeData> nodes = GetConfiguredNodesSorted();
        int currentCount = nodeButtons != null ? nodeButtons.Length : 0;
        if (nodes.Count == 0 || currentCount >= nodes.Count)
            return;

        BindMazeNodeButtons();
    }

    private void EnsureNodeButtonArray(int nodeCount)
    {
        if (nodeButtonRoot == null)
            return;

        List<Button> buttons = FindNamedNodeButtons(nodeCount);
        for (int i = 1; i <= nodeCount; i++)
        {
            if (i <= buttons.Count && buttons[i - 1] != null)
                continue;

            Button created = CreateButton(nodeButtonRoot, $"MazeNode_{i}", i.ToString());
            buttons.Add(created);
        }

        nodeButtons = buttons.ToArray();
    }

    private List<Button> FindNamedNodeButtons(int nodeCount)
    {
        List<Button> buttons = new List<Button>();
        for (int i = 1; i <= nodeCount; i++)
        {
            Button button = FindPrefabComponent<Button>($"MazeNode_{i}") ?? FindPrefabComponent<Button>($"NodeButton_{i}");
            if (button != null)
                buttons.Add(button);
        }

        return buttons;
    }

    private void ApplyNodeButtonPosition(Button button, MazeNodeData node)
    {
        if (button == null || node == null || nodeButtonRoot == null)
            return;
        if (node.mapX <= 0f && node.mapY <= 0f)
            return;

        RectTransform buttonRect = button.transform as RectTransform;
        if (buttonRect == null)
            return;

        float x = Mathf.Clamp01(node.mapX);
        float y = 1f - Mathf.Clamp01(node.mapY);
        buttonRect.anchorMin = new Vector2(x, y);
        buttonRect.anchorMax = new Vector2(x, y);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = nodeButtonSize;
    }

    private void RefreshNodeButtonStates(MazeRunService mazeRun)
    {
        if (nodeButtons == null || nodeButtons.Length == 0)
            return;

        List<MazeNodeData> nodes = GetConfiguredNodesSorted();
        for (int i = 0; i < nodeButtons.Length && i < nodes.Count; i++)
        {
            Button button = nodeButtons[i];
            MazeNodeData node = nodes[i];
            if (button == null || node == null)
                continue;

            bool visited = mazeRun.CurrentResult != null && mazeRun.CurrentResult.visitedNodeIds.Contains(node.nodeId);
            bool available = mazeRun.CanInteractNode(node.nodeId);
            bool pendingAction = HasPendingNodeAction(node, mazeRun);
            bool locked = !visited && !available;
            button.gameObject.SetActive(mazeRun.State == MazeRunState.Running);
            button.interactable = mazeRun.State == MazeRunState.Running && (available || pendingAction);
            ApplyNodeButtonVisual(button, visited && !pendingAction, locked, pendingAction);
        }
    }

    private void RefreshFunctionButtonStates(MazeRunService mazeRun)
    {
        SetGameplayButtonState(rewardGameplayButton, mazeRun, MazeGameplayCategory.Reward);
        SetGameplayButtonState(switchGameplayButton, mazeRun, MazeGameplayCategory.Switch);
        SetGameplayButtonState(puzzleGameplayButton, mazeRun, MazeGameplayCategory.Puzzle);
        SetGameplayButtonState(trapGameplayButton, mazeRun, MazeGameplayCategory.Trap);
        SetGameplayButtonState(evacuateGameplayButton, mazeRun, MazeGameplayCategory.Evacuate);
        SetGameplayButtonState(exitGameplayButton, mazeRun, MazeGameplayCategory.Exit);
    }

    private void SetGameplayButtonState(Button button, MazeRunService mazeRun, MazeGameplayCategory category)
    {
        if (button == null)
            return;

        button.interactable = mazeRun != null && mazeRun.State == MazeRunState.Running && FindFirstGameplayNode(category, mazeRun) != null;
    }

    private void InteractFirstGameplayNode(MazeGameplayCategory category)
    {
        MazeRunService mazeRun = GameManager.Instance?.Services?.MazeRun;
        MazeNodeData node = FindFirstGameplayNode(category, mazeRun);
        if (node == null)
        {
            ShowMessage($"No available {category} gameplay node.");
            return;
        }

        InteractNode(node.nodeId);
    }

    private MazeNodeData FindFirstGameplayNode(MazeGameplayCategory category, MazeRunService mazeRun)
    {
        if (mazeRun == null || mazeRun.State != MazeRunState.Running)
            return null;

        List<MazeNodeData> nodes = GetConfiguredNodesSorted();
        MazeNodeData pending = null;
        for (int i = 0; i < nodes.Count; i++)
        {
            MazeNodeData node = nodes[i];
            if (!MatchesGameplayCategory(node, category))
                continue;
            if (mazeRun.CanInteractNode(node.nodeId))
                return node;
            if (pending == null && HasPendingNodeAction(node, mazeRun))
                pending = node;
        }

        return pending;
    }

    private static bool MatchesGameplayCategory(MazeNodeData node, MazeGameplayCategory category)
    {
        if (node == null)
            return false;

        string type = node.nodeType ?? string.Empty;
        switch (category)
        {
            case MazeGameplayCategory.Reward:
                return type.Contains("reward") || type.Contains("energy");
            case MazeGameplayCategory.Switch:
                return type.Contains("switch");
            case MazeGameplayCategory.Puzzle:
                return !string.IsNullOrEmpty(node.puzzleId) || type.Contains("puzzle");
            case MazeGameplayCategory.Trap:
                return !string.IsNullOrEmpty(node.trapId) || type.Contains("trap");
            case MazeGameplayCategory.Evacuate:
                return type.Contains("evacuate");
            case MazeGameplayCategory.Exit:
                return type.Contains("exit");
            default:
                return false;
        }
    }

    private void ApplyNodeButtonVisual(Button button, bool visited, bool locked, bool pending)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;
        if (image == null)
            image = button.GetComponent<Image>();
        if (image == null)
            return;

        Color color = pending ? nodePendingColor : visited ? nodeVisitedColor : locked ? nodeLockedColor : nodeNormalColor;
        image.color = color;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = pending ? new Color(1f, 0.82f, 0.32f, 0.9f) : new Color(1f, 0.9f, 0.45f, Mathf.Max(color.a, 0.75f));
        colors.pressedColor = new Color(0.2f, 0.95f, 0.55f, 0.85f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = locked ? nodeLockedColor : nodeVisitedColor;
        button.colors = colors;
    }

    private void ShowNodeActionPanel(MazeNodeData node)
    {
        if (node == null)
            return;

        if (nodeActionPanel == null)
        {
            ShowMessage("Node action panel is missing from MazeUI.");
            return;
        }

        ClearActionPanelEvents();

        UIHelper.SetText(nodeActionTitleText, GetNodeDisplayName(node));
        UIHelper.SetText(nodeActionBodyText, BuildNodeActionBody(node));

        SetButtonVisible(nodeActionPrimaryButton, true);
        SetButtonVisible(nodeActionSecondaryButton, true);

        string type = node.nodeType ?? string.Empty;
        if (type.Contains("exit"))
        {
            ConfigureActionButton(nodeActionPrimaryButton, "Exit", ShowExitPanel);
            ConfigureActionButton(nodeActionSecondaryButton, "Keep Exploring", HideNodeActionPanel);
        }
        else if (type.Contains("evacuate"))
        {
            ConfigureActionButton(nodeActionPrimaryButton, "Evacuate", () => ShowExitChoice(false));
            ConfigureActionButton(nodeActionSecondaryButton, "Keep Exploring", HideNodeActionPanel);
        }
        else if (!string.IsNullOrEmpty(node.puzzleId))
        {
            string puzzleId = node.puzzleId;
            ConfigureActionButton(nodeActionPrimaryButton, "Start Puzzle", () => ShowPuzzlePanel(puzzleId));
            ConfigureActionButton(nodeActionSecondaryButton, "Later", HideNodeActionPanel);
        }
        else if (!string.IsNullOrEmpty(node.trapId))
        {
            string trapId = node.trapId;
            ConfigureActionButton(nodeActionPrimaryButton, "Start Challenge", () => ShowTrapPanel(trapId));
            ConfigureActionButton(nodeActionSecondaryButton, "Later", HideNodeActionPanel);
        }
        else
        {
            ConfigureActionButton(nodeActionPrimaryButton, "OK", HideNodeActionPanel);
            SetButtonVisible(nodeActionSecondaryButton, false);
        }

        nodeActionPanel.SetActive(true);
        RefreshNow();
    }

    private string BuildNodeActionBody(MazeNodeData node)
    {
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(node.note))
            sb.AppendLine(node.note);

        if (node.rewardItems != null && node.rewardItems.Count > 0)
        {
            sb.Append("Rewards: ");
            AppendDictionaryInline(sb, node.rewardItems);
            sb.AppendLine();
        }

        if (node.energyDelta != 0)
            sb.AppendLine(node.energyDelta > 0 ? $"Energy +{node.energyDelta}" : $"Energy {node.energyDelta}");

        if (!string.IsNullOrEmpty(node.puzzleId))
        {
            MazePuzzleData puzzle = GameDatabase.Instance?.Get<MazePuzzleData>("maze_puzzles", node.puzzleId);
            sb.AppendLine($"Puzzle: {ResolvePuzzleLabel(puzzle)}");
            if (!string.IsNullOrEmpty(puzzle?.hintText))
                sb.AppendLine(puzzle.hintText);
        }

        if (!string.IsNullOrEmpty(node.trapId))
        {
            MazeTrapData trap = GameDatabase.Instance?.Get<MazeTrapData>("maze_traps", node.trapId);
            sb.AppendLine($"Trap: {ResolveTrapLabel(trap)}");
            if (trap != null && trap.failEnergyCost > 0)
                sb.AppendLine($"Failed challenge costs {trap.failEnergyCost} energy.");
        }

        string type = node.nodeType ?? string.Empty;
        if (type.Contains("evacuate"))
            sb.AppendLine("Leave now and keep partial rewards.");
        else if (type.Contains("exit"))
            sb.AppendLine("Finish the run from this exit.");

        return sb.Length > 0 ? sb.ToString() : "Explore this point on the maze map.";
    }

    private bool HasPendingNodeAction(MazeNodeData node, MazeRunService mazeRun)
    {
        if (node == null || mazeRun?.CurrentResult == null || mazeRun.State != MazeRunState.Running)
            return false;
        if (!mazeRun.CurrentResult.visitedNodeIds.Contains(node.nodeId))
            return false;
        if (!string.IsNullOrEmpty(node.puzzleId) && !mazeRun.CurrentResult.completedPuzzleIds.Contains(node.puzzleId))
            return true;
        if (!string.IsNullOrEmpty(node.trapId) && !mazeRun.CurrentResult.triggeredTrapIds.Contains(node.trapId))
            return true;

        string type = node.nodeType ?? string.Empty;
        return type.Contains("exit") || type.Contains("evacuate");
    }

    private static bool HasNodeAction(MazeNodeData node)
    {
        if (node == null)
            return false;

        string type = node.nodeType ?? string.Empty;
        return !string.IsNullOrEmpty(node.puzzleId)
            || !string.IsNullOrEmpty(node.trapId)
            || type.Contains("exit")
            || type.Contains("evacuate");
    }

    private void HideNodeActionPanel()
    {
        if (nodeActionPanel != null)
            nodeActionPanel.SetActive(false);
    }

    private void ConfigureActionButton(Button button, string label, UnityAction action)
    {
        if (button == null)
            return;

        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null)
            text.text = label;
        button.onClick.AddListener(action);
        actionPanelActions[button] = action;
    }

    private void ClearActionPanelEvents()
    {
        foreach (KeyValuePair<Button, UnityAction> kv in actionPanelActions)
        {
            if (kv.Key != null && kv.Value != null)
                kv.Key.onClick.RemoveListener(kv.Value);
        }
        actionPanelActions.Clear();
    }

    private void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }

    private void ShowPuzzlePanel(string puzzleId)
    {
        MazePuzzleData puzzle = GameDatabase.Instance?.Get<MazePuzzleData>("maze_puzzles", puzzleId);
        if (puzzle == null)
        {
            ShowMessage($"Cannot find puzzle {puzzleId}.");
            return;
        }

        currentPuzzleId = puzzleId;
        currentPuzzleInput = string.Empty;
        puzzleSelectedOptions.Clear();
        UIHelper.SetText(puzzleTitleText, ResolvePuzzleLabel(puzzle));
        UIHelper.SetText(puzzleBodyText, BuildPuzzleBody(puzzle));
        UIHelper.SetText(puzzleInputText, "Input: none");
        RebuildPuzzleOptions(puzzle);
        if (puzzlePanel != null)
            puzzlePanel.SetActive(true);
    }

    private string BuildPuzzleBody(MazePuzzleData puzzle)
    {
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(puzzle.hintText))
            sb.AppendLine(puzzle.hintText);
        sb.AppendLine("Choose the correct answer to earn the room reward and a puzzle fragment.");
        if (puzzle.failEnergyCost > 0)
            sb.AppendLine($"Wrong answer costs {puzzle.failEnergyCost} energy.");
        return sb.ToString();
    }

    private void RebuildPuzzleOptions(MazePuzzleData puzzle)
    {
        DestroyChildren(puzzleOptionsRoot);
        dynamicPuzzleButtons.Clear();

        List<string> options = GetPuzzleOptions(puzzle);
        for (int i = 0; i < options.Count; i++)
        {
            string option = options[i];
            Button button = CreateButton(puzzleOptionsRoot, $"PuzzleOption_{i + 1}", ResolveOptionLabel(puzzle, option));
            button.onClick.AddListener(() => SelectPuzzleOption(puzzle, option));
            dynamicPuzzleButtons.Add(button);
        }
    }

    private List<string> GetPuzzleOptions(MazePuzzleData puzzle)
    {
        if (puzzle?.optionKeys != null && puzzle.optionKeys.Count > 0)
            return new List<string>(puzzle.optionKeys);

        string type = puzzle?.puzzleType ?? string.Empty;
        if (type.Contains(PuzzleTypeNumericInput))
            return new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "clear" };
        if (type.Contains(PuzzleTypeFloorChoice))
            return new List<string> { "profit", "fame", "power", "trust", "fear", "gold", "silence", "escape" };
        if (type.Contains(PuzzleTypeBlockWords))
            return new List<string> { "旧", "名", "渊", "剑", "信", "清", "泉" };

        return new List<string> { "correct_items", "wrong_items", "old_coin", "empty_bowl" };
    }

    private void SelectPuzzleOption(MazePuzzleData puzzle, string option)
    {
        string type = puzzle?.puzzleType ?? string.Empty;
        if (type.Contains(PuzzleTypeNumericInput))
        {
            if (option == "clear")
                currentPuzzleInput = string.Empty;
            else
                currentPuzzleInput += option;
        }
        else if (type.Contains(PuzzleTypeBlockWords))
        {
            if (puzzleSelectedOptions.Contains(option))
                puzzleSelectedOptions.Remove(option);
            else
                puzzleSelectedOptions.Add(option);

            currentPuzzleInput = JoinSelectedInOptionOrder(GetPuzzleOptions(puzzle), puzzleSelectedOptions);
        }
        else
        {
            currentPuzzleInput = option;
        }

        UIHelper.SetText(puzzleInputText, string.IsNullOrEmpty(currentPuzzleInput) ? "Input: none" : $"Input: {currentPuzzleInput}");
    }

    private string JoinSelectedInOptionOrder(List<string> options, List<string> selected)
    {
        List<string> ordered = new List<string>();
        for (int i = 0; i < options.Count; i++)
        {
            if (selected.Contains(options[i]))
                ordered.Add(options[i]);
        }

        return string.Join(",", ordered);
    }

    private void ConfirmPuzzleAnswer()
    {
        if (string.IsNullOrEmpty(currentPuzzleId))
            return;

        SubmitPuzzleAnswer(currentPuzzleId, currentPuzzleInput);
    }

    private void HidePuzzlePanel()
    {
        if (puzzlePanel != null)
            puzzlePanel.SetActive(false);
        currentPuzzleId = string.Empty;
        currentPuzzleInput = string.Empty;
        puzzleSelectedOptions.Clear();
    }

    private void ShowTrapPanel(string trapId)
    {
        MazeTrapData trap = GameDatabase.Instance?.Get<MazeTrapData>("maze_traps", trapId);
        if (trap == null)
        {
            ShowMessage($"Cannot find trap {trapId}.");
            return;
        }

        currentTrapId = trapId;
        trapActive = true;
        trapProgress = 0f;
        bool isButtonMash = (trap.trapType ?? string.Empty).Contains(TrapTypeButtonMash);
        trapGoal = isButtonMash ? 20f : 100f;
        trapTimer = isButtonMash ? 6f : 8f;
        UIHelper.SetText(trapTitleText, ResolveTrapLabel(trap));
        SetButtonText(trapActionButton, isButtonMash ? "Mash / Space" : "Run / Space");
        RefreshTrapPanelText();
        if (trapPanel != null)
            trapPanel.SetActive(true);
    }

    private void AdvanceTrapProgress()
    {
        if (!trapActive)
            return;

        MazeTrapData trap = GameDatabase.Instance?.Get<MazeTrapData>("maze_traps", currentTrapId);
        bool isButtonMash = (trap?.trapType ?? string.Empty).Contains(TrapTypeButtonMash);
        trapProgress += isButtonMash ? 1f : 14f;
        RefreshTrapPanelText();
    }

    private void RefreshTrapPanelText()
    {
        MazeTrapData trap = GameDatabase.Instance?.Get<MazeTrapData>("maze_traps", currentTrapId);
        float percent = trapGoal <= 0f ? 1f : Mathf.Clamp01(trapProgress / trapGoal);
        string action = (trap?.trapType ?? string.Empty).Contains(TrapTypeButtonMash)
            ? "Click the button or press Space quickly."
            : "Click the button or press Space to fill the escape progress.";
        UIHelper.SetText(trapBodyText, $"{action}\nProgress: {Mathf.FloorToInt(percent * 100f)}%\nTime: {Mathf.CeilToInt(Mathf.Max(0f, trapTimer))}s");
    }

    private void ResolveTrapOutcome(bool success)
    {
        if (!trapActive)
            return;

        string trapId = currentTrapId;
        trapActive = false;
        HideTrapPanel();

        MazeRunService mazeRun = GameManager.Instance?.Services?.MazeRun;
        if (mazeRun == null)
            return;

        bool survived = mazeRun.ResolveTrap(trapId, success);
        MazeTrapData trap = GameDatabase.Instance?.Get<MazeTrapData>("maze_traps", trapId);
        if (success && survived)
            ShowMessage($"Trap cleared: {ResolveTrapLabel(trap)}");
        else if (!success && !string.IsNullOrEmpty(trap?.guideNodeId))
            ShowMessage($"Trap failed. Guide points to {ResolveNodeName(trap.guideNodeId)}.");
        else
            ShowMessage(success ? "Trap cleared." : "Trap failed.");
        HideNodeActionPanel();
        RefreshNow();
    }

    private void HideTrapPanel()
    {
        if (trapPanel != null)
            trapPanel.SetActive(false);
        trapActive = false;
        currentTrapId = string.Empty;
    }

    private void ShowExitPanel()
    {
        ShowExitChoice(true);
    }

    private void ShowExitChoice(bool allowPerfect)
    {
        MazeRunService mazeRun = GameManager.Instance?.Services?.MazeRun;
        if (mazeRun == null || mazeRun.State != MazeRunState.Running)
            return;

        if (!allowPerfect)
        {
            UIHelper.SetText(exitTitleText, "Evacuate");
            UIHelper.SetText(exitBodyText, "Evacuate now and keep 50% of collected ingredients.");
            SetButtonText(exitNormalButton, "Evacuate");
            SetButtonText(exitPerfectButton, "Perfect Locked");
            exitPerfectButton.interactable = false;
        }
        else
        {
            int total = mazeRun.GetTotalFragmentCount();
            int collected = mazeRun.GetCollectedFragmentCount();
            bool complete = total > 0 && collected >= total;
            int missingPercent = total <= 0 ? 100 : Mathf.CeilToInt((total - collected) * 100f / total);
            UIHelper.SetText(exitTitleText, "Maze Exit");
            UIHelper.SetText(exitBodyText, complete
                ? "All puzzle fragments collected. Compose the exit puzzle for 200% rewards and a blueprint."
                : $"You are {missingPercent}% away from perfect clear. Leaving now grants 100% rewards.");
            SetButtonText(exitNormalButton, "Leave");
            SetButtonText(exitPerfectButton, complete ? "Perfect Clear" : "Missing Fragments");
            exitPerfectButton.interactable = complete;
        }

        if (exitPanel != null)
            exitPanel.SetActive(true);
    }

    private void CompleteNormalExit()
    {
        MazeRunService mazeRun = GameManager.Instance?.Services?.MazeRun;
        if (mazeRun == null)
            return;

        MazeRunResult result;
        if (exitTitleText != null && exitTitleText.text == "Evacuate")
            result = mazeRun.EvacuateRun();
        else
            result = mazeRun.CompleteRun();
        HideExitPanel();
        ShowResult(result);
    }

    private void HideExitPanel()
    {
        if (exitPanel != null)
            exitPanel.SetActive(false);
    }

    private void OpenFoodPanel()
    {
        MazeRunService mazeRun = GameManager.Instance?.Services?.MazeRun;
        PlayerDatabase player = GameDatabase.Instance?.GetPlayerData();
        if (mazeRun == null || player?.inventory == null)
            return;

        DestroyChildren(foodOptionsRoot);
        dynamicFoodButtons.Clear();

        int optionCount = 0;
        IReadOnlyList<FoodData> foods = GameDatabase.Instance?.GetAll<FoodData>("foods");
        if (foods != null)
        {
            for (int i = 0; i < foods.Count; i++)
            {
                FoodData food = foods[i];
                if (food == null || string.IsNullOrEmpty(food.foodId) || food.energyRestore <= 0)
                    continue;

                int amount = player.inventory.GetAmount(food.foodId);
                if (amount <= 0)
                    continue;

                optionCount++;
                string foodId = food.foodId;
                int restore = food.energyRestore;
                Button button = CreateButton(foodOptionsRoot, $"FoodOption_{optionCount}", $"{ResolveDisplayName(foodId)} x{amount} (+{restore})");
                button.onClick.AddListener(() => UseFood(foodId, restore));
                dynamicFoodButtons.Add(button);
            }
        }

        UIHelper.SetText(foodTitleText, "Food");
        UIHelper.SetText(foodBodyText, optionCount > 0 ? "Use food to restore energy inside the maze." : "No energy-restoring food in backpack.");
        if (foodPanel != null)
            foodPanel.SetActive(true);
    }

    private void UseFood(string foodId, int restoreAmount)
    {
        MazeRunService mazeRun = GameManager.Instance?.Services?.MazeRun;
        if (mazeRun == null)
            return;

        bool success = mazeRun.UseFoodForEnergy(foodId, restoreAmount);
        ShowMessage(success ? $"Used {ResolveDisplayName(foodId)}." : $"Cannot use {ResolveDisplayName(foodId)}.");
        OpenFoodPanel();
        RefreshNow();
    }

    private void HideFoodPanel()
    {
        if (foodPanel != null)
            foodPanel.SetActive(false);
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
        if (result.rewardBlueprintIds != null && result.rewardBlueprintIds.Count > 0)
        {
            sb.AppendLine();
            sb.Append("Blueprints: ");
            for (int i = 0; i < result.rewardBlueprintIds.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(ResolveDisplayName(result.rewardBlueprintIds[i]));
            }
        }
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
            sb.Append(ResolveDisplayName(kv.Key));
            sb.Append(" x");
            sb.Append(kv.Value);
            first = false;
        }
    }

    private List<MazeNodeData> GetConfiguredNodesSorted()
    {
        List<MazeNodeData> result = new List<MazeNodeData>();
        IReadOnlyList<MazeNodeData> nodes = GameDatabase.Instance?.GetAll<MazeNodeData>("maze_nodes");
        if (nodes != null)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null)
                    result.Add(nodes[i]);
            }
        }

        result.Sort((a, b) => a.index.CompareTo(b.index));
        return result;
    }

    private string BuildEnergyText()
    {
        PlayerProfile profile = GameDatabase.Instance?.GetPlayerData()?.profile;
        if (profile == null)
            return string.Empty;
        return $"{profile.energy}/{profile.maxEnergy}";
    }

    private string BuildFragmentsText(MazeRunService mazeRun)
    {
        if (mazeRun == null)
            return "Fragments: none";

        int collected = mazeRun.GetCollectedFragmentCount();
        int total = mazeRun.GetTotalFragmentCount();
        if (collected <= 0)
            return total > 0 ? $"Fragments: 0/{total}" : "Fragments: none";

        return $"Fragments: {collected}/{total}";
    }

    private string BuildNodeDetailText(MazeRunResult result)
    {
        if (result == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder();
        sb.Append("Visited: ");
        if (result.visitedNodeIds == null || result.visitedNodeIds.Count == 0)
            sb.Append("none");
        else
        {
            bool first = true;
            foreach (string nodeId in result.visitedNodeIds)
            {
                if (!first)
                    sb.Append(", ");
                sb.Append(ResolveNodeName(nodeId));
                first = false;
            }
        }

        if (!string.IsNullOrEmpty(result.guideNodeId))
        {
            sb.AppendLine();
            sb.Append("Guide: ");
            sb.Append(ResolveNodeName(result.guideNodeId));
        }

        return sb.ToString();
    }

    private string ResolveNodeName(string nodeId)
    {
        MazeNodeData node = GameDatabase.Instance?.Get<MazeNodeData>("maze_nodes", nodeId);
        return node != null ? GetNodeDisplayName(node) : nodeId;
    }

    private string ResolveDisplayName(string id)
    {
        if (string.IsNullOrEmpty(id))
            return string.Empty;

        FoodData food = GameDatabase.Instance?.Get<FoodData>("foods", id);
        if (food != null && !string.IsNullOrEmpty(food.displayName))
            return food.displayName;
        IngredientData ingredient = GameDatabase.Instance?.Get<IngredientData>("ingredients", id);
        if (ingredient != null && !string.IsNullOrEmpty(ingredient.displayName))
            return ingredient.displayName;
        BlueprintData blueprint = GameDatabase.Instance?.Get<BlueprintData>("blueprints", id);
        if (blueprint != null && !string.IsNullOrEmpty(blueprint.displayName))
            return blueprint.displayName;
        ItemData item = GameDatabase.Instance?.Get<ItemData>("items", id);
        if (item != null && !string.IsNullOrEmpty(item.name))
            return item.name;

        return id;
    }

    private static string GetNodeDisplayName(MazeNodeData node)
    {
        if (node == null)
            return string.Empty;
        return string.IsNullOrEmpty(node.title) ? node.nodeId : node.title;
    }

    private static string ResolvePuzzleLabel(MazePuzzleData puzzle)
    {
        if (puzzle == null || string.IsNullOrEmpty(puzzle.puzzleType))
            return "Puzzle";

        switch (puzzle.puzzleType)
        {
            case PuzzleTypeItemSocket:
                return "道具圆盘";
            case PuzzleTypeNumericInput:
                return "深潭数字";
            case PuzzleTypeFloorChoice:
                return "信义地砖";
            case PuzzleTypeBlockWords:
                return "光束遮字";
            default:
                return puzzle.puzzleType;
        }
    }

    private static string ResolveTrapLabel(MazeTrapData trap)
    {
        if (trap == null || string.IsNullOrEmpty(trap.trapType))
            return "Trap";

        switch (trap.trapType)
        {
            case TrapTypeButtonMash:
                return "宝箱逃脱";
            case TrapTypeRunEscape:
                return "落石逃离";
            default:
                return trap.trapType;
        }
    }

    private static string ResolveOptionLabel(MazePuzzleData puzzle, string option)
    {
        if (string.IsNullOrEmpty(option))
            return string.Empty;

        if ((puzzle?.puzzleType ?? string.Empty).Contains(PuzzleTypeItemSocket))
        {
            switch (option)
            {
                case "correct_items":
                    return "放入房间拾取的正确道具";
                case "wrong_items":
                    return "放入不相干的旧物";
                case "old_coin":
                    return "放入古旧钱币";
                case "empty_bowl":
                    return "放入空碗";
            }
        }

        switch (option)
        {
            case "profit":
                return "逐利";
            case "fame":
                return "功名";
            case "power":
                return "权势";
            case "trust":
                return "信义";
            case "fear":
                return "畏惧";
            case "gold":
                return "千金";
            case "silence":
                return "沉默";
            case "escape":
                return "逃亡";
            case "clear":
                return "Clear";
            default:
                return option;
        }
    }

    private static void SetButtonText(Button button, string label)
    {
        Text text = button != null ? button.GetComponentInChildren<Text>(true) : null;
        if (text != null)
            text.text = label;
    }

    private static void DestroyChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
    }
}

public enum MazeGameplayCategory
{
    Reward,
    Switch,
    Puzzle,
    Trap,
    Evacuate,
    Exit
}

public sealed class MazeUIRuntimeDriver : MonoBehaviour
{
    private MazeUIController controller;

    public void Bind(MazeUIController value)
    {
        controller = value;
    }

    private void Update()
    {
        controller?.Tick(Time.deltaTime);
    }
}
