using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public readonly partial struct EventType {
    public static EventType OnBeginDragEvent = Next();
    public static EventType OnDragEvent = Next();
    public static EventType OnEndDragEvent = Next();
}

public class IDragable : MonoBehaviour, Interactable, IBeginDragHandler, IDragHandler, IEndDragHandler {
    public bool restrictToParent = false; // 是否限制在父级内
    public float snapRadius = 100f;        // 吸附半径：松手时离最近的吸附点小于该值才吸附，否则回弹
    public Transform snapContainer;        // 吸附点所在容器；为空则默认在当前父级下查找

    private RectTransform rectTransform;
    private Vector2 originalLocalPointerPosition;
    private Vector2 homePosition;          // 本次拖拽起点（松手未吸附时回弹到此）
    private Vector2 orignPositon;   // 原点世界坐标快照（Init 时记录，用于与槽位做距离比较）
    private List<RectTransform> _points = new();

    public void Init(DataBag options = null) {
        rectTransform = GetComponent<RectTransform>();
        homePosition = rectTransform.anchoredPosition;
        // 固定记录"原本的位置"：吸附点比较的是这个快照，而不是会跟着拖拽移动的 rectTransform 本身
        orignPositon = rectTransform.anchoredPosition;
        if (options != null) {
            _points = options.Get("points", _points);
            restrictToParent = options.Get("restrictToParent", restrictToParent);
        }
    }
    public void Dispose() {
        _points.Clear();
    }
    public void OnBeginDrag(PointerEventData eventData) {
        // 确保射线被阻断，防止拖拽时穿透下层UI触发点击
        // 可以通过 CanvasGroup 或 EventSystem 的点击阻挡处理
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out originalLocalPointerPosition);

        // 记录拖拽起点：松手未吸附到任何吸附点时回弹到这里
        homePosition = rectTransform.anchoredPosition;
        // 拖拽期间把自身置顶，避免被兄弟节点遮挡
        rectTransform.SetAsLastSibling();

        GameContext.DispatchEvent(EventType.OnBeginDragEvent);
    }

    public void OnDrag(PointerEventData eventData) {
        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out localPointerPosition)) {
            Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
            Vector2 newPos = rectTransform.anchoredPosition + (Vector2)offsetToOriginal;

            // 边界限制逻辑：把拖拽位置夹在父节点矩形内，保证物品不超出父级
            if (restrictToParent && rectTransform.parent != null && rectTransform.parent is RectTransform parentRectTransform) {
                Rect parentRect = parentRectTransform.rect; // 父节点本地坐标矩形（以自身 pivot 为原点）
                Rect itemRect = rectTransform.rect;         // 自身本地矩形（以 pivot 为原点，pivot 即 anchoredPosition 所在点）

                // 可移动范围 = 父矩形边界 - 自身尺寸；仅适用于子物体锚点相同的常规布局
                float minX = parentRect.xMin - itemRect.xMin;
                float maxX = parentRect.xMax - itemRect.xMax;
                float minY = parentRect.yMin - itemRect.yMin;
                float maxY = parentRect.yMax - itemRect.yMax;

                // 物品比父节点还大时保证 min <= max，避免 Clamp 翻转
                if (minX > maxX) minX = maxX = (minX + maxX) * 0.5f;
                if (minY > maxY) minY = maxY = (minY + maxY) * 0.5f;

                newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
                newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
            }

            rectTransform.anchoredPosition = newPos;
        }
        GameContext.DispatchEvent(EventType.OnDragEvent);
    }

    public void OnEndDrag(PointerEventData eventData) {
        //结束时自动吸附最近的位置
        RectTransform nearest = FindNearestSnapPoint(out bool snapToOrigin);
        if (nearest != null) {
            // 槽位可能挂在嵌套容器（如 Slots）下，父级不同直接赋 anchoredPosition 会偏移；
            // 改为把槽位的世界坐标换算成物品父节点坐标再对齐
            rectTransform.anchoredPosition = WorldToAnchoredPosition(nearest.position);
        }
        else if (snapToOrigin) {
            // 离原点最近：吸回原本的位置
            rectTransform.anchoredPosition = orignPositon;
        }
        else {
            // 未命中任何吸附点，回弹到拖拽起点
            rectTransform.anchoredPosition = homePosition;
        }

        // 把"哪个物品放到了哪个槽位"传给业务层（如 MazePuzzleItemSocketController 用于判定对错）
        GameContext.DispatchEvent(EventType.OnEndDragEvent, new DataBag {
            { "Item", rectTransform },
            { "Slot", nearest != null ? nearest : null }
        });
    }

    /// <summary>把世界坐标换算成当前父节点下的 anchoredPosition（兼容任意锚点/父级）。</summary>
    private Vector2 WorldToAnchoredPosition(Vector3 worldPoint) {
        if (rectTransform.parent is RectTransform parentRT) {
            Vector2 localPoint = parentRT.InverseTransformPoint(worldPoint);
            // 锚点参考点在父节点本地坐标中的位置：(anchorMin - 父pivot) * 父尺寸
            Vector2 anchorRef = (rectTransform.anchorMin - parentRT.pivot) * parentRT.rect.size;
            return localPoint - anchorRef;
        }
        // 无父节点时退化为原始世界坐标偏移（UI 场景通常不会走到）
        return worldPoint;
    }

    /// <summary>寻找距离自身最近的吸附点（槽位 + 自身原点）；超过 snapRadius 则返回 null。</summary>
    private RectTransform FindNearestSnapPoint(out bool snapToOrigin) {
        snapToOrigin = false;
        RectTransform nearest = null;
        float minSqrDist = float.MaxValue;

        // 候选1：槽位吸附点（优先用 Init 注入的列表；为空才回退扫描节点下的所有 RectTransform）
        if (_points.Count == 0) {
            Transform root = snapContainer != null ? snapContainer : rectTransform.parent;
            if (root != null) root.GetComponentsInChildren(true, _points);
        }
        foreach (RectTransform sp in _points) {
            if (sp == null || sp.gameObject == gameObject) continue;
            // 用世界坐标算距离，不受父节点缩放影响
            float sqrDist = (sp.position - rectTransform.position).sqrMagnitude;
            if (sqrDist < minSqrDist) {
                minSqrDist = sqrDist;
                nearest = sp;
            }
        }

        // 候选2：自身原点（物品初始位置），让物品也能吸回原位
        float originSqr = (orignPositon - WorldToAnchoredPosition(rectTransform.position)).sqrMagnitude;
        if (originSqr < minSqrDist) {
            minSqrDist = originSqr;
            nearest = null;
            snapToOrigin = true;
        }

        // 都没落在吸附半径内则视为未命中
        if (minSqrDist > snapRadius * snapRadius) {
            snapToOrigin = false;
            return null;
        }
        return nearest; // 为 null 时 snapToOrigin 为 true，表示吸回原点
    }


}