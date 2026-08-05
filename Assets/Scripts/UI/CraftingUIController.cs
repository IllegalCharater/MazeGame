public sealed class CraftingUIController : BaseUIController {
    private string defaultRecipeId = "recipe1";

    public void StartDefaultRecipe() {
        if (GameManager.Instance?.services?.Crafting != null)
            GameManager.Instance.services.Crafting.StartCraft(defaultRecipeId);
    }

    public void CompleteFirstQueuedJob() {
        var queue = GameManager.Instance?.services?.Crafting.GetQueue();
        if (queue == null || queue.Count == 0)
            return;

        GameManager.Instance.services.Crafting.CompleteCraft(queue[0].jobId);
    }
}
