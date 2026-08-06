using System;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using System.Threading.Tasks;
public enum ViewState {
    None,
    Loading,
    Opened,
    Hidden,
    Closed
}

public class Controller {

    protected Model model { get; private set; }
    protected View view { get; private set; }

    protected DataBag _data = new DataBag();

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
        model.state = ViewState.Loading;
        model.UpdateData(_data);
        //拼装传入view的参数
        view.BindUI();
        view.UpdateData(_data);

        OnOpenView();
        _BindEvents();
        BindEvents();
        model.state = ViewState.Opened;
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

        OnViewRefresh();
    }
    public virtual void OnViewRefresh() {

    }
    //ui隐藏时调用
    public void OnHide() {
        _OnHide();
    }
    /// <summary>
    /// Controller 调用的唯一数据入口（传入通用数据包）
    /// </summary>
    public void EnterViewWithData(DataBag data) {
        data ??= new DataBag();
        _data = data;

        //读取配置项
        bool shouldRefresh = data.Get("shouldRefresh", false);
        if (shouldRefresh) {
            OnRefresh();
        }
        else {
            OnOpen();
        }
    }
    // 触发事件（带参数）
    public void DispatchEvent<T>(string eventName, T payload) {
        model.DispatchEvent(eventName, new object[] { payload });
    }
    // 触发事件（无参数）
    public void DispatchEvent(string eventName) {
        model.DispatchEvent(eventName);
    }
    // 执行指令（返回结果，可 await 或忽略）
    public Task<CommandResult> ExecuteCommand(string commandName, DataBag payload = null) {
        return model.ExecuteCommand(commandName, payload);
    }
    // 订阅专门事件,点击事件
    public virtual void BindEvents() { }
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
    protected void BindClickEvent(GameObject obj, UnityAction action) {
        var button = obj.GetComponent<Button>();
        if (!button) {
            Debug.LogWarning("button component is not exist");
        }
        model.AddClickEvent(obj.GetComponent<Button>(), action);
    }
    protected void BindClickEvent(string path, UnityAction action) {
        Button button = view.GetChildByPath(path).GetComponent<Button>();
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
        //todo:加载并组装数据
        LoadData();
    }
    public virtual void LoadData() {

    }
    private void _OnClose() {
        _UnbindEvents();
        model.Dispose();
        view.OnDestroy();
    }
    private void _OnRefresh() {
        model.UpdateData(_data);
        view.UpdateData(_data);

        view.root.SetActive(true);
        view.root.transform.SetAsLastSibling();
    }
    private void _OnHide() { }

    //绑定通用事件
    private void _BindEvents() {
        //绑定外部数据更新事件
        BindEvent(model.viewName + "UpdateData", (DataBag data) => { EnterViewWithData(data); });
    }

    //解绑通用事件
    private void _UnbindEvents() {

    }
}

