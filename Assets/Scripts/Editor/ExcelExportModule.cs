using System;
using System.Collections.Generic;
using UnityEngine;
using System.Data;

public partial class ExcelToJsonExporter
{
    public abstract class ExcelExportModule
    {
        /// <summary>最近一次成功并入的表 A1（与 SheetName 一致）。</summary>
        public string SheetRootKey { get; set; } = string.Empty;

        public Dictionary<string, BaseData> dataList { get; protected set; } = new();

        public void Init(string logicalRootKey)
        {
            dataList.Clear();
            SheetRootKey = logicalRootKey ?? string.Empty;
        }

        public virtual void Parse(DataTable dt)
        {
            ParseDatas(dt);
        }
        public virtual void Validate()
        {
            if (dataList.Count == 0)
                Debug.LogError($"[ExcelExport] {SheetRootKey} 数据为空");
        }
        public virtual void Write()
        {
            if (string.IsNullOrEmpty(SheetRootKey))
            {
                Debug.LogWarning("[ExcelExport] Write 已跳过：SheetRootKey 为空（请勿在未绑定模块的情况下导出）");
                return;
            }

            // 优先复用 GetExportStemMap 已载入的 stem；否则新建 GUID 后缀
            if (!ExportJsonStemMap.TryGetValue(SheetRootKey, out var uniqueStem) || string.IsNullOrEmpty(uniqueStem))
                uniqueStem = $"{SheetRootKey}_{Guid.NewGuid():N}";

            var payload = new Dictionary<string, object>
            {
                [SheetRootKey] = new Dictionary<string, BaseData>(dataList)
            };
            WriteJson($"{OutDir}/{uniqueStem}.json", payload);
            RegisterExportMapping(SheetRootKey, uniqueStem);
        }

        protected abstract void ParseDatas(DataTable dt);
        
    }
}