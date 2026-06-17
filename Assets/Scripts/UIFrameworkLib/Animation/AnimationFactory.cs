using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>
    /// 动画策略工厂
    /// 按枚举即时创建并缓存策略实例，直接返回策略供调用方使用
    /// </summary>
    public static class AnimationFactory
    {
        private static readonly Dictionary<UIAnimationType, IAnimationStrategy> _cache = new();

        /// <summary>获取策略实例（内部缓存）</summary>
        public static IAnimationStrategy GetStrategy(UIAnimationType type)
        {
            if (!_cache.TryGetValue(type, out var strategy))
            {
                strategy = type switch
                {
                    UIAnimationType.Fade      => new FadeStrategy(),
                    UIAnimationType.Scale     => new ScaleStrategy(),
                    UIAnimationType.SlideUp   => new SlideUpStrategy(),
                    UIAnimationType.SlideDown => new SlideDownStrategy(),
                    UIAnimationType.BlackFade => new BlackFadeStrategy(),
                    _                         => new NoneStrategy(),
                };
                _cache[type] = strategy;
            }
            return strategy;
        }
    }
}
