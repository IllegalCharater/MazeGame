using System;
using System.Data;

public partial class ExcelToJsonExporter
{
    public sealed class IngredientExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string ingredientId = Str(dt, r, "ingredientId");
                if (string.IsNullOrWhiteSpace(ingredientId)) continue;
                if (dataList.ContainsKey(ingredientId)) throw new Exception($"ingredients primary key duplicated ingredientId={ingredientId}, row={r + 3}");

                dataList[ingredientId] = new IngredientData
                {
                    ingredientId = ingredientId,
                    displayName = Str(dt, r, "displayName"),
                    ingredientType = Str(dt, r, "ingredientType")
                };
            }
        }
    }
}
