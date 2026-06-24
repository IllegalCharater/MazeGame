using UnityEngine;

/// <summary>
/// 从相机发射射线，在可交互 Layer 上检测碰撞体，对其上实现的 <see cref="IInteractable"/> 调用 Interact。
/// 不在 Update 里 GetComponent：仅在按下交互键时做一次遍历查找接口实现。
/// </summary>
[DisallowMultipleComponent]
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [Header("射线起点（为空则用玩家 Transform）")]
    [SerializeField] private Transform rayOrigin;
    [Tooltip("从 rayOrigin 的本地坐标偏移作为射线起点（例如胸口/眼睛高度）。")]
    [SerializeField] private Vector3 rayOriginLocalOffset = new Vector3(0f, 1.4f, 0f);
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool onlyAllowInMaze = true;

    private readonly Vector2[] dirs = new Vector2[4] {
        Vector2.left, // 左（-X）
        Vector2.right, // 右（+X）
        Vector2.up, // 上（+Y）
        Vector2.down // 下（-Y）
    };

    private void Reset()
    {
        viewCamera = GetComponentInChildren<Camera>();
        if (viewCamera == null)
            viewCamera = Camera.main;
    }

    private void Update()
    {
        if (onlyAllowInMaze && !IsInMaze())
            return;
        // 2D 项目：使用 Physics2D，方向固定为 X/Y 轴（左/右/上/下）
        // 起点：玩家本体的本地偏移
        Transform origin = transform;
        Vector3 originPos = origin.TransformPoint(rayOriginLocalOffset);
        Vector2 origin2 = originPos;
        if (!Input.GetKeyDown(interactKey))
            return;
        foreach (var dir in dirs)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin2, dir, interactDistance, interactableLayers);
            if (hit.collider != null)
            {
                IInteractable interactable = FindInteractable2D(hit.collider);
                interactable?.Interact(gameObject);
                break;
            }
        }
    }
    private void FixedUpdate()
    {
        // 2D 项目：使用 Physics2D，方向固定为 X/Y 轴（左/右/上/下）
        // 起点：玩家本体的本地偏移
        Transform origin = transform;
        Vector3 originPos = origin.TransformPoint(rayOriginLocalOffset);
        Vector2 origin2 = originPos;

#if UNITY_EDITOR
        foreach (var dir in dirs)
        {
            Debug.DrawLine((Vector3)origin2, (Vector3)origin2 + (Vector3)dir * interactDistance, Color.green);
        }
#endif
        
    }
    private static IInteractable FindInteractable2D(Collider2D col)
    {
        if (col == null)
            return null;

        foreach (MonoBehaviour mb in col.GetComponentsInParent<MonoBehaviour>(true))
        {
            if (mb is IInteractable i)
                return i;
        }

        foreach (MonoBehaviour mb in col.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb is IInteractable i)
                return i;
        }

        return null;
    }

    private static bool IsInMaze()
    {
        return GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.InMaze;
    }
}
