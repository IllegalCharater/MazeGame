using System.Collections.Generic;
public static class DatabaseHelper
{
    public static Dictionary<string, BaseData> CreateDataMap(string rootKey)
    {
        var database = new BaseDatabase();
        database.Init(rootKey);
        database.LoadData();
        return database.dataList;
    }
}