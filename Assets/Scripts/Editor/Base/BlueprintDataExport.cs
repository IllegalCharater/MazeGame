using System;
using System.Data;

public partial class ExcelToJsonExporter
{
    public sealed class BlueprintExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string blueprintId = Str(dt, r, "blueprintId");
                if (string.IsNullOrWhiteSpace(blueprintId)) continue;
                if (dataList.ContainsKey(blueprintId)) throw new Exception($"blueprints primary key duplicated blueprintId={blueprintId}, row={r + 3}");

                dataList[blueprintId] = new BlueprintData
                {
                    blueprintId = blueprintId,
                    displayName = Str(dt, r, "displayName"),
                    price = UnityEngine.Mathf.Max(0, Int(dt, r, "price", 0)),
                    buffId = Norm(Str(dt, r, "buffId")),
                    requirementIds = ParseList(dt, r, "requirementIds")
                };
            }
        }
    }
}
