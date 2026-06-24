using System.Collections.Generic;
using UnityEngine;
using System;
using System.Data;

public partial class ExcelToJsonExporter
{
    public sealed class ItemExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string itemId =Str(dt, r, "itemId");
                if (string.IsNullOrWhiteSpace(itemId)) continue;
                if (dataList.ContainsKey(itemId)) throw new Exception($"items 主键重复 itemId={itemId}, row={r + 3}（Excel 行号，含 meta+表头）");

                dataList[itemId] = new ItemData
                {
                    itemId = itemId,
                    name = Str(dt, r, "name"),
                    price = Mathf.Max(0, Int(dt, r, "price", 0))
                };
            }
            
        }
    }
}


