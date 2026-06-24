using UnityEngine;

[DisallowMultipleComponent]
public sealed class MazeHazard : MonoBehaviour
{
    [SerializeField] private int energyCost = 2;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string playerTag = "Player";

    private bool triggered;

    public void Configure(int energyCost, bool triggerOnce = true)
    {
        this.energyCost = Mathf.Max(1, energyCost);
        this.triggerOnce = triggerOnce;
        triggered = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && triggered)
            return;
        if (!IsValidPlayer(other != null ? other.gameObject : null))
            return;

        triggered = true;
        if (GameManager.Instance?.Services?.MazeRun != null)
            GameManager.Instance.Services.MazeRun.ConsumeActionEnergy(Mathf.Max(1, energyCost));
    }

    private bool IsValidPlayer(GameObject go)
    {
        if (go == null)
            return false;
        if (string.IsNullOrWhiteSpace(playerTag))
            return true;
        return go.CompareTag(playerTag) || go.name == playerTag;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        energyCost = Mathf.Max(1, energyCost);
    }
#endif
}
