namespace UIFrameworkLib
{

	public enum UIControllerState
	{
		AnimationEnter,
		Opened,
		AnimationExit,
		Disable,
	}

	/// <summary>
	/// UI 控制器关闭表现
	/// </summary>
	public enum UIControllerClosePerformance
	{
		/// <summary>立刻关闭</summary>
		Immediate,
		/// <summary>播放完退出动画关闭</summary>
		ExitAnimation,
	}

	//目前最好拆分成5层
	public enum UILayer
	{
		Background,
		Normal,
		Popup,
		Loading,     //加载之类的
		Tutorial, //引导之类的教程
	}
}
