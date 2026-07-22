public sealed class CraftingUIController : BaseUIController
{
    private string defaultRecipeId = "recipe1";

    public void StartDefaultRecipe()
    {
        if (GameManager.Instance?.Services?.Crafting != null)
            GameManager.Instance.Services.Crafting.StartCraft(defaultRecipeId);
    }

    public void CompleteFirstQueuedJob()
    {
        var queue = GameManager.Instance?.Services?.Crafting.GetQueue();
        if (queue == null || queue.Count == 0)
            return;

        GameManager.Instance.Services.Crafting.CompleteCraft(queue[0].jobId);
    }
}
