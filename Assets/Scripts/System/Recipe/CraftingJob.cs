using System;
using System.Collections.Generic;

public sealed class CraftingJob
{
    public string jobId;
    public string recipeId;
    public bool completed;
    public List<string> outputItems = new List<string>();
    public List<int> outputAmounts = new List<int>();

    public CraftingJob(string recipeId)
    {
        jobId = Guid.NewGuid().ToString("N");
        this.recipeId = recipeId;
    }
}
