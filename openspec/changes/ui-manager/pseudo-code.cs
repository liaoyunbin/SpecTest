/* ============================================================
 * UIFrameworkLib — UI 框架完整伪代码（v9）
 * 命名空间：UIFrameworkLib
 * 语言：C# (Unity + UniTask)
 *
 * 本文件供 AI 理解框架设计和需求，和实际代码不完全一致。
 * 实际代码见 Assets/Scripts/UIFrameworkLib/
 * ============================================================ */

// ============================================================
// 1. CORE — 核心模型
// ============================================================

// --- 1a. UILayer ---
// 三种层级，按显示顺序由低到高
enum UILayer
{
    Background, // 底层背景，打开时弹出其上所有 Normal 和 Popup
    Normal,     // 标准面板，入栈管理，支持 Back
    Popup,      // 弹窗，相互替换，可自带遮罩
}

// --- 1b. UIStateMachine ---
// 6 状态状态机，所有转换走 TryTransitionTo()
// 替代 CancellationTokenSource 进行异步中断判断
enum UIState
{
    None,
    Loading,
    AnimationEnter,
    Opened,
    AnimationExit,
    Closed,
}

// 合法转换表：
//   None → Loading
//   Loading → AnimationEnter, Closed
//   AnimationEnter → Opened, Closed
//   Opened → AnimationExit
//   AnimationExit → Closed, AnimationEnter（覆盖式重新打开）
//   Closed → Loading（缓存复用）

class UIStateMachine
{
    UIState CurrentState     // 当前状态
    bool IsTransitioning     // Loading / AnimationEnter / AnimationExit
    event OnStateChanged(old, new)

    bool TryTransitionTo(UIState newState)
    {
        // 查 ValidTransitions 映射表
        // 合法 → 切换 + 触发事件，return true
        // 非法 → DebugLogWarning + return false
    }
}

// --- 1c. UIItemConfig ---
// 配置模型，由外部数据源转换为本模型后注入框架
class UIItemConfig
{
    string  UIKey       // UI 唯一标识（与 Controller 类名对应）
    string  PrefabPath  // Resources 中预制体路径
    UILayer Layer       // 层级（默认 Normal）
}

// --- 1d. UIContext ---
// 每个 UI 实例的运行时状态容器
// 生命周期：每次打开创建新 Context → 关闭后释放
class UIContext
{
    // 标识
    string UIKey
    int    InstanceId     // 自增，每次打开递增

    // 核心引用
    UIView          View        // 表现层引用
    IUIController   Controller  // 逻辑层引用
    UIItemConfig    Config      // 配置

    // 状态
    UIStateMachine  StateMachine
    float           OpenTime     // 打开时间戳
    UIContext       PreviousContext  // 栈中上一个 UI

    // 方法
    void BindController(IUIController controller)
    {
        // View 创建前调用
        Controller = controller
        controller.BindContext(this)
    }

    void SetView(UIView view)
    {
        // Controller 创建 View 后调用
        View = view
        Controller?.SetView(view)
    }

    void Dispose()
    {
        // 状态 → Closed
        // Controller.OnDispose()
        // 若 View 存在则 Destroy(View.gameObject)
        // 清空引用
    }
}


// ============================================================
// 2. CONTROLLER — 逻辑层
// ============================================================

// --- 2a. IUIController（内部接口）---
// 业务层不应直接使用此接口
internal interface IUIController
{
    UIContext Context { get; }
    void BindContext(UIContext context)
    void SetView(UIView view)
    void OnInit()      // 仅一次
    void OnOpen(args)  // 每次打开
    void OnShown()     // 入场动画结束
    void OnHide()      // 退场开始 / 被覆盖
    void OnDispose()   // 销毁时
}

// --- 2b. UIController<T>（泛型基类，业务继承）---
// 折中方案：Controller 自管理 View 创建，框架提供工具方法
abstract class UIController<T> : IUIController where T : UIView
{
    // === 公开属性 ===
    T           View     // 强类型 View 引用
    UIContext   Context  // 运行上下文
    UIItemConfig Config  // 配置快捷方式

    // === View 创建（子类必须实现）===
    abstract UniTask<T> CreateViewAsync()
    // 框架在 OnInit 之后、OnOpen 之前调用

    // === 框架提供的工具方法（子类在 CreateViewAsync 中调用）===

    // 从 Resources 加载并实例化 Prefab
    // 自动检查缓存（隐藏的旧实例），有则直接复用
    protected async UniTask<T> LoadFromResources(string prefabPath)
    {
        uiKey = Context.UIKey

        // 1. 检查缓存
        cached = UIManager.Instance.GetCachedView(uiKey)
        if cached != null
            cached.SetActive(true)
            return cached.GetComponent<T>()

        // 2. 异步加载 Prefab
        prefab = await UIResourceLoader.Instance.LoadPrefabAsync(prefabPath)
        if prefab == null return null

        // 3. 实例化到对应层级
        parent = UIRoot.Instance.GetLayer(Config.Layer)
        instance = Instantiate(prefab, parent)
        instance.name = uiKey
        return instance.GetComponent<T>()
    }

    // 仅从缓存获取（无缓存时返回 null）
    protected T LoadFromCache()
    {
        cached = UIManager.Instance.GetCachedView(Context.UIKey)
        if cached != null
            cached.SetActive(true)
            return cached.GetComponent<T>()
        return null
    }

    // === 生命周期钩子（业务层重写）===
    protected internal virtual void OnInit()       { }
    protected internal virtual void OnOpen(args)   { }
    protected internal virtual void OnShown()      { }
    protected internal virtual void OnHide()       { }
    protected internal virtual void OnDispose()    { }

    // === 辅助方法 ===
    protected void CloseSelf()
    {
        UIManager.Instance.Close(ResolveUIKey())
    }

    // UIKey 解析：类名去掉 "Controller" 后缀
    string ResolveUIKey()
    {
        name = GetType().Name
        return name.EndsWith("Controller") ? name[..^10] : name
    }

    // === IUIController 显式实现 ===
    void IUIController.BindContext(UIContext context) { Context = context }
    void IUIController.SetView(UIView view)           { View = view as T }

    // 生命周期转发（internal 到 protected internal）
    void IUIController.OnInit()    { OnInit() }
    void IUIController.OnOpen(a)   { OnOpen(a) }
    void IUIController.OnShown()   { OnShown() }
    void IUIController.OnHide()    { OnHide() }
    void IUIController.OnDispose() { OnDispose() }
}


// ============================================================
// 3. VIEW — 表现层
// ============================================================

// --- 3a. UIView ---
// 纯表现层：动画 + 交互控制 + 遮罩
// 子类在 Awake 中手动绑定组件，无代码生成
abstract class UIView : MonoBehaviour
{
    // === 属性 ===
    UIContext Context         // 运行时上下文（框架设置）
    IUIController Controller  // 关联 Controller（跨缓存持久化）

    // === 交互控制 ===
    void SetInteractive(bool enabled)
        // CanvasGroup.interactable + blocksRaycasts

    void DisableAllSelectables()
        // 快照所有 Selectable → 设为不可交互
        // 入场动画前调用

    void RestoreSelectables()
        // 恢复快照
        // 入场动画结束时调用

    // === 动画（virtual，子类 override 选择效果）===
    virtual void PlayEnterAnimation(Action onComplete)
        // 默认 FadeEnter（淡入 0.3s）
    virtual void PlayExitAnimation(Action onComplete)
        // 默认 FadeExit（淡出 0.2s）

    // === 内置动画 Helper（子类直接在 override 中调用）===
    // 淡入淡出
    protected void FadeEnter(Action onComplete, float duration = 0.3f)
    protected void FadeExit(Action onComplete, float duration = 0.2f)

    // 缩放弹性（弹到 1.1 再回到 1.0）
    protected void ScaleEnter(Action onComplete, float duration = 0.3f)
    protected void ScaleExit(Action onComplete, float duration = 0.2f)

    // 滑动
    protected void SlideUpEnter(Action onComplete, float duration = 0.3f)
    protected void SlideUpExit(Action onComplete, float duration = 0.2f)
    protected void SlideDownEnter(Action onComplete, float duration = 0.3f)
    protected void SlideDownExit(Action onComplete, float duration = 0.2f)

    // 黑屏渐入渐出（需传入 BlackImage 引用）
    protected void BlackFadeEnter(Image blackImage, Action onComplete, ...)
    protected void BlackFadeExit(Image blackImage, Action onComplete, ...)

    // === Popup 遮罩（虚方法，子类实现）===
    protected virtual void ShowMask()
    protected virtual void HideMask()

    // === 框架内部调用 ===
    void Internal_SetContext(UIContext context) { Context = context }
}

// --- 3b. UIRoot ---
// 场景 UI 根节点，管理三个层级容器
class UIRoot : MonoBehaviour
{
    static UIRoot Instance   // 单例

    Transform BackgroundLayer  // SortingOrder=0
    Transform NormalLayer      // SortingOrder=100
    Transform PopupLayer       // SortingOrder=200

    Transform GetLayer(UILayer layer)

    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        // 创建 [UIRoot] GameObject
        // 添加 Canvas (ScreenSpaceOverlay, SortingOrder=10000)
        // 添加 CanvasScaler + GraphicRaycaster
        // 创建三个层级子节点（各带 Canvas overrideSorting + GraphicRaycaster）
        // DontDestroyOnLoad
    }
}


// ============================================================
// 4. SINGLETON — 泛型单例基类
// ============================================================

abstract class Singleton<T> where T : class, new()
{
    private static T _instance
    private static readonly object _lock = new()

    static T Instance
    {
        get
        {
            // 双检锁线程安全
            if _instance == null
                lock (_lock)
                    if _instance == null
                        _instance = new T()
            return _instance
        }
    }
}


// ============================================================
// 5. UIMANAGER — 核心管理器
// ============================================================

class UIManager : Singleton<UIManager>
{
    // === 内部组件 ===
    UIConfigLoader ConfigLoader  // 配置加载器

    // === 核心数据 ===
    // Controller 持久化（首次创建后常驻）
    Dictionary<string, IUIController> _controllers
    // 已调用过 OnInit 的 Controller
    HashSet<string> _initializedControllers
    // 活跃 UI Context 字典
    Dictionary<string, UIContext> _activeContexts

    // === 层级管理 ===
    UIContext           _backgroundContext  // 当前 Background
    List<UIContext>     _normalStack        // Normal 栈
    UIContext           _currentPopup       // 当前 Popup

    // === 双通道 ===
    UIContext _enteringContext  // 入场通道
    UIContext _exitingContext   // 退场通道
    bool IsBusy => _enteringContext != null || _exitingContext != null

    // === 请求队列 ===
    Queue<QueueItem> _queue
    const int MAX_QUEUE_SIZE = 10
    bool _isProcessing

    // === 隐藏 View 缓存（替代 UIPool）===
    // 关闭时 SetActive(false) 缓存，复用减少加载
    Dictionary<string, GameObject> CachedViews

    // ============================================================
    // 对外接口
    // ============================================================

    // 通过 Controller 类型打开 UI
    void Open<T>(object args = null) where T : IUIController
    {
        uiKey = ResolveUIKey<T>()
        EnqueueOpen(uiKey, args)
    }

    // 通过 UIKey 打开（调试用）
    void Open(string uiKey, object args = null)
    {
        EnqueueOpen(uiKey, args)
    }

    // 通过 Controller 类型关闭 UI
    void Close<T>() where T : IUIController
    {
        uiKey = ResolveUIKey<T>()
        Close(uiKey)
    }

    // 通过 UIKey 关闭
    void Close(string uiKey)
    {
        // 查 _activeContexts，若 Opened → StartExit(ctx)
    }

    // 紧急关闭所有 UI
    void CloseAll()
    {
        // 清空队列 + 重置 _isProcessing
        // 清理 _enteringContext / _exitingContext
        // 遍历所有活跃 Context → Closed + OnDispose + Destroy View
        // 清空 _activeContexts / _normalStack / 层级引用
        // 清空所有缓存 View（Destroy）
        // 清空所有持久 Controller
    }

    // 清空缓存（手动调用）
    void ClearCache()
    {
        // Destroy 所有 CachedViews 中的 GameObject
        // CachedViews.Clear()
    }

    // View 缓存读写（Controller 工具方法调用）
    GameObject GetCachedView(string uiKey)
    void CacheView(string uiKey, GameObject go)
        // go.SetActive(false); CachedViews[uiKey] = go

    // 调试属性
    int ActiveCount => _activeContexts.Count
    int CachedViewCount => CachedViews.Count
    IReadOnlyList<UIContext> NormalStack => _normalStack
    IEnumerable<UIContext> GetAllActiveContexts() => _activeContexts.Values

    // ============================================================
    // 队列调度
    // ============================================================

    void EnqueueOpen(string uiKey, object args)
    {
        config = ConfigLoader.Get(uiKey)
        if config == null → LogError + return

        // 同一界面已打开（Opened 状态）→ 直接刷新
        if _activeContexts 中存在 Opened 实例
            Controller.OnOpen(args) + return

        // 同一界面已在队列中 → 刷新 args（去重）
        if _queue 中有同 UIKey 元素
            更新该元素 Args + return

        // 有任务在执行或双通道忙 → 入队
        if _isProcessing || IsBusy
            队满则丢弃最旧
            _queue.Enqueue(QueueItem(uiKey, args))
            return

        // 空闲 → 直接执行
        _isProcessing = true
        ExecuteOpen(QueueItem(uiKey, args))
    }

    void ProcessNext()
    {
        if _queue.Count > 0
            ExecuteOpen(_queue.Dequeue())
        else
            _isProcessing = false
    }

    // ============================================================
    // 执行打开（异步）
    // ============================================================

    async void ExecuteOpen(QueueItem item)
    {
        config = ConfigLoader.Get(item.UIKey)
        ctx = new UIContext(item.UIKey, config)
        ctx.StateMachine → Loading
        _enteringContext = ctx

        try:
            // 1. 获取或持久化 Controller
            if _controllers 不包含 item.UIKey
                // 通过反射创建 Controller 实例
                controller = CreateController($"命名空间.{item.UIKey}Controller")
                if controller == null → Closed + CleanupAndNext + return
                _controllers[item.UIKey] = controller

            ctx.BindController(controller)

            // 首次 → OnInit
            if _initializedControllers.Add(item.UIKey)
                SafeExecute(controller.OnInit)

            // 2. Controller 创建 View
            view = await controller.CreateViewAsync()

            // 【状态机替代 CancellationToken】检查是否被中断
            if ctx.StateMachine.CurrentState != UIState.Loading
                // 被 CloseAll 等中断
                if view != null → Destroy(view.gameObject)
                CleanupAndNext(ctx) + return

            if view == null → LogError + Closed + CleanupAndNext + return

            view.Controller = controller
            view.Internal_SetContext(ctx)
            ctx.SetView(view)

            // 3. 注册到层级管理
            RegisterContext(ctx)
            ctx.StateMachine → AnimationEnter

            // 4. 入场前准备
            view.SetInteractive(false)
            view.DisableAllSelectables()
            if config.Layer == Popup → view.ShowMask()

            // 5. 调用 OnOpen
            SafeExecute(controller.OnOpen(item.Args))

            // 6. 入场动画（等待完成）
            animTcs = UniTaskCompletionSource
            view.PlayEnterAnimation(() → animTcs.TrySetResult())
            await animTcs.Task

            // 【状态机检查】动画期间是否被中断
            if ctx.StateMachine.CurrentState != UIState.AnimationEnter
                CleanupAndNext(ctx) + return

            // 7. Opened
            ctx.StateMachine → Opened
            view.RestoreSelectables()
            view.SetInteractive(true)
            ctx.OpenTime = Time.time
            SafeExecute(controller.OnShown)

        catch Exception e → LogError + CleanupAndNext(ctx) + return

        finally:
            _enteringContext = null
            ProcessNext()
    }

    // ============================================================
    // 退场
    // ============================================================

    async void StartExit(UIContext ctx)
    {
        if ctx.StateMachine.CurrentState != Opened → return

        ctx.StateMachine → AnimationExit
        _exitingContext = ctx

        try:
            SafeExecute(ctx.Controller.OnHide)
            ctx.View.SetInteractive(false)

            tcs = UniTaskCompletionSource
            ctx.View.PlayExitAnimation(() → tcs.TrySetResult())
            await tcs.Task

            ctx.StateMachine → Closed

            // 不销毁 View，隐藏缓存
            CacheView(ctx.UIKey, ctx.View.gameObject)

        catch Exception e → LogError

        finally:
            UnregisterContext(ctx)
            _exitingContext = null
            RestorePreviousNormal()
            if !IsBusy → ProcessNext()
    }

    // ============================================================
    // 栈管理
    // ============================================================

    void RegisterContext(UIContext ctx)
    {
        _activeContexts[ctx.UIKey] = ctx

        switch ctx.Config.Layer:
            Background:
                CloseAllNormalAndPopup()  // 弹出其上所有
                if _backgroundContext != null && != ctx
                    StartExit(_backgroundContext)
                _backgroundContext = ctx

            Normal:
                if _normalStack 不为空
                    上一个 Normal → OnHide
                    ctx.PreviousContext = 上一个
                _normalStack.Add(ctx)

            Popup:
                if _currentPopup != null && != ctx
                    StartExit(_currentPopup)
                _currentPopup = ctx
                ctx.PreviousContext = _normalStack 栈顶（可为 null）
    }

    void UnregisterContext(UIContext ctx)
    {
        _activeContexts.Remove(ctx.UIKey)
        switch ctx.Config.Layer:
            Background → _backgroundContext = null (if equal)
            Normal     → _normalStack.Remove(ctx)
            Popup      → _currentPopup = null (if equal)
    }

    void RestorePreviousNormal()
    {
        if _normalStack 不为空
            栈顶 Normal 且状态为 Opened
                → Controller.OnShown + SetInteractive(true)
    }

    void CloseAllNormalAndPopup()
    {
        // 关闭当前 Popup → 关闭所有 Normal → 清空栈
    }

    // ============================================================
    // 辅助方法
    // ============================================================

    string ResolveUIKey<T>() where T : IUIController
    {
        name = typeof(T).Name
        return name.EndsWith("Controller") ? name[..^10] : name
    }

    IUIController CreateController(string typeName)
    {
        // Type.GetType(typeName) → Activator.CreateInstance → as IUIController
    }

    void SafeExecute(Action action, string context)
    {
        // try-catch，异常时 LogError
    }

    void CleanupAndNext(UIContext ctx)
    {
        ctx.Dispose()
        UnregisterContext(ctx)
        _enteringContext = null
        ProcessNext()
    }

    // === 内部类 ===
    class QueueItem
    {
        string UIKey
        object Args { get; set }
    }
}


// ============================================================
// 6. UIResourceLoader — 资源管理器
// ============================================================

class UIResourceLoader : Singleton<UIResourceLoader>
{
    // 职责：仅从 Resources 异步加载 Prefab，返回原始 GameObject
    // 不负责实例化、不负责缓存、不负责对象池
    async UniTask<GameObject> LoadPrefabAsync(string prefabPath)
    {
        if string.IsNullOrEmpty(prefabPath) → LogError + return null

        // 超时看门狗（5秒仅日志警告）
        TimeoutGuard(prefabPath, 5f).Forget()

        req = Resources.LoadAsync<GameObject>(prefabPath)
        await req.ToUniTask()

        if req.asset == null → LogError + return null
        return req.asset as GameObject
    }

    async UniTaskVoid TimeoutGuard(string path, float timeout)
    {
        await UniTask.Delay(timeout * 1000)
        DebugLogWarning($"加载超过 {timeout}s")
    }

    // 未来可继承重写：Addressables / AssetBundle 等
}


// ============================================================
// 7. UIConfigLoader — 配置加载器
// ============================================================

class UIConfigLoader
{
    Dictionary<string, UIItemConfig> _configMap

    void Load(List<UIItemConfig> configs)
        // 清空 → 逐条存入 _configMap
    UIItemConfig Get(string uiKey)
        // _configMap.TryGetValue
    string[] GetAllKeys()
        // _configMap.Keys 复制
    void Reload(List<UIItemConfig> configs)
        // Load(configs)
}


// ============================================================
// 8. UIEditorWindow — 编辑器调试窗口
// ============================================================

class UIEditorWindow : EditorWindow
{
    // 菜单 UIFrameworkLib/UI Debugger
    // 显示：
    //   - 所有活跃 UI（状态 + 层级 + 打开时间）
    //   - Normal 栈
    //   - 缓存 View 统计
    // 操作按钮：
    //   - CloseAll
    //   - ClearCache
    //   - 打开指定 UI（下拉选择 UIKey）
}


// ============================================================
// 9. 业务层使用示例
// ============================================================

// --- 9a. ShopView（表现层）---
class ShopView : UIView
{
    Button m_BtnClose
    Text   m_TitleText
    Transform m_ItemRoot

    // 手动绑定组件
    void Awake()
    {
        m_BtnClose  = transform.Find("BtnClose").GetComponent<Button>()
        m_TitleText = transform.Find("Title").GetComponent<Text>()
        m_ItemRoot  = transform.Find("ItemList")
    }

    // 选择动画效果
    override void PlayEnterAnimation(Action onComplete)
        ScaleEnter(onComplete, 0.35f)   // 缩放弹入

    override void PlayExitAnimation(Action onComplete)
        FadeExit(onComplete, 0.15f)     // 淡出

    // View 层提供 UI 更新接口
    void SetTitle(string text) => m_TitleText.text = text
}

// --- 9b. ShopController（逻辑层）---
class ShopController : UIController<ShopView>
{
    // 创建 View
    override async UniTask<ShopView> CreateViewAsync()
        return await LoadFromResources("Prefabs/UI/ShopPanel")

    // 初始化（仅一次）
    override void OnInit()
    {
        View.m_BtnClose.onClick.AddListener(CloseSelf)
    }

    // 每次打开
    override void OnOpen(object args)
    {
        categoryId = (int)args  // 参数
        View.SetTitle($"商店 - 分类{categoryId}")
        // 加载商品数据...
    }

    // 入场结束
    override void OnShown() { }

    // 退场开始
    override void OnHide() { }
}

// --- 9c. 调用方使用 ---
// 启动时加载配置
UIConfigLoader loader = UIManager.Instance.ConfigLoader
loader.Load(new List<UIItemConfig>{
    new() { UIKey = "Shop", PrefabPath = "Prefabs/UI/ShopPanel", Layer = UILayer.Normal },
    new() { UIKey = "Bag",  PrefabPath = "Prefabs/UI/BagPanel",  Layer = UILayer.Normal },
    new() { UIKey = "ConfirmPopup", PrefabPath = "Prefabs/UI/ConfirmPopup", Layer = UILayer.Popup },
})

// 打开
UIManager.Instance.Open<ShopController>()           // 无参数
UIManager.Instance.Open<ShopController>(args: 1)    // 有参数

// 关闭
UIManager.Instance.Close<ShopController>()

// 紧急关闭
UIManager.Instance.CloseAll()

// 清空缓存
UIManager.Instance.ClearCache()


// ============================================================
// 10. 生命周期流程图
// ============================================================

// === 打开流程 ===
// EnqueueOpen(uiKey, args)
//   ├─ 已打开（Opened）→ 直接刷新 OnOpen(args)
//   ├─ 已在队列 → 刷新 args
//   ├─ 队列忙 → 入队等待
//   └─ 空闲 → 执行
//
// ExecuteOpen(item):
//   1. 创建 UIContext, 状态 → Loading
//   2. 获取/创建 Controller（持久化，首次 OnInit）
//   3. Controller.CreateViewAsync()
//      ├─ 检查缓存 → 复用
//      └─ 异步加载 Prefab → 实例化
//   4. 【状态检查】是否仍为 Loading？
//   5. RegisterContext → 层级管理
//   6. 状态 → AnimationEnter
//   7. 禁用 Selectable + 遮罩(Popup)
//   8. OnOpen(args)
//   9. PlayEnterAnimation → await
//   10. 【状态检查】是否仍为 AnimationEnter？
//   11. 状态 → Opened
//   12. 恢复 Selectable + SetInteractive(true)
//   13. OnShown()
//   14. _enteringContext = null
//   15. ProcessNext()

// === 关闭流程 ===
// StartExit(ctx):
//   1. 检查状态必须是 Opened
//   2. 状态 → AnimationExit
//   3. OnHide()
//   4. SetInteractive(false)
//   5. PlayExitAnimation → await
//   6. 状态 → Closed
//   7. CacheView (SetActive(false)，不销毁)
//   8. UnregisterContext
//   9. RestorePreviousNormal（若有）
//   10. _exitingContext = null
//   11. ProcessNext()

// === 关闭全部 ===
// CloseAll():
//   清空队列 + 清空通道 + Destroy 所有活跃 UI + 清空缓存 + 清空 Controller