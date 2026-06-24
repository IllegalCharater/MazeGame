using System.Collections.Generic;

public sealed class CraftingQueue
{
    private readonly List<CraftingJob> jobs = new List<CraftingJob>();

    public IReadOnlyList<CraftingJob> Jobs => jobs;

    public void Add(CraftingJob job)
    {
        if (job != null)
            jobs.Add(job);
    }

    public CraftingJob Find(string jobId)
    {
        return jobs.Find(job => job.jobId == jobId);
    }
}
