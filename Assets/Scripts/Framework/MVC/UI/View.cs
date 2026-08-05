using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IView {
    GameObject root { get; }
    void OnCreate();
    void OnDestroy();
    void OnShow();
    void OnHide();
}


public class View : MonoBehaviour, IView {
    public GameObject root => gameObject;
    protected DataBag _data;

    public View() {

    }
    public void Initialize() {
        OnCreate();
    }
    public void OnCreate() {
        //通用创建逻辑

        //专用创建逻辑
        OnViewCreate();
    }
    public void OnDestroy() {

        OnViewDestroy();
    }
    public void OnShow() {
        //通用显示逻辑

        //专用显示逻辑
        OnViewShow();
    }
    public virtual void OnHide() {
        OnViewHide();
    }

    public void BindUI() {
        //绑定通用ui

        //绑定专用ui
        BindViewUI();
    }

    public virtual void BindViewUI() {

    }
    protected virtual void OnViewCreate() {
        //专用创建逻辑
    }

    public virtual void OnViewShow() {
        //专用显示逻辑
    }
    public virtual void OnViewHide() {
        //专用隐藏逻辑
    }
    public virtual void OnViewDestroy() {
        //专用销毁逻辑
    }

    /// <summary>
    /// Controller 调用的唯一数据入口（传入通用数据包）
    /// </summary>
    public void UpdateData(DataBag data) {
        _data = data;

        // // 自动拆包：将 DataBag 中的值映射到当前 View 的标记字段
        // AutoMapFields(data);

        // 子类重写此方法刷新 UI
        OnShow();
    }

    // 自动映射（利用反射 + 缓存）
    // private void AutoMapFields(DataBag data) {
    //     // 使用之前定义的 DataBinder，但适配为从 DataBag 读取
    //     // 这里直接调用扩展方法（见下方）
    //     this.BindFromDataBag(data);
    // }
    protected GameObject GetChildByPath(string path, GameObject parent = null) {

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
