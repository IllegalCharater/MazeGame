using UnityEngine;

/// <summary>
/// 可交互物体统一接口。用接口而非具体基类，便于拾取物、门、NPC 各自继承 MonoBehaviour 并实现本接口。
/// Unity 无法对接口直接 GetComponent，因此由 <see cref="PlayerInteractor"/> 在命中碰撞体上遍历 MonoBehaviour 并做 is 判断。
/// </summary>
public interface IInteractable
{
    /// <summary>玩家按下交互键且射线命中该物体时调用。</summary>
    /// <param name="interactor">发起交互的玩家根物体（便于传递引用）。</param>
    void Interact(GameObject interactor);
}
