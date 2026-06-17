using System;
using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>
    /// Controller 泛型基类
    /// 一个 UI 对应一个 Controller，T 是对应的 UIView 类型
    ///
    /// 使用示例：
    /// <code>
    /// public class ShopController : UIController&lt;ShopView&gt;
    /// {
    ///     protected internal override void OnInit()
    ///     {
    ///         View.m_BtnClose.onClick.AddListener(CloseSelf);
    ///     }
    ///
    ///     protected internal override void OnOpen(object args)
    ///     {
    ///         int categoryId = (int)(args ?? 0);
    ///         RefreshUI();
    ///     }
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="T">对应的 UIView 类型</typeparam>
    public abstract class UIController<T> : IUIController where T : UIView
    {
        // ================================================================
        // 公开属性
        // ================================================================

        /// <summary>强类型 View 引用</summary>
        public T View { get; private set; }

        /// <summary>运行上下文</summary>
        public UIContext Context { get; private set; }

        /// <summary>UI 配置</summary>
        public UIItemConfig Config => Context?.Config;

        /// <summary>取消令牌（用于异步操作）</summary>
        public CancellationToken CancellationToken =>
            Context?.Cts?.Token ?? System.Threading.CancellationToken.None;

        // ================================================================
        // IUIController 显式实现
        // ================================================================

        UIContext IUIController.Context => Context;

        void IUIController.BindContext(UIContext context)
        {
            Context = context;
            View = context.View as T;

            if (View == null)
                Debug.LogError($"[UIFrameworkLib] Controller<{typeof(T).Name}> 与 View 类型不匹配。"
                    + $" View 实际类型: {context.View?.GetType().Name}");
        }

        // ================================================================
        // 生命周期钩子（业务层重写）
        // ================================================================

        /// <summary>仅一次：View 创建后，OnOpen 之前调用。适合注册事件。</summary>
        protected internal virtual void OnInit() { }

        /// <summary>
        /// 每次打开时调用。
        /// 接收外部传入的参数，刷新数据。
        /// </summary>
        /// <param name="args">打开参数</param>
        protected internal virtual void OnOpen(object args) { }

        /// <summary>入场动画结束后调用。此时 UI 已可见且可交互。</summary>
        protected internal virtual void OnShown() { }

        /// <summary>退场动画开始时调用。此时 UI 即将隐藏。</summary>
        protected internal virtual void OnHide() { }

        /// <summary>销毁时调用。清理事件绑定、对象引用等。</summary>
        protected internal virtual void OnDispose() { }

        // ================================================================
        // 辅助方法
        // ================================================================

        /// <summary>关闭当前 UI</summary>
        protected void CloseSelf()
        {
            UIManager.Instance.Close(ResolveUIKey());
        }

        /// <summary>
        /// 从 Controller 类型解析 UIKey
        /// 规则：类名去掉 "Controller" 后缀
        /// 示例：ShopController → "Shop"
        /// </summary>
        private string ResolveUIKey()
        {
            var name = GetType().Name;
            return name.EndsWith("Controller")
                ? name[..^"Controller".Length]
                : name;
        }
    }
}