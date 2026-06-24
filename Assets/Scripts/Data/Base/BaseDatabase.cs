using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class BaseDatabase
{
    public string rootKey = string.Empty;
    public TextAsset JsonSourceAsset = null;
    public Dictionary<string, BaseData> dataList = new Dictionary<string, BaseData>();

    public void Init(string rootKey)
    {
        this.rootKey = rootKey;
    }

    public void LoadData()
    {
        ReloadFromJson();
    }

    private void ReloadFromJson()
    {
        dataList.Clear();

        string jsonStem = GetJsonName();
        if (string.IsNullOrEmpty(jsonStem))
        {
            Debug.LogWarning($"[{GetType().Name}] No JSON mapping found for rootKey: {rootKey}");
            return;
        }

        JsonSourceAsset = Resources.Load<TextAsset>(jsonStem);
        if (JsonSourceAsset == null)
        {
            Debug.LogWarning($"[{GetType().Name}] JSON not found in Resources: {jsonStem}");
            return;
        }

        try
        {
            var root = JObject.Parse(JsonSourceAsset.text);
            JToken tableToken = root[rootKey];
            if (tableToken == null || tableToken.Type != JTokenType.Object)
            {
                Debug.LogWarning($"[{GetType().Name}] Table node not found: {rootKey}");
                return;
            }

            if (!DataConfig.DataTypes.TryGetValue(rootKey, out Type rowType) || rowType == null)
            {
                Debug.LogWarning($"[{GetType().Name}] DataConfig type not registered: {rootKey}");
                return;
            }

            var tableObj = (JObject)tableToken;
            foreach (var prop in tableObj.Properties())
            {
                string id = prop.Name;
                if (string.IsNullOrEmpty(id))
                    continue;

                try
                {
                    var row = prop.Value.ToObject(rowType) as BaseData;
                    if (row == null)
                        continue;

                    // AssignRowId(row, id);
                    dataList[id] = row;
                }
                catch (Exception rowEx)
                {
                    Debug.LogWarning($"[{GetType().Name}] Failed to parse row id={id}: {rowEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{GetType().Name}] Failed to read JSON: {ex.Message} {ex.StackTrace}");
        }
    }

    private string GetJsonName()
    {
        var manifest = Resources.Load<TextAsset>(DataConfig.BaseJsonName);
        if (manifest == null)
            return string.Empty;

        try
        {
            var root = JObject.Parse(manifest.text);
            string stem = root.Value<string>(rootKey);
            return string.IsNullOrEmpty(stem) ? string.Empty : stem;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[{GetType().Name}] Failed to parse {DataConfig.BaseJsonName}.json: {ex.Message}");
            return string.Empty;
        }
    }

    // private void AssignRowId(BaseData row, string id)
    // {
    //     string fieldName = ResolveIdFieldName(rootKey);
    //     if (string.IsNullOrEmpty(fieldName))
    //         return;
    //
    //     var field = row.GetType().GetField(fieldName);
    //     if (field != null && field.FieldType == typeof(string))
    //         field.SetValue(row, id);
    // }

    // private static string ResolveIdFieldName(string rootKey)
    // {
    //     switch (rootKey)
    //     {
    //         case "items":
    //             return "itemId";
    //         case "player_items":
    //             return "playerId";
    //         case "recipes":
    //             return "recipeId";
    //         case "foods":
    //             return "foodId";
    //         case "ingredients":
    //             return "ingredientId";
    //         case "blueprints":
    //             return "blueprintId";
    //         case "outfits":
    //             return "outfitId";
    //         case "furniture":
    //             return "furnitureId";
    //         case "buffs":
    //             return "buffId";
    //         default:
    //             return string.Empty;
    //     }
    // }
}
