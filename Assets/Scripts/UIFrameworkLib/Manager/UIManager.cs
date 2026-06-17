using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIFrameworkLib
{
    /// <summary>
    /// UIManager — UI 框架核心管理器
    ///
    /// 职责：
    /// - 提供 Open<T>() / Close<T>() 对外接口
    /// - 三层栈管理（Background / Normal / Popup）
    /// - 队列调度（N 个排队）
    /// - 层级仲裁
    /// - View 加载 / 缓存
    ///
    /// 使用方式：
    /// <code>
    /// UIManager.Instance.Open&lt;ShopController&gt;();
    /// UIManager.Instance.Close&lt;ShopController&gt;();
    /// </code>
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        // ================================================================
        // 字段
        // ================================================================

        private readonly Dictionary<UILayer, Stack<IUIController>> _stacks = new()
        {
            [UILayer.Background] = new(),
            [UILayer.Normal]     = new(),
            [UILayer.Popup]      = new(),
        };

        private readonly Queue<QueueItem> _queue = new();

        // ================================================================
        // 公开属性
        // ================================================================

        /// <summary>当前是否忙碌（最顶层 UI 正在加载或动画中）</summary>
        public bool IsBusy
        {
            get
            {
                var top = GetTopMostUI();
                return top != null && (top.IsLoading || top.IsInAnimation);
            }
        }

        // ================================================================
        // 对外接口
        // ================================================================

        /// <summary>打开 UI（默认 WaitForAnimation）</summary>
        public void Open<T>(object args = null, QueueMode mode = QueueMode.WaitForAnimation)
            where T : IUIController
        {
            Open(typeof(T), args, mode);
        }

        /// <summary>通过 Type 打开 UI</summary>
        public void Open(Type controllerType, object args = null, QueueMode mode = QueueMode.WaitForAnimation)
        {
            if (controllerType == null) return;

            var topUI = GetTopMostUI();

            // 最顶层 UI 就是目标且已打开 → 直接刷新
            if (topUI != null && topUI.GetType() == controllerType && topUI.IsOpened)
            {
                topUI.PendingClose = false;
                topUI.OnOpen(args);
                return;
            }

            // 最顶层 UI 正在加载或动画中 → 入队等待
            if (topUI != null && (topUI.IsLoading || topUI.IsInAnimation))
            {
                _queue.Enqueue(new QueueItem { ControllerType = controllerType, Args = args, Mode = mode });
                return;
            }

            // 其他情况 → 直接执行
            StartOpening(controllerType, args);
        }

        /// <summary>通过 Type 关闭 UI</summary>
        public void CloseByType(Type key)
        {
            var ctrl = FindController(key);
            if (ctrl == null) return;

            if (ctrl.IsOpened)
                StartExit(ctrl);
            else
                ctrl.PendingClose = true; // 加载 / 动画中 → 标记
        }

        /// <summary>关闭所有 UI（只隐藏不销毁）</summary>
        public void CloseAll()
        {
            _queue.Clear();

            foreach (var stack in _stacks.Values)
            {
                foreach (var ctrl in stack)
                {
                    ctrl.TryTransition(UIState.Closed);
                    ctrl.OnHide();
                    if (ctrl.View != null)
                    {
                        ctrl.View.gameObject.SetActive(false);
                    }
                }
                stack.Clear();
            }
        }

        // ================================================================
        // 核心流程
        // ================================================================

        /// <summary>开始打开流程</summary>
        private async void StartOpening(Type key, object args)
        {
            var ctrl = UIControllerRegistry.GetController(key);
            if (ctrl == null)
            {
                Debug.LogError($"[UIManager] 找不到 Controller: {key.Name}");
                return;
            }

            ctrl.TryTransition(UIState.Loading);

            try
            {
                await InitView(ctrl);
                if (!ctrl.IsLoading) { Object.Destroy(ctrl.View.gameObject); return; }
                if (ctrl.PendingClose) { ctrl.View.gameObject.SetActive(false); ctrl.TryTransition(UIState.Closed); return; }

                Activate(ctrl);
                if (!await ctrl.EnterAsync()) return;
                if (ctrl.PendingClose) { StartExit(ctrl); return; }

                ctrl.TryTransition(UIState.Opened);
                ctrl.View.SetInteractive(true);
                ctrl.View.IsInteractable = true;
                ctrl.OnOpen(args);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIManager] Open 异常: {e}");
                AbortOpen(ctrl);
            }
            finally
            {
                ProcessQueue();
            }
        }

        /// <summary>初始化 View（复用已有或加载新的）</summary>
        private async UniTask InitView(IUIController ctrl)
        {
            Type key = ctrl.GetType();

            // Controller 已有 View → 直接激活
            if (ctrl.View != null)
            {
                ctrl.View.gameObject.SetActive(true);
                return;
            }

            // 没有 View，加载新的
            var prefab = await AssetMgr.Instance.LoadPrefabAsync(ctrl.PrefabPath);
            if (prefab == null) { AbortOpen(ctrl); return; }

            var instance = Object.Instantiate(prefab, UIRoot.Instance.GetLayer(ctrl.Layer));
            var view = instance.GetComponent<UIView>();
            if (view == null)
            {
                Object.Destroy(instance);
                AbortOpen(ctrl);
                return;
            }

            ctrl.SetView(view);
            ctrl.OnInit();
        }

        /// <summary>激活 UI：入栈 + 层级仲裁</summary>
        private void Activate(IUIController ctrl)
        {
            switch (ctrl.Layer)
            {
                case UILayer.Background:
                    if (_stacks[UILayer.Popup].Count > 0)
                        StartExit(_stacks[UILayer.Popup].Peek());
                    foreach (var n in _stacks[UILayer.Normal].ToArray())
                        StartExit(n);
                    _stacks[UILayer.Normal].Clear();
                    _stacks[UILayer.Popup].Clear();
                    if (_stacks[UILayer.Background].Count > 0)
                        StartExit(_stacks[UILayer.Background].Peek());
                    _stacks[UILayer.Background].Push(ctrl);
                    break;

                case UILayer.Normal:
                    if (_stacks[UILayer.Popup].Count > 0)
                        StartExit(_stacks[UILayer.Popup].Peek());
                    _stacks[UILayer.Popup].Clear();
                    if (_stacks[UILayer.Normal].Count > 0)
                        _stacks[UILayer.Normal].Peek().OnHide();
                    _stacks[UILayer.Normal].Push(ctrl);
                    break;

                case UILayer.Popup:
                    if (_stacks[UILayer.Popup].Count > 0)
                        StartExit(_stacks[UILayer.Popup].Peek());
                    _stacks[UILayer.Popup].Push(ctrl);
                    break;
            }

            ctrl.TryTransition(UIState.AnimationEnter);
        }

        /// <summary>开始退出流程</summary>
        private async void StartExit(IUIController ctrl)
        {
            try
            {
                await ctrl.ExitAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIManager] StartExit 异常: {e}");
            }
            finally
            {
                OnExitCleanup(ctrl);
            }
        }

        /// <summary>退出后的栈管理 + 队列调度</summary>
        private void OnExitCleanup(IUIController ctrl)
        {
            _stacks[ctrl.Layer].Pop();

            // 恢复上一个 Normal 层的交互
            if (_stacks[UILayer.Normal].Count > 0)
            {
                var top = _stacks[UILayer.Normal].Peek();
                if (top.IsOpened)
                    top.View.SetInteractive(true);
            }

            ProcessQueue();
        }

        /// <summary>关闭所有（只隐藏不销毁）</summary>
        public void CloseAll()
        {
            _queue.Clear();

            foreach (var stack in _stacks.Values)
            {
                foreach (var ctrl in stack)
                {
                    ctrl.TryTransition(UIState.Closed);
                    ctrl.OnHide();
                    if (ctrl.View != null)
                    {
                        ctrl.View.gameObject.SetActive(false);
                    }
                }
                stack.Clear();
            }
        }

        // ================================================================
        // 队列调度
        // ================================================================

        private void ProcessQueue()
        {
            if (_queue.Count == 0) return;

            var next = _queue.Peek();
            var topUI = GetTopMostUI();

            bool canProcess = next.Mode switch
            {
                QueueMode.WaitForAnimation => topUI == null || !topUI.IsInAnimation,
                QueueMode.WaitForClose     => topUI == null,
                _                          => true,
            };

            if (canProcess)
            {
                _queue.Dequeue();
                StartOpening(next.ControllerType, next.Args);
            }
        }

        // ================================================================
        // 工具方法
        // ================================================================

        /// <summary>获取最顶层 UI（优先级：Popup > Normal > Background）</summary>
        private IUIController GetTopMostUI()
        {
            if (_stacks[UILayer.Popup].Count > 0)
                return _stacks[UILayer.Popup].Peek();
            if (_stacks[UILayer.Normal].Count > 0)
                return _stacks[UILayer.Normal].Peek();
            if (_stacks[UILayer.Background].Count > 0)
                return _stacks[UILayer.Background].Peek();
            return null;
        }

        /// <summary>通过 Type 查找 Controller</summary>
        private IUIController FindController(Type key)
        {
            return UIControllerRegistry.GetController(key);
        }

        /// <summary>异常时清理</summary>
        private void AbortOpen(IUIController ctrl)
        {
            ctrl.OnDispose();
            if (ctrl.View != null)
                Object.Destroy(ctrl.View.gameObject);
        }

        // ================================================================
        // 队列项
        // ================================================================

        private class QueueItem
        {
            public Type ControllerType;
            public object Args;
            public QueueMode Mode;
        }
    }

    /// <summary>队列模式</summary>
    public enum QueueMode
    {
        /// <summary>等待上一个界面动画结束后打开</summary>
        WaitForAnimation,
        /// <summary>等待上一个界面完全关闭后打开</summary>
        WaitForClose,
    }
}
