using System;
using System.Data;

public partial class ExcelToJsonExporter
{
    public sealed class BuffExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string buffId = Str(dt, r, "buffId");
                if (string.IsNullOrWhiteSpace(buffId)) continue;
                if (dataList.ContainsKey(buffId)) throw new Exception($"buffs primary key duplicated buffId={buffId}, row={r + 3}");

                dataList[buffId] = new BuffData
                {
                    buffId = buffId,
                    displayName = Str(dt, r, "displayName"),
                    category = ParseBuffCategory(Str(dt, r, "category")),
                    durationSeconds = UnityEngine.Mathf.Max(0f, Float(dt, r, "durationSeconds", 0f)),
                    value = Float(dt, r, "value", 0f)
                };
            }
        }

        private BuffCategory ParseBuffCategory(string raw)
        {
            if (Enum.TryParse(raw, true, out BuffCategory category))
                return category;

            return BuffCategory.Maze;
        }
    }
}
