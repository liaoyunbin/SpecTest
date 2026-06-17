using System;
using Cysharp.Threading.Tasks;

namespace UIFrameworkLib
{
    /// <summary>
    /// Controller 公共接口
    /// 用于框架内部多态操作
    /// </summary>
    public interface IUIController
    {
        // === 框架注入 ===
        void SetView(UIView view);

        // === 生命周期（业务层重写） ===
        void OnInit();           // 首次：SetView 之后
        void OnOpen(object args); // 每次打开：动画结束后
        void OnHide();            // 退场前
        void OnDispose();         // 清理

        // === 生命周期编排（UIManager 委托给 Controller） ===
        UniTask<bool> EnterAsync(); // 入场：设置交互→播放动画。返回 false 表示中断
        UniTask ExitAsync();        // 退场：关闭交互→OnHide→播放动画→隐藏 View

		string PrefabPath { get; }
        UILayer Layer { get; }

        // === 语义化状态 ===
        bool IsOpened { get; }
        bool IsLoading { get; }
        bool IsInAnimation { get; }
        bool IsClosed { get; }
    }
}
