using UnityEngine;
using System.Collections.Generic;
using System.Data;
using System;
using System.Linq;

public partial class ExcelToJsonExporter
{
    public sealed class RecipeExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string recipeId = Str(dt, r, "recipeId");
                if (string.IsNullOrWhiteSpace(recipeId)) continue;
                if (dataList.ContainsKey(recipeId)) throw new Exception($"recipes 主键重复 recipeId={recipeId}, row={r + 3}（Excel 行号，含 meta+表头）");

                dataList[recipeId] = new RecipeData
                {
                    recipeId = recipeId,
                    displayName = Str(dt, r, "displayName"),
                    outputItems = ParseList(dt, r, "outputItems"),
                    outputAmounts = ParseList(dt, r, "outputAmounts").ConvertAll(int.Parse),
                    ingredients = ParseDictionary(dt, r, "ingredients").ToDictionary(
                        kv => kv.Key, kv => Mathf.Max(0, int.Parse(kv.Value))
                    )
                };
            }
        }
    }
}
