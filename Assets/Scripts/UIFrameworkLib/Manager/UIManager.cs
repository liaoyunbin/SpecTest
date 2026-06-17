using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIFrameworkLib
{
    /// <summary>
    /// UIManager — UI 框架核心管理器
    ///
    /// 职责：
    /// - 提供 Open&lt;T&gt;() / Close&lt;T&gt;() 对外接口
    /// - 管理 Controller 持久化与队列调度
    /// - 双通道动画（退场 + 进场可重叠）
    /// - Normal 栈管理
    /// - 隐藏 View 缓存（复用上次关闭的实例，不销毁）
    ///
    /// 使用方式：
    /// <code>
    /// // 打开 UI
    /// UIManager.Instance.Open&lt;ShopController&gt;();
    /// UIManager.Instance.Open&lt;ShopController&gt;(categoryId: 1);
    ///
    /// // 关闭 UI
    /// UIManager.Instance.Close&lt;ShopController&gt;();
    /// </code>
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        // ================================================================
        // 构造
        // ================================================================

        public UIManager() { }

        // ================================================================
        // 内部字段
        // ================================================================

        /// <summary>配置加载器</summary>
        public UIConfigLoader ConfigLoader { get; } = new();

        /// <summary>缓存的隐藏 View（关闭时 setActive(false) 保留，复用减少加载）</summary>
        public readonly Dictionary<string, GameObject> CachedViews = new();

        /// <summary>持久化的 Controller（首次创建后常驻）</summary>
        private readonly Dictionary<string, IUIController> _controllers = new();

        /// <summary>已调用过 OnInit 的 Controller</summary>
        private readonly HashSet<string> _initializedControllers = new();

        /// <summary>活跃的 UI Context 字典</summary>
        private readonly Dictionary<string, UIContext> _activeContexts = new();

        /// <summary>当前 Background（同一时间只允许一个）</summary>
        private UIContext _backgroundContext;

        /// <summary>Normal 栈</summary>
        private readonly List<UIContext> _normalStack = new();

        /// <summary>当前 Popup（相互替换）</summary>
        private UIContext _currentPopup;

        /// <summary>请求队列</summary>
        private readonly Queue<QueueItem> _queue = new();

        /// <summary>队列最大长度</summary>
        private const int MAX_QUEUE_SIZE = 10;

        /// <summary>是否正在处理队列</summary>
        private bool _isProcessing;

        // ================================================================
        // 双通道
        // ================================================================

        /// <summary>当前正退出的 UI</summary>
        private UIContext _exitingContext;

        /// <summary>当前正进入的 UI</summary>
        private UIContext _enteringContext;

        private bool IsBusy => _exitingContext != null || _enteringContext != null;

        // ================================================================
        // 公开属性
        // ================================================================

        /// <summary>当前活跃的 UI 数</summary>
        public int ActiveCount => _activeContexts.Count;

        /// <summary>当前 Normal 栈（只读）</summary>
        public IReadOnlyList<UIContext> NormalStack => _normalStack;

        /// <summary>所有活跃的 Context（供调试工具使用）</summary>
        public IEnumerable<UIContext> GetAllActiveContexts() => _activeContexts.Values;

        /// <summary>缓存的 View 数</summary>
        public int CachedViewCount => CachedViews.Count;

        // ================================================================
        // 对外接口
        // ================================================================

        /// <summary>通过 Controller 类型打开 UI</summary>
        public void Open<T>(object args = null) where T : IUIController
        {
            var uiKey = ResolveUIKey<T>();
            EnqueueOpen(uiKey, args);
        }

        /// <summary>通过 UIKey 打开 UI（供编辑器调试或非泛型场景使用）</summary>
        public void Open(string uiKey, object args = null)
        {
            EnqueueOpen(uiKey, args);
        }

        /// <summary>通过 Controller 类型关闭 UI</summary>
        public void Close<T>() where T : IUIController
        {
            var uiKey = ResolveUIKey<T>();
            Close(uiKey);
        }

        /// <summary>通过 UIKey 关闭 UI</summary>
        public void Close(string uiKey)
        {
            if (_activeContexts.TryGetValue(uiKey, out var ctx)
                && ctx.StateMachine.CurrentState == UIState.Opened)
            {
                StartExit(ctx);
            }
        }

        /// <summary>立即关闭所有 UI（场景切换等紧急情况）</summary>
        public void CloseAll()
        {
            _queue.Clear();
            _isProcessing = false;

            if (_enteringContext != null)
            {
                CleanupContext(_enteringContext);
                _enteringContext = null;
            }

            if (_exitingContext != null)
            {
                CleanupContext(_exitingContext);
                _exitingContext = null;
            }

            // 关闭所有活跃 UI
            var allKeys = _activeContexts.Keys.ToList();
            foreach (var key in allKeys)
            {
                if (_activeContexts.TryGetValue(key, out var ctx))
                {
                    try
                    {
                        ctx.StateMachine.TryTransitionTo(UIState.Closed);
                        ctx.Controller?.OnDispose();
                        if (ctx.View != null && ctx.View.gameObject != null)
                            Object.Destroy(ctx.View.gameObject);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[UIFrameworkLib] CloseAll 清理 {key} 异常: {e}");
                    }
                }
            }
            _activeContexts.Clear();
            _normalStack.Clear();
            _backgroundContext = null;
            _currentPopup = null;

            // 清理缓存 View
            foreach (var kv in CachedViews)
            {
                if (kv.Value != null) Object.Destroy(kv.Value);
            }
            CachedViews.Clear();

            // 清理持久 Controller
            foreach (var kv in _controllers)
            {
                try { (kv.Value as IDisposable)?.Dispose(); }
                catch { /* 忽略 */ }
            }
            _controllers.Clear();
            _initializedControllers.Clear();
        }

        /// <summary>清空所有缓存的隐藏 View</summary>
        public void ClearCache()
        {
            foreach (var kv in CachedViews)
            {
                if (kv.Value != null) Object.Destroy(kv.Value);
            }
            CachedViews.Clear();
        }

        // ================================================================
        // View 缓存（供 Controller 工具方法调用）
        // ================================================================

        /// <summary>获取缓存的隐藏 View GameObject</summary>
        public GameObject GetCachedView(string uiKey)
        {
            return CachedViews.TryGetValue(uiKey, out var go) && go != null ? go : null;
        }

        /// <summary>缓存隐藏的 View（关闭时调用）</summary>
        public void CacheView(string uiKey, GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            CachedViews[uiKey] = go;
        }

        // ================================================================
        // 队列调度
        // ================================================================

        private void EnqueueOpen(string uiKey, object args)
        {
            var config = ConfigLoader.Get(uiKey);
            if (config == null)
            {
                Debug.LogError($"[UIFrameworkLib] 未找到配置: {uiKey}");
                return;
            }

            // 同一界面已打开 → 直接刷新
            if (_activeContexts.TryGetValue(uiKey, out var existing)
                && existing.StateMachine.CurrentState == UIState.Opened)
            {
                existing.Controller?.OnOpen(args);
                return;
            }

            // 同一界面已在队列中 → 刷新参数（去重）
            var sameInQueue = _queue.FirstOrDefault(q => q.UIKey == uiKey);
            if (sameInQueue != null)
            {
                sameInQueue.Args = args;
                return;
            }

            // 有任务在执行 → 入队
            if (_isProcessing || IsBusy)
            {
                if (_queue.Count >= MAX_QUEUE_SIZE)
                {
                    var dropped = _queue.Dequeue();
                    Debug.LogWarning($"[UIFrameworkLib] 队列满，丢弃最旧: {dropped.UIKey}");
                }
                _queue.Enqueue(new QueueItem(uiKey, args));
                return;
            }

            // 无任务 → 直接执行
            _isProcessing = true;
            ExecuteOpen(new QueueItem(uiKey, args));
        }

        private void ProcessNext()
        {
            if (_queue.Count > 0)
            {
                var next = _queue.Dequeue();
                ExecuteOpen(next);
            }
            else
            {
                _isProcessing = false;
            }
        }

        // ================================================================
        // 执行打开
        // ================================================================

        private async void ExecuteOpen(QueueItem item)
        {
            var config = ConfigLoader.Get(item.UIKey);
            if (config == null)
            {
                Debug.LogError($"[UIFrameworkLib] ExecuteOpen 配置为空: {item.UIKey}");
                _isProcessing = false;
                ProcessNext();
                return;
            }

            var ctx = new UIContext(item.UIKey, config);
            ctx.StateMachine.TryTransitionTo(UIState.Loading);
            _enteringContext = ctx;

            try
            {
                // ---- 1. 获取或创建 Controller ----
                IUIController controller;
                if (!_controllers.TryGetValue(item.UIKey, out controller))
                {
                    var typeName = $"{GetControllerTypeNamespace()}.{item.UIKey}Controller";
                    controller = CreateController(typeName);
                    if (controller == null)
                    {
                        ctx.StateMachine.TryTransitionTo(UIState.Closed);
                        CleanupAndNext(ctx);
                        return;
                    }
                    _controllers[item.UIKey] = controller;
                }

                ctx.BindController(controller);

                // 首次创建时才调用 OnInit
                if (_initializedControllers.Add(item.UIKey))
                {
                    SafeExecute(() => controller.OnInit(), $"{item.UIKey}.OnInit");
                }

                // ---- 2. Controller 创建 View ----
                // CreateViewAsync 会走 Controller 上的工具方法（LoadFromResources 等）
                var viewTask = controller.CreateViewAsync();
                // 使用状态机检查替代 CancellationToken
                var view = await viewTask;
                if (ctx.StateMachine.CurrentState != UIState.Loading)
                {
                    // 被 CloseAll 等情况中断
                    if (view != null && view.gameObject != null)
                        Object.Destroy(view.gameObject);
                    CleanupAndNext(ctx);
                    return;
                }

                if (view == null)
                {
                    Debug.LogError($"[UIFrameworkLib] {item.UIKey} CreateViewAsync 返回 null");
                    ctx.StateMachine.TryTransitionTo(UIState.Closed);
                    CleanupAndNext(ctx);
                    return;
                }

                view.Controller = controller;
                view.Internal_SetContext(ctx);
                ctx.SetView(view);

                // ---- 3. 注册到活跃列表 ----
                RegisterContext(ctx);
                ctx.StateMachine.TryTransitionTo(UIState.AnimationEnter);

                // ---- 4. 入场动画 ----
                view.SetInteractive(false);
                view.DisableAllSelectables();
                if (config.Layer == UILayer.Popup) view.ShowMask();

                SafeExecute(() => controller.OnOpen(item.Args), $"{item.UIKey}.OnOpen");

                // 等待入场动画
                var animTcs = new UniTaskCompletionSource();
                view.PlayEnterAnimation(() => animTcs.TrySetResult());
                await animTcs.Task;

                if (ctx.StateMachine.CurrentState != UIState.AnimationEnter)
                {
                    // 动画期间被 CloseAll 中断
                    CleanupAndNext(ctx);
                    return;
                }

                // ---- 5. 动画结束 → Opened ----
                ctx.StateMachine.TryTransitionTo(UIState.Opened);
                view.RestoreSelectables();
                view.SetInteractive(true);
                ctx.OpenTime = Time.time;

                SafeExecute(() => controller.OnShown(), $"{item.UIKey}.OnShown");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIFrameworkLib] {item.UIKey} 加载异常: {e}");
                CleanupAndNext(ctx);
            }
            finally
            {
                _enteringContext = null;
                ProcessNext();
            }
        }

        // ================================================================
        // 退场
        // ================================================================

        private async void StartExit(UIContext ctx)
        {
            if (ctx.StateMachine.CurrentState != UIState.Opened) return;

            ctx.StateMachine.TryTransitionTo(UIState.AnimationExit);
            _exitingContext = ctx;

            try
            {
                SafeExecute(() => ctx.Controller?.OnHide(), $"{ctx.UIKey}.OnHide");
                ctx.View.SetInteractive(false);

                var tcs = new UniTaskCompletionSource();
                ctx.View.PlayExitAnimation(() => tcs.TrySetResult());
                await tcs.Task;

                ctx.StateMachine.TryTransitionTo(UIState.Closed);

                // 不销毁 View，缓存起来下次复用
                CacheView(ctx.UIKey, ctx.View.gameObject);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIFrameworkLib] {ctx.UIKey} 退场异常: {e}");
            }
            finally
            {
                UnregisterContext(ctx);
                _exitingContext = null;
                RestorePreviousNormal();
                if (!IsBusy) ProcessNext();
            }
        }

        // ================================================================
        // 栈管理
        // ================================================================

        private void RegisterContext(UIContext ctx)
        {
            _activeContexts[ctx.UIKey] = ctx;

            switch (ctx.Config.Layer)
            {
                case UILayer.Background:
                    CloseAllNormalAndPopup();
                    if (_backgroundContext != null && _backgroundContext != ctx)
                        StartExit(_backgroundContext);
                    _backgroundContext = ctx;
                    break;

                case UILayer.Normal:
                    if (_normalStack.Count > 0)
                    {
                        var prev = _normalStack[^1];
                        SafeExecute(() => prev.Controller?.OnHide(), $"{prev.UIKey}.OnHide(from stack)");
                        ctx.PreviousContext = prev;
                    }
                    _normalStack.Add(ctx);
                    break;

                case UILayer.Popup:
                    if (_currentPopup != null && _currentPopup != ctx)
                        StartExit(_currentPopup);
                    _currentPopup = ctx;
                    ctx.PreviousContext = _normalStack.Count > 0 ? _normalStack[^1] : null;
                    break;
            }
        }

        private void UnregisterContext(UIContext ctx)
        {
            _activeContexts.Remove(ctx.UIKey);

            switch (ctx.Config.Layer)
            {
                case UILayer.Background:
                    if (_backgroundContext == ctx) _backgroundContext = null;
                    break;
                case UILayer.Normal:
                    _normalStack.Remove(ctx);
                    break;
                case UILayer.Popup:
                    if (_currentPopup == ctx) _currentPopup = null;
                    break;
            }
        }

        private void RestorePreviousNormal()
        {
            if (_normalStack.Count > 0)
            {
                var top = _normalStack[^1];
                if (top.StateMachine.CurrentState == UIState.Opened)
                {
                    SafeExecute(() => top.Controller?.OnShown(), $"{top.UIKey}.OnShown(from restore)");
                    top.View.SetInteractive(true);
                }
            }
        }

        private void CloseAllNormalAndPopup()
        {
            if (_currentPopup != null)
            {
                if (_currentPopup.StateMachine.CurrentState == UIState.Opened)
                    StartExit(_currentPopup);
                _currentPopup = null;
            }
            foreach (var normal in _normalStack.ToArray())
            {
                if (normal.StateMachine.CurrentState == UIState.Opened)
                    StartExit(normal);
            }
            _normalStack.Clear();
        }

        // ================================================================
        // 辅助方法
        // ================================================================

        private string ResolveUIKey<T>() where T : IUIController
        {
            var name = typeof(T).Name;
            return name.EndsWith("Controller")
                ? name[..^"Controller".Length]
                : name;
        }

        /// <summary>
        /// 获取 Controller 所在的命名空间。
        /// 业务方可覆盖此方法以指定自定义命名空间。
        /// </summary>
        protected virtual string GetControllerTypeNamespace()
        {
            return "UIFrameworkLib";
        }

        private static IUIController CreateController(string controllerTypeName)
        {
            if (string.IsNullOrEmpty(controllerTypeName)) return null;

            try
            {
                var type = Type.GetType(controllerTypeName);
                if (type == null)
                {
                    Debug.LogError($"[UIFrameworkLib] 找不到 Controller 类型: {controllerTypeName}");
                    return null;
                }
                return Activator.CreateInstance(type) as IUIController;
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIFrameworkLib] 创建 Controller 失败 {controllerTypeName}: {e}");
                return null;
            }
        }

        private static void SafeExecute(Action action, string context)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIFrameworkLib] 执行 {context} 时异常: {e}");
            }
        }

        private void CleanupAndNext(UIContext ctx)
        {
            ctx.Dispose();
            UnregisterContext(ctx);
            _enteringContext = null;
            ProcessNext();
        }

        private void CleanupContext(UIContext ctx)
        {
            try
            {
                ctx.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIFrameworkLib] CleanupContext {ctx?.UIKey} 异常: {e}");
            }
            UnregisterContext(ctx);
        }

        // ================================================================
        // 内部类
        // ================================================================

        private class QueueItem
        {
            public string UIKey { get; }
            public object Args { get; set; }

            public QueueItem(string uiKey, object args)
            {
                UIKey = uiKey;
                Args = args;
            }
        }
    }
}