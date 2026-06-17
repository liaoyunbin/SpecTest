using System;
using System.Threading;
using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>
    /// UIContext：每个 UI 实例的运行时状态容器和生命周期锚点
    /// UIManager 通过 Context 管理所有 UI
    /// </summary>
    public class UIContext
    {
        // ================================================================
        // 标识
        // ================================================================

        /// <summary>UI 唯一标识</summary>
        public string UIKey { get; }

        /// <summary>实例 ID（同一 UIKey 可能有多个实例，如 Toast）</summary>
        public int InstanceId { get; }

        // ================================================================
        // 核心引用
        // ================================================================

        /// <summary>UI View 组件</summary>
        public UIView View { get; private set; }

        /// <summary>UI Controller</summary>
        public IUIController Controller { get; private set; }

        /// <summary>UI 配置</summary>
        public UIItemConfig Config { get; }

        // ================================================================
        // 状态
        // ================================================================

        /// <summary>状态机</summary>
        public UIStateMachine StateMachine { get; } = new();

        /// <summary>取消令牌源</summary>
        public CancellationTokenSource Cts { get; set; }

        /// <summary>打开时间戳</summary>
        public float OpenTime { get; set; }

        /// <summary>栈中上一个 UI</summary>
        public UIContext PreviousContext { get; set; }

        private static int _nextInstanceId;

        // ================================================================
        // 构造
        // ================================================================

        public UIContext(string uiKey, UIItemConfig config)
        {
            UIKey = uiKey;
            InstanceId = _nextInstanceId++;
            Config = config;
        }

        // ================================================================
        // 方法
        // ================================================================

        /// <summary>绑定 View 和 Controller</summary>
        public void Bind(UIView view, IUIController controller)
        {
            View = view;
            Controller = controller;
            controller.BindContext(this);
        }

        /// <summary>取消当前异步操作</summary>
        public void Cancel()
        {
            Cts?.Cancel();
            Cts?.Dispose();
            Cts = null;
        }

        /// <summary>完全清理</summary>
        public void Dispose()
        {
            Cancel();

            StateMachine.TryTransitionTo(UIState.Closed);

            try
            {
                Controller?.OnDispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIFrameworkLib] {UIKey} OnDispose 异常: {e}");
            }

            if (View != null)
            {
                if (View.gameObject != null)
                    GameObject.Destroy(View.gameObject);
                View = null;
            }

            Controller = null;
        }
    }
}