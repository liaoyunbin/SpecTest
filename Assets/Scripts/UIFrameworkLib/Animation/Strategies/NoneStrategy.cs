using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>无动画策略（立即完成）</summary>
    public class NoneStrategy : IAnimationStrategy
    {
        public int EnterDurationMs => 0;
        public int ExitDurationMs => 0;

        public void Enter(RectTransform target) { }

		public void Exit(RectTransform target) { }

	}
}
