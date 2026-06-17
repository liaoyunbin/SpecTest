using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>
    /// 动画策略接口
    /// 每个策略同时包含 Enter 和 Exit 两个动作
    /// </summary>
    public interface IAnimationStrategy
    {
        int EnterDurationMs { get; }
        int ExitDurationMs { get; }
        void Enter(RectTransform target);
        void Exit(RectTransform target);
    }
}
