using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;

public static class MazePuzzlePrefabBuilder
{
    private static readonly string[] PrefabPaths =
    {
        "Assets/UI/Prefabs/MazePuzzleItemSocketUI.prefab",
        "Assets/UI/Prefabs/MazePuzzleCandleNumberUI.prefab",
        "Assets/UI/Prefabs/MazePuzzleFloorChoiceUI.prefab",
        "Assets/UI/Prefabs/MazePuzzleRockWordUI.prefab"
    };

    // 通用按钮切图目录。AddButton 的 imgPath 与 ConfigureAddressables 都从这里取，别写第二份。
    private const string ButtonArtDir = "Assets/UI/asset/common/Button";

    private static Font font;

    [MenuItem("MazeGame/UI/Rebuild Puzzle Room Prefabs")]
    public static void Rebuild()
    {
        GameObject mazePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/MazeUI.prefab");
        Text mazeText = mazePrefab != null ? mazePrefab.GetComponentInChildren<Text>(true) : null;
        font = mazeText != null && mazeText.font != null
            ? mazeText.font
            : Resources.GetBuiltinResource<Font>("Arial.ttf");
        BuildItemSocket();
        BuildCandleNumber();
        BuildFloorChoice();
        BuildRockWord();
        ConfigureAddressables();
        AssetDatabase.SaveAssets();
        Debug.Log("[MazePuzzlePrefabBuilder] Four puzzle prefabs rebuilt and registered as Addressables.");
    }

    // 解密一：七个槽位按北斗七星勺形排布，底部是本房间拾取到的七枚道具。
    private static readonly string[] StarSlotNames = { "天枢", "天璇", "天玑", "天权", "玉衡", "开阳", "瑶光" };

    private static readonly Vector2[] StarSlotPositions =
    {
        new Vector2(60f, -35f),
        new Vector2(-8f, -123f),
        new Vector2(-160f, -77f),
        new Vector2(-135f, 1f),
        new Vector2(-212f, 63f),
        new Vector2(-268f, 118f),
        new Vector2(-341f, 156f)
    };

    private static void BuildItemSocket()
    {
        GameObject root = CreateBase("MazePuzzleItemSocketUI", "解密一 · 圆坛置物");
        Transform panel = root.transform.Find("PuzzlePanel");

        for (int i = 0; i < StarSlotPositions.Length; i++)
            AddButton("SlotButton_" + i, panel, "+", new Vector2(72f, 72f), StarSlotPositions[i], "SlotText_" + i);

        AddVerticalText("HintColumn_0", panel, "一枢二璇三玑四权", new Vector2(400f, 60f), 26);
        AddVerticalText("HintColumn_1", panel, "五衡六阳七光", new Vector2(340f, 0f), 26);

        // 托盘压在 FeedbackText 上沿之上，7 枚道具步进 95 给 RemoveButton 让出右端。
        for (int i = 0; i < StarSlotNames.Length; i++)
            AddButton("ItemButton_" + i, panel, StarSlotNames[i], new Vector2(86f, 56f), new Vector2(-330f + i * 95f, -210f), "ItemText_" + i, $"{ButtonArtDir}/star_{i + 1}.png", new Color(0f,0f,0f,1f));

        AddButton("RemoveButton", panel, "移除道具", new Vector2(140f, 50f), new Vector2(390f, -210f));
        SavePrefab(root, PrefabPaths[0]);
    }

    // 解密二：九座烛台环绕中央水潭。编号顺序照策划案刻意打乱，玩家需要"数"而非"读最大值"。
    private static readonly int[] CandleLabels = { 1, 6, 8, 4, 5, 2, 7, 9, 3 };

    private static readonly Vector2[] CandlePositions =
    {
        new Vector2(-360f, 205f),
        new Vector2(-120f, 205f),
        new Vector2(120f, 205f),
        new Vector2(330f, 205f),
        new Vector2(-400f, 10f),
        new Vector2(-360f, -200f),
        new Vector2(-120f, -200f),
        new Vector2(120f, -200f),
        new Vector2(330f, -200f)
    };

    private static void BuildCandleNumber()
    {
        GameObject root = CreateBase("MazePuzzleCandleNumberUI", "解密二 · 九烛深潭");
        Transform panel = root.transform.Find("PuzzlePanel");

        for (int i = 0; i < CandlePositions.Length; i++)
            AddButton("CandleButton_" + i, panel, "烛台\n" + CandleLabels[i], new Vector2(92f, 66f), CandlePositions[i], "CandleText_" + i);

        AddButton("PoolButton", panel, "水潭", new Vector2(200f, 200f), Vector2.zero);
        AddText("EntranceText", panel, "入口", new Vector2(80f, 40f), new Vector2(430f, 10f), 22, TextAnchor.MiddleCenter);

        // 数字键盘是弹层，点击水潭后才显示（由 CandleNumberPuzzleUIController 控制）。
        GameObject numberPanel = AddPanel("NumberPanel", panel, new Vector2(700f, 400f), Vector2.zero, new Color(0.96f, 0.96f, 0.96f, 1f));
        Transform keypad = numberPanel.transform;

        for (int i = 1; i <= 8; i++)
        {
            int row = (i - 1) / 4;
            int col = (i - 1) % 4;
            AddButton("NumberButton_" + i, keypad, i.ToString(), new Vector2(60f, 60f), new Vector2(-215f + col * 78f, 100f - row * 78f), "NumberText_" + i);
        }
        AddButton("NumberButton_9", keypad, "9", new Vector2(60f, 60f), new Vector2(-215f, -56f), "NumberText_9");
        AddButton("NumberButton_0", keypad, "0", new Vector2(60f, 60f), new Vector2(19f, -56f), "NumberText_0");

        AddText("KeypadTipText", keypad, "根据提示输入答案", new Vector2(320f, 40f), new Vector2(-100f, -140f), 22, TextAnchor.MiddleCenter)
            .color = new Color(0.1f, 0.1f, 0.12f, 1f);
        AddVerticalText("PoolHintText_0", keypad, "剑隐深潭", new Vector2(215f, 60f), 30).color = new Color(0.1f, 0.1f, 0.12f, 1f);
        AddVerticalText("PoolHintText_1", keypad, "龙居渊底", new Vector2(285f, 20f), 30).color = new Color(0.1f, 0.1f, 0.12f, 1f);
        AddButton("ClearButton", keypad, "清空", new Vector2(110f, 48f), new Vector2(100f, -140f));
        numberPanel.SetActive(false);
        SavePrefab(root, PrefabPaths[1]);
    }

    // 解密三：八块字砖分两排，中间穿插六句竖排提示（推理依据，静态装饰）。
    private static readonly Vector2[] FloorTilePositions =
    {
        new Vector2(-260f, 205f),
        new Vector2(-90f, 205f),
        new Vector2(85f, 205f),
        new Vector2(255f, 205f),
        new Vector2(-260f, -195f),
        new Vector2(-90f, -195f),
        new Vector2(85f, -195f),
        new Vector2(255f, -195f)
    };

    private static readonly string[] FloorHintPhrases = { "非为功名", "非为珍宝", "唯守一片清白", "身死不留余念", "不负渡江之诺", "不贪千金神兵" };
    private static readonly float[] FloorHintX = { -390f, -258f, -88f, 85f, 255f, 400f };

    private static void BuildFloorChoice()
    {
        GameObject root = CreateBase("MazePuzzleFloorChoiceUI", "解密三 · 八方择信");
        Transform panel = root.transform.Find("PuzzlePanel");

        for (int i = 0; i < FloorTilePositions.Length; i++)
            AddButton("OptionButton_" + i, panel, "地块" + (i + 1), new Vector2(80f, 80f), FloorTilePositions[i], "OptionText_" + i);

        for (int i = 0; i < FloorHintPhrases.Length; i++)
            AddVerticalText("HintPhrase_" + i, panel, FloorHintPhrases[i], new Vector2(FloorHintX[i], 0f), 22, 26f);

        SavePrefab(root, PrefabPaths[2]);
    }

    // 解密四：七个字一排，三块可推动的礁石，礁石旁一张纸条，房间底部一排烛台。
    private static readonly float[] RockIndicatorX = { -30f, 70f, 285f };

    private static void BuildRockWord()
    {
        GameObject root = CreateBase("MazePuzzleRockWordUI", "解密四 · 礁石遮字");
        Transform panel = root.transform.Find("PuzzlePanel");

        for (int i = 0; i < 7; i++)
            AddButton("OptionButton_" + i, panel, "字", new Vector2(82f, 82f), new Vector2(-300f + i * 100f, 200f), "OptionText_" + i);

        for (int i = 0; i < RockIndicatorX.Length; i++)
            AddText("RockIndicator_" + i, panel, "礁石", new Vector2(76f, 50f), new Vector2(RockIndicatorX[i], 60f), 20, TextAnchor.MiddleCenter);

        AddButton("NoteButton", panel, "纸条", new Vector2(60f, 48f), new Vector2(-30f, -10f), "NoteButtonText");
        AddButton("LightButton", panel, "一排烛台", new Vector2(700f, 70f), new Vector2(0f, -180f), "LightButtonText");
        SavePrefab(root, PrefabPaths[3]);
    }

    private static GameObject CreateBase(string viewName, string title)
    {
        GameObject root = new GameObject(viewName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 0.88f);

        GameObject panel = CreateRect("PuzzlePanel", root.transform, new Vector2(980f, 720f), Vector2.zero);
        panel.AddComponent<CanvasRenderer>();
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.15f, 0.19f, 1f);

        // 公共文本一律贴边（顶部两行 + 底部一行 + 底栏左右角），把 y ∈ [-238, 246]
        // 这一整块净空留给各房间的玩法节点，避免烛台/字砖/托盘压住 Hint 与 Reward。
        AddText("TitleText", panel.transform, title, new Vector2(620f, 48f), new Vector2(0f, 316f), 34, TextAnchor.MiddleCenter);
        AddText("EnergyText", panel.transform, "Energy", new Vector2(180f, 40f), new Vector2(360f, 316f), 20, TextAnchor.MiddleRight);
        AddText("HintText", panel.transform, "Hint", new Vector2(820f, 40f), new Vector2(0f, 268f), 20, TextAnchor.MiddleCenter);
        AddText("FeedbackText", panel.transform, string.Empty, new Vector2(820f, 46f), new Vector2(0f, -262f), 22, TextAnchor.MiddleCenter);
        AddText("SelectionText", panel.transform, "Selection: none", new Vector2(220f, 40f), new Vector2(-320f, -315f), 18, TextAnchor.MiddleLeft);
        AddText("RewardText", panel.transform, "Reward", new Vector2(220f, 40f), new Vector2(320f, -315f), 18, TextAnchor.MiddleRight);
        AddButton("SubmitButton", panel.transform, "确认", new Vector2(180f, 54f), new Vector2(120f, -315f));
        AddButton("CloseButton", panel.transform, "关闭", new Vector2(180f, 54f), new Vector2(-120f, -315f));
        return root;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return go;
    }

    private static Text AddText(string name, Transform parent, string value, Vector2 size, Vector2 position, int fontSize, TextAnchor alignment, Color? color = null)
    {
        GameObject go = CreateRect(name, parent, size, position);
        go.AddComponent<CanvasRenderer>();
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color ?? new Color(0.94f, 0.92f, 0.86f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Button AddButton(string name, Transform parent, string label, Vector2 size, Vector2 position, string labelName = null, string imgPath = null, Color? textColor = null)
    {
        GameObject go = CreateRect(name, parent, size, position);
        go.AddComponent<CanvasRenderer>();
        Image image = go.AddComponent<Image>();
        // 如果没有传入图片
        if (string.IsNullOrEmpty(imgPath))
        {
            image.color = new Color(0.32f, 0.38f, 0.46f, 1f);
        }
        else
        {
            Sprite sp = string.IsNullOrEmpty(imgPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<Sprite>(imgPath);
            //如果路径不对
            if(sp != null)
            {
                image.sprite = sp;
            }
            else
            {
                Debug.LogWarning($"[MazePuzzlePrefabBuilder] Sprite 未找到，回落纯色: {imgPath}");
                image.color = new Color(0.32f, 0.38f, 0.46f, 1f);
            }
        }
        Button button = go.AddComponent<Button>();
        Text text = AddText(labelName ?? name + "Text", go.transform, label, size, Vector2.zero, 22, TextAnchor.MiddleCenter, textColor);
        text.raycastTarget = false;
        return button;
    }

    // 竖排文字：逐字换行，用于策划案里右侧的竖排提示与解密三地面上的句子。
    private static Text AddVerticalText(string name, Transform parent, string content, Vector2 position, int fontSize, float lineHeight = 30f)
    {
        content = content ?? string.Empty;
        System.Text.StringBuilder builder = new System.Text.StringBuilder(content.Length * 2);
        for (int i = 0; i < content.Length; i++)
        {
            if (i > 0)
                builder.Append('\n');
            builder.Append(content[i]);
        }

        Vector2 size = new Vector2(fontSize + 14f, content.Length * lineHeight + 10f);
        Text text = AddText(name, parent, builder.ToString(), size, position, fontSize, TextAnchor.UpperCenter);
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        return text;
    }

    private static GameObject AddPanel(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        GameObject go = CreateRect(name, parent, size, position);
        go.AddComponent<CanvasRenderer>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        bool success;
        PrefabUtility.SaveAsPrefabAsset(root, path, out success);
        UnityEngine.Object.DestroyImmediate(root);
        if (!success)
            throw new InvalidOperationException("Failed to save prefab: " + path);
    }

    private static void ConfigureAddressables()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            throw new InvalidOperationException("Addressable settings are not available.");

        for (int i = 0; i < PrefabPaths.Length; i++)
        {
            string path = PrefabPaths[i];
            string guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false);
            entry.address = path;
            EditorUtility.SetDirty(entry.parentGroup);
        }
        EditorUtility.SetDirty(settings);
    }
}
