using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIFrameworkLib
{
	/// <summary>
	/// UIManager — UI 框架核心管理器
	///
	/// 职责：
	/// - 提供 Open<T>() / Close<T>() 对外接口
	/// - 三层栈管理（Background / Normal / Popup）
	/// - 队列调度（N 个排队）
	/// - 层级仲裁
	/// - View 加载 / 缓存
	///
	/// 使用方式：
	/// <code>
	/// UIManager.Instance.Open&lt;ShopController&gt;();
	/// UIManager.Instance.Close&lt;ShopController&gt;();
	/// </code>
	/// </summary>
	public class UIManager : Singleton<UIManager>
	{
		private readonly Dictionary<UILayer, Stack<UIController>> _stacks = new()
		{
			[UILayer.Background] = new(),
			[UILayer.Normal] = new(),
			[UILayer.Popup] = new(),
		};

		#region public function
		private UIController GetTopMostUI()
		{
			if (_stacks[UILayer.Popup].Count > 0)
				return _stacks[UILayer.Popup].Peek();
			if (_stacks[UILayer.Normal].Count > 0)
				return _stacks[UILayer.Normal].Peek();
			if (_stacks[UILayer.Background].Count > 0)
				return _stacks[UILayer.Background].Peek();
			return null;
		}

		/// <summary>通过 Type 查找 Controller</summary>
		private UIController FindController(Type key)
		{
			return UIControllerRegistry.GetController(key);
		}
		/// <summary>
		/// 关闭所有UI
		/// </summary>
		public void CloseAllUI()
		{
			// 按弹窗→正常→背景顺序关闭
			LayerClearAndPopTopUI(UILayer.Popup);
			LayerClearAndPopTopUI(UILayer.Normal);
			LayerClearAndPopTopUI(UILayer.Background);
		}

		/// <summary>
		/// 打开UI（统一接口）
		/// 自动处理栈逻辑
		/// </summary>
		public void OpenUI<T>(params object[] args) where T : UIController
		{
			T enterUI = GetController<T>();
			UILayer enterLayer = enterUI.layer;
			Stack<UIController> stack = _stacks[enterLayer];

			switch (enterLayer)
			{
				case UILayer.Background: // A层：替换逻辑
					LayerClearAndPopTopUI(UILayer.Popup);
					LayerClearAndPopTopUI(UILayer.Normal);
					LayerPopTopUI(UILayer.Background);

					// 新背景入栈并显示
					stack.Push(enterUI);
					enterUI.UIMgrShow(this, args);
					break;

				case UILayer.Normal: // B层：隐藏上一个，显示当前

					//清空popUp层
					LayerClearAndPopTopUI(UILayer.Popup);

					//之前没有normal界面打开，底部有背景层，禁用背景层逻辑，新UI入栈并显示
					if (stack.Count <= 0)
					{
						Stack<UIController> bgStack = _stacks[UILayer.Background];
						if (bgStack.Count > 0)
						{
							bgStack.Peek().UIMgrPause();
						}
						// 新UI入栈并显示
						stack.Push(enterUI);
						enterUI.UIMgrShow(this, args);
						break;
					}

					//之前有normal界面打开，栈顶是自己，重新刷新
					if (stack.Peek() == enterUI)
					{
						enterUI.UIMgrShow(this, args);
						break;
					}

					//栈中原先有此UI。之前有normal界面打开，栈顶不是自己，原先的Normal栈里包含了此数据
					if (stack.Contains(enterUI))
					{
						//隐藏当前正常层栈顶
						if (stack.Count > 0)
						{
							UIController currentNormal = stack.Pop();  // 查看栈顶（不弹出）
							currentNormal.UIMgrHide();
						}
						while (stack.Peek() != enterUI)
						{
							stack.Pop();
						}
						stack.Peek().UIMgrShow(this, args);
					}
					else
					{
						//栈顶不是自己， 隐藏当前正常层栈顶
						if (stack.Count > 0)
						{
							UIController currentNormal = stack.Peek();  // 查看栈顶（不弹出）
							currentNormal.UIMgrHide();
						}
						// 新UI入栈并显示
						stack.Push(enterUI);
						enterUI.UIMgrShow(this, args);
					}
					break;

				case UILayer.Popup: // C层：替换逻辑

					if (stack.Count <= 0)
					{
						Stack<UIController> bgStack = _stacks[UILayer.Normal];
						if (bgStack.Count > 0)
						{
							bgStack.Peek().UIMgrPause();
						}
						// 新UI入栈并显示
						stack.Push(enterUI);
						enterUI.UIMgrShow(this, args);
						break;
					}

					// 隐藏当前弹窗
					UIController currentPopup = stack.Pop();
					currentPopup.UIMgrHide();
					// 新弹窗入栈并显示
					stack.Push(enterUI);
					enterUI.UIMgrShow(this, args);
					break;
			}

			Debug.Log($"[OpenUI] {enterUI.Name} | 层级: {enterLayer} | 栈大小: {stack.Count}");
		}
		/// <summary>
		/// 层级界面清空并关闭当前层的顶部UI
		/// </summary>
		/// <param name="layer"></param>
		private void LayerClearAndPopTopUI(UILayer layer)
		{
			Stack<UIController> stack = _stacks[layer];
			if (stack.Count > 0)
			{
				UIController ui = stack.Pop();
				ui.UIMgrHide();
			}
			//清空当前层栈数据
			stack.Clear();
		}

		/// <summary>
		/// 关闭当前层的顶部UI
		/// </summary>
		private bool LayerPopTopUI(UILayer layer)
		{
			Stack<UIController> stack = _stacks[layer];
			if (stack.Count > 0)
			{
				UIController ui = stack.Pop();
				ui.UIMgrHide();
				return true;
			}
			return false;
		}

		/// <summary>
		/// 显示当前层栈顶UI
		/// </summary>
		/// <param name="layer"></param>
		private bool ShowTopUI(UILayer layer)
		{
			Stack<UIController> stack = _stacks[layer];
			// 显示上一个UI（如果有）
			if (stack.Count > 0)
			{
				UIController previousUI = stack.Peek();
				previousUI.UIMgrShow(this);
				return true;
			}
			return false;
		}
		/// <summary>
		/// 关闭当前显示的UI（统一接口）
		/// 按照 C层→B层→A层 优先级关闭
		/// </summary>
		public void CloseNowUI()
		{
			//如果有弹窗，优先关闭弹窗，打开上一层； 没有弹窗，优先关闭normal，打开上一层
			if (LayerPopTopUI(UILayer.Popup) || LayerPopTopUI(UILayer.Normal))
			{
				bool normalCanOpen = ShowTopUI(UILayer.Normal);
				if (!normalCanOpen)
				{
					ShowTopUI(UILayer.Background);
				}
				return;
			}
			LayerPopTopUI(UILayer.Background);

			Debug.LogWarning("[CloseNowUI] 没有可关闭的UI");
		}
		#endregion
	}
}
