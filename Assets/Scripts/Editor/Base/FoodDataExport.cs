using System;
using System.Data;

public partial class ExcelToJsonExporter
{
    public sealed class FoodExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string foodId = Str(dt, r, "foodId");
                if (string.IsNullOrWhiteSpace(foodId)) continue;
                if (dataList.ContainsKey(foodId)) throw new Exception($"foods primary key duplicated foodId={foodId}, row={r + 3}");

                dataList[foodId] = new FoodData
                {
                    foodId = foodId,
                    displayName = Str(dt, r, "displayName"),
                    price = UnityEngine.Mathf.Max(0, Int(dt, r, "price", 0)),
                    energyRestore = UnityEngine.Mathf.Max(0, Int(dt, r, "energyRestore", 0)),
                    ingredientItems = ParseList(dt, r, "ingredientItems"),
                    ingredientAmounts = ParseList(dt, r, "ingredientAmounts").ConvertAll(int.Parse)
                };
            }
        }
    }
}
