using System;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace UIFrameworkLib
{
    /// <summary>
    /// Controller 非泛型基类
    /// 提供通用状态管理和生命周期编排，供泛型版本继承
    /// </summary>
    public abstract class UIController : IUIController
    {
        // === 框架注入 ===
        public UIView View { get; protected set; }
        internal UIStateMachine StateMachine { get; } = new();
        internal bool PendingClose { get; set; } // Close 请求在加载/动画中途到达时标记

        // === 语义化状态（UIManager 通过这些方法判断） ===
        public bool IsOpened      => StateMachine.CurrentState == UIState.Opened;
        public bool IsLoading     => StateMachine.CurrentState == UIState.Loading;
        public bool IsInAnimation => StateMachine.CurrentState == UIState.AnimationEnter
                                  || StateMachine.CurrentState == UIState.AnimationExit;
        public bool IsClosed      => StateMachine.CurrentState == UIState.Closed;
        internal bool TryTransition(UIState target) => StateMachine.TryTransitionTo(target);

        // === 静态元数据（子类声明，UIManager 读取） ===
        public abstract string PrefabPath { get; }
        public abstract UILayer Layer { get; }

        // === IUIController 显式实现 ===
		public void ChangeState(UIState state)
		{
			UIState oldState;
			switch (state)
			{
				case UIState.Opened:
					break;
			}
		}
		public override void UIMgrShow(UIManager _parentMgr, params object[] args)
		{
			m_LoadedCount++;
			if (wnd != null)
			{
				//设置窗口有效
				getGameObj().SetActive(true);
				//调用事件函数
				_onShowWnd();
				//处理动画，然后延迟处理函数
				_dealAniAction(wnd.showAniName, wnd.showAniTime, null, null);
				return;
			}

			GameObject parent = _parentMgr.GetLayerParent(layer);
			bool success = LoadUI(parent == null ? null : parent.transform);
			if (!success)
			{
				return;
			}
			_dealAniAction(wnd.showAniName, wnd.showAniTime, () => DoShow(args), null);
		}
		/// <summary>
		/// 仅供UIMgr调用
		/// </summary>
		public override void UIMgrHide()
		{
			m_LoadedCount--;
			_onHideWnd();
			if (wnd != null)
			{
				_dealAniAction(wnd.hideAniName, wnd.hideAniTime, null, DoHide);
				return;
			}
		}

		/// <summary>
		/// 仅供UIMgr调用
		/// </summary>
		public override void UIMgrPause() { }

		// === 生命周期编排 ===
		public virtual async UniTask<bool> EnterAsync()
        {
            TryTransition(UIState.AnimationEnter);
            var view = View;
            view.IsInteractable = false;
            view.SetInteractive(false);
            try
            {
                // 先播放动画
                view.PlayEnterAnimation();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIController] Enter 异常: {e}");
            }
			await UniTask.Delay(view.GetEnterAnimationDurationMs());
			return IsInAnimation; // false = 中途被中断
        }

        public virtual async UniTask ExitAsync()
        {
            if (!IsOpened) return;

            TryTransition(UIState.AnimationExit);

            var view = View;
            view.IsInteractable = false;
            OnHide();
            view.SetInteractive(false);

            try
            {
                // 先播放动画
                view.PlayExitAnimation();         
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIController] Exit 异常: {e}");
            }
			await UniTask.Delay(view.GetExitAnimationDurationMs());
			TryTransition(UIState.Closed);
            view.gameObject.SetActive(false);
        }

		// === 生命周期（业务层重写） ===
		public abstract void OnInit(UIView view);
		public abstract void OnOpen(object args);
		public abstract void OnHide();
		public abstract void OnDispose();
	}

    /// <summary>
    /// Controller 泛型基类（继承非泛型 UIController）
    /// 一个 UI 对应一个 Controller，T 是对应的 UIView 类型。
    /// </summary>
    /// <typeparam name="T">对应的 UIView 类型</typeparam>
    public abstract class UIController<T> : UIController where T : UIView
    {
		public override void OnInit(UIView view)
		{
			View = view as T;
			if (View == null)
				Debug.LogError($"[UIFrameworkLib] 类型不匹配: {typeof(T).Name} vs {view?.GetType().Name}");
		}

		// === 辅助 ===
		protected void CloseSelf() => UIManager.Instance.CloseByType(GetType());
	}
}
