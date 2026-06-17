using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIFrameworkLib
{
    /// <summary>
    /// Controller 泛型基类
    /// 一个 UI 对应一个 Controller，T 是对应的 UIView 类型。
    ///
    /// Controller 负责：
    /// - 通过 <see cref="CreateViewAsync"/> 创建自己的 View
    /// - 处理业务逻辑、事件订阅
    ///
    /// 使用示例：
    /// <code>
    /// public class ShopController : UIController&lt;ShopView&gt;
    /// {
    ///     // 使用框架提供的工具方法加载 View
    ///     public override async UniTask&lt;ShopView&gt; CreateViewAsync()
    ///     {
    ///         return await LoadFromResources("Prefabs/ShopPanel");
    ///     }
    ///
    ///     protected internal override void OnInit()
    ///     {
    ///         View.m_BtnClose.onClick.AddListener(CloseSelf);
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

        // ================================================================
        // IUIController 显式实现
        // ================================================================

        UIContext IUIController.Context => Context;

        void IUIController.BindContext(UIContext context)
        {
            Context = context;
        }

        void IUIController.SetView(UIView view)
        {
            View = view as T;
            if (View == null)
                Debug.LogError($"[UIFrameworkLib] Controller<{typeof(T).Name}> 与 View 类型不匹配。"
                    + $" View 实际类型: {view?.GetType().Name}");
        }

        // ================================================================
        // View 创建（子类必须实现）
        // ================================================================

        /// <summary>
        /// 创建或获取 View。
        /// 框架在 OnInit 之后、OnOpen 之前调用此方法。
        /// 子类应调用框架提供的工具方法（如 <see cref="LoadFromResources"/>）实现。
        /// </summary>
        public abstract UniTask<T> CreateViewAsync();

		// ================================================================
		// 框架工具方法
		// ================================================================

		/// <summary>
		/// 从 Resources 加载 Prefab 并实例化。
		/// 自动检查是否有缓存的 View（隐藏的旧实例），有则直接复用。
		/// </summary>
		/// <param name="prefabPath">Resources 中的 Prefab 路径</param>
		public async UniTask<T> LoadFromResources(string prefabPath)
        {
            var uiKey = Context.UIKey;

            // 1. 检查是否有缓存的隐藏实例
            var cached = UIManager.Instance.GetCachedView(uiKey);
            if (cached != null)
            {
                cached.SetActive(true);
                var view = cached.GetComponent<T>();
                return view;
            }

            // 2. 异步加载 Prefab
            var prefab = await UIResourceLoader.Instance.LoadPrefabAsync(prefabPath);
            if (prefab == null) return null;

            // 3. 实例化
            return InstantiateView(prefab);
        }

        /// <summary>
        /// 从对象池/缓存获取 View（如果之前有缓存的隐藏实例）。
        /// 没有缓存时返回 null。
        /// </summary>
        protected T LoadFromCache()
        {
            var cached = UIManager.Instance.GetCachedView(Context.UIKey);
            if (cached != null)
            {
                cached.SetActive(true);
                return cached.GetComponent<T>();
            }
            return null;
        }

        /// <summary>实例化 Prefab 并挂载到对应层级</summary>
        private T InstantiateView(GameObject prefab)
        {
            var parent = UIRoot.Instance.GetLayer(Config.Layer);
            var instance = Object.Instantiate(prefab, parent);
            instance.name = Context.UIKey;

            var view = instance.GetComponent<T>();
            if (view == null)
            {
                Debug.LogError($"[UIFrameworkLib] {Context.UIKey} 缺少 {typeof(T).Name} 组件");
                Object.Destroy(instance);
                return null;
            }

            return view;
        }

		// ================================================================
		// 生命周期钩子（业务层重写）
		// ================================================================

		/// <summary>仅一次：Controller 首次创建后调用。适合注册事件。</summary>
		public virtual void OnInit() { }

		/// <summary>每次打开时调用，接收外部传入的参数。</summary>
		public virtual void OnOpen(object args) { }

		/// <summary>入场动画结束后调用，此时 UI 已可见且可交互。</summary>
		public virtual void OnShown() { }

		/// <summary>退场动画开始时调用。</summary>
		public virtual void OnHide() { }

		/// <summary>销毁时调用，清理事件绑定、对象引用等。</summary>
		public virtual void OnDispose() { }

        // ================================================================
        // 辅助方法
        // ================================================================

        /// <summary>关闭当前 UI</summary>
        protected void CloseSelf()
        {
            UIManager.Instance.Close(ResolveUIKey());
        }

        private string ResolveUIKey()
        {
            var name = GetType().Name;
            return name.EndsWith("Controller")
                ? name[..^"Controller".Length]
                : name;
        }
    }
}
