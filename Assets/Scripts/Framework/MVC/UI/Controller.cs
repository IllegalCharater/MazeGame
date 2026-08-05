using System;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

public class Controller {

    protected Model model { get; private set; }
    protected View view { get; private set; }

    protected DataBag _data;

    public Controller(Model model, View view) {
        this.model = model;
        this.view = view;
    }
    //初始化
    public virtual void Initialize() {
        if (model.HasInputBlocker) {
            //创建输入拦截器
            UIManager.Instance.EnsureInputBlocker(model.viewName);
        }
    }

    //ui打开时初始化
    public void OnOpen() {
        _OnOpen();
        //加载初始化数据（从model和传入的参数中获得）

        //拼装传入view的参数
        view.BindUI();
        view.UpdateData(_data);

        OnOpenView();
        BindEvents();
    }

    public virtual void OnOpenView() {

    }
    //ui关闭时调用
    public void OnClose() {
        _OnClose();

    }
    //ui刷新时调用
    public void OnRefresh() {
        _OnRefresh();
    }
    //ui隐藏时调用
    public void OnHide() {
        _OnHide();
    }
    /// <summary>
    /// Controller 调用的唯一数据入口（传入通用数据包）
    /// </summary>
    public void EnterViewWithData(DataBag data) {
        _data = data;

        // // 自动拆包：将 DataBag 中的值映射到当前 View 的标记字段
        // AutoMapFields(data);

        // 子类重写此方法刷新 UI
        bool shouldRefresh = data.Get("shouldRefresh", false);
        if (shouldRefresh) {
            OnRefresh();
        }
        else {
            OnOpen();
        }
    }
    // 订阅专门事件,点击事件
    public virtual void BindEvents() {

    }
    //事件相关操作
    protected void BindEvent(string eventName, Action action) {
        model.AddEvent(eventName, action);
    }
    protected void BindEvent<T>(string eventName, Action<T> action) {
        model.AddEvent(eventName, action);
    }
    protected void UnbindEvent(string eventName, Action action) {
        model.RemoveEvent(eventName, action);
    }
    protected void UnbindEvent<T>(string eventName, Action<T> action) {
        model.RemoveEvent(eventName, action);
    }

    //绑定按钮点击事件
    protected void BindClickEvent(Button button, UnityAction action) {
        model.AddClickEvent(button, action);
    }
    protected void BindClickEvent(string path, UnityAction action) {
        Button button = UIHelper.FindTransformByPath(view.root.transform, path).GetComponent<Button>();
        if (!button) Debug.LogWarning("button is not exist");
        model.AddClickEvent(button, action);
    }

    protected void UnbindClickEvent(Button button) {
        model.RemoveClickEvent(button);
    }

    //获得配置表数据
    protected T GetConfigData<T>(string rootKey, string id) where T : BaseData {
        return model.GetConfigData<T>(rootKey, id);
    }

    //通用流程
    private void _OnOpen() {
        _BindEvents();
    }
    private void _OnClose() {
        _UnbindEvents();
        model.Dispose();
        view.OnDestroy();
    }
    private void _OnRefresh() {
        view.root.SetActive(true);
        view.root.transform.SetAsLastSibling();
    }
    private void _OnHide() { }

    //绑定通用事件
    private void _BindEvents() {
        //绑定加载ui事件，卸载ui事件
    }

    //解绑通用事件
    private void _UnbindEvents() {

    }
}

public enum ViewState {
    None,
    Loading,
    Opened,
    Hidden,
    Closed
}