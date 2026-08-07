using System;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using System.Threading.Tasks;
public enum ViewState {
    None,
    // Loading, 从uimanger中判断加载状态
    Opened,
    Hidden,
    // Closed，ui关闭后会立刻删除实例
}

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
        //向model和view分发数据，自动化刷新逻辑在model和view中重写
        //加载初始化数据（从model和传入的参数中获得）
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
        // model.state = ViewState.Closed;
        OnViewClose();
        _OnClose();
    }
    public virtual void OnViewClose() {

    }
    //ui刷新时调用
    public void OnRefresh() {
        _OnRefresh();
        OnViewRefresh();
        model.state = ViewState.Opened;
    }
    public virtual void OnViewRefresh() {

    }
    //ui隐藏时调用
    public void OnHide() {
        model.state = ViewState.Hidden;
        _OnHide();
        OnViewHide();
    }
    public virtual void OnViewHide() {

    }
    /// <summary>
    /// Controller 调用的唯一数据入口（传入通用数据包）
    /// </summary>
    public void EnterViewWithData(DataBag data) {
        //读取配置
        bool shouldRefresh = false;
        bool isActive = true;
        if (data != null) {
            _data = data;
            shouldRefresh = _data.Get<bool>("shouldRefresh");
            isActive = _data.Get<bool>("isActive");
        }
        if (shouldRefresh) {
            if (isActive) {
                OnRefresh();
            }
            else {
                OnHide();
            }
            return;
        }

        //默认
        OnOpen();
    }
    // 触发事件（带参数）
    public void DispatchEvent<T>(EventType eventType, T payload) {
        model.DispatchEvent(eventType, payload);
    }
    // 触发事件（无参数）
    public void DispatchEvent(EventType eventType) {
        model.DispatchEvent(eventType);
    }
    // 执行指令（返回结果，可 await 或忽略）
    public Task<CommandResult> ExecuteCommand(CommandType commandType, DataBag payload = null) {
        return model.ExecuteCommand(commandType, payload);
    }
    // 订阅专门事件,点击事件
    protected virtual void BindEvents() { }
    //事件相关操作
    protected void BindEvent(EventType eventType, Action action) {
        model.AddEvent(eventType, action);
    }
    protected void BindEvent<T>(EventType eventType, Action<T> action) {
        model.AddEvent(eventType, action);
    }
    protected void UnbindEvent(EventType eventType, Action action) {
        model.RemoveEvent(eventType, action);
    }
    protected void UnbindEvent<T>(EventType eventType, Action<T> action) {
        model.RemoveEvent(eventType, action);
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
    //子类重写自定义数据装载方式
    public virtual void LoadData() {
        //组装_data
    }
    private void _OnClose() {

        _UnbindEvents();
        model?.Dispose();
        view?.Destroy();
    }
    private void _OnRefresh() {
        model.UpdateData(_data);
        view.UpdateData(_data);

        view.root.SetActive(true);
        view.root.transform.SetAsLastSibling();
    }
    private void _OnHide() {
        view.root.SetActive(false);
    }

    //绑定通用事件
    private void _BindEvents() {

    }

    //解绑通用事件
    private void _UnbindEvents() {

    }
}

