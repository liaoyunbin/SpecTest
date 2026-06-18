//UTF-8格式
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIFrameworkLib
{
    public class UIQueueManager : Singleton<UIQueueManager>
	{
		//todo:
		//队列管理
		//动画的Operator的管理

		public void OpenUI<T>(object arg) where T : UIController;
		public void Close<T>();
		public void Back();
	}
}
