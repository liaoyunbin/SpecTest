/* ============================================================
 * UIFrameworkLib — 完整伪代码
 * 命名空间：UIFrameworkLib
 * 语言：C# (Unity)
 * ============================================================ */

// ============================================================
// 1. CORE — 核心模型
// ============================================================

// --- 1a. UILayer ---
enum UILayer
{
    Background, // 底层背景，打开时弹出其上所有 Normal 和 Popup
    Normal,     // 标准面板，入栈，支持 Back
    Popup,      // 弹窗，相互替换，自带遮罩
}

// --- 1b. UIStateMachine ---
enum UIState
{
    None,
    Loading,
    AnimationEnter,
    Opened,
    AnimationExit,
    Closed,
}

class UIStateMachine
{
    UIState CurrentState             // 当前状态
    bool    IsTransitioning          // 是否处于过渡中（Loading/AnimEnter/AnimExit）
    event OnStateChanged(old, new)   // 状态变更事件

    bool TryTransitionTo(UIState newState)
    {
        // 查 ValidTransitions 映射表
        // 合法 → 切换 + 触发事件
        // 非法 → LogWarning + return false
    }

    // 合法转换表（硬编码）:
    //   None → Loading
    //   Loading → AnimationEnter, Closed
    //   AnimationEnter → Opened, Closed
    //   Opened → AnimationExit
    //   AnimationExit → Closed, AnimationEnter
    //   Closed → Loading
}

// --- 1c. UIItemConfig ---
class UIItemConfig
{
    string  UIKey           // 唯一标识
    string  PrefabPath      // Resources 路径
    UILayer Layer           // 层级（默认 Normal）
    int     EnterAnimId     // 入场动画策略 ID（默认 0）
    int     ExitAnimId      // 退场动画策略 ID（默认 0）
}

// --- 1d. UIContext ---
class UIContext : IDisposable
{
    // 标识
    string  UIKey
    int     InstanceId           // 自增 ID

    // 核心引用
    UIView           View         // 表现层
    IUIController    Controller   // 逻辑层
    UIItemConfig     Config       // 配置

    // 状态
    UIStateMachine   StateMachine
    CancellationTokenSource Cts  // 异步取消
    float            OpenTime    // 打开时间戳
    UIContext        PreviousContext  // 上一个 Normal

    // 方法
    void Bind(UIView view, IUIController controller)
    void Cancel()                // 取消加载/动画
    void Dispose()               // 释放 Cts + 清理
}

// --- 1e. UIKey解析（命名约定）---
string ResolveUIKey<T>()
{
    // 类名去掉 "Controller" 后缀
    // 示例: ShopController → "Shop"
    var name = typeof(T).Name
    return name.EndsWith("Controller")
        ? name[..^"Controller".Length]
        : name
}


// ============================================================
// 2. CONTROLLER — 逻辑层
// ============================================================

// --- 2a. IUIController（内部接口，供框架多态调用）---
internal interface IUIController
{
    // 属性
    UIContext Context { get; }

    // 绑定（框架内部调用）
    void BindContext(UIContext context)

    // 生命周期（框架内部调用）
    void OnInit()      // 仅一次
    void OnOpen(args)  // 每次打开
    void OnShown()     // 入场动画结束
    void OnHide()      // 退场开始 / 被覆盖
    void OnDispose()   // 销毁
}

// --- 2b. UIController<T>（基类，业务继承）---
abstract class UIController<T> : IUIController
    where T : UIView
{
    // 公开属性
    T           View              // 强类型 View
    UIContext   Context           // 上下文
    UIItemConfig Config           // 配置（快捷方式）
    CancellationToken CancellationToken  // 取消令牌

    // --- 生命周期（业务重写）---
    protected internal virtual void OnInit()       { }
    protected internal virtual void OnOpen(args)   { }
    protected internal virtual void OnShown()      { }
    protected internal virtual void OnHide()       { }
    protected internal virtual void OnDispose()    { }

    // --- 辅助方法 ---
    protected void CloseSelf()
    {
        // 通过 UIRegistry 解析 UIKey 后调用 UIManager.Close(uiKey)
        var uiKey = UIManager.Instance.Registry.ResolveUIKey(this.GetType())
        UIManager.Instance.Close(uiKey)
    }

    // --- IUIController 显式实现 ---
    void IUIController.BindContext(UIContext context)
    {
        this.Context = context
        this.View = context.View as T
    }
}


// ============================================================
// 3. VIEW — 表现层
// ============================================================

// --- 3a. UIView ---
abstract class UIView : MonoBehaviour
{
    // 属性
    UIContext Context                    // 运行时上下文

    // --- 自动绑定（代码生成器实现）---
    protected abstract void AutoBind()

    // --- 交互控制 ---
    void SetInteractive(bool enabled)    // 控制 CanvasGroup
    void DisableAllSelectables()         // 快照所有 Selectable，设为不可交互
    void RestoreSelectables()            // 恢复快照

    // --- 动画（委托给策略）---
    // 默认从 UIAnimationRegistry 获取策略
    protected virtual IUIAnimationStrategy GetEnterStrategy()
        → UIAnimationRegistry.Get(Context.Config.EnterAnimId)
    protected virtual IUIAnimationStrategy GetExitStrategy()
        → UIAnimationRegistry.Get(Context.Config.ExitAnimId)

    void PlayEnterAnimation(Action onComplete)
    {
        var overlay = UIRoot.Instance?.AnimationOverlay
        GetEnterStrategy().PlayEnter(gameObject, overlay, onComplete)
    }
    void PlayExitAnimation(Action onComplete)
    {
        var overlay = UIRoot.Instance?.AnimationOverlay
        GetExitStrategy().PlayExit(gameObject, overlay, onComplete)
    }

    // --- Popup 遮罩 ---
    protected virtual RectTransform GetOverlay()
        → UIRoot.Instance?.AnimationOverlay

    protected virtual void ShowMask()
    {
        // 在全屏覆盖层上创建/显示半透明遮罩 Image
    }
    protected virtual void HideMask()
    {
        // 隐藏遮罩 Image
    }

    // --- 框架内部调用 ---
    void Internal_SetContext(UIContext context)  // 设置 Context
    void Internal_OnShown()                      // 入场动画结束自动调用
}

// --- 3b. UIRoot ---
class UIRoot : MonoBehaviour
{
    static UIRoot Instance              // 单例

    Transform BackgroundLayer          // 各层级父节点
    Transform NormalLayer
    Transform PopupLayer
    RectTransform AnimationOverlay      // 全屏动画覆盖层（黑屏/白屏载体）

    Transform GetLayer(UILayer layer)   // 根据层级返回对应 Transform

    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        // 1. 创建 Canvas（ScreenSpaceCamera, SortingOrder 基准）
        // 2. 为每层创建 GameObject（各自带 Canvas + GraphicRaycaster）
        //    各层 SortingOrder: Normal=0, Popup=100, Toast=200, Loading=300
        // 3. 创建 AnimationOverlay（全屏 Image, 初始 alpha=0, raycastTarget=false）
        // 4. DontDestroyOnLoad
    }
}


// ============================================================
// 4. MANAGER — 核心调度层
// ============================================================

// --- 4a. UIRegistry ---
class UIRegistry
{
    // ControllerType → UIKey
    Dictionary<Type, string> _controllerToUIKey
    // ControllerType → ViewType
    Dictionary<Type, Type>   _controllerToViewType

    // 启动时统一注册
    void RegisterAll()
    {
        Register<ShopController>()
        Register<BagController>()
        Register<ConfirmPopupController>()
        // 新增 UI 只需要在这里加一行
    }

    void Register<TController>()
        where TController : IUIController
    {
        // 1. 获取 typeof(TController)
        // 2. 遍历基类链，找到 UIController<> 泛型基类
        // 3. 提取泛型参数 → View 类型
        // 4. UIKey = ViewType.Name（如 "ShopPanel"）
        // 5. 存入 _controllerToUIKey 和 _controllerToViewType
    }

    // 查询
    string ResolveUIKey(Type controllerType)       // Controller → UIKey
    Type   GetViewType(Type controllerType)        // Controller → ViewType
    IEnumerable<string> GetAllUIKeys()              // 所有已注册的 Key（调试用）

    // 创建 Controller 实例
    IUIController CreateController(Type controllerType)
    {
        // 检查是否已注册
        return Activator.CreateInstance(controllerType) as IUIController
    }
}

// --- 4b. UIPool ---
class UIPool
{
    // UIKey → Stack<GameObject>
    Dictionary<string, Stack<GameObject>> _pools
    const int MAX_PER_KEY = 5

    int TotalCount                             // 总缓存数

    GameObject Get(string key)                 // 从池中取出
    void       Return(string key, GameObject)  // 归还到池
    void       Clear()                         // 清理所有
}

// --- 4c. UIManager（核心）--- 
class UIManager
{
    // --- 单例 ---
    static UIManager Instance

    // --- 内部组件 ---
    UIConfigLoader        ConfigLoader   // 配置加载器
    UIPool                Pool           // 对象池
    UIRegistry            Registry       // 注册表
    UIResourceLoader      ResourceLoader // 资源加载器（池 + 异步加载 + 实例化）

    // --- 私有状态 ---
    Dictionary<string, UIContext> _activeContexts   // 活跃 UI
    UIContext                     _backgroundContext // 当前 Background
    List<UIContext>               _normalStack       // Normal 栈
    UIContext                     _currentPopup      // 当前 Popup
    Queue<QueueItem>              _queue             // 请求队列
    const int                     MAX_QUEUE_SIZE = 10

    bool   _isProcessing           // 是否正在处理队列
    UIContext _enteringContext     // 当前入场通道
    UIContext _exitingContext      // 当前退场通道
    bool IsBusy → _enteringContext != null || _exitingContext != null

    // ============================================================
    // 对外接口
    // ============================================================

    // 打开（默认 WaitForOpen）
    void Open<T>(object args = null) where T : IUIController
    {
        var uiKey = Registry.ResolveUIKey(typeof(T))
        if uiKey == null → LogError + return
        EnqueueOpen(uiKey, args, QueueMode.WaitForOpen)
    }

    // 打开（指定队列模式）
    void Open<T>(QueueMode mode, object args = null) where T : IUIController
    {
        var uiKey = Registry.ResolveUIKey(typeof(T))
        if uiKey == null → LogError + return
        EnqueueOpen(uiKey, args, mode)
    }

    // 按 UIKey 打开（调试/非泛型）
    void Open(string uiKey, object args = null, QueueMode mode = WaitForOpen)

    // 关闭（泛型）
    void Close<T>() where T : IUIController
    {
        var uiKey = Registry.ResolveUIKey(typeof(T))
        Close(uiKey)
    }

    // 关闭（按 Key）
    void Close(string uiKey)
    {
        // 查 _activeContexts，若 Opened → StartExit(ctx)
    }

    // 紧急关闭所有
    void CloseAll()
    {
        // 清空队列 + 取消通道 + 销毁所有活跃 UI + 清空栈
    }

    // 清空对象池
    void ClearPool() → Pool.Clear()

    // 调试接口
    int ActiveCount → _activeContexts.Count
    IReadOnlyList<UIContext> NormalStack → _normalStack
    IEnumerable<UIContext> GetAllActiveContexts() → _activeContexts.Values

    // ============================================================
    // 队列调度
    // ============================================================

    void EnqueueOpen(string uiKey, object args, QueueMode mode)
    {
        config = ConfigLoader.Get(uiKey)
        if config == null → LogError + return

        // 单例已打开 → 直接刷新 OnOpen
        if _activeContexts 中存在 Opened 的实例
            → Controller.OnOpen(args) + return

        item = new QueueItem(uiKey, args, mode)

        // 忙 → 入队
        if _isProcessing || IsBusy
            if _queue.Count >= MAX_QUEUE_SIZE → 丢弃最旧
            _queue.Enqueue(item)
            return

        // 空闲 → 直接执行
        _isProcessing = true
        ExecuteOpen(item)
    }

    void ProcessNext()
    {
        while _queue.Count > 0
            next = _queue.Peek()

            // WaitForClose：系统中有非 Closed 的 UI → 暂不执行
            if next.Mode == QueueMode.WaitForClose && HasActiveUI()
                return

            _queue.Dequeue()
            _isProcessing = true
            ExecuteOpen(next)
            return

        _isProcessing = false
    }

    bool HasActiveUI()
    {
        return _enteringContext != null
            || _exitingContext != null
            || _activeContexts.Values 中存在 State != Closed 的 Context
    }

    // ============================================================
    // 异步加载 + 入场
    // ============================================================

    async void ExecuteOpen(QueueItem item)
    {
        config = ConfigLoader.Get(item.UIKey)
        if config == null → LogError + next

        ctx = new UIContext(item.UIKey, config)
        ctx.Cts = new CancellationTokenSource()
        ctx.StateMachine → Loading
        _enteringContext = ctx

        try:
            // 1. 加载 + 实例化（委托给 ResourceLoader）
            loadResult = await ResourceLoader.LoadAsync(config, ctx.Cts.Token)
            if loadResult == null → Closed + Cleanup + next
            if ctx.Cts cancelled → Destroy + Cleanup + next

            view = loadResult.View
            view.Internal_SetContext(ctx)

            // 2. AutoBind
            SafeExecute(view.AutoBind)

            // 3. 创建 Controller + 绑定
            controller = Registry.CreateController(Registry.GetControllerType(item.UIKey))
            if controller != null → ctx.Bind(view, controller)

            // 7. 注册到活跃列表
            RegisterContext(ctx)
            ctx.StateMachine → AnimationEnter

            // 8. 入场前准备
            view.SetInteractive(false)
            view.DisableAllSelectables()
            if config.Layer == Popup → view.ShowMask()

            // 9. 生命周期
            SafeExecute(controller.OnInit)
            SafeExecute(controller.OnOpen(item.Args))

            // 10. 入场动画
            tcs = TaskCompletionSource<bool>
            view.PlayEnterAnimation(() → tcs.SetResult(true))
            await tcs.Task.WithCancellation(ct)
            if ct cancelled → CleanupAndNext + return

            // 11. 动画结束 → Opened
            ctx.StateMachine → Opened
            view.RestoreSelectables()
            view.SetInteractive(true)
            ctx.OpenTime = Time.time
            SafeExecute(controller.OnShown)

        catch OperationCanceledException → 正常取消，忽略
        catch Exception e → LogError + CleanupAndNext + return

        finally:
            _enteringContext = null
            ctx.Cts.Dispose()
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

            tcs = TaskCompletionSource<bool>
            ctx.View.PlayExitAnimation(() → tcs.SetResult(true))
            await tcs.Task

            ctx.StateMachine → Closed

            // 缓存到对象池
            SafeExecute(ctx.Controller.OnDispose)
            ctx.View.gameObject.SetActive(false)
            Pool.Return(ctx.UIKey, ctx.View.gameObject)

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
        key = ctx.UIKey
        _activeContexts[key] = ctx

        if ctx.Config.Layer == Normal
            if _normalStack 不为空
                上一个 Normal → OnHide
                ctx.PreviousContext = 上一个
            _normalStack.Add(ctx)
        else if ctx.Config.Layer == Popup
            ctx.PreviousContext = _normalStack 栈顶（可为 null）
    }

    void UnregisterContext(UIContext ctx)
    {
        _activeContexts.Remove(ctx.UIKey)
        if ctx.Config.Layer == Normal → _normalStack.Remove(ctx)
    }

    void RestorePreviousNormal()
    {
        if _normalStack 不为空
            栈顶 Normal 且状态为 Opened
                → Controller.OnShown + view.SetInteractive(true)
    }
}


// ============================================================
// 5. ANIMATION — 动画策略层
// ============================================================

// --- 5a. 策略接口 ---
interface IUIAnimationStrategy
{
    // target: 目标 GameObject
    // overlay: 全屏覆盖层 RectTransform（黑屏/白屏载体，可为 null）
    // onComplete: 动画完成回调
    void PlayEnter(GameObject target, RectTransform overlay, Action onComplete)
    void PlayExit(GameObject target, RectTransform overlay, Action onComplete)
}

// --- 5b. 淡入淡出 ---
class FadeStrategy : IUIAnimationStrategy
{
    void PlayEnter(target, overlay, onComplete)
    {
        // CanvasGroup alpha 0→1, 0.3s, OutQuad
    }
    void PlayExit(target, overlay, onComplete)
    {
        // CanvasGroup alpha 1→0, 0.2s, InQuad
    }
}

// --- 5c. 黑屏渐入渐出 ---
class BlackFadeStrategy : IUIAnimationStrategy
{
    // 参数：fadeInDuration, holdDuration, fadeOutDuration, overlayColor

    void PlayEnter(target, overlay, onComplete)
    {
        if overlay == null → 回退到 FadeStrategy

        // 1. 遮罩 alpha 0→1（黑屏）
        // 2. 黑屏中 target alpha = 1（显示目标）
        // 3. 遮罩 alpha 1→0（恢复）
    }
    void PlayExit(target, overlay, onComplete)
    {
        if overlay == null → 回退到 FadeStrategy

        // 1. 遮罩 alpha 0→1 + target alpha 1→0（同时）
        // 2. 遮罩 alpha 1→0（恢复）
    }
}

// --- 5d. 缩放 ---
class ScaleStrategy : IUIAnimationStrategy
{
    void PlayEnter(target, overlay, onComplete)
    {
        // scale 0→1 + alpha 0→1（弹性动画）
    }
    void PlayExit(target, overlay, onComplete)
    {
        // scale 1→0.8 + alpha 1→0
    }
}

// --- 5e. 滑入滑出 ---
class SlideStrategy : IUIAnimationStrategy
{
    enum Direction { Left, Right, Top, Bottom }

    void PlayEnter(target, overlay, onComplete)
    {
        // 从指定方向滑入
    }
    void PlayExit(target, overlay, onComplete)
    {
        // 从指定方向滑出
    }
}

// --- 5f. 动画注册中心 ---
static class UIAnimationRegistry
{
    static Dictionary<int, Func<IUIAnimationStrategy>> _registry

    static constructor()
    {
        // ID 0: Fade（默认）
        Register(0, () → new FadeStrategy())
        // ID 1: 黑屏淡入淡出
        Register(1, () → new BlackFadeStrategy(...))
        // ID 2: 白屏闪光
        Register(2, () → new BlackFadeStrategy(overlayColor: white))
        // ID 3: 缩放
        Register(3, () → new ScaleStrategy())
        // ID 4: 左侧滑入
        Register(4, () → new SlideStrategy(Left))
        // ID 5: 右侧滑入
        Register(5, () → new SlideStrategy(Right))
    }

    static void Register(int id, Func<IUIAnimationStrategy> factory)
    static IUIAnimationStrategy Get(int id)
        → _registry.TryGet(id) ?? _registry[0]()
}

// --- 5g. 组合策略（可选扩展）---
class CompositeStrategy : IUIAnimationStrategy
{
    // 同时播放多个策略（如 Fade + Scale）
    List<IUIAnimationStrategy> _strategies

    void PlayEnter(target, overlay, onComplete)
    {
        int count = _strategies.Count
        foreach strategy in _strategies
            strategy.PlayEnter(target, overlay, () → if --count == 0 → onComplete)
    }
}


// ============================================================
// 6. CONFIG — 配置加载器
// ============================================================

class UIConfigLoader
{
    Dictionary<string, UIItemConfig> _configMap

    // 从外部 List<UIItemConfig> 加载
    void Load(List<UIItemConfig> configs)
    {
        foreach config in configs
            _configMap[config.UIKey] = config
    }

    // 按 Key 查找
    UIItemConfig Get(string uiKey)
        → _configMap.TryGet(uiKey)

    // 获取所有 Key
    IEnumerable<string> GetAllKeys()
        → _configMap.Keys

    // 运行时热重载
    void Reload(List<UIItemConfig> configs)
    {
        _configMap.Clear()
        Load(configs)
    }
}


// ============================================================
// 7. RESOURCE LOADER — 资源加载器
// ============================================================

class UIResourceLoader
{
    UIPool _pool

    UIResourceLoader(UIPool pool)

    // 异步加载 + 实例化 UI
    async Task<UIResourceLoadResult> LoadAsync(UIItemConfig config, CancellationToken ct)
    {
        // 1. 优先从池获取
        go = _pool.Get(config.UIKey)
        if go != null → return InstantiateUI(go, config)

        // 2. 异步加载 Prefab
        go = await LoadPrefabAsync(config, ct)
        if go == null || ct cancelled → return null

        // 3. 实例化
        return InstantiateUI(go, config)
    }

    // 异步加载 Prefab（可被子类重写，支持 Addressables / AssetBundle）
    protected virtual async Task<GameObject> LoadPrefabAsync(UIItemConfig config, CancellationToken ct)
    {
        // Resources.LoadAsync + 5s 超时警告
    }

    // 实例化 + 获取 UIView
    protected virtual UIResourceLoadResult InstantiateUI(GameObject prefab, UIItemConfig config)
    {
        instance = Instantiate(prefab, UIRoot.Instance.GetLayer(config.Layer))
        view = instance.GetComponent<UIView>()
        return new UIResourceLoadResult { Instance = instance, View = view }
    }
}

// 加载结果
class UIResourceLoadResult
{
    GameObject Instance
    UIView     View
}


// ============================================================
// 8. EDITOR — 编辑器工具
// ============================================================

// --- 8a. UIMark（标记组件）---
class UIMark : MonoBehaviour
{
    string Comment    // 备注（可选）
}

// --- 8b. UIEditorWindow（调试窗口）---
class UIEditorWindow : EditorWindow
{
    [MenuItem("UIFrameworkLib/UI Debugger")]
    static void Open()

    void OnGUI()
    {
        // 标题：UIFrameworkLib Debugger

        // 当前活跃 UI 列表
        //   UIKey | State | Layer | OpenTime | Actions[Close]
        //   遍历 UIManager.GetAllActiveContexts()

        // Normal 栈
        //   遍历 UIManager.NormalStack → UIKey

        // 对象池统计
        //   总缓存数: UIManager.Pool.TotalCount

        // 操作按钮
        //   [CloseAll] [ClearPool]

        // 手动打开 UI
        //   下拉框选择 UIKey（从 Registry.GetAllUIKeys() 获取）
        //   [Open] 按钮

        // 自动刷新（EditorApplication.update += Repaint）
    }
}


// ============================================================
// 8. 启动流程（UIBootstrapper）
// ============================================================

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void Bootstrap()
{
    // 1. 初始化 UIRoot（创建 Canvas + 层级节点）
    UIRoot.Initialize()

    // 2. 注册所有 UI 映射
    UIManager.Instance.Registry.RegisterAll()

    // 3. 加载配置（外部适配：Luban / ScriptableObject / JSON → List&lt;UIItemConfig&gt;）
    var configs = ExternalConfigAdapter.LoadUIItemConfigs()
    UIManager.Instance.ConfigLoader.Load(configs)

    Debug.Log("[UIFrameworkLib] 启动完成")
}


// ============================================================
// 9. 业务使用示例
// ============================================================

// --- 9a. 定义 View ---
// ShopView.cs（UIMark 生成绑定代码）
class ShopView : UIView
{
    Button m_BtnClose
    Text   m_TxtGold
    Transform m_ItemRoot

    override void AutoBind()
    {
        // 由代码生成器自动填充
        m_BtnClose = transform.Find("BtnClose").GetComponent<Button>()
        m_TxtGold  = transform.Find("TxtGold").GetComponent<Text>()
        m_ItemRoot = transform.Find("ItemRoot")
    }
}

// --- 9b. 定义 Controller ---
// ShopController.cs
class ShopController : UIController<ShopView>
{
    override void OnInit()
    {
        View.m_BtnClose.onClick.AddListener(CloseSelf)
    }

    override void OnOpen(object args)
    {
        int categoryId = (int)(args ?? 0)
        RefreshUI(categoryId)
    }

    override void OnDispose()
    {
        View.m_BtnClose.onClick.RemoveAllListeners()
    }

    void RefreshUI(int categoryId)
    {
        View.m_TxtGold.text = PlayerData.Gold.ToString()
    }
}

// --- 9c. 调用 ---
void GameStart()
{
    // 打开商店（打开后调入道具列表）
    UIManager.Instance.Open<ShopController>(categoryId: 1)

    // 打开背包（连续打开，自动排队）
    UIManager.Instance.Open<BagController>()

    // 弹出确认对话框（等待上一个界面完全关闭后出现）
    UIManager.Instance.Open<ConfirmPopupController>(QueueMode.WaitForClose)

    // 关闭商店
    UIManager.Instance.Close<ShopController>()

    // 关闭所有（场景转换时）
    UIManager.Instance.CloseAll()
}


// ============================================================
// 附录：队列模式枚举
// ============================================================

enum QueueMode
{
    WaitForOpen,     // 前一个 UI 到达 Opened 后执行（默认）
    WaitForClose,    // 前一个 UI 完全关闭后执行
}

// 队列元素
class QueueItem
{
    string    UIKey
    object    Args
    QueueMode Mode
}