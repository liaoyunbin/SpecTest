using System;
using Cysharp.Threading.Tasks;

namespace UIFrameworkLib
{
    /// <summary>
    /// Controller 公共接口
    /// 用于框架内部多态操作（避免泛型协变问题）
    /// </summary>
    public interface IUIController
    {
        /// <summary>运行上下文</summary>
        UIContext Context { get; }
		/// <summary>绑定上下文（框架内部调用）</summary>
		void BindContext(UIContext context);

        /// <summary>设置 View（Controller 创建 View 后调用）</summary>
        void SetView(UIView view);

        /// <summary>仅一次：View 创建后调用</summary>
        void OnInit();

        /// <summary>每次打开时调用</summary>
        void OnOpen(object args);

        /// <summary>入场动画结束后调用</summary>
        void OnShown();

        /// <summary>退场动画开始时调用</summary>
        void OnHide();

        /// <summary>销毁时调用，清理资源</summary>
        void OnDispose();
    }
}
