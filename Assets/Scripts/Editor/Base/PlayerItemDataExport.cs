using System.Collections.Generic;
using UnityEngine;
using System;
using System.Data;
using System.Linq;

public partial class ExcelToJsonExporter
{
    public sealed class PlayerStartExportModule : ExcelExportModule
    {
        protected override void ParseDatas(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string playerId = Str(dt, r, "playerId");
                string playerDisplayName=Str(dt, r, "playerDisplayName");
                int level=Int(dt, r, "level",0);
                int currency=Int(dt, r, "currency",0);
                int energy=Int(dt, r, "energy",0);
                
                if (string.IsNullOrWhiteSpace(playerId)) continue;
                if (dataList.ContainsKey(playerId)) throw new Exception($"players 主键重复 playerId={playerId}, row={r + 3}（Excel 行号，含 meta+表头）");
                dataList[playerId] = new PlayerStartData
                {
                    profile= new PlayerProfile
                    {
                        playerId = playerId,
                        playerDisplayName = playerDisplayName,
                        level = level,
                        currency = currency,
                        energy = energy,
                    },
                    items = ParseDictionary(dt, r, "items").ToDictionary(
                        kv => kv.Key, kv => Mathf.Max(0, int.Parse(kv.Value))
                    )
                };
            }
        }
    }
}
