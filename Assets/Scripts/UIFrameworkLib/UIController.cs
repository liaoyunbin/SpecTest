using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UIFrameworkLib
{

	public abstract class UIController
	{
		public UIControllerState State { get; protected set; }
		public UIView View { get; protected set; }

		public abstract void DoBind(UIView view);
		public abstract UniTask DoShow(object args);
		public abstract UniTask DoHide(UIControllerClosePerformance closeState);

		public abstract string PrefabPath { get; }
		public abstract UILayer Layer { get; }
		public bool Aborted { get; set; }

		public abstract void OnShowInternal();
		public abstract void OnHideInternal();
	}

	public abstract class UIController<T> : UIController where T : UIView
	{
		public override void DoBind(UIView view)
		{
			View = view as T;
			if (View == null)
				Debug.LogError($"[UIFrameworkLib] 类型不匹配: {typeof(T).Name} vs {view?.GetType().Name}");
		}
		public override async UniTask DoShow(object args)
		{
			State = UIControllerState.AnimationEnter;
			View.IsInteractable = false;
			try
			{
				// 先播放动画
				View.PlayEnterAnimation();
			}
			catch (Exception e)
			{
				Debug.LogError($"[UIController] Enter 异常: {e}");
			}
			await UniTask.Delay(View.GetEnterAnimationDurationMs());

			State = UIControllerState.Opened;
			View.IsInteractable = true;
			OnShowInternal();
		}




		}

		private async UniTask DoHide_Immediate()
		{
			//todo:
			//
			//后续看下怎么取消进入动画
			View.IsInteractable = false;
			OnHideInternal();
			State = UIControllerState.Disable;
			View.gameObject.SetActive(false);
		}

		private async UniTask DoHide_ExitAnimation()
		{
			State = UIControllerState.AnimationExit;
			View.IsInteractable = false;

			OnHideInternal();
			try
			{
				View.PlayExitAnimation();
			}
			catch (Exception e)
			{
				Debug.LogError($"[UIController] Exit 异常: {e}");
			}
			await UniTask.Delay(View.GetExitAnimationDurationMs());
			State = UIControllerState.Disable;
			View.gameObject.SetActive(false);
		}
	}
}
