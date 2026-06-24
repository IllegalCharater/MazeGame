using System.Collections.Generic;

[System.Serializable]
public class FoodData : BaseData
{
    public string foodId;
    public string displayName;
    public int price;
    public int energyRestore;
    public List<string> ingredientItems;
    public List<int> ingredientAmounts;
}
