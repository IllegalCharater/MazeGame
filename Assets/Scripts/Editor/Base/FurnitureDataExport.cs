using System;
using System.Data;

public partial class ExcelToJsonExporter
{
    public sealed class FurnitureExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string furnitureId = Str(dt, r, "furnitureId");
                if (string.IsNullOrWhiteSpace(furnitureId)) continue;
                if (dataList.ContainsKey(furnitureId)) throw new Exception($"furniture primary key duplicated furnitureId={furnitureId}, row={r + 3}");

                dataList[furnitureId] = new FurnitureData
                {
                    furnitureId = furnitureId,
                    displayName = Str(dt, r, "displayName"),
                    price = UnityEngine.Mathf.Max(0, Int(dt, r, "price", 0))
                };
            }
        }
    }
}
