using AtomString.Operator;
using UnityEngine;

namespace UIFrameworkLib
{
	public abstract class UIView :MonoBehaviour
	{

		// === 子类重写 ===
		//public abstract void PlayEnterAnimation();
		//public abstract void PlayExitAnimation();
		//// === 时长获取 ===
		//public abstract int GetEnterAnimationDurationMs();
		//public abstract int GetExitAnimationDurationMs();
		//public bool IsInteractable { get; internal set; }
		public IOperate _OnShow;
		public IOperate _OnHide;
		public bool IsInteractable { get; internal set; }
	}
}
