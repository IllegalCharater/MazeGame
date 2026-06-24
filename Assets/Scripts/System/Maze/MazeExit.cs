using UnityEngine;

[DisallowMultipleComponent]
public sealed class MazeExit : MonoBehaviour, IInteractable
{
    [SerializeField] private bool allowInteractKey = true;
    [SerializeField] private bool autoTriggerOnEnter = true;
    [SerializeField] private bool returnToShopOnExit;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool logTransition;

    private bool hasTriggered;

    public void Interact(GameObject interactor)
    {
        if (!allowInteractKey || !IsValidPlayer(interactor))
            return;

        TryExit();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoTriggerOnEnter || !IsValidPlayer(other != null ? other.gameObject : null))
            return;

        TryExit();
    }

    private bool IsValidPlayer(GameObject go)
    {
        if (go == null)
            return false;
        if (string.IsNullOrWhiteSpace(playerTag))
            return true;
        if (go.CompareTag(playerTag))
            return true;

        return go.name == playerTag;
    }

    private void TryExit()
    {
        if (hasTriggered)
            return;
        hasTriggered = true;

        if (GameManager.Instance != null && GameManager.Instance.Services?.MazeRun != null)
            GameManager.Instance.Services.MazeRun.CompleteRun();

        if (returnToShopOnExit && SceneFlow.Instance != null)
        {
            SceneFlow.Instance.GoToShop();
            if (logTransition)
                Debug.Log("[MazeExit] Completed maze run and requested shop transition.");
            return;
        }

        if (logTransition)
            Debug.Log("[MazeExit] Completed maze run.");
    }
}
