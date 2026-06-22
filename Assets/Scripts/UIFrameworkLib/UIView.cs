using AtomString.Operator;
using UnityEngine;

namespace UIFrameworkLib
{
	public abstract class UIView :MonoBehaviour
	{
		public IOperate _OnShow;
		public IOperate _OnHide;
		public bool IsInteractable { get; internal set; }
	}
}
