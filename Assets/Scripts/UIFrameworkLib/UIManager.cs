using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace UIFrameworkLib
{

	public class UIManager : Singleton<UIManager>
	{


		//public void OpenUI<T>(object arg) where T : UIController;
		//public void Close<T>();
		//public void Back();

		private readonly Dictionary<UILayer, Stack<UIController>> _stacks = new();
		private UIController FindController<T>() where T : UIController
		{
			return UIControllerRegistry.GetController(typeof(T));
		}
		public async UniTask OpenUI<T>(object arg) where T : UIController
		{
			var ctrl = UIControllerRegistry.GetController(typeof(T));
			ctrl.Aborted = false;
			if (ctrl.View == null)
			{
#if UNITY_EDITOR  //后续更换成其他宏编译
				var prefab = await AssetMgr.Instance.LoadPrefabAsync(ctrl.PrefabPath);
				if (prefab == null)
				{
					throw new Exception($"UIManager OpenUI {ctrl.PrefabPath} 未加载");
				}
				var instance = Object.Instantiate(prefab, UIRoot.Instance.GetLayer(ctrl.Layer));
				var view = instance.GetComponent<UIView>();
				if (view == null)
				{
					Object.Destroy(instance);
					throw new Exception($"UIManager OpenUI {ctrl.PrefabPath} UIView 未存在");
				}
				ctrl.DoBind(view);
#else
			var prefab = await AssetMgr.Instance.LoadPrefabAsync(ctrl.PrefabPath);
			if(prefab == null)
			{
				UnityEngine.Debug.LogError($"UIManager OpenUI {ctrl.PrefabPath} 未加载");
				return;
			}
			var instance = Object.Instantiate(prefab, UIRoot.Instance.GetLayer(ctrl.Layer));
			var view = instance.GetComponent<UIView>();
			if (view == null)
			{
				Object.Destroy(instance);
				UnityEngine.Debug.LogError($"UIManager OpenUI {ctrl.PrefabPath} UIView 未存在");
				return;
			}
			 ctrl.DoBind(view);
#endif
			}
			if (ctrl.Aborted)
			{
				ctrl.View.gameObject.SetActive(false);
				return;
			}
			//处理对应入栈
			ctrl.View.gameObject.SetActive(true);
			ctrl.DoShow(arg);
		}

		public void Close<T>(UIControllerClosePerformance closeState) where T : UIController
		{
			var ctrl = UIControllerRegistry.GetController(typeof(T));
			if (ctrl.View == null)
			{
				ctrl.Aborted = true;
			}
			else
			{
				//当前是不是要过渡效果之类的来决定
				switch(ctrl.State)
				{
					case UIControllerState.AnimationEnter:
						break;
					case UIControllerState.Opened:
						break;
					case UIControllerState.AnimationExit:
						break;
					case UIControllerState.Disable: //不做任何
						break;
				}
				ctrl.DoHide(closeState);
			}
		}

		public void PushStatck<T>() where T : UIController
		{
			var ctrl = UIControllerRegistry.GetController(typeof(T));
		}

	}
}
