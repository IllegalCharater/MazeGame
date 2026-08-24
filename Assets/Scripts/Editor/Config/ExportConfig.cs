using System.Collections.Generic;
public static partial class ExcelToJsonExporter {
    /// <summary>单元格值的强制类型，用于覆盖 Auto 推断。</summary>
    public enum ColumnValueType { Auto, String, Int, Float, Bool, List, Dict, Json }

    /// <summary>个别列强制类型：rootKey → 列名 → 类型（默认空，全走 Auto 推断）。</summary>
    public static Dictionary<string, Dictionary<string, ColumnValueType>> ColumnTypeHints = new();

    /// <summary>true 则字符串值 Norm 小写（恢复旧导出行为）。</summary>
    public static bool LowercaseStrings = false;

    private static string getJsonPath(string jsonName) {
        return $"{OutDir}/{jsonName}.json";
    }
}
