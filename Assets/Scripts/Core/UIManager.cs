using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class UIManager
{
    public static UIManager Instance { get; private set; }

    private readonly Dictionary<string, BaseUIController> openedViews = new Dictionary<string, BaseUIController>();
    private readonly Dictionary<string, Transform> layers = new Dictionary<string, Transform>();

    // 缓存已经加载过的 UI Prefab，避免重复加载 Addressables。
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> prefabHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

    // 防止同一个界面在异步加载过程中被重复打开，并让后续 await 调用拿到同一个结果。
    private readonly Dictionary<string, Task<BaseUIController>> loadingViewTasks = new Dictionary<string, Task<BaseUIController>>();

    private GameObject uiRoot;

    private UIManager()
    {
    }

    public void Init()
    {
        ensureUILayers();

        // Addressables 是异步加载，这里用兼容写法，不要求调用方 await。
        GotoView("HUD");
    }

    public static UIManager GetInstance()
    {
        if (Instance == null)
            Instance = new UIManager();

        return Instance;
    }

    /// <summary>
    /// 加载ui界面
    /// </summary>
    public bool isLoading = false;
    public static async Task<BaseUIController> GotoView(string viewName, string layerName = null)
    {
        BaseUIController opened = GetInstance().GetOpenedView(viewName);
        
        if (opened != null)
        {
            if (opened.root == null) Debug.LogError(viewName+"has no root!");
            opened.root.SetActive(true);
            opened.root.transform.SetAsLastSibling();
            Instance.isLoading = true;
            opened.OnRefresh();
            Instance.isLoading = false;
            
            return opened;
        }
        
        Instance.isLoading = true;
        BaseUIController controller = await GetInstance().gotoView(viewName, layerName);
        return controller;
    }

    public static BaseUIController GetView(string viewName)
    {
        return GetInstance().GetOpenedView(viewName);
    }

    public static T GetView<T>(string viewName) where T : BaseUIController
    {
        return GetView(viewName) as T;
    }

    public static bool IsOpen(string viewName)
    {
        return GetInstance().GetOpenedView(viewName) != null;
    }

    public static void CloseView(string viewName)
    {
        GetInstance().CloseOpenedView(viewName);
    }

    public static void CloseAll()
    {
        GetInstance().CloseAllViews();
    }

    public static void ReleaseAllLoadedPrefabs()
    {
        GetInstance().ReleaseAllPrefabHandles();
    }

    private async Task<BaseUIController> gotoView(string viewName, string layerName = null)
    {
        if (string.IsNullOrEmpty(viewName))
        {
            Debug.LogError("[UIManager] View name is empty.");
            return null;
        }

        ensureUILayers();

        BaseUIController opened = GetOpenedView(viewName);
        if (opened != null)
        {
            if (opened.root != null)
            {
                opened.root.SetActive(true);
                opened.root.transform.SetAsLastSibling();
            }
            return opened;
        }

        if (loadingViewTasks.TryGetValue(viewName, out Task<BaseUIController> loadingTask))
        {
            Debug.LogWarning($"[UIManager] UI is already loading: {viewName}");
            return await loadingTask;
        }

        TaskCompletionSource<BaseUIController> loadingCompletion = new TaskCompletionSource<BaseUIController>();
        Task<BaseUIController> openTask = loadingCompletion.Task;
        loadingViewTasks[viewName] = openTask;

        try
        {
            BaseUIController controller = await CreateView(viewName, layerName);
            loadingCompletion.TrySetResult(controller);
            return controller;
        }
        catch (Exception ex)
        {
            loadingCompletion.TrySetException(ex);
            throw;
        }
        finally
        {
            if (loadingViewTasks.TryGetValue(viewName, out Task<BaseUIController> currentTask) && currentTask == openTask)
            {
                Instance.isLoading = false;
                loadingViewTasks.Remove(viewName);
            }
        }
    }

    private async Task<BaseUIController> CreateView(string viewName, string layerName = null)
    {
        try
        {
            GameObject prefab = await LoadPrefab(viewName);
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] Addressable UI prefab not found: {viewName}. Please check Addressables address '{viewName}'.");
                return null;
            }

            Type controllerType = ResolveControllerType(viewName);
            if (controllerType == null)
            {
                Debug.LogError("[UIManager] Could not find UI controller for view: " + viewName);
                return null;
            }

            if (!typeof(BaseUIController).IsAssignableFrom(controllerType))
            {
                Debug.LogError($"[UIManager] Controller type for '{viewName}' must inherit from BaseUIController: {controllerType.Name}");
                return null;
            }

            BaseUIController controller = (BaseUIController)Activator.CreateInstance(controllerType);
            Transform parent = GetLayer(layerName == null ? controller.layerName : layerName);

            GameObject viewInstance = UnityEngine.Object.Instantiate(prefab, parent);
            viewInstance.name = viewName;
            StretchToParent(viewInstance);

            if (controller.hasInputBlocker)
                EnsureInputBlocker(viewInstance);

            controller.Initialize(viewName, viewInstance);
            openedViews[viewName] = controller;
            return controller;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UIManager] Failed to open UI '{viewName}': {ex}");
            return null;
        }
    }

    private void ensureUILayers()
    {
        if (uiRoot == null)
            uiRoot = GameObject.Find(UIConfig.RootName);

        if (uiRoot == null)
        {
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
        if (rootRect != null)
        {
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

    private Transform EnsureLayer(string layerName)
    {
        if (layers.TryGetValue(layerName, out Transform cached) && cached != null)
            return cached;

        Transform layer = uiRoot.transform.Find(layerName);
        if (layer == null)
        {
            GameObject layerObject = new GameObject(layerName, typeof(RectTransform));
            layerObject.transform.SetParent(uiRoot.transform, false);
            layer = layerObject.transform;
        }

        RectTransform rect = layer as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        layers[layerName] = layer;
        return layer;
    }

    private Transform GetLayer(string layerName)
    {
        if (string.IsNullOrEmpty(layerName))
            layerName = UIConfig.NormalLayerName;

        return EnsureLayer(layerName);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        UnityEngine.Object.DontDestroyOnLoad(eventSystem);
    }

    private BaseUIController GetOpenedView(string viewName)
    {
        if (string.IsNullOrEmpty(viewName))
            return null;

        if (!openedViews.TryGetValue(viewName, out BaseUIController view))
            return null;

        if (view != null && view.root != null)
            return view;

        openedViews.Remove(viewName);
        return null;
    }

    private void CloseOpenedView(string viewName)
    {
        BaseUIController view = GetOpenedView(viewName);
        if (view == null)
            return;

        view.Dismiss();
        GameObject root = view.root;
        openedViews.Remove(viewName);
        if (prefabHandles.TryGetValue(viewName,out AsyncOperationHandle<GameObject> handle))
        {
            if(handle.IsValid()) Addressables.Release(handle.Result);
            prefabHandles.Remove(viewName);
        }

        if (root != null)
            UnityEngine.Object.Destroy(root);
    }

    private void CloseAllViews()
    {
        List<string> viewNames = new List<string>(openedViews.Keys);
        for (int i = 0; i < viewNames.Count; i++)
            CloseOpenedView(viewNames[i]);
    }

    private static Type ResolveControllerType(string viewName)
    {
        if (UIConfig.viewMap.TryGetValue(viewName, out Type controllerType))
            return controllerType;

        return Type.GetType(viewName);
    }

    private async Task<GameObject> LoadPrefab(string viewName)
    {
        if (prefabHandles.TryGetValue(viewName, out AsyncOperationHandle<GameObject> cachedHandle))
        {
            if (cachedHandle.IsValid())
                return cachedHandle.Result;

            prefabHandles.Remove(viewName);
        }

        // 这里的 viewName 必须和 Addressables 中的 Address 一致，例如 HUD、MainMenuUI。
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(UIConfig.GetAddress(viewName));
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
        {
            prefabHandles[viewName] = handle;
            return handle.Result;
        }

        if (handle.IsValid())
            Addressables.Release(handle);

        return null;
    }

    private void ReleaseAllPrefabHandles()
    {
        foreach (AsyncOperationHandle<GameObject> handle in prefabHandles.Values)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        prefabHandles.Clear();
    }

    private static void StretchToParent(GameObject view)
    {
        RectTransform rect = view.transform as RectTransform;
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void EnsureInputBlocker(GameObject view)
    {
        if (view == null)
            return;

        RectTransform parentRect = view.transform as RectTransform;
        if (parentRect == null)
            return;

        Transform existing = view.transform.Find("UIInputBlocker");
        GameObject blocker = existing != null ? existing.gameObject : null;
        if (blocker == null)
        {
            blocker = new GameObject("UIInputBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blocker.transform.SetParent(view.transform, false);
        }

        RectTransform rect = blocker.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        Image image = blocker.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0f, 0f, 0f, 0.001f);
            image.raycastTarget = true;
        }

        blocker.transform.SetAsFirstSibling();
        blocker.SetActive(true);
    }
}
