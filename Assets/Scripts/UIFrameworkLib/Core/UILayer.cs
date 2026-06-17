namespace UIFrameworkLib
{
    /// <summary>
    /// UI 层级枚举
    ///
    /// Background：底层背景界面。
    ///   打开时弹出（关闭）其上所有 Normal 和 Popup。
    ///   同一时间只允许一个 Background 活跃。
    ///
    /// Normal：标准界面。
    ///   打开新 Normal 时隐藏上一个 Normal（入栈），支持 Back 返回。
    ///   一次只显示一个 Normal。
    ///
    /// Popup：弹窗界面。
    ///   相互替换，新 Popup 替换当前 Popup。
    ///   自带全屏遮罩，在 Normal 之上。
    /// </summary>
    public enum UILayer
    {
        Background,
        Normal,
        Popup,
    }
}