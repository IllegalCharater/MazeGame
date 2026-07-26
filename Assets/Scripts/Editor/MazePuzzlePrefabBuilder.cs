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

    private static void BuildItemSocket()
    {
        GameObject root = CreateBase("MazePuzzleItemSocketUI", "解密一 · 圆坛置物");
        Transform panel = root.transform.Find("PuzzlePanel");
        AddText("AltarText", panel, "圆坛放置槽", new Vector2(300f, 70f), new Vector2(0f, 100f), 26, TextAnchor.MiddleCenter);
        for (int i = 0; i < 4; i++)
        {
            float x = -270f + i * 180f;
            AddButton("OptionButton_" + i, panel, "道具" + (i + 1), new Vector2(150f, 64f), new Vector2(x, 5f), "OptionText_" + i);
        }
        AddButton("RemoveButton", panel, "移除道具", new Vector2(180f, 50f), new Vector2(0f, -75f));
        SavePrefab(root, PrefabPaths[0]);
    }

    private static void BuildCandleNumber()
    {
        GameObject root = CreateBase("MazePuzzleCandleNumberUI", "解密二 · 九烛深潭");
        Transform panel = root.transform.Find("PuzzlePanel");
        for (int i = 0; i < 9; i++)
        {
            int row = i / 3;
            int col = i % 3;
            AddButton("CandleButton_" + i, panel, "烛" + (i + 1), new Vector2(92f, 58f), new Vector2(-310f + col * 105f, 100f - row * 70f), "CandleText_" + i);
        }
        AddButton("PoolButton", panel, "查看水潭", new Vector2(180f, 76f), new Vector2(0f, 25f));
        for (int i = 0; i <= 9; i++)
        {
            int row = i / 5;
            int col = i % 5;
            AddButton("NumberButton_" + i, panel, i.ToString(), new Vector2(68f, 54f), new Vector2(190f + col * 76f, 70f - row * 65f), "NumberText_" + i);
        }
        AddButton("ClearButton", panel, "清空", new Vector2(120f, 48f), new Vector2(340f, -75f));
        SavePrefab(root, PrefabPaths[1]);
    }

    private static void BuildFloorChoice()
    {
        GameObject root = CreateBase("MazePuzzleFloorChoiceUI", "解密三 · 八方择信");
        Transform panel = root.transform.Find("PuzzlePanel");
        for (int i = 0; i < 8; i++)
        {
            int row = i / 4;
            int col = i % 4;
            AddButton("OptionButton_" + i, panel, "地块" + (i + 1), new Vector2(170f, 78f), new Vector2(-270f + col * 180f, 80f - row * 95f), "OptionText_" + i);
        }
        SavePrefab(root, PrefabPaths[2]);
    }

    private static void BuildRockWord()
    {
        GameObject root = CreateBase("MazePuzzleRockWordUI", "解密四 · 礁石遮字");
        Transform panel = root.transform.Find("PuzzlePanel");
        AddButton("LightButton", panel, "点亮烛台", new Vector2(180f, 58f), new Vector2(0f, 110f), "LightButtonText");
        for (int i = 0; i < 7; i++)
        {
            float x = -300f + i * 100f;
            AddButton("OptionButton_" + i, panel, "字", new Vector2(82f, 82f), new Vector2(x, 0f), "OptionText_" + i);
        }
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

        AddText("TitleText", panel.transform, title, new Vector2(620f, 56f), new Vector2(0f, 315f), 34, TextAnchor.MiddleCenter);
        AddText("EnergyText", panel.transform, "Energy", new Vector2(180f, 40f), new Vector2(360f, 315f), 20, TextAnchor.MiddleRight);
        AddText("HintText", panel.transform, "Hint", new Vector2(820f, 80f), new Vector2(0f, 245f), 22, TextAnchor.MiddleCenter);
        AddText("SelectionText", panel.transform, "Selection: none", new Vector2(760f, 44f), new Vector2(0f, 180f), 20, TextAnchor.MiddleCenter);
        AddText("FeedbackText", panel.transform, string.Empty, new Vector2(820f, 58f), new Vector2(0f, -190f), 22, TextAnchor.MiddleCenter);
        AddText("RewardText", panel.transform, "Reward", new Vector2(820f, 82f), new Vector2(0f, -245f), 18, TextAnchor.MiddleCenter);
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

    private static Text AddText(string name, Transform parent, string value, Vector2 size, Vector2 position, int fontSize, TextAnchor alignment)
    {
        GameObject go = CreateRect(name, parent, size, position);
        go.AddComponent<CanvasRenderer>();
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.92f, 0.86f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Button AddButton(string name, Transform parent, string label, Vector2 size, Vector2 position, string labelName = null)
    {
        GameObject go = CreateRect(name, parent, size, position);
        go.AddComponent<CanvasRenderer>();
        Image image = go.AddComponent<Image>();
        image.color = new Color(0.32f, 0.38f, 0.46f, 1f);
        Button button = go.AddComponent<Button>();
        Text text = AddText(labelName ?? name + "Text", go.transform, label, size, Vector2.zero, 22, TextAnchor.MiddleCenter);
        text.raycastTarget = false;
        return button;
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
