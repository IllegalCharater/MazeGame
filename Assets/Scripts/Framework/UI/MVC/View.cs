using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IView {
    GameObject root { get; }
    void Create();
    void Destroy();
}

public class View : MonoBehaviour, IView {
    public GameObject root => gameObject;
    protected DataBag _data;

    public View() {

    }
    //初始化
    public void Initialize() {
        Create();
    }
    public void Create() {
        //通用创建逻辑

        //专用创建逻辑
        OnViewCreate();
    }
    public void Destroy() {
        // 实例可能已被外部销毁（Unity 假空）：此时访问 gameObject 会抛 MissingReferenceException，直接返回
        if (this == null)
            return;

        // 先执行自定义销毁钩子（此刻对象仍存活，可安全访问子节点/组件）
        OnViewDestroy();

        // 再删除实例；全限定 Unity 方法，避免与自身 Destroy() 方法名遮蔽
        if (root != null)
            UnityEngine.Object.Destroy(root);
    }

    public void BindUI() {
        //绑定通用ui

        //绑定专用ui
        BindViewUI();
    }

    protected virtual void BindViewUI() {

    }
    protected virtual void OnViewCreate() {
        //专用创建逻辑
    }
    //根据传入数据自动刷新页面，主要从controller中控制view
    public virtual void OnViewDestroy() {
        //专用销毁逻辑
    }

    /// <summary>
    /// Controller 调用的唯一数据入口（传入通用数据包）
    /// </summary>
    public void UpdateData(DataBag data) {
        if (data != null) {
            _data = data.Get("View", _data);
        }
        // 子类重写此方法刷新 UI
        SyncView();
    }
    protected virtual void SyncView() {

    }

    public GameObject GetChildByPath(string path, GameObject parent = null) {

        Transform childTransform = UIHelper.FindTransformByPath(parent ? parent.transform : transform, path);
        var child = childTransform != null ? childTransform.gameObject : null;
        if (child == null) {
            Debug.LogWarning($"Child not found at path: {path} in view: {name}");
        }
        return child;
    }

    protected void SetText(Text textComponent, object text) {
        UIHelper.SetText(textComponent, text);
    }

}
