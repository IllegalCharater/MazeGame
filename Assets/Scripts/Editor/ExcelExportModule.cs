using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static partial class ExcelToJsonExporter {
    /// <summary>
    /// 通用导出模块：不依赖预定义数据类，直接按工作表列名把每一行转成
    /// 「列名 → 推断类型的值」的字典并序列化为 JSON。
    /// </summary>
    public class ExcelExportModule {
        /// <summary>最近一次成功并入的表 A1（与 SheetName 一致）。</summary>
        public string SheetRootKey { get; set; } = string.Empty;

        /// <summary>主键 → 行数据（每行为 列名→值 的字典）。</summary>
        public Dictionary<string, object> dataList { get; protected set; } = new();

        public void Init(string logicalRootKey) {
            dataList.Clear();
            SheetRootKey = logicalRootKey ?? string.Empty;
        }

        public virtual void Parse(DataTable dt) {
            ParseDatas(dt);
        }

        /// <summary>
        /// 通用解析：第 1 列为主键（重复抛错）；每行按列名直接生成字典。
        /// 个别表如需特殊处理可 override 此方法。
        /// </summary>
        protected virtual void ParseDatas(DataTable dt) {
            if (dt == null || dt.Columns.Count == 0 || dt.Rows.Count == 0)
                return;

            for (int r = 0; r < dt.Rows.Count; r++) {
                string key = Norm(dt.Rows[r][0]?.ToString() ?? string.Empty);
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                var row = new Dictionary<string, object>();
                foreach (DataColumn c in dt.Columns) {
                    object v = ConvertCell(dt, r, c.ColumnName);
                    if (v != null)
                        row[c.ColumnName] = v;
                }
                AddRow(key, row, r + 3);
            }
        }

        /// <summary>集中的主键判空/重复检查（Excel 行号含 meta+表头）。</summary>
        protected void AddRow(string key, object row, int excelRow) {
            if (string.IsNullOrWhiteSpace(key))
                return;
            if (dataList.ContainsKey(key))
                throw new Exception($"{SheetRootKey} 主键重复 {key}, row={excelRow}（Excel 行号，含 meta+表头）");
            dataList[key] = row;
        }

        public virtual void Validate() {
            if (dataList.Count == 0)
                Debug.LogError($"[ExcelExport] {SheetRootKey} 数据为空");
        }

        public virtual void Write() {
            if (string.IsNullOrEmpty(SheetRootKey)) {
                Debug.LogWarning("[ExcelExport] Write 已跳过：SheetRootKey 为空（请勿在未绑定模块的情况下导出）");
                return;
            }

            // 优先复用 GetExportStemMap 已载入的 stem；否则新建 GUID 后缀
            if (!ExportJsonStemMap.TryGetValue(SheetRootKey, out var uniqueStem) || string.IsNullOrEmpty(uniqueStem))
                uniqueStem = $"{SheetRootKey}_{Guid.NewGuid():N}";

            var payload = new Dictionary<string, object> {
                [SheetRootKey] = new Dictionary<string, object>(dataList)
            };
            WriteJson(getJsonPath(uniqueStem), payload);
            RegisterExportMapping(SheetRootKey, uniqueStem);
        }

        // ---------- 单元格类型推断 ----------

        /// <summary>
        /// 把单元格文本推断为 JSON 值：空→null(省略)；[..]→数组；{..}→对象；bool/int/float；其余→string。
        /// 可先用 <see cref="ColumnTypeHints"/> 按列强制类型。
        /// </summary>
        private object ConvertCell(DataTable dt, int row, string col) {
            string raw = dt.Rows[row][col]?.ToString()?.Trim() ?? string.Empty;
            if (raw.Length == 0)
                return null;

            if (ColumnTypeHints.TryGetValue(SheetRootKey, out var hints)
                && hints.TryGetValue(col, out var hint)
                && hint != ColumnValueType.Auto)
                return ConvertHinted(dt, row, col, hint, raw);

            if (raw[0] == '[' && raw[raw.Length - 1] == ']') {
                try {
                    return JArray.Parse(raw);
                }
                catch (JsonException) {
                    // return BuildStringArray(raw.Substring(1, raw.Length - 2));   // [a,b] 无引号数组
                }
            }
            if (raw[0] == '{' && raw[raw.Length - 1] == '}') {
                try {
                    return JObject.Parse(raw);
                }
                catch (JsonException) {
                    Debug.LogWarning($"[ExcelExport] {SheetRootKey}.{col} 列 '{raw}' 不是合法 JSON 对象，按字符串输出");
                    return RawString(raw);
                }
            }

            if (bool.TryParse(raw, out bool b))
                return b;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                return i;
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                return f;

            return RawString(raw);
        }

        private object ConvertHinted(DataTable dt, int row, string col, ColumnValueType hint, string raw) {
            switch (hint) {
                case ColumnValueType.String:
                    return RawString(raw);
                case ColumnValueType.Int:
                    return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)
                        ? i : RawString(raw);
                case ColumnValueType.Float:
                    return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)
                        ? f : RawString(raw);
                case ColumnValueType.Bool:
                    return Bool(dt, row, col, false);
                case ColumnValueType.List:
                    return new JArray(ParseList(dt, row, col));
                case ColumnValueType.Dict:
                    try {
                        return JObject.FromObject(ParseDictionary(dt, row, col));
                    }
                    catch (JsonException) {
                        Debug.LogWarning($"[ExcelExport] {SheetRootKey}.{col} 列 '{raw}' 无法按 Dict 解析");
                        return RawString(raw);
                    }
                case ColumnValueType.Json:
                    return JToken.Parse(raw);
                default:
                    return RawString(raw);
            }
        }

        /// <summary>按逗号切分（兼容 [item_a,item_b] 无引号数组）。</summary>
        // private static JArray BuildStringArray(string inner) {
        //     var arr = new JArray();
        //     foreach (string p in inner.Split(',')) {
        //         string t = p.Trim();
        //         if (t.Length > 0)
        //             arr.Add(t);
        //     }
        //     return arr;
        // }

        /// <summary>字符串输出（可按 <see cref="LowercaseStrings"/> 恢复旧的 Norm 小写行为）。</summary>
        private static string RawString(string raw) => LowercaseStrings ? Norm(raw) : raw;
    }

    #region 工具（供 Modules 中 Parse 使用）
    public static string Str(DataTable dt, int row, string col) {
        object v = dt.Rows[row][col];
        var res = v == null ? string.Empty : v.ToString()?.Trim() ?? string.Empty;
        return Norm(res);
    }

    public static int Int(DataTable dt, int row, string col, int def) {
        string s = Str(dt, row, col);
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : def;
    }

    public static float Float(DataTable dt, int row, string col, float def) {
        string s = Str(dt, row, col);
        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : def;
    }

    public static Dictionary<string, string> ParseDictionary(DataTable dt, int row, string col) {
        string s = Str(dt, row, col);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(s))
            return result;

        // JSON 对象格式：{"item_gold":"10","item_potion_red":"10"}
        var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
        return jsonDict;

    }

    public static Dictionary<string, int> ParseIntDictionary(DataTable dt, int row, string col) {
        Dictionary<string, string> raw = ParseDictionary(dt, row, col);
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var kv in raw) {
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

    public static bool Bool(DataTable dt, int row, string col, bool def) {
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
    /// 解析列表单元格：支持标准 JSON 数组 <c>["a","b"]</c>
    /// </summary>
    public static List<string> ParseList(DataTable dt, int row, string col) {
        string s = Str(dt, row, col);
        if (string.IsNullOrWhiteSpace(s))
            return new List<string>();

        s = s.Trim();
        if (s.Length >= 2 && s[0] == '[' && s[s.Length - 1] == ']') {
            try {
                var fromJson = JsonConvert.DeserializeObject<List<string>>(s);
                if (fromJson != null)
                    return fromJson;
            }
            catch (JsonException) {
                // 非 JSON
            }

            s = s.Substring(1, s.Length - 2).Trim();
        }

        var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var list = new List<string>(parts.Length);
        foreach (var p in parts) {
            var t = p.Trim();
            if (t.Length > 0)
                list.Add(t);
        }
        return list;
    }

    public static string Norm(string raw) => string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim().ToLowerInvariant();

    public static void EnsureDirectory(string assetDir) {
        string abs = ToAbsolutePath(assetDir);
        if (!Directory.Exists(abs)) Directory.CreateDirectory(abs);
    }

    public static void WriteJson(string assetPath, object obj) {
        string abs = ToAbsolutePath(assetPath);
        string json = JsonConvert.SerializeObject(obj, JsonSettings);
        File.WriteAllText(abs, json, new UTF8Encoding(false));
    }

    public static string ToAbsolutePath(string unityAssetPath) {
        string rel = unityAssetPath.Replace("Assets", "").TrimStart('/', '\\');
        return Path.Combine(Application.dataPath, rel);
    }

    // private static T[] ToArray<T>(IEnumerable<T> source) {
    //     if (source is T[] arr) return arr;
    //     return new List<T>(source).ToArray();
    // }

    private static string[] GetTableNames(DataSet ds) {
        var list = new List<string>(ds.Tables.Count);
        foreach (DataTable table in ds.Tables) list.Add(table.TableName);
        return list.ToArray();
    }

    private static void AppendDebugLog(string hypothesisId, string location, string message, string dataJson, string runId) {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string logPath = Path.Combine(projectRoot, "debug-fbc7e3.log");
        string line = $"{{\"sessionId\":\"fbc7e3\",\"runId\":\"{EscapeJson(runId)}\",\"hypothesisId\":\"{EscapeJson(hypothesisId)}\",\"location\":\"{EscapeJson(location)}\",\"message\":\"{EscapeJson(message)}\",\"data\":{dataJson},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}";
        File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
    }

    private static string EscapeJson(string s) {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
    }
    #endregion


}
