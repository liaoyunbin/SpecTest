using System;
using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>
    /// UIContext：每个 UI 实例的运行时状态容器
    /// UIManager 通过 Context 管理所有 UI。
    /// 生命周期：每次打开创建一个新 Context，关闭后释放。
    /// </summary>
    public class UIContext
    {
        // ================================================================
        // 标识
        // ================================================================

        /// <summary>UI 唯一标识</summary>
        public string UIKey { get; }

        /// <summary>实例 ID（同一 UIKey 每次打开递增）</summary>
        public int InstanceId { get; }

        private static int _nextInstanceId;

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

        /// <summary>打开时间戳</summary>
        public float OpenTime { get; set; }

        /// <summary>栈中上一个 UI</summary>
        public UIContext PreviousContext { get; set; }

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

        /// <summary>绑定 Controller（View 创建前调用）</summary>
        public void BindController(IUIController controller)
        {
            Controller = controller;
            controller.BindContext(this);
        }

        /// <summary>设置 View（Controller 创建 View 后调用）</summary>
        public void SetView(UIView view)
        {
            View = view;
            Controller?.SetView(view);
        }

        /// <summary>完全清理（出错或 CloseAll 时销毁）</summary>
        public void Dispose()
        {
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
                    UnityEngine.Object.Destroy(View.gameObject);
                View = null;
            }

            Controller = null;
        }
    }
}