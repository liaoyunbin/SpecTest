using System;
using AtomString.Operator;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UIFrameworkLib
{

	public abstract class UIController
	{
		public UIView View { get; protected set; }

		public abstract void DoBind(UIView view);
		public abstract string PrefabPath { get; }
		public abstract UILayer Layer { get; }
		public bool Aborted { get; set; }

		public abstract void OnShowInternal(IOperate operate);
		public abstract void OnHideInternal(IOperate operate);
	}

	public abstract class UIController<T> : UIController where T : UIView
	{
		public override void DoBind(UIView view)
		{
			View = view as T;
			if (View == null)
			{
				Debug.LogError($"[UIFrameworkLib] 类型不匹配: {typeof(T).Name} vs {view?.GetType().Name}");
			}
			View._OnShow.OnCompleted(OnShowInternal);
			View._OnHide.OnCompleted(OnHideInternal);
		}
	}
}
