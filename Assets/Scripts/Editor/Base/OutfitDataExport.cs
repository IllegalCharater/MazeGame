using System;
using System.Data;

public partial class ExcelToJsonExporter
{
    public sealed class OutfitExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string outfitId = Str(dt, r, "outfitId");
                if (string.IsNullOrWhiteSpace(outfitId)) continue;
                if (dataList.ContainsKey(outfitId)) throw new Exception($"outfits primary key duplicated outfitId={outfitId}, row={r + 3}");

                dataList[outfitId] = new OutfitData
                {
                    outfitId = outfitId,
                    displayName = Str(dt, r, "displayName"),
                    price = UnityEngine.Mathf.Max(0, Int(dt, r, "price", 0))
                };
            }
        }
    }
}
