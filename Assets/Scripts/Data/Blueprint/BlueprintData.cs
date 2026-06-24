using System.Collections.Generic;

[System.Serializable]
public class BlueprintData : BaseData
{
    public string blueprintId;
    public string displayName;
    public int price;
    public string buffId;
    public List<string> requirementIds;
}
