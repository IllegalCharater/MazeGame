using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
public sealed class UIEntry {
    public View view;
    public Model model;
    public Controller controller;
}
public class UIManager {
    public static UIManager Instance { get; private set; }

    //已打开的UI界面，key为viewName
    private readonly Dictionary<string, UIEntry> openedViews = new Dictionary<string, UIEntry>();
    private readonly Dictionary<string, Transform> layers = new Dictionary<string, Transform>();

    // 缓存已经加载过的 UI Prefab，避免重复加载 Addressables。
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> prefabHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

    // 防止同一个界面在异步加载过程中被重复打开，并让后续 await 调用拿到同一个结果。
    private readonly Dictionary<string, Task<UIEntry>> loadingViewTasks = new Dictionary<string, Task<UIEntry>>();

    private GameObject uiRoot;

    private UIManager() { }

    public void Init() {
        ensureUILayers();
        // Addressables 是异步加载，这里用兼容写法，不要求调用方 await。
        // GotoView("HUD");
        Injector.Instance.Register(Instance);
    }

    public static UIManager GetInstance() {
        if (Instance == null)
            Instance = new UIManager();

        return Instance;
    }

    /// <summary>
    /// 加载ui界面
    /// </summary>
    public static async Task<UIEntry> GotoView(string viewName, DataBag options = null) {
        if (string.IsNullOrEmpty(viewName)) {
            Debug.LogError("[UIManager] View name is empty.");
            return null;
        }

        UIEntry entry = GetView(viewName);
        if (entry != null) {
            if (entry.view.root == null) Debug.LogError(viewName + "has no root!");
            var controller = entry.controller;
            controller.OnRefresh();
            return entry;
        }
        entry = await GetInstance().gotoView(viewName, options);
        return entry;
    }

    //获得已打开的界面
    public static UIEntry GetView(string viewName) {
        return GetInstance().GetOpenedView(viewName);
    }

    //界面是否打开
    public static bool IsOpen(string viewName) {
        return GetInstance().GetOpenedView(viewName) != null;
    }

    //关闭已打开的界面
    public static void CloseView(string viewName) {
        GetInstance().CloseOpenedView(viewName);
    }

    //关闭所有已打开的界面
    public static void CloseAll() {
        GetInstance().CloseAllViews();
    }

    //释放所有已加载的UI Prefab资源
    public static void ReleaseAllLoadedPrefabs() {
        GetInstance().ReleaseAllPrefabHandles();
    }

    private async Task<UIEntry> gotoView(string viewName, DataBag options = null) {

        UIEntry entry;

        if (loadingViewTasks.TryGetValue(viewName, out Task<UIEntry> loadingTask)) {
            Debug.LogWarning($"[UIManager] UI is already loading: {viewName}");
            return await loadingTask;
        }

        TaskCompletionSource<UIEntry> loadingCompletion = new TaskCompletionSource<UIEntry>();
        Task<UIEntry> openTask = loadingCompletion.Task;
        loadingViewTasks[viewName] = openTask;

        try {
            entry = await HandleViewTask(viewName, options);
            loadingCompletion.TrySetResult(entry);
            return entry;
        }
        catch (Exception ex) {
            loadingCompletion.TrySetException(ex);
            throw;
        }
        finally {
            if (loadingViewTasks.TryGetValue(viewName, out Task<UIEntry> currentTask) && currentTask == openTask) {
                loadingViewTasks.Remove(viewName);
            }
        }
    }

    private async Task<UIEntry> HandleViewTask(string viewName, DataBag options) {
        try {
            GameObject prefab = await LoadPrefab(viewName);
            if (prefab == null) {
                Debug.LogError($"[UIManager] Addressable UI prefab not found: {viewName}. Please check Addressables address '{viewName}'.");
                return null;
            }
            //获取option配置项
            string layerName = options.Get<string>("layerName", null);

            //根据viewName创建view,model,controller
            Type controllerType = getControllerType(viewName);
            Type modelType = getModelType(viewName);
            Type viewType = getViewType(viewName);

            //注入依赖
            var injector = Injector.Instance;
            Model model = injector.Resolve(modelType,
            new object[] { viewName, GameContext.Instance.context }) as Model;
            if (layerName == null) {
                layerName = model.Layer;
            }
            GameObject viewInstance = instantiatePrefab(prefab, layerName);
            //monobehaviour类型的view无法直接通过IOC容器创建，需要手动添加到实例化的GameObject上
            View view = viewInstance.AddComponent(viewType) as View;
            Controller controller = injector.Resolve(controllerType, new object[] { model, view }) as Controller;

            model.Initialize();
            view.Initialize();
            controller.Initialize();

            StretchToParent(viewInstance);

            controller.EnterViewWithData(options);//传入初始化参数

            UIEntry entry = new UIEntry {
                view = view,
                model = model,
                controller = controller
            };
            openedViews[viewName] = entry;
            return entry;
        }
        catch (Exception ex) {
            Debug.LogError($"[UIManager] Failed to open UI '{viewName}': {ex}");
            return null;
        }
    }

    private void ensureUILayers() {
        if (uiRoot == null)
            uiRoot = GameObject.Find(UIConfig.RootName);

        if (uiRoot == null) {
            uiRoot = new GameObject(UIConfig.RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(uiRoot);
        }

        Canvas canvas = uiRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = uiRoot.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (uiRoot.GetComponent<GraphicRaycaster>() == null)
            uiRoot.AddComponent<GraphicRaycaster>();

        RectTransform rootRect = uiRoot.GetComponent<RectTransform>();
        if (rootRect != null) {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        EnsureLayer(UIConfig.BackgroundLayerName);
        EnsureLayer(UIConfig.NormalLayerName);
        EnsureLayer(UIConfig.PopupLayerName);
        EnsureLayer(UIConfig.TopLayerName);
        EnsureLayer(UIConfig.ToastLayerName);
        EnsureEventSystem();
    }

    private Transform EnsureLayer(string layerName) {
        if (layers.TryGetValue(layerName, out Transform cached) && cached != null)
            return cached;

        Transform layer = uiRoot.transform.Find(layerName);
        if (layer == null) {
            GameObject layerObject = new GameObject(layerName, typeof(RectTransform));
            layerObject.transform.SetParent(uiRoot.transform, false);
            layer = layerObject.transform;
        }

        RectTransform rect = layer as RectTransform;
        if (rect != null) {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        layers[layerName] = layer;
        return layer;
    }

    private Transform GetLayer(string layerName) {
        if (string.IsNullOrEmpty(layerName))
            layerName = UIConfig.NormalLayerName;

        return EnsureLayer(layerName);
    }

    private void EnsureEventSystem() {
        if (EventSystem.current != null || UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        UnityEngine.Object.DontDestroyOnLoad(eventSystem);
    }

    private UIEntry GetOpenedView(string viewName) {
        if (string.IsNullOrEmpty(viewName))
            return null;

        if (!openedViews.TryGetValue(viewName, out UIEntry entry))
            return null;

        if (entry.controller != null && entry.view.root != null)
            return entry;

        openedViews.Remove(viewName);
        return null;
    }

    private void CloseOpenedView(string viewName) {
        UIEntry entry = GetOpenedView(viewName);
        if (entry == null)
            return;

        entry.controller.OnClose();
        GameObject root = entry.view.root;
        openedViews.Remove(viewName);
        if (prefabHandles.TryGetValue(viewName, out AsyncOperationHandle<GameObject> handle)) {
            if (handle.IsValid()) Addressables.Release(handle.Result);
            prefabHandles.Remove(viewName);
        }

        if (root != null)
            UnityEngine.Object.Destroy(root);
    }

    private void CloseAllViews() {
        List<string> viewNames = new List<string>(openedViews.Keys);
        for (int i = 0; i < viewNames.Count; i++)
            CloseOpenedView(viewNames[i]);
    }

    private Type getControllerType(string viewName) {
        return getUIType(viewName + "Controller");
    }
    private Type getModelType(string viewName) {
        return getUIType(viewName + "Model");
    }
    private Type getViewType(string viewName) {
        return getUIType(viewName + "View");
    }
    private Type getUIType(string typeName) {
        Type type = Type.GetType(typeName);
        if (type == null) {
            Debug.LogError("[UIManager] Could not find UI controller for view: " + typeName);
            return null;
        }
        // if (!type.IsAssignableFrom(controllerType)) {
        //     Debug.LogError($"[UIManager] Controller type for '{viewName}' must inherit from BaseUIController: {controllerType.Name}");
        //     return null;
        // }
        return type;
    }

    private GameObject instantiatePrefab(GameObject prefab, string layerName) {
        if (prefab == null) {
            Debug.LogError("[UIManager] Prefab is null.");
            return null;
        }
        Transform parent = GetLayer(layerName);
        GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);
        instance.name = prefab.name;
        StretchToParent(instance);
        return instance;
    }

    private async Task<GameObject> LoadPrefab(string viewName) {
        if (prefabHandles.TryGetValue(viewName, out AsyncOperationHandle<GameObject> cachedHandle)) {
            if (cachedHandle.IsValid())
                return cachedHandle.Result;

            prefabHandles.Remove(viewName);
        }

        // 这里的 viewName 必须和 Addressables 中的 Address 一致，例如 HUD、MainMenuUI。
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(UIConfig.GetAddress(viewName));
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null) {
            prefabHandles[viewName] = handle;
            return handle.Result;
        }

        if (handle.IsValid())
            Addressables.Release(handle);

        return null;
    }

    private void ReleaseAllPrefabHandles() {
        foreach (AsyncOperationHandle<GameObject> handle in prefabHandles.Values) {
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        prefabHandles.Clear();
    }

    private static void StretchToParent(GameObject view) {
        RectTransform rect = view.transform as RectTransform;
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    //暂时改为公有方法，后续改为订阅添加blocker事件
    public void EnsureInputBlocker(string viewName) {
        UIEntry entry = GetOpenedView(viewName);
        if (entry != null) {
            ensureInputBlocker(entry.view.root);
        }
    }

    private void ensureInputBlocker(GameObject view) {
        if (view == null)
            return;

        RectTransform parentRect = view.transform as RectTransform;
        if (parentRect == null)
            return;

        Transform existing = view.transform.Find("UIInputBlocker");
        GameObject blocker = existing != null ? existing.gameObject : null;
        if (blocker == null) {
            blocker = new GameObject("UIInputBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blocker.transform.SetParent(view.transform, false);
        }

        RectTransform rect = blocker.transform as RectTransform;
        if (rect != null) {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        Image image = blocker.GetComponent<Image>();
        if (image != null) {
            image.color = new Color(0f, 0f, 0f, 0.001f);
            image.raycastTarget = true;
        }

        blocker.transform.SetAsFirstSibling();
        blocker.SetActive(true);
    }


    public void Dispose() {
        CloseAll();
        ReleaseAllLoadedPrefabs();
    }
}
