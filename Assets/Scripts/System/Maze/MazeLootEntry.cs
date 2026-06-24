using System;
using UnityEngine;

[Serializable]
public sealed class MazeLootEntry
{
    public string itemId = "ingredient_carrot";
    public int minAmount = 1;
    public int maxAmount = 2;
    public int weight = 1;

    public int RollAmount(System.Random random)
    {
        minAmount = Mathf.Max(1, minAmount);
        maxAmount = Mathf.Max(minAmount, maxAmount);
        if (random == null)
            return minAmount;

        return random.Next(minAmount, maxAmount + 1);
    }
}
