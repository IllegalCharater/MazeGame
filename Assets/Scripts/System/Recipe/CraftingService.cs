using System.Collections.Generic;

public class CraftingService {
    private GameDatabase database;
    private PlayerDatabase player;
    private readonly CraftingQueue queue = new CraftingQueue();
    private readonly List<CraftingJob> history = new List<CraftingJob>();

    public void Initialize(GameDatabase database, PlayerDatabase player) {
        this.database = database;
        this.player = player;
    }

    public bool CanCraft(string recipeId) {
        RecipeData recipe = database?.Get<RecipeData>("recipes", recipeId);
        if (recipe == null || player?.Inventory == null)
            return false;

        return player.Inventory.HasAll(recipe.ingredients);
    }

    public CraftingJob StartCraft(string recipeId) {
        RecipeData recipe = database?.Get<RecipeData>("recipes", recipeId);
        if (recipe == null || player?.Inventory == null)
            return null;
        if (!player.Inventory.TryConsume(recipe.ingredients))
            return null;

        CraftingJob job = new CraftingJob(recipeId);
        if (recipe.outputItems != null)
            job.outputItems.AddRange(recipe.outputItems);
        if (recipe.outputAmounts != null)
            job.outputAmounts.AddRange(recipe.outputAmounts);

        queue.Add(job);
        GameEvents.RaiseCraftingChanged();
        return job;
    }

    public bool CompleteCraft(string jobId) {
        CraftingJob job = queue.Find(jobId);
        if (job == null || job.completed || player?.Inventory == null)
            return false;

        for (int i = 0; i < job.outputItems.Count; i++) {
            int amount = i < job.outputAmounts.Count ? job.outputAmounts[i] : 1;
            player.Inventory.TryAdd(job.outputItems[i], amount);
            player.Collections.Unlock(CollectionCategory.Food, job.outputItems[i]);
        }

        job.completed = true;
        history.Add(job);
        GameEvents.RaiseCraftingChanged();
        GameEvents.RaiseCollectionChanged(CollectionCategory.Food);
        return true;
    }

    public IReadOnlyList<CraftingJob> GetCraftHistory() {
        return history;
    }

    public IReadOnlyList<CraftingJob> GetQueue() {
        return queue.Jobs;
    }
}
