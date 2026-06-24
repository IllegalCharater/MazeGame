using System;
using System.Collections.Generic;
using UnityEngine;

public class RecipeData : BaseData
{
    public string recipeId;
    public string displayName;
    public List<string> outputItems;
    public List<int> outputAmounts;
    public Dictionary<string, int> ingredients;
}
