namespace UIFrameworkLib
{
    /// <summary>
    /// UI 面板配置
    /// 由外部配置源（Luban / ScriptableObject / JSON）通过 UIConfigLoader 填充
    /// </summary>
    public class UIItemConfig
    {
        /// <summary>UI 唯一标识（与 UIRegistry 注册的 Controller 对应）</summary>
        public string UIKey { get; set; }

        /// <summary>预制体 Resources 路径</summary>
        public string PrefabPath { get; set; }

        /// <summary>层级（Background / Normal / Popup）</summary>
        public UILayer Layer { get; set; } = UILayer.Normal;

        /// <summary>入场动画策略 ID（默认 0 = Fade）</summary>
        public int EnterAnimId { get; set; }

        /// <summary>退场动画策略 ID（默认 0 = Fade）</summary>
        public int ExitAnimId { get; set; }
    }
}