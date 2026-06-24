using UnityEngine;

/// <summary>
/// 验收用：挂到带 Collider 的物体上，Layer 设为可交互层，玩家对准按 E 应在 Console 打印。
/// </summary>
public sealed class InteractableTest : MonoBehaviour, IInteractable
{
    [SerializeField] private string message = "交互成功（InteractableTest）";

    public void Interact(GameObject interactor)
    {
        Debug.Log($"[InteractableTest] {message} ← 来自 {interactor.name}");
    }
}
