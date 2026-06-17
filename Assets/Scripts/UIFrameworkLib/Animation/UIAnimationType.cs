namespace UIFrameworkLib
{
    /// <summary>
    /// 内置动画策略枚举
    /// </summary>
    public enum UIAnimationType
    {
        Fade,       // 淡入淡出（默认）
        Scale,      // 缩放弹入弹出
        SlideUp,    // 从下往上滑入 / 往上滑出
        SlideDown,  // 从上往下滑入 / 往下滑出
        BlackFade,  // 黑屏渐入渐出
        None,       // 无动画，立即完成
    }
}
