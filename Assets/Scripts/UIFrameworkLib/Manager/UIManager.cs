using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIFrameworkLib
{
    /// <summary>
    /// UIManager — UI 框架核心管理器
    ///
    /// 职责：
    /// - 提供 Open&lt;T&gt;() / Close&lt;T&gt;() 对外接口
    /// - 管理异步加载、队列调度
    /// - 双通道动画（退场 + 进场可重叠）
    /// - Normal 栈管理
    /// - 对象池
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
    public class UIManager
    {
        // ================================================================
        // Singleton
        // ================================================================

        private static UIManager _instance;
        public static UIManager Instance => _instance ??= new UIManager();

        // ================================================================
        // 内部组件
        // ================================================================

        public UIConfigLoader ConfigLoader { get; } = new();
        public UIPool Pool { get; } = new();
        public UIResourceLoader ResourceLoader { get; private set; }

        private UIManager()
        {
            ResourceLoader = new UIResourceLoader(Pool);
        }

        // ================================================================
        // 内部组件
        // ================================================================

        /// <summary>配置加载器</summary>
        public UIConfigLoader ConfigLoader { get; } = new();

        /// <summary>对象池</summary>
        public UIPool Pool { get; } = new();

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

        // ================================================================
        // 对外接口
        // ================================================================

        /// <summary>
        /// 通过 Controller 类型打开 UI
        /// 内部异步加载，调用方无需等待
        /// </summary>
        /// <typeparam name="T">Controller 类型</typeparam>
        /// <param name="args">打开参数</param>
        public void Open<T>(object args = null) where T : IUIController
        {
            var uiKey = ResolveUIKey<T>();
            EnqueueOpen(uiKey, args);
        }

        /// <summary>
        /// 通过 UIKey 打开 UI（供编辑器调试或非泛型场景使用）
        /// </summary>
        public void Open(string uiKey, object args = null)
        {
            EnqueueOpen(uiKey, args);
        }

        /// <summary>
        /// 通过 Controller 类型关闭 UI
        /// </summary>
        /// <typeparam name="T">Controller 类型</typeparam>
        public void Close<T>() where T : IUIController
        {
            var uiKey = ResolveUIKey<T>();
            Close(uiKey);
        }

        /// <summary>
        /// 通过 UIKey 关闭 UI
        /// </summary>
        public void Close(string uiKey)
        {
            var singletonKey = GetSingletonKey(uiKey);
            if (_activeContexts.TryGetValue(singletonKey, out var ctx)
                && ctx.StateMachine.CurrentState == UIState.Opened)
            {
                StartExit(ctx);
            }
        }

        /// <summary>
        /// 立即关闭所有 UI（用于场景切换等紧急情况）
        /// </summary>
        public void CloseAll()
        {
            // 清空队列
            _queue.Clear();
            _isProcessing = false;

            // 取消所有正在进行的操作
            if (_enteringContext != null)
            {
                _enteringContext.Cancel();
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
        }

        /// <summary>清空对象池</summary>
        public void ClearPool() => Pool.Clear();

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

            // 单例已打开 → 直接刷新
            if (_activeContexts.TryGetValue(uiKey, out var existing)
                && existing.StateMachine.CurrentState == UIState.Opened)
            {
                existing.Controller?.OnOpen(args);
                return;
            }

            var item = new QueueItem(uiKey, args);

            // 有任务正在执行 → 入队
            if (_isProcessing || IsBusy)
            {
                if (_queue.Count >= MAX_QUEUE_SIZE)
                {
                    var dropped = _queue.Dequeue();
                    Debug.LogWarning($"[UIFrameworkLib] 队列满，丢弃最旧: {dropped.UIKey}");
                }
                _queue.Enqueue(item);
                return;
            }

            _isProcessing = true;
            ExecuteOpen(item);
        }

        private void ProcessNext()
        {
            if (_queue.Count > 0)
            {
                var next = _queue.Dequeue();
                _isProcessing = true;
                ExecuteOpen(next);
            }
            else
            {
                _isProcessing = false;
            }
        }

        // ================================================================
        // 异步加载 + 入场
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
            ctx.Cts = new CancellationTokenSource();
            var ct = ctx.Cts.Token;

            ctx.StateMachine.TryTransitionTo(UIState.Loading);
            _enteringContext = ctx;

            try
            {
                // ---- 1. 加载 Prefab + 实例化 ----
                var loadResult = await ResourceLoader.LoadAsync(config, ct);
                if (loadResult == null)
                {
                    ctx.StateMachine.TryTransitionTo(UIState.Closed);
                    CleanupAndNext(ctx);
                    return;
                }

                if (ct.IsCancellationRequested) { Object.Destroy(loadResult.Instance); CleanupAndNext(ctx); return; }

                var view = loadResult.View;
                view.Internal_SetContext(ctx);

                // ---- 2. 自动绑定 ----
                SafeExecute(() => view.AutoBind(), $"{item.UIKey}.AutoBind");

                // ---- 3. 创建 Controller ----
                var controllerTypeName = $"{GetControllerTypeNamespace()}.{item.UIKey}Controller";
                var controller = CreateController(controllerTypeName);
                if (controller != null)
                {
                    ctx.Bind(view, controller);
                }

                // ---- 6. 注册到活动列表 ----
                RegisterContext(ctx);
                ctx.StateMachine.TryTransitionTo(UIState.AnimationEnter);

                // ---- 7. 入场动画 ----
                view.SetInteractive(false);
                view.DisableAllSelectables();
                if (config.Layer == UILayer.Popup) view.ShowMask();

                SafeExecute(() => controller?.OnInit(), $"{item.UIKey}.OnInit");
                SafeExecute(() => controller?.OnOpen(item.Args), $"{item.UIKey}.OnOpen");

                // 等待入场动画
                var animTcs = new TaskCompletionSource<bool>();
                view.PlayEnterAnimation(() => animTcs.TrySetResult(true));
                await animTcs.Task.WithCancellation(ct);

                if (ct.IsCancellationRequested) { CleanupAndNext(ctx); return; }

                // ---- 8. 动画结束 → Opened ----
                ctx.StateMachine.TryTransitionTo(UIState.Opened);
                view.RestoreSelectables();
                view.SetInteractive(true);
                ctx.OpenTime = Time.time;

                SafeExecute(() => controller?.OnShown(), $"{item.UIKey}.OnShown");
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不做处理
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIFrameworkLib] {item.UIKey} 加载异常: {e}");
                CleanupAndNext(ctx);
                return;
            }
            finally
            {
                _enteringContext = null;
                ctx.Cts?.Dispose();
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

                var tcs = new TaskCompletionSource<bool>();
                ctx.View.PlayExitAnimation(() => tcs.TrySetResult(true));
                await tcs.Task;

                ctx.StateMachine.TryTransitionTo(UIState.Closed);

                SafeExecute(() => ctx.Controller?.OnDispose(), $"{ctx.UIKey}.OnDispose");
                ctx.View.gameObject.SetActive(false);
                Pool.Return(ctx.UIKey, ctx.View.gameObject);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIFrameworkLib] {ctx.UIKey} 退场异常: {e}");
            }
            finally
            {
                UnregisterContext(ctx);
                _exitingContext = null;

                // 恢复栈中上一个 Normal
                RestorePreviousNormal();

                if (!IsBusy) ProcessNext();
            }
        }

        // ================================================================
        // 栈管理
        // ================================================================

        /// <summary>
        /// 注册 Context 到活跃列表，按层级执行对应管理策略
        /// </summary>
        private void RegisterContext(UIContext ctx)
        {
            _activeContexts[ctx.UIKey] = ctx;

            switch (ctx.Config.Layer)
            {
                case UILayer.Background:
                    // 打开 Background → 弹出其上的所有 Normal 和 Popup
                    CloseAllNormalAndPopup();
                    // 替换当前 Background
                    if (_backgroundContext != null && _backgroundContext != ctx)
                        StartExit(_backgroundContext);
                    _backgroundContext = ctx;
                    break;

                case UILayer.Normal:
                    // 隐藏上一个 Normal（入栈）
                    if (_normalStack.Count > 0)
                    {
                        var prev = _normalStack[^1];
                        SafeExecute(() => prev.Controller?.OnHide(), $"{prev.UIKey}.OnHide(from stack)");
                        ctx.PreviousContext = prev;
                    }
                    _normalStack.Add(ctx);
                    break;

                case UILayer.Popup:
                    // 替换当前 Popup
                    if (_currentPopup != null && _currentPopup != ctx)
                        StartExit(_currentPopup);
                    _currentPopup = ctx;
                    // 记录栈顶 Normal（用于后续 Back 链路）
                    ctx.PreviousContext = _normalStack.Count > 0 ? _normalStack[^1] : null;
                    break;
            }
        }

        /// <summary>
        /// 从活跃列表中注销 Context
        /// </summary>
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

        /// <summary>
        /// 恢复栈顶 Normal（当前 Normal 退场或被关闭后调用）
        /// </summary>
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

        /// <summary>
        /// 关闭所有 Normal 和 Popup（打开 Background 时调用）
        /// </summary>
        private void CloseAllNormalAndPopup()
        {
            // 关闭当前 Popup
            if (_currentPopup != null)
            {
                if (_currentPopup.StateMachine.CurrentState == UIState.Opened)
                    StartExit(_currentPopup);
                _currentPopup = null;
            }
            // 关闭所有 Normal
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

        /// <summary>
        /// 从 Controller 类型解析 UIKey
        /// 规则：类名去掉 "Controller" 后缀
        /// 示例：ShopController → "Shop"
        /// </summary>
        private string ResolveUIKey<T>() where T : IUIController
        {
            var name = typeof(T).Name;
            return name.EndsWith("Controller")
                ? name[..^"Controller".Length]
                : name;
        }

        /// <summary>单例 Key（等同 UIKey）</summary>
        private static string GetSingletonKey(string uiKey) => uiKey;

        /// <summary>
        /// 获取 Controller 所在的命名空间
        /// 子类可通过 partial class 方式覆盖此方法以指定自定义命名空间
        /// </summary>
        protected virtual string GetControllerTypeNamespace()
        {
            // 默认命名空间约定：{项目Assembly名}.UIFrameworkLib.Controller
            // 业务方可 override 返回实际命名空间
            return "UIFrameworkLib";
        }

        /// <summary>反射创建 Controller</summary>
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

        /// <summary>安全执行（全局 try-catch）</summary>
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

        /// <summary>清理 Context 并处理下一个队列</summary>
        private void CleanupAndNext(UIContext ctx)
        {
            ctx.Dispose();
            UnregisterContext(ctx);
            _enteringContext = null;
            ProcessNext();
        }

        /// <summary>仅清理 Context（不处理队列）</summary>
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

        /// <summary>队列元素</summary>
        private class QueueItem
        {
            public string UIKey { get; }
            public object Args { get; }

            public QueueItem(string uiKey, object args)
            {
                UIKey = uiKey;
                Args = args;
            }
        }

        /// <summary>
        /// Task 取消扩展：在 CancellationToken 触发时抛出 OperationCanceledException
        /// 参考：https://github.com/Unity-Technologies/UnityCsReference
        /// </summary>
        private static class TaskExtensions
        {
            public static async Task WithCancellation(this Task task, CancellationToken token)
            {
                var tcs = new TaskCompletionSource<bool>();
                using (token.Register(() => tcs.TrySetResult(true)))
                {
                    if (await Task.WhenAny(task, tcs.Task) == tcs.Task)
                        throw new OperationCanceledException(token);
                }
                await task; // 传播原始异常
            }
        }
    }
}