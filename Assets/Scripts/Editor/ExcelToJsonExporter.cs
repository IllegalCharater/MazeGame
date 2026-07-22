#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using ExcelDataReader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
public static partial class ExcelToJsonExporter
{
    public static string ExcelDir = DataConfig.ExcelDir;
    public static string OutDir = DataConfig.OutDir;
    public static string BaseJsonName = DataConfig.BaseJsonName;
    // public static string DatabasesDir = DataConfig.DatabasesDir;
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        Culture = CultureInfo.InvariantCulture
    };

    [MenuItem("MazeGame/Config/Export Excel To Json", priority = 119)]
    private static void Export()
    {
        try
        {
            GetExportStemMap();
            var excelPaths = EnumerateExcelFilesInExcelDir();
            if (excelPaths.Count == 0)
                Debug.LogWarning($"[ExcelExport] 目录下无 xlsx（已跳过 Excel 临时锁文件 ~$*.xlsx）：{ExcelDir}");

            EnsureDirectory(OutDir);

            foreach (var kv in ExportModules)
                kv.Value.Init(kv.Key);

            foreach (var absPath in excelPaths)
            {
                var ds = ReadExcel(absPath);
                if (ds.Tables.Count == 0)
                    continue;

                foreach (DataTable table in ds.Tables)
                {
                    string rootKey = GetSheetRootKey(table);
                    if (string.IsNullOrEmpty(rootKey))
                        continue;
                    if (!ExportModules.TryGetValue(rootKey, out var module))
                    {
                        Debug.LogWarning($"[ExcelExport] 未找到 rootKey 对应模块: {rootKey}（{Path.GetFileName(absPath)}）");
                        continue;
                    }
                    DataTable body = GetSheetBody(table);
                    module.Parse(body);
                }
            }
            foreach (var module in ExportModules.Values)
            {
                module.Validate();
                module.Write();
            }

            WriteBaseJsonManifest();
            AssetDatabase.Refresh();
            Debug.Log($"[ExcelExport] 导出成功，扫描 xlsx={excelPaths.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ExcelExport] 导出失败: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>遍历 ExcelDir 下所有 .xlsx（仅当前目录，不含子文件夹）。</summary>
    private static List<string> EnumerateExcelFilesInExcelDir()
    {
        string absDir = ToAbsolutePath(ExcelDir);
        var list = new List<string>();
        if (!Directory.Exists(absDir))
            return list;

        foreach (var f in Directory.GetFiles(absDir, "*.xlsx", SearchOption.TopDirectoryOnly))
        {
            if (Path.GetFileName(f).StartsWith("~$", StringComparison.Ordinal))
                continue;
            list.Add(f);
        }

        list.Sort(StringComparer.Ordinal);
        return list;
    }

    [MenuItem("MazeGame/Config/Refresh Excel Json Imports", priority = 120)]
    private static void RefreshExcelJsonImports()
    {
        // 1) 刷新资源，确保刚导出的 json 可被加载
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        // EnsureEditorFolder(DatabasesDir);
        // // 2) 删除旧 SO 并重建；3) 挂载对应 json
        // foreach (var (databaseType, jsonFileName) in DatabaseTypes)
        // {
        //     RecreateDatabaseSO(
        //         databaseType,
        //         $"{DatabasesDir}/{databaseType.Name}.asset",
        //         $"{OutDir}/{jsonFileName}.json");
        // }
        // AssetDatabase.SaveAssets();
        // AssetDatabase.Refresh();
        // Debug.Log("[ExcelExport] JSON 导入与 Database SO 重建完成。");
    }

    // ---------- 读取 ----------
    // private static DataTable ReadPrimaryTable(string tableName)
    // {
    //     // #region agent log
    //     AppendDebugLog(
    //         "H1",
    //         "ExcelToJsonExporter.ReadPrimaryTable:49",
    //         "开始读取主表",
    //         $"{{\"tableName\":\"{EscapeJson(tableName)}\"}}",
    //         "before-fix");
    //     // #endregion

    //     string excelAssetPath = $"{ExcelDir}/{tableName}.xlsx";
    //     string absExcel = ToAbsolutePath(excelAssetPath);
    //     // #region agent log
    //     AppendDebugLog(
    //         "H2",
    //         "ExcelToJsonExporter.ReadPrimaryTable:59",
    //         "Excel路径解析结果",
    //         $"{{\"excelAssetPath\":\"{EscapeJson(excelAssetPath)}\",\"absExcel\":\"{EscapeJson(absExcel)}\",\"exists\":{(File.Exists(absExcel) ? "true" : "false")}}}",
    //         "before-fix");
    //     // #endregion
    //     if (!File.Exists(absExcel))
    //         throw new Exception($"Excel 不存在: {excelAssetPath}");

    //     var ds = ReadExcel(absExcel);
    //     // #region agent log
    //     AppendDebugLog(
    //         "H3",
    //         "ExcelToJsonExporter.ReadPrimaryTable:70",
    //         "DataSet读取结果",
    //         $"{{\"tableName\":\"{EscapeJson(tableName)}\",\"tableCount\":{ds.Tables.Count},\"tableNames\":\"{EscapeJson(string.Join("|", GetTableNames(ds)))}\"}}",
    //         "before-fix");
    //     // #endregion
    //     if (ds.Tables.Contains(tableName))
    //         return ds.Tables[tableName];
    //     if (ds.Tables.Count > 0)
    //         return ds.Tables[0];

    //     throw new Exception($"Excel 无可用工作表: {excelAssetPath}");
    // }
    [MenuItem("MazeGame/Config/delete all Jsons", priority =121)]
    public static void deleteJsons()
    {
        string absDir = ToAbsolutePath(OutDir);
        if (!Directory.Exists(absDir))
        {
            Debug.LogWarning($"[ExcelExport] JSON 输出目录不存在，跳过删除: {OutDir}");
            return;
        }

        string[] jsonFiles = Directory.GetFiles(absDir, "*.json", SearchOption.TopDirectoryOnly);
        if (jsonFiles.Length == 0)
        {
            Debug.Log($"[ExcelExport] JSON 输出目录下没有可删除文件: {OutDir}");
            return;
        }

        int deletedCount = 0;
        var failedFiles = new List<string>();

        foreach (string absPath in jsonFiles)
        {
            string assetPath = $"{OutDir}/{Path.GetFileName(absPath)}".Replace('\\', '/');
            if (AssetDatabase.DeleteAsset(assetPath))
            {
                deletedCount++;
                continue;
            }

            try
            {
                File.Delete(absPath);
                string metaPath = $"{absPath}.meta";
                if (File.Exists(metaPath))
                    File.Delete(metaPath);
                deletedCount++;
            }
            catch (Exception ex)
            {
                failedFiles.Add($"{assetPath}: {ex.Message}");
            }
        }

        ExportJsonStemMap.Clear();
        AssetDatabase.Refresh();

        if (failedFiles.Count > 0)
            Debug.LogWarning($"[ExcelExport] 删除 JSON 完成，成功 {deletedCount} 个，失败 {failedFiles.Count} 个:\n{string.Join("\n", failedFiles)}");
        else
            Debug.Log($"[ExcelExport] 已删除 {deletedCount} 个 JSON 文件: {OutDir}");
    }
    private static DataSet ReadExcel(string absPath)
    {
        if (!Path.GetExtension(absPath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new Exception($"仅支持 .xlsx 文件: {absPath}");

        using var stream = File.Open(absPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var ds = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            // 导表数据最终都会按字符串/显式转换处理，关闭类型推断可减少一次遍历开销。
            UseColumnDataType = false,
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                // 第 1 行：A1 为 JSON rootKey；第 2 行：字段名；第 3 行起：数据（由 ExcelExportModule 规范化）
                UseHeaderRow = false
            }
        });

        // #region agent log
        AppendDebugLog(
            "H4",
            "ExcelToJsonExporter.ReadExcel",
            "ExcelDataReader读取结果",
            $"{{\"absPath\":\"{EscapeJson(absPath)}\",\"tableCount\":{ds.Tables.Count},\"tableNames\":\"{EscapeJson(string.Join("|", GetTableNames(ds)))}\"}}",
            "exceldatareader");
        // #endregion

        return ds;
    }

    /// <summary>
    /// 读取工作表中单元格 <b>A1</b> 的文本。
    /// ExcelDataReader 映射为 <see cref="DataTable"/>：行索引 0、列索引 0 对应 A1。
    /// </summary>
    /// <param name="sheet">已由 ReadExcel 等工作表读出的表；为空或无行列时返回空串。</param>
    private static string GetSheetRootKey(DataTable sheet)
    {
        if (sheet == null || sheet.Rows.Count == 0 || sheet.Columns.Count == 0)
            return string.Empty;

        object cell = sheet.Rows[0][0];
        return cell == null ? string.Empty : cell.ToString()?.Trim() ?? string.Empty;
    }
    /// <summary>
    /// 表结构约定：第 1 行为 meta（A1 为 rootKey）；第 2 行为字段名；第 3 行起为数据。
    /// 返回仅含「列名 + 数据行」的新表：列名来自原表第 2 行，行 0 对应 Excel 第 3 行。
    /// 若某数据行<strong>第一列</strong>为 END（忽略大小写），则该行及之后不再导入。
    /// </summary>
    private static DataTable GetSheetBody(DataTable sheet)
    {
        if (sheet == null)
            throw new ArgumentNullException(nameof(sheet));
        if (sheet.Rows.Count < 2)
            return new DataTable();

        var headerRow = sheet.Rows[1];
        var columnNames = new List<string>();
        var used = new HashSet<string>(StringComparer.Ordinal);

        for (int c = 0; c < sheet.Columns.Count; c++)
        {
            string baseName = headerRow[c]?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(baseName))
                baseName = $"col_{c}";

            string unique = baseName;
            for (int i = 1; used.Contains(unique); i++)
                unique = $"{baseName}_{i}";
            used.Add(unique);
            columnNames.Add(unique);
        }

        var body = new DataTable();
        foreach (var cn in columnNames)
            body.Columns.Add(cn, typeof(object));

        for (int r = 2; r < sheet.Rows.Count; r++)
        {
            string firstCell = sheet.Rows[r][0]?.ToString()?.Trim() ?? string.Empty;
            if (string.Equals(firstCell, "END", StringComparison.OrdinalIgnoreCase))
                break;

            var row = body.NewRow();
            for (int c = 0; c < columnNames.Count; c++)
                row[c] = sheet.Rows[r][c];
            body.Rows.Add(row);
        }

        return body;
    }

    // private static void RecreateDatabaseSO(Type databaseType, string soPath, string jsonPath)
    // {
    //     if (databaseType == null || !typeof(BaseDatabase).IsAssignableFrom(databaseType))
    //         throw new ArgumentException("databaseType 必须继承 BaseDatabase", nameof(databaseType));

    //     var old = AssetDatabase.LoadAssetAtPath(soPath, databaseType);
    //     if (old != null)
    //         AssetDatabase.DeleteAsset(soPath);

    //     var db = ScriptableObject.CreateInstance(databaseType) as BaseDatabase;
    //     if (db == null)
    //         throw new InvalidOperationException($"无法创建 Database 实例: {databaseType.Name}");

    //     db.JsonSourceAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);

    //     if (db.JsonSourceAsset == null)
    //         Debug.LogWarning($"[ExcelExport] 未找到 JSON：{jsonPath}");

    //     AssetDatabase.CreateAsset(db, soPath);
    //     EditorUtility.SetDirty(db);
    // }

    // private static void EnsureEditorFolder(string folderPath)
    // {
    //     if (AssetDatabase.IsValidFolder(folderPath)) return;

    //     string[] segments = folderPath.Split('/');
    //     string current = segments[0];
    //     for (int i = 1; i < segments.Length; i++)
    //     {
    //         string next = $"{current}/{segments[i]}";
    //         if (!AssetDatabase.IsValidFolder(next))
    //             AssetDatabase.CreateFolder(current, segments[i]);
    //         current = next;
    //     }
    // }

/// <summary>逻辑名（如 items）→ Resources 加载用文件名（无扩展名，含唯一后缀）</summary>
    private static Dictionary<string, string> ExportJsonStemMap = new Dictionary<string, string>();

    /// <summary>供 ExcelExportModule.Write 登记本次导出生成的 json 资源名（不含 .json）。</summary>
    private static void RegisterExportMapping(string logicalName, string resourceStemWithoutExtension)
    {
        if (string.IsNullOrEmpty(logicalName))
            return;
        ExportJsonStemMap[logicalName] = resourceStemWithoutExtension;
    }

    /// <summary>
    /// 从 <c>base.json</c> 载入逻辑名 → stem；若无文件则创建空的 <c>base.json</c>。
    /// </summary>
    private static void GetExportStemMap()
    {
        ExportJsonStemMap.Clear();
        EnsureDirectory(OutDir);

        string assetPath = $"{OutDir}/{BaseJsonName}.json";
        string abs = ToAbsolutePath(assetPath);

        if (!File.Exists(abs))
        {
            WriteJson(assetPath, ExportJsonStemMap);
            return;
        }

        try
        {
            string json = File.ReadAllText(abs, Encoding.UTF8);
            var root = JObject.Parse(json);
            foreach (var prop in root.Properties())
            {
                if (string.IsNullOrEmpty(prop.Name))
                    continue;
                string v = prop.Value.Type == JTokenType.String
                    ? prop.Value.Value<string>()?.Trim()
                    : prop.Value.ToString()?.Trim();
                if (!string.IsNullOrEmpty(v))
                    ExportJsonStemMap[prop.Name] = v;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ExcelExport] {BaseJsonName}.json 解析失败，使用空映射：{ex.Message}");
            ExportJsonStemMap.Clear();
        }
    }
    /// <summary>写入 BaseJson，运行时 BaseDatabase 据此 Resources.Load 实际文件。</summary>
    private static void WriteBaseJsonManifest()
    {
        WriteJson($"{OutDir}/{BaseJsonName}.json", ExportJsonStemMap);
    }
    #region 工具（供 Modules 中 Parse 使用）
    public static string Str(DataTable dt, int row, string col)
    {
        object v = dt.Rows[row][col];
        var res= v == null ? string.Empty : v.ToString()?.Trim() ?? string.Empty;
        return Norm(res);
    }

    public static int Int(DataTable dt, int row, string col, int def)
    {
        string s = Str(dt, row, col);
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : def;
    }

    public static float Float(DataTable dt, int row, string col, float def)
    {
        string s = Str(dt, row, col);
        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : def;
    }

    public static Dictionary<string, string> ParseDictionary(DataTable dt, int row, string col)
    {
        string s = Str(dt, row, col);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(s))
            return result;

        // JSON 对象格式：{"item_gold":"10","item_potion_red":"10"}
        var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
        return jsonDict;

    }

    public static Dictionary<string, int> ParseIntDictionary(DataTable dt, int row, string col)
    {
        Dictionary<string, string> raw = ParseDictionary(dt, row, col);
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var kv in raw)
        {
            if (string.IsNullOrWhiteSpace(kv.Key))
                continue;
            if (!int.TryParse(kv.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                continue;
            if (value <= 0)
                continue;

            result[Norm(kv.Key)] = value;
        }

        return result;
    }

    public static bool Bool(DataTable dt, int row, string col, bool def)
    {
        string s = Str(dt, row, col);
        if (string.IsNullOrWhiteSpace(s))
            return def;
        if (bool.TryParse(s, out bool b))
            return b;
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
            return i != 0;

        return s == "yes" || s == "y" || s == "true" || s == "是";
    }
    /// <summary>
    /// 解析列表单元格：支持标准 JSON 数组 <c>["a","b"]</c>，以及 <c>[a,b]</c> 无引号形式（逗号分隔，自动 Trim）。
    /// </summary>
    public static List<string> ParseList(DataTable dt, int row, string col)
    {
        string s = Str(dt, row, col);
        if (string.IsNullOrWhiteSpace(s))
            return new List<string>();

        s = s.Trim();
        if (s.Length >= 2 && s[0] == '[' && s[s.Length - 1] == ']')
        {
            try
            {
                var fromJson = JsonConvert.DeserializeObject<List<string>>(s);
                if (fromJson != null)
                    return fromJson;
            }
            catch (JsonException)
            {
                // 非 JSON（如 [item_a,item_b]），去掉括号后按逗号切分
            }

            s = s.Substring(1, s.Length - 2).Trim();
        }

        var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var list = new List<string>(parts.Length);
        foreach (var p in parts)
        {
            var t = p.Trim();
            if (t.Length > 0)
                list.Add(t);
        }
        return list;
    }

    public static string Norm(string raw) => string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim().ToLowerInvariant();

    public static void EnsureDirectory(string assetDir)
    {
        string abs = ToAbsolutePath(assetDir);
        if (!Directory.Exists(abs)) Directory.CreateDirectory(abs);
    }

    public static void WriteJson(string assetPath, object obj)
    {
        string abs = ToAbsolutePath(assetPath);
        string json = JsonConvert.SerializeObject(obj, JsonSettings);
        File.WriteAllText(abs, json, new UTF8Encoding(false));
    }

    public static string ToAbsolutePath(string unityAssetPath)
    {
        string rel = unityAssetPath.Replace("Assets", "").TrimStart('/', '\\');
        return Path.Combine(Application.dataPath, rel);
    }

    private static T[] ToArray<T>(IEnumerable<T> source)
    {
        if (source is T[] arr) return arr;
        return new List<T>(source).ToArray();
    }

    private static string[] GetTableNames(DataSet ds)
    {
        var list = new List<string>(ds.Tables.Count);
        foreach (DataTable table in ds.Tables) list.Add(table.TableName);
        return list.ToArray();
    }

    private static void AppendDebugLog(string hypothesisId, string location, string message, string dataJson, string runId)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string logPath = Path.Combine(projectRoot, "debug-fbc7e3.log");
        string line = $"{{\"sessionId\":\"fbc7e3\",\"runId\":\"{EscapeJson(runId)}\",\"hypothesisId\":\"{EscapeJson(hypothesisId)}\",\"location\":\"{EscapeJson(location)}\",\"message\":\"{EscapeJson(message)}\",\"data\":{dataJson},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}";
        File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
    }

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
    }
    #endregion


}

#endif

